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
    [Header("Chicken Creation Settings")]
    [SerializeField] private int chickenCount = 100;
    
    [Header("Spline Generation Settings")]
    [SerializeField] private float controlPointHeight = 2f;
    [SerializeField] private Vector2 controlPointRandomOffset = new Vector2(2f, 2f);
    [SerializeField] private float tangentStrength = 1f;
    [SerializeField] private int additionalControlPoints = 1;
    [Space(5f)]
    [SerializeField] private float loopRadius = 200f;
    [SerializeField] private int loopPoints = 8;
    [SerializeField, MinMaxRange(0,360)] private RangedFloat loopRotationRange = new RangedFloat(0f, 360f);

    [Header("Splines Animation Settings")] 
    [SerializeField, MinMaxRange(0,100)] private RangedFloat speedVariationRange = new RangedFloat(0f, 25f);
    [SerializeField] private float linerSpeed = 190f;
    [SerializeField] private float loopSpeed = 190f;
    [SerializeField] private float lastChickenDelay;
    
    [Header("References")]
    [SerializeField] private BoxCollider startArea;
    [SerializeField] private Transform loopCenter;
    [SerializeField] private Transform introSplineParent;
    [SerializeField] private Transform loopSplineParent;
    [SerializeField] private Transform chickensParent;
    [SerializeField] private SplineAnimate[] chickens;
    [SerializeField] private ChanceList<SplineAnimate> chickensPrefab;

    private SplineContainer[] _individualSplineContainers;
    private SplineContainer[] _loopSplineContainers;
    private float[] _chickenSpeedVariations;

    private void Awake()
    {
        RestoreSplineReferences();
    }

    private void RestoreSplineReferences()
    {
        if (chickens == null || chickens.Length == 0)
        {
            Debug.LogWarning("No chickens found to restore spline references. Consider calling 'Create Chickens' first.");
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
        
        // Initialize speed variations if not already done
        if (_chickenSpeedVariations == null || _chickenSpeedVariations.Length != chickens.Length)
        {
            GenerateSpeedVariations();
        }

        for (int i = 0; i < chickens.Length && i < chickenSplines.Count; i++)
        {
            if (chickens[i] != null && chickenSplines[i] != null)
            {
                ConfigureChickenSpline(chickens[i], chickenSplines[i], i);
            }
        }
    }

    [Button("Create Chickens")]
    private void CreateChickens()
    {
        if (chickensPrefab == null || chickensPrefab.Count == 0)
        {
            Debug.LogError("No chicken prefabs available in ChanceList!");
            return;
        }

        if (chickensParent == null)
        {
            Debug.LogError("Chickens parent transform must be assigned!");
            return;
        }

        // Clear existing chickens
        ClearExistingChickens();

        // Create new chickens array
        chickens = new SplineAnimate[chickenCount];

        // Create chickens from prefabs
        for (int i = 0; i < chickenCount; i++)
        {
            // Get random chicken prefab from the chance list
            SplineAnimate chickenPrefab = chickensPrefab.GetRandomItem();
            
            if (chickenPrefab == null)
            {
                Debug.LogWarning($"Failed to get chicken prefab for index {i}");
                continue;
            }

            // Instantiate the chicken
            GameObject chickenGO = Instantiate(chickenPrefab.gameObject, chickensParent);
            chickenGO.name = $"Chicken_{i}";
            
            // Get the SplineAnimate component
            SplineAnimate chicken = chickenGO.GetComponent<SplineAnimate>();
            if (chicken == null)
            {
                Debug.LogError($"Chicken prefab at index {i} doesn't have a SplineAnimate component!");
                DestroyImmediate(chickenGO);
                continue;
            }

            // Add to array
            chickens[i] = chicken;
        }

        // Generate speed variations for all chickens
        GenerateSpeedVariations();

        Debug.Log($"Created {chickenCount} chickens with variations!");
    }

    [Button("Find All Chickens")]
    private void FindAllChickens()
    {
        chickens = GetComponentsInChildren<SplineAnimate>();
        Debug.Log($"Found {chickens.Length} chickens");
        
        // Generate new speed variations when chickens are found
        GenerateSpeedVariations();
    }

    private void ClearExistingChickens()
    {
        // Clear existing chickens from the parent
        if (chickensParent != null)
        {
            for (int i = chickensParent.childCount - 1; i >= 0; i--)
            {
                Transform child = chickensParent.GetChild(i);
                if (child.name.StartsWith("Chicken_"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        // Clear the chickens array
        chickens = null;
    }

    private void GenerateSpeedVariations()
    {
        if (chickens == null || chickens.Length == 0) return;
        
        _chickenSpeedVariations = new float[chickens.Length];
        for (int i = 0; i < chickens.Length; i++)
        {
            _chickenSpeedVariations[i] = speedVariationRange.RandomValue;
        }
    }

    [Button]
    private void GenerateSplines()
    {
        if (chickens == null || chickens.Length == 0)
        {
            Debug.LogError("No chickens found! Use 'Create Chickens' first.");
            return;
        }

        if (startArea == null || loopCenter == null || introSplineParent == null || loopSplineParent == null)
        {
            Debug.LogError("Start area, loop center, and both spline parents must be assigned!");
            return;
        }

        ClearExistingSplines();
        
        // Generate speed variations for all chickens
        GenerateSpeedVariations();

        _individualSplineContainers = new SplineContainer[chickens.Length];
        _loopSplineContainers = new SplineContainer[chickens.Length];

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
            float rotationOffset = Random.Range(loopRotationRange.minValue, loopRotationRange.maxValue);
            float randomStartRotation = Random.Range(0f, 360f);
            loopSplineGo.transform.rotation = Quaternion.Euler(rotationOffset, randomStartRotation, rotationOffset);

            // Get end point for intro spline (aim near the loop but not necessarily exact)
            Vector3 endPoint = GetLoopStartPoint(loopContainer);
            Vector3 startPoint = GetRandomPointInCollider(startArea);

            // Create intro spline
            Spline introSpline = CreateSmoothSplinePath(introContainer, startPoint, endPoint);
            introContainer.Splines = new[] { introSpline };

            ConfigureChickenSpline(chickens[i], introContainer, i);
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

    private void ConfigureChickenSpline(SplineAnimate chicken, SplineContainer container, int chickenIndex)
    {
        chicken.Container = container;
        chicken.Loop = SplineAnimate.LoopMode.Once;
        chicken.Easing = SplineAnimate.EasingMode.None;
        chicken.AnimationMethod = SplineAnimate.Method.Speed;
        
        // Apply consistent speed variation for this specific chicken
        float speedVariation = GetChickenSpeedVariation(chickenIndex);
        float adjustedSpeed = linerSpeed + speedVariation;
        chicken.MaxSpeed = Mathf.Max(adjustedSpeed, 10f); // Minimum speed to prevent stopping
    }

    private void ConfigureChickenLoopSpline(SplineAnimate chicken, SplineContainer container, int chickenIndex)
    {
        chicken.Container = container;
        chicken.Loop = SplineAnimate.LoopMode.Loop;
        chicken.Easing = SplineAnimate.EasingMode.None;
        chicken.AnimationMethod = SplineAnimate.Method.Speed;
        
        // Apply the same speed variation for this specific chicken
        float speedVariation = GetChickenSpeedVariation(chickenIndex);
        float adjustedSpeed = loopSpeed + speedVariation;
        chicken.MaxSpeed = Mathf.Max(adjustedSpeed, 10f); // Minimum speed to prevent stopping
    }

    private float GetChickenSpeedVariation(int chickenIndex)
    {
        if (_chickenSpeedVariations == null || chickenIndex >= _chickenSpeedVariations.Length)
        {
            Debug.LogWarning($"Speed variation not found for chicken {chickenIndex}. Using default.");
            return 0f;
        }
        
        return _chickenSpeedVariations[chickenIndex];
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
            for (int i = 0; i < chickens.Length; i++)
            {
                if (!chickens[i]) continue;
                
                // Check if this is the last chicken and apply delay
                bool isLastChicken = i == chickens.Length - 1;
                
                if (isLastChicken && lastChickenDelay > 0f)
                {
                    StartCoroutine(PlayChickenWithDelay(i, lastChickenDelay));
                }
                else
                {
                    PlayChicken(i);
                }
            }
        }
    }

    private IEnumerator PlayChickenWithDelay(int chickenIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayChicken(chickenIndex);
    }

    private void PlayChicken(int chickenIndex)
    {
        // Configure intro spline
        if (chickenIndex < _individualSplineContainers.Length && _individualSplineContainers[chickenIndex] != null)
        {
            ConfigureChickenSpline(chickens[chickenIndex], _individualSplineContainers[chickenIndex], chickenIndex);
            chickens[chickenIndex].Play();
            
            // Start coroutine to switch to loop after intro completes
            StartCoroutine(SwitchToLoop(chickenIndex));
        }
    }

    private IEnumerator SwitchToLoop(int chickenIndex)
    {
        SplineAnimate chicken = chickens[chickenIndex];
        
        // Wait until the intro animation is complete
        while (chicken.IsPlaying)
        {
            yield return null;
        }
        
        // Switch to loop spline with the same speed variation
        if (chickenIndex < _loopSplineContainers.Length && _loopSplineContainers[chickenIndex] != null)
        {
            ConfigureChickenLoopSpline(chicken, _loopSplineContainers[chickenIndex], chickenIndex);
            chicken.Restart(true);
        }
    }
}