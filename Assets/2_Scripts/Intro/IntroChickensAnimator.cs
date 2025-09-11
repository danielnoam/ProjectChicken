using System.Collections;
using System.Collections.Generic;
using DNExtensions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using VInspector;
using Random = UnityEngine.Random;

public class IntroChickensAnimator : MonoBehaviour
{
    [Header("Animation Settings")] 
    [SerializeField] private float animationDuration = 5f;
    [SerializeField, MinMaxRange(0,25)] private RangedFloat loopDelayRange = new RangedFloat(0f, 3f);
    [SerializeField] private float transitionDuration = 0.25f; 
    
    [Header("Spline Generation Settings")]
    [SerializeField] private float controlPointHeight = 2f;
    [SerializeField] private Vector2 controlPointRandomOffset = new Vector2(2f, 2f);
    [SerializeField] private float tangentStrength = 1f;
    [SerializeField] private int additionalControlPoints = 1;

    [Header("Loop Spline Settings")]
    [SerializeField] private float loopRadius = 3f;
    [SerializeField] private int loopPoints = 8;
    [SerializeField] private float loopDuration = 10f;
    [SerializeField] private float minRotationOffset;
    [SerializeField] private float maxRotationOffset = 360f;

    [Header("References")]
    [SerializeField] private BoxCollider startArea;
    [SerializeField] private Transform loopCenter;
    [SerializeField] private Transform introSplineParent;
    [SerializeField] private Transform loopSplineParent;
    [SerializeField] private SplineAnimate[] chickens;

    private SplineContainer[] _individualSplineContainers;
    private SplineContainer[] _loopSplineContainers;
    private Coroutine[] _transitionCoroutines;

    private void Awake()
    {
        RestoreSplineReferences();
    }

    private void RestoreSplineReferences()
    {
        if (chickens == null || chickens.Length == 0)
        {
            Debug.LogWarning("No chickens found to restore spline references. Consider calling 'Find All Chickens' first.");
            return;
        }

        var chickenSplines = new List<SplineContainer>();
        var loopSplines = new List<SplineContainer>();
        
        for (int i = 0; i < chickens.Length; i++)
        {
            SplineContainer introSpline = introSplineParent?.Find($"ChickenSpline_{i}")?.GetComponent<SplineContainer>();
            SplineContainer loopSpline = loopSplineParent?.Find($"LoopSpline_{i}")?.GetComponent<SplineContainer>();
            
            if (introSpline != null) chickenSplines.Add(introSpline);
            if (loopSpline != null) loopSplines.Add(loopSpline);
        }

        if (chickenSplines.Count < chickens.Length)
        {
            Debug.Log("Not enough spline containers found. Regenerating splines...");
            GenerateSplines();
            return;
        }

        _individualSplineContainers = chickenSplines.ToArray();
        _loopSplineContainers = loopSplines.ToArray();
        _transitionCoroutines = new Coroutine[chickens.Length];

        for (int i = 0; i < chickens.Length && i < chickenSplines.Count; i++)
        {
            if (chickens[i] != null && chickenSplines[i] != null)
            {
                ConfigureChickenSpline(chickens[i], chickenSplines[i]);
            }
        }
        
    }

    [Button]
    private void FindAllChickens()
    {
        chickens = GetComponentsInChildren<SplineAnimate>();
        Debug.Log($"Found {chickens.Length} chickens");
    }

    [Button]
    private void GenerateSplines()
    {
        if (chickens == null || chickens.Length == 0)
        {
            Debug.LogError("No chickens found! Use 'Find All Chickens' first.");
            return;
        }

        if (startArea == null || loopCenter == null || introSplineParent == null || loopSplineParent == null)
        {
            Debug.LogError("Start area, loop center, and both spline parents must be assigned!");
            return;
        }

        ClearExistingSplines();

        _individualSplineContainers = new SplineContainer[chickens.Length];
        _loopSplineContainers = new SplineContainer[chickens.Length];
        _transitionCoroutines = new Coroutine[chickens.Length];

        for (int i = 0; i < chickens.Length; i++)
        {
            if (chickens[i] == null) continue;

            // Create intro spline
            GameObject introSplineGo = new GameObject($"ChickenSpline_{i}");
            introSplineGo.transform.SetParent(introSplineParent);
            SplineContainer introContainer = introSplineGo.AddComponent<SplineContainer>();
            _individualSplineContainers[i] = introContainer;

            // Create loop spline at loop center
            GameObject loopSplineGo = new GameObject($"LoopSpline_{i}");
            loopSplineGo.transform.SetParent(loopSplineParent);
            loopSplineGo.transform.position = loopCenter.position;
            
            SplineContainer loopContainer = loopSplineGo.AddComponent<SplineContainer>();
            _loopSplineContainers[i] = loopContainer;

            // Create the circular spline first
            Spline loopSpline = CreateCircularLoopSpline(loopContainer);
            loopContainer.Splines = new[] { loopSpline };
            
            // Apply rotation after spline creation (for visual variety and random start points)
            float rotationOffset = Random.Range(minRotationOffset, maxRotationOffset);
            float randomStartRotation = Random.Range(0f, 360f);
            loopSplineGo.transform.rotation = Quaternion.Euler(rotationOffset, randomStartRotation, rotationOffset);

            // Get end point for intro spline (aim near the loop but not necessarily exact)
            Vector3 endPoint = GetLoopStartPoint(loopContainer);
            Vector3 startPoint = GetRandomPointInCollider(startArea);

            // Create intro spline
            Spline introSpline = CreateSmoothSplinePath(introContainer, startPoint, endPoint);
            introContainer.Splines = new[] { introSpline };

            ConfigureChickenSpline(chickens[i], introContainer);
        }

        Debug.Log($"Generated {_individualSplineContainers.Length} spline sets for chickens!");
    }

