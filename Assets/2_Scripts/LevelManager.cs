using System;
using System.Collections;
using Core.Attributes;
using KBCore.Refs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using VInspector;




public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Path Settings")]
    [SerializeField, Min(0)] private Vector2 enemyBoundary = new Vector2(25f,15f);
    [SerializeField, Min(0)] private Vector2 playerBoundary = new Vector2(10f,6f);
    [SerializeField] private float playerOffset = -20f;
    [SerializeField] private float enemyOffset = 25f;
    [SerializeField] private float startOffset;
    
    [Header("Level Stages")]
    [SerializeField, ReadOnly] private int currentStageIndex;
    [SerializeField] private SOLevelStage[] levelStages;

    
    
    [Header("References")]
    [SerializeField, Child] private SplineContainer splineContainer;
    [SerializeField] private Transform currentPositionOnPath;



    public Vector3 PlayerPosition { get; private set; }
    public Vector3 EnemyPosition { get; private set; }
    public float SplineLength { get; private set; }
    public SOLevelStage CurrentStage { get; private set; }
    public SplinePath <Spline> SplinePath  { get; private set; }
    public Vector2 PlayerBoundary => playerBoundary;
    public Vector2 EnemyBoundary => enemyBoundary;
    public Transform CurrentPositionOnPath => currentPositionOnPath;



    public event Action<SOLevelStage> OnStageChanged;


    private void OnValidate()
    {
        this.ValidateRefs(); 
    
        // Update Current Position on Path position if it exists based on the startOffset offset
        if (currentPositionOnPath && splineContainer && splineContainer.Splines.Count > 0)
        {
            // Calculate spline length first
            var tempSplinePath = new SplinePath<Spline>(splineContainer.Splines);
            float tempSplineLength = tempSplinePath.GetLength();
        
            if (tempSplineLength > 0)
            {
                float offsetDistance = startOffset;
                float normalizedOffset = offsetDistance / tempSplineLength;
                float startT = normalizedOffset % 1.0f;
                if (startT < 0) startT += 1.0f;
            
                currentPositionOnPath.position = splineContainer.EvaluatePosition(startT);
            }
        }
    }

    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        SetUpSpline();
        StartLevel();
    }


    private void Update()
    {
        MoveAlongSpline();
    }

    
    
    
    
    #region Stage Management ---------------------------------------------------------------------------------

    [Button]
    private void StartLevel()
    {
        if (levelStages == null || levelStages.Length == 0)
        {
            Debug.LogError("No level stages defined!");
            return;
        }
        
        SetStage(0);
    }
    
    [Button]
    private void SetNextStage()
    {
        int nextStageIndex = currentStageIndex + 1;
        if (nextStageIndex < levelStages.Length)
        {
            SetStage(nextStageIndex);
        }
        else
        {
            Debug.Log("No more stages available");
        }
    }
    
    private void SetStage(int newStageIndex)
    {
        if (newStageIndex < 0 || newStageIndex >= levelStages.Length) return;

        currentStageIndex = newStageIndex;
        CurrentStage = levelStages[currentStageIndex];
        
        switch (CurrentStage.StageType)
        {
            case StageType.Checkpoint:
                StartCoroutine(ChangeStageAfterDelay(CurrentStage.StageDuration));
                break;
            case StageType.EnemyWave:
                break;
        }

        Debug.Log("Setting stage: " + CurrentStage.name);
        OnStageChanged?.Invoke(CurrentStage);
    }
    

    
    private IEnumerator ChangeStageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetNextStage();
    }

    #endregion Stage Management ---------------------------------------------------------------------------------



    #region Spline Positinoning ---------------------------------------------------------------------------------

    private void SetUpSpline()
    {
        if (!splineContainer || !currentPositionOnPath) return;
        
        // Initialize the spline 
        SplinePath = new SplinePath<Spline>(splineContainer.Splines);
        SplineLength = SplinePath.GetLength();
        
        // Set starting position based on startOffset offset
        float offsetDistance = startOffset;
        float normalizedOffset = offsetDistance / SplineLength;
        float startT = normalizedOffset;
        if (startT < 0)
        {
            startT = 1.0f + startT;
        }
        currentPositionOnPath.position = splineContainer.EvaluatePosition(startT);
        
    }
    
    private void MoveAlongSpline()
    {
        if (!splineContainer || !currentPositionOnPath || !CurrentStage) return;
    
        // Move along the spline using the speed from current stage
        float deltaDistance = CurrentStage.PathFollowSpeed * Time.deltaTime;
        float currentT = GetCurrentSplineT(currentPositionOnPath.position);
        float deltaNormalized = deltaDistance / SplineLength;
        float newT = currentT + deltaNormalized;
    
        // Wrap around instead of clamping
        newT = newT % 1.0f; // This wraps values > 1.0 back to 0.0-1.0 range
        if (newT < 0) newT += 1.0f; // Handle negative values (shouldn't happen with forward movement)
    
        // Update position
        Vector3 newPosition = splineContainer.EvaluatePosition(newT);
        currentPositionOnPath.position = newPosition;
    
        // Update rotation based on spline tangent and up vector
        Vector3 tangent = splineContainer.EvaluateTangent(newT);
        Vector3 up = splineContainer.EvaluateUpVector(newT);
        currentPositionOnPath.rotation = GetAlignedRotation(tangent, up);
    
        // Calculate player and enemy positions 
        float playerStageOffset = CurrentStage ? CurrentStage.PlayerStageOffset : 0f;
        float playerOffsetNormalized = (playerOffset + playerStageOffset) / SplineLength;
        float playerT = (newT + playerOffsetNormalized) % 1.0f;
        if (playerT < 0) playerT += 1.0f;
        PlayerPosition = splineContainer.EvaluatePosition(playerT);

        float enemyStageOffset = CurrentStage ? CurrentStage.EnemyStageOffset : 0f;
        float enemyOffsetNormalized = (enemyOffset + enemyStageOffset) / SplineLength;
        float enemyT = (newT + enemyOffsetNormalized) % 1.0f;
        if (enemyT < 0) enemyT += 1.0f;
        EnemyPosition = splineContainer.EvaluatePosition(enemyT);
    }
    
    #endregion Spline Positinoning ---------------------------------------------------------------------------------
    

    
    #region Helper ---------------------------------------------------------------------------------------------

    public float GetCurrentSplineT(Vector3 point)
    {
        if (!currentPositionOnPath || !splineContainer) return 0f;

        SplineUtility.GetNearestPoint(splineContainer.Spline, point, out var nearestPoint, out var t);
        return t;
    }
    
    public Vector3 EvaluateTangentOnSpline(float t)
    {
        if (!splineContainer) return Vector3.forward;

        // Get the tangent vector at the specified t value on the spline
        Vector3 tangent = splineContainer.EvaluateTangent(t);
        
        // Ensure the tangent is normalized
        return tangent.normalized;
    }
    
    public Vector3 GetDirectionOnSpline(Vector3 point)
    {
        if (!splineContainer) return Vector3.forward;
    
        // Get the current position and a slightly ahead position to calculate a direction
        Vector3 currentPos = point;
        float currentT = GetCurrentSplineT(point);
    
        // Sample a small step ahead on the spline to get a direction
        float stepSize = 0.01f; // Small step forward
        float aheadT = Mathf.Clamp01(currentT + stepSize);
        Vector3 aheadPos = splineContainer.EvaluatePosition(aheadT);
    
        Vector3 direction = (aheadPos - currentPos).normalized;
        return direction.magnitude > 0.001f ? direction : Vector3.forward;
    }
    
    public float GetPositionOnSpline(Vector3 position)
    {
        if (!splineContainer) return 0f;

        // Get the current T value on the spline based on the position
        SplineUtility.GetNearestPoint(splineContainer.Spline, position, out var nearestPoint, out var positionAlongSpline);
        
        // Normalize the position along the spline to a value between 0 and 1
        float normalizedPosition = positionAlongSpline / SplineLength;
        return Mathf.Clamp01(normalizedPosition);
    }
    
    
    private Quaternion GetAlignedRotation(Vector3 splineTangent, Vector3 splineUp)
    {
        Vector3 forward = GetAxisVector(splineTangent, CurrentStage.ForwardAxis);
        Vector3 up = GetAxisVector(splineUp, CurrentStage.UpAxis);
    
        return Quaternion.LookRotation(forward, up);
    }

    private Vector3 GetAxisVector(Vector3 splineVector, SplineAnimate.AlignAxis axis)
    {
        switch (axis)
        {
            case SplineAnimate.AlignAxis.XAxis:
                return splineVector;
            case SplineAnimate.AlignAxis.YAxis:
                return splineVector;
            case SplineAnimate.AlignAxis.ZAxis:
                return splineVector;
            case SplineAnimate.AlignAxis.NegativeXAxis:
                return -splineVector;
            case SplineAnimate.AlignAxis.NegativeYAxis:
                return -splineVector;
            case SplineAnimate.AlignAxis.NegativeZAxis:
                return -splineVector;
            default:
                return splineVector;
        }
    }


    #endregion Helper ---------------------------------------------------------------------------------------------
    
    
    
    #region Editor -----------------------------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        // Draw the current position on the path
        if (currentPositionOnPath)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentPositionOnPath.position, 0.5f);
        }

        if (Application.isPlaying)
        {
            // Draw player position
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(PlayerPosition, 0.3f);
            
            // Draw enemy position
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(EnemyPosition, 0.3f);
        }

    }

    #endregion Editor -----------------------------------------------------------------------------------------------


}