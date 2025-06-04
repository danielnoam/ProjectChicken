using System;
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
    [SerializeField] private float playerOffset = -10f;
    [SerializeField] private float enemyOffset = 10f;
    [SerializeField] private float pathFollowSpeed = 5f;
    [SerializeField] private bool autoStartMoving = true;
    [SerializeField] private SplineAnimate.LoopMode loopMode = SplineAnimate.LoopMode.Loop;
    
    [Header("References")]
    [SerializeField, Child] private SplineContainer splineContainer;
    [SerializeField] private Transform currentPositionOnPath;
    
    
    
    private SplineAnimate _positionAnimator;
    private float _splineLength;
    public Vector3 PlayerPosition { get; private set; }
    public Vector3 EnemyPosition { get; private set; }
    public Vector2 PlayerBoundary => playerBoundary;
    public Vector2 EnemyBoundary => enemyBoundary;
    public SplineContainer SplineContainer => splineContainer;
    public Transform CurrentPositionOnPath => currentPositionOnPath;


    private void OnValidate() { this.ValidateRefs(); }

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
        
        SetUpSplineAnimator();
    }
    
    private void Start()
    {
        if (autoStartMoving) _positionAnimator.Play();
    }
    
    private void OnDestroy()
    {
        if (_positionAnimator)
        {
            _positionAnimator.Updated -= OnSplineMovement;
        }
    }
    



    #region Spline Positinoning ---------------------------------------------------------------------------------

    private void SetUpSplineAnimator()
    {
        _positionAnimator = currentPositionOnPath.GetComponent<SplineAnimate>();
        _positionAnimator.MaxSpeed = pathFollowSpeed;
        _positionAnimator.Loop = loopMode;
        
        // Calculate spline length
        SplinePath<Spline> splinePath = new SplinePath<Spline>(splineContainer.Splines);
        _splineLength = splinePath.GetLength();
        
        // Set starting position based on player offset
        float offsetDistance = Mathf.Abs(playerOffset);
        float normalizedOffset = offsetDistance / _splineLength;
        _positionAnimator.StartOffset = -normalizedOffset;
        
        _positionAnimator.Updated += OnSplineMovement;
    }
    
    private void OnSplineMovement(Vector3 position, Quaternion rotation)
    {
        // Get current normalized position on spline
        float currentT = GetCurrentSplineT();
    
        // Calculate player position (current position + player offset)
        float playerOffsetNormalized = playerOffset / _splineLength;
        float playerT = Mathf.Clamp01(currentT + playerOffsetNormalized);
        PlayerPosition = splineContainer.EvaluatePosition(playerT);
    
        // Calculate enemy position (current position + enemy offset)
        float enemyOffsetNormalized = enemyOffset / _splineLength;
        float enemyT = Mathf.Clamp01(currentT + enemyOffsetNormalized);
        EnemyPosition = splineContainer.EvaluatePosition(enemyT);
    }


    
    
    [Button] 
    private void ToggleSplineMovement()
    {
        if (!Application.isPlaying || !_positionAnimator) return;
        
        if (_positionAnimator.IsPlaying)
        {
            _positionAnimator.Pause();
        }
        else
        {
            _positionAnimator.Play();
        }
    }

    #endregion Spline Positinoning ---------------------------------------------------------------------------------



    #region Helper ---------------------------------------------------------------------------------------------

    public float GetCurrentSplineT()
    {
        if (!currentPositionOnPath || !splineContainer) return 0f;

        SplineUtility.GetNearestPoint(splineContainer.Spline, currentPositionOnPath.position, out var nearestPoint, out var t);
        return t;
    }
    
    public Vector3 GetEnemyDirectionOnSpline(Vector3 point)
    {
        if (!SplineContainer) return Vector3.forward;
    
        // Get the current position and a slightly ahead position to calculate direction
        Vector3 currentPos = point;
        float currentT = GetCurrentSplineT();
    
        // Sample a small step ahead on the spline to get direction
        float stepSize = 0.01f; // Small step forward
        float aheadT = Mathf.Clamp01(currentT + stepSize);
        Vector3 aheadPos = SplineContainer.EvaluatePosition(aheadT);
    
        Vector3 direction = (aheadPos - currentPos).normalized;
        return direction.magnitude > 0.001f ? direction : Vector3.forward;
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
    
        // Draw player and enemy positions relative to current position
        if (splineContainer && splineContainer.Splines.Count > 0 && currentPositionOnPath)
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