    private Vector3 GetLoopStartPoint(SplineContainer loopContainer)
    {
        if (loopContainer.Splines == null || loopContainer.Splines.Count == 0)
            return loopContainer.transform.position;

        Spline loopSpline = loopContainer.Splines[0];
        
        // Get the first knot of the loop spline
        if (loopSpline.Count > 0)
        {
            BezierKnot firstKnot = loopSpline[0];
            return loopContainer.transform.TransformPoint(firstKnot.Position);
        }
        
        return loopContainer.transform.position;
    }

    private Quaternion GetLoopStartRotation(SplineContainer loopContainer)
    {
        if (loopContainer.Splines == null || loopContainer.Splines.Count == 0)
            return loopContainer.transform.rotation;

        Spline loopSpline = loopContainer.Splines[0];
        
        // Get the tangent at the start of the loop to determine rotation
        loopSpline.Evaluate(0f, out float3 position, out float3 tangent, out float3 up);
        
        if (math.length(tangent) > 0.01f)
        {
            Vector3 worldTangent = loopContainer.transform.TransformDirection(tangent);
            return Quaternion.LookRotation(worldTangent, Vector3.up);
        }
        
        return loopContainer.transform.rotation;
    }

    private Spline CreateCircularLoopSpline(SplineContainer container)
    {
        Spline spline = new Spline();
        
        for (int i = 0; i < loopPoints; i++)
        {
            float angle = (float)i / loopPoints * 360f;
            float radians = angle * Mathf.Deg2Rad;
            
            Vector3 pointPosition = new Vector3(
                Mathf.Cos(radians) * loopRadius,
                0,
                Mathf.Sin(radians) * loopRadius
            );
            
            Vector3 tangentDirection = new Vector3(-Mathf.Sin(radians), 0, Mathf.Cos(radians));
            float tangentLength = (2f * Mathf.PI * loopRadius / loopPoints) * 0.33f;
            
            Vector3 inTangent = -tangentDirection * tangentLength;
            Vector3 outTangent = tangentDirection * tangentLength;
            
            BezierKnot knot = new BezierKnot(pointPosition, inTangent, outTangent);
            spline.Add(knot);
        }

        spline.Closed = true;
        return spline;
    }

