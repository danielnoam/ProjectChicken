using System;
using System.Collections;
using KBCore.Refs;
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
    [SerializeField, Tooltip("The smoothness applied when stages have different move speeds")] private float pathFollowSmoothness = 0.1f;
    
    [Header("Level Stages")]
    [SerializeField, ReadOnly] private int currentStageIndex;
    [SerializeField] private SOLevelStage[] levelStages;

    
    
    [Header("References")]
    [SerializeField, Scene] private EnemyWaveSpawner enemyWaveSpawner;
    [SerializeField, Child] private SplineContainer splineContainer;
    [SerializeField] private Transform currentPositionOnPath;



    private Coroutine _stageChangeCoroutine;
    private float _currentPathSpeed;
    private bool _settingStageFlag;
    
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
        if (Application.isPlaying) return;
        
        
        
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

    private void OnEnable()
    {
        if (enemyWaveSpawner) enemyWaveSpawner.OnEnemyWaveCleared += OnEnemyWaveCleared;
    }
    
    private void OnDisable()
    {
        if (enemyWaveSpawner) enemyWaveSpawner.OnEnemyWaveCleared -= OnEnemyWaveCleared;
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
                SetStageAfterDelay(CurrentStage.StageDuration);
                break;
            case StageType.EnemyWave:
                break;
        }

        _settingStageFlag = false;
        
        Debug.Log("Setting stage: " + CurrentStage.name);
        OnStageChanged?.Invoke(CurrentStage);
    }
    
    private void SetStageAfterDelay(float delay)
    {
        if (_stageChangeCoroutine != null)
        {
            StopCoroutine(_stageChangeCoroutine);
        }
        
        _stageChangeCoroutine = StartCoroutine(ChangeStageAfterDelay(delay));
    }
    

    
    private IEnumerator ChangeStageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetNextStage();
    }
    
    private void OnEnemyWaveCleared()
    {
        if (!CurrentStage || CurrentStage.StageType != StageType.EnemyWave || _settingStageFlag) return;

        _settingStageFlag = true;
        
        if (CurrentStage.DelayBeforeNextStage <= 0)
        {
            SetNextStage();
        }
        else
        {
            SetStageAfterDelay(CurrentStage.DelayBeforeNextStage);
        }

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

        float currentT = GetCurrentSplineT(currentPositionOnPath.position);
        
        // Lerp the current speed towards the target speed
        float targetSpeed = CurrentStage.PathFollowSpeed;
        _currentPathSpeed = Mathf.Lerp(_currentPathSpeed, targetSpeed, pathFollowSmoothness * Time.deltaTime);

        // Determine movement direction (forward or backward)
        Vector3 movementDir = GetMovementDirection(currentT);
        Vector3 splineTangent = splineContainer.EvaluateTangent(currentT);

        // Check if we should move forward or backward along spline
        float directionMultiplier = Vector3.Dot(movementDir, splineTangent) >= 0 ? 1f : -1f;

        // Move along the spline using the lerped speed
        float deltaDistance = _currentPathSpeed * Time.deltaTime * directionMultiplier;
        float deltaNormalized = deltaDistance / SplineLength;
        float newT = currentT + deltaNormalized;

        // Wrap around instead of clamping
        newT = newT % 1.0f;
        if (newT < 0) newT += 1.0f;

        // Update position and rotation
        Vector3 newPosition = splineContainer.EvaluatePosition(newT);
        currentPositionOnPath.position = newPosition;

        Vector3 tangent = splineContainer.EvaluateTangent(newT);
        Vector3 up = splineContainer.EvaluateUpVector(newT);
        currentPositionOnPath.rotation = GetAlignedRotation(tangent, up);

        // Update player and enemy positions
        UpdatePlayerAndEnemyPositions(newT);
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
        if (!CurrentStage) return Quaternion.identity;

        Vector3 forward, up;

        switch (CurrentStage.AlignmentMode)
        {
            case SplineAnimate.AlignmentMode.None:
                // No alignment - keep current rotation
                return currentPositionOnPath.rotation;

            case SplineAnimate.AlignmentMode.SplineElement:
                // Use spline's tangent and up vectors with axis mapping
                forward = GetAxisVector(splineTangent, CurrentStage.ForwardAxis);
                up = GetAxisVector(splineUp, CurrentStage.UpAxis);
                break;

            case SplineAnimate.AlignmentMode.SplineObject:
                // Use spline container's transform axes
                Vector3 splineForward = splineContainer.transform.forward;
                Vector3 splineUpVector = splineContainer.transform.up;
                forward = GetAxisVector(splineForward, CurrentStage.ForwardAxis);
                up = GetAxisVector(splineUpVector, CurrentStage.UpAxis);
                break;

            case SplineAnimate.AlignmentMode.World:
                // Use world space axes
                forward = GetAxisVector(Vector3.forward, CurrentStage.ForwardAxis);
                up = GetAxisVector(Vector3.up, CurrentStage.UpAxis);
                break;

            default:
                forward = Vector3.forward;
                up = Vector3.up;
                break;
        }

        return Quaternion.LookRotation(forward, up);
    }

    private Vector3 GetAxisVector(Vector3 splineVector, SplineComponent.AlignAxis axis)
    {
        Vector3 normalized = splineVector.normalized;
    
        switch (axis)
        {
            case SplineComponent.AlignAxis.XAxis:
                return Vector3.right;
            case SplineComponent.AlignAxis.YAxis:
                return Vector3.up;
            case SplineComponent.AlignAxis.ZAxis:
                return Vector3.forward;
            case SplineComponent.AlignAxis.NegativeXAxis:
                return Vector3.left;
            case SplineComponent.AlignAxis.NegativeYAxis:
                return Vector3.down;
            case SplineComponent.AlignAxis.NegativeZAxis:
                return Vector3.back;
            default:
                return normalized;
        }
    }
    
    private Vector3 GetMovementDirection(float currentT)
    {
        if (!CurrentStage) return Vector3.forward;
    
        Vector3 splineTangent = splineContainer.EvaluateTangent(currentT);
        Vector3 splineUp = splineContainer.EvaluateUpVector(currentT);
    
        switch (CurrentStage.AlignmentMode)
        {
            case SplineAnimate.AlignmentMode.None:
                // Use current object's forward direction
                return currentPositionOnPath.forward;
            
            case SplineAnimate.AlignmentMode.SplineElement:
                // Use spline's tangent direction
                return splineTangent.normalized;
            
            case SplineAnimate.AlignmentMode.SplineObject:
                // Use spline container's forward direction
                return splineContainer.transform.forward;
            
            case SplineAnimate.AlignmentMode.World:
                // Use world forward direction
                return Vector3.forward;
            
            default:
                return splineTangent.normalized;
        }
    }

    private void UpdatePlayerAndEnemyPositions(float currentT)
    {
        // Calculate player position
        float playerStageOffset = CurrentStage ? CurrentStage.PlayerStageOffset : 0f;
        float playerOffsetNormalized = (playerOffset + playerStageOffset) / SplineLength;
        float playerT = (currentT + playerOffsetNormalized) % 1.0f;
        if (playerT < 0) playerT += 1.0f;
        PlayerPosition = splineContainer.EvaluatePosition(playerT);

        // Calculate enemy position
        float enemyStageOffset = CurrentStage ? CurrentStage.EnemyStageOffset : 0f;
        float enemyOffsetNormalized = (enemyOffset + enemyStageOffset) / SplineLength;
        float enemyT = (currentT + enemyOffsetNormalized) % 1.0f;
        if (enemyT < 0) enemyT += 1.0f;
        EnemyPosition = splineContainer.EvaluatePosition(enemyT);
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