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
    [SerializeField] private bool startMovingOnStart = true;
    [SerializeField] private SplineAnimate.LoopMode loopMode = SplineAnimate.LoopMode.Loop;
    
    [Header("References")]
    [SerializeField, Child] private SplineContainer levelPath;
    [SerializeField] private Transform currentPositionOnPath;
    
    private SplineAnimate _positionAnimator;
    private float _splineLength;
    private float _targetEndPosition;
    
    public SplineContainer LevelPath => levelPath;
    public Transform CurrentPositionOnPath => currentPositionOnPath;
    public Vector2 PlayerBoundary => playerBoundary;
    public Vector2 EnemyBoundary => enemyBoundary;
    public float PlayerOffset => playerOffset;
    public float EnemyOffset => enemyOffset;
    
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
        if (startMovingOnStart) _positionAnimator.Play();
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
        SplinePath<Spline> splinePath = new SplinePath<Spline>(levelPath.Splines);
        _splineLength = splinePath.GetLength();
        
        // Set starting position based on player offset
        float offsetDistance = Mathf.Abs(playerOffset);
        float normalizedOffset = offsetDistance / _splineLength;
        _positionAnimator.StartOffset = normalizedOffset;
        
        // Calculate where to stop based on enemy offset
        float distanceFromEnd = Mathf.Abs(enemyOffset);
        _targetEndPosition = 1f - (distanceFromEnd / _splineLength);
        
        _positionAnimator.Updated += OnSplineMovement;
    }
    
    private void OnSplineMovement(Vector3 position, Quaternion rotation)
    {
        if (_positionAnimator.NormalizedTime >= _targetEndPosition)
        {
            // Dont stop for now so we loop
            // _positionAnimator.Pause();
        }
    }

    public float GetCurrentSplineT()
    {
        if (!currentPositionOnPath || !levelPath) return 0f;

        SplineUtility.GetNearestPoint(levelPath.Spline, currentPositionOnPath.position, out var nearestPoint, out var t);
        return t;
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


    #region Editor -----------------------------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        // Draw the start and end position
        if (levelPath && levelPath.Splines.Count > 0)
        {
            // Calculate positions if we don't have them yet
            if (_splineLength <= 0)
            {
                SplinePath<Spline> splinePath = new SplinePath<Spline>(levelPath.Splines);
                float tempSplineLength = splinePath.GetLength();
                
                // Draw start position (player offset)
                float offsetDistance = Mathf.Abs(playerOffset);
                float normalizedStartOffset = offsetDistance / tempSplineLength;
                Vector3 startPos = levelPath.EvaluatePosition(normalizedStartOffset);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(startPos, 0.3f);
                
                // Draw end position (enemy offset)
                float distanceFromEnd = Mathf.Abs(enemyOffset);
                float normalizedEndPosition = 1f - (distanceFromEnd / tempSplineLength);
                Vector3 endPos = levelPath.EvaluatePosition(normalizedEndPosition);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(endPos, 0.3f);
            }
            else
            {
                // Use calculated values
                float normalizedStartOffset = Mathf.Abs(playerOffset) / _splineLength;
                Vector3 startPos = levelPath.EvaluatePosition(normalizedStartOffset);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(startPos, 0.3f);
                
                Vector3 endPos = levelPath.EvaluatePosition(_targetEndPosition);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(endPos, 0.3f);
            }
        }
        
        // Draw the current position on the path
        if (currentPositionOnPath)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentPositionOnPath.position, 0.5f);
        }
    }

    #endregion Editor -----------------------------------------------------------------------------------------------


}