    private void ClearExistingSplines()
    {
        // Clear intro splines
        if (introSplineParent != null)
        {
            for (int i = introSplineParent.childCount - 1; i >= 0; i--)
            {
                Transform child = introSplineParent.GetChild(i);
                if (child.name.StartsWith("ChickenSpline_"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        // Clear loop splines
        if (loopSplineParent != null)
        {
            for (int i = loopSplineParent.childCount - 1; i >= 0; i--)
            {
                Transform child = loopSplineParent.GetChild(i);
                if (child.name.StartsWith("LoopSpline_"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }

    private Spline CreateSmoothSplinePath(SplineContainer container, Vector3 worldStart, Vector3 worldEnd)
    {
        Spline spline = new Spline();
        
        Vector3 localStart = container.transform.InverseTransformPoint(worldStart);
        Vector3 localEnd = container.transform.InverseTransformPoint(worldEnd);

        Vector3[] controlPoints = GenerateControlPoints(localStart, localEnd, additionalControlPoints);
        
        for (int i = 0; i < controlPoints.Length; i++)
        {
            Vector3 inTangent = Vector3.zero;
            Vector3 outTangent = Vector3.zero;
            
            if (i > 0 && i < controlPoints.Length - 1)
            {
                Vector3 direction = (controlPoints[i + 1] - controlPoints[i - 1]).normalized;
                float distance = Vector3.Distance(controlPoints[i - 1], controlPoints[i + 1]) * tangentStrength * 0.3f;
                
                inTangent = -direction * distance;
                outTangent = direction * distance;
            }
            else if (i == 0 && controlPoints.Length > 1)
            {
                Vector3 direction = (controlPoints[i + 1] - controlPoints[i]).normalized;
                float distance = Vector3.Distance(controlPoints[i], controlPoints[i + 1]) * tangentStrength * 0.3f;
                outTangent = direction * distance;
            }
            else if (i == controlPoints.Length - 1 && controlPoints.Length > 1)
            {
                Vector3 direction = (controlPoints[i] - controlPoints[i - 1]).normalized;
                float distance = Vector3.Distance(controlPoints[i - 1], controlPoints[i]) * tangentStrength * 0.3f;
                inTangent = -direction * distance;
            }
            
            BezierKnot knot = new BezierKnot(controlPoints[i], inTangent, outTangent);
            spline.Add(knot);
        }

        spline.Closed = false;
        return spline;
    }

    private Vector3[] GenerateControlPoints(Vector3 start, Vector3 end, int additionalPoints)
    {
        Vector3[] points = new Vector3[additionalPoints + 2];
        points[0] = start;
        points[^1] = end;
        
        for (int i = 1; i <= additionalPoints; i++)
        {
            float t = (float)i / (additionalPoints + 1);
            Vector3 basePoint = Vector3.Lerp(start, end, t);
            
            basePoint.y += controlPointHeight * Mathf.Sin(Mathf.PI * t);
            
            basePoint.x += Random.Range(-controlPointRandomOffset.x, controlPointRandomOffset.x);
            basePoint.z += Random.Range(-controlPointRandomOffset.y, controlPointRandomOffset.y);
            
            points[i] = basePoint;
        }
        
        return points;
    }

    private void ConfigureChickenSpline(SplineAnimate chicken, SplineContainer container)
    {
        chicken.Container = container;
        chicken.Loop = SplineAnimate.LoopMode.Once;
        chicken.Duration = animationDuration;
    }

    private void ConfigureChickenLoopSpline(SplineAnimate chicken, SplineContainer container)
    {
        chicken.Container = container;
        chicken.Loop = SplineAnimate.LoopMode.Loop;
        chicken.Easing = SplineAnimate.EasingMode.EaseIn;
        chicken.Duration = loopDuration + loopDelayRange.RandomValue;
    }

    private Vector3 GetRandomPointInCollider(BoxCollider boxCollider)
    {
        if (boxCollider == null) return Vector3.zero;

        Bounds bounds = boxCollider.bounds;
        
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }

    [Button]
    public void PlayAnimation()
    {
        
        if (chickens is { Length: > 0 })
        {
            foreach (var chicken in chickens)
            {
                if (!chicken) continue;
                chicken.Play();
            }
            
            StartCoroutine(TransitionToLoopSplines());
        }
    }

    private IEnumerator TransitionToLoopSplines()
    {
        // Wait for intro animations to complete
        yield return new WaitForSeconds(animationDuration);
        
        if (_loopSplineContainers is { Length: > 0 })
        {
            for (int i = 0; i < chickens.Length && i < _loopSplineContainers.Length; i++)
            {
                if (chickens[i] != null && _loopSplineContainers[i] != null)
                {
                    // Start individual transition for each chicken
                    _transitionCoroutines[i] = StartCoroutine(TransitionChickenToLoop(i));
                }
            }
        }
    }

    private IEnumerator TransitionChickenToLoop(int chickenIndex)
    {
        SplineAnimate chicken = chickens[chickenIndex];
        SplineContainer loopContainer = _loopSplineContainers[chickenIndex];
        
        // Get the target position and rotation for the loop start
        Vector3 targetPosition = GetLoopStartPoint(loopContainer);
        Quaternion targetRotation = GetLoopStartRotation(loopContainer);
        
        // Get starting position and rotation
        Transform chickenTransform = chicken.transform;
        Vector3 startPosition = chickenTransform.position;
        Quaternion startRotation = chickenTransform.rotation;
        
        // Pause the chicken's spline animation during transition
        chicken.Pause();
        
        // Smoothly move the chicken to the loop start position
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            
            // Use smooth curve for transition
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            // Interpolate position and rotation
            chickenTransform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            chickenTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we're exactly at the target
        chickenTransform.position = targetPosition;
        chickenTransform.rotation = targetRotation;
        
        // Configure and start the loop animation
        ConfigureChickenLoopSpline(chicken, loopContainer);
        chicken.Restart(true);
    }
}