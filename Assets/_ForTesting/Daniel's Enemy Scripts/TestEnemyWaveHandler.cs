using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;

public class TestEnemyWaveHandler : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private bool alignToSplineDirection = true;
    [SerializeField] private Vector2 xSpawnPositionRange = new Vector2(-10f, 10f);
    [SerializeField] private Vector2 ySpawnPositionRange = new Vector2(-10f, 10f);

    [Header("References")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Transform followPointHolder;
    [SerializeField] private Transform enemyHolder;
    [SerializeField] private TestEnemyBase testEnemyPrefab;
    
    private readonly Dictionary<Transform, Vector3> _followPoints = new Dictionary<Transform, Vector3>();
    private Quaternion _splineRotation = Quaternion.identity;

    private void Awake()
    {
        ClearPoints();
    }

    private void Update()
    {
        HandleSplineRotation();
        MovePointsAlongPath();
        
        // If there are no enemies, spawn a new wave
        if (FindObjectsByType<TestEnemyBase>(FindObjectsSortMode.None).Length <= 0)
        {
            SpawnEnemyWave(Random.Range(1, 7));
        }
    }

    private void HandleSplineRotation()
    {
        if (!alignToSplineDirection || !levelManager || !levelManager.SplineContainer)
        {
            _splineRotation = Quaternion.identity;
            return;
        }

        // Get the spline direction at the enemy position
        Vector3 splineForward = GetSplineDirection();
        
        if (splineForward != Vector3.zero)
        {
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineForward, Vector3.up);
            _splineRotation = Quaternion.Slerp(_splineRotation, targetSplineRotation, 5f * Time.deltaTime);
        }
    }

    #region Enemy Spawning  ------------------------------------------------------------

    [Button]
    private void SpawnEnemyWave(int waveSize = 3)
    {
        if (!Application.isPlaying) return;
        
        ClearEnemies();
        CreateFollowPoints(waveSize);

        foreach (Transform followPoint in _followPoints.Keys)
        {
            if (followPoint)
            {
                SpawnEnemy(followPoint);
            }
        }
    }
    
    private void SpawnEnemy(Transform followPoint)
    {
        if (!levelManager || !testEnemyPrefab) return;
        
        Vector3 spawnPoint = levelManager.EnemyPosition + new Vector3(Random.Range(xSpawnPositionRange.x, xSpawnPositionRange.y), Random.Range(ySpawnPositionRange.x, ySpawnPositionRange.y), 0);
        
        TestEnemyBase newTestEnemy = Instantiate(testEnemyPrefab, spawnPoint, Quaternion.identity, enemyHolder);
        newTestEnemy.SetUp(followPoint);
    }
    
    private void ClearEnemies()
    {
        var enemies = FindObjectsByType<TestEnemyBase>(FindObjectsSortMode.None);
        if (enemies.Length > 0)
        {
            foreach (var enemy in enemies)
            {
                if (enemy)
                {
                    Destroy(enemy.gameObject);
                }
            }
        }
    }

    #endregion Enemy Spawning  ------------------------------------------------------------

    #region Follow Points ------------------------------------------------------------

    private void ClearPoints()
    {
        foreach (Transform point in _followPoints.Keys)
        {
            if (point)
            {
                Destroy(point.gameObject);
            }
        }
        
        _followPoints.Clear();
    }
    
    private void CreateFollowPoints(int count)
    {
        ClearPoints();
        
        for (int i = 0; i < count; i++)
        {
            Transform newPoint = new GameObject($"FollowPoint_{i}").transform;
            newPoint.SetParent(followPointHolder);
            newPoint.position = levelManager.CurrentPositionOnPath.position;
            
            // Generate random offset within the enemy boundary
            Vector3 randomOffset = new Vector3(
                Random.Range(-levelManager.EnemyBoundary.x, levelManager.EnemyBoundary.x),
                Random.Range(-levelManager.EnemyBoundary.y, levelManager.EnemyBoundary.y),
                0
            );
            
            _followPoints.Add(newPoint, randomOffset);
        }
    }

    private void MovePointsAlongPath()
    {
        if (_followPoints.Count <= 0 || !levelManager) return;
        
        Vector3 enemySplinePosition = levelManager.EnemyPosition;
        
        foreach (var point in _followPoints)
        {
            if (point.Key)
            {
                // Calculate the target position
                Vector3 localOffset = point.Value;
                Vector3 targetPosition;
                
                if (alignToSplineDirection)
                {
                    // Transform offset to world space using spline rotation
                    Vector3 worldOffset = _splineRotation * localOffset;
                    targetPosition = enemySplinePosition + worldOffset;
                    
                    // Apply boundary clamping in local spline space
                    Vector3 relativePosition = targetPosition - enemySplinePosition;
                    Vector3 localRelativePosition = Quaternion.Inverse(_splineRotation) * relativePosition;
                    
                    // Clamp in the local spline space
                    localRelativePosition.x = Mathf.Clamp(localRelativePosition.x, 
                        -levelManager.EnemyBoundary.x, levelManager.EnemyBoundary.x);
                    localRelativePosition.y = Mathf.Clamp(localRelativePosition.y, 
                        -levelManager.EnemyBoundary.y, levelManager.EnemyBoundary.y);
                    
                    // Transform back to world space
                    targetPosition = enemySplinePosition + (_splineRotation * localRelativePosition);
                }
                else
                {
                    // Traditional world-space positioning
                    targetPosition = enemySplinePosition + localOffset;
                    
                    // Apply boundary clamping in world space
                    targetPosition.x = Mathf.Clamp(targetPosition.x, 
                        enemySplinePosition.x - levelManager.EnemyBoundary.x, 
                        enemySplinePosition.x + levelManager.EnemyBoundary.x);
                    targetPosition.y = Mathf.Clamp(targetPosition.y, 
                        enemySplinePosition.y - levelManager.EnemyBoundary.y, 
                        enemySplinePosition.y + levelManager.EnemyBoundary.y);
                }
                
                point.Key.position = targetPosition;
                
                // Rotate the follow point to match the spline direction if needed
                if (alignToSplineDirection)
                {
                    point.Key.rotation = _splineRotation;
                }
            }
        }
    }

    #endregion Follow Points ------------------------------------------------------------

    
    #region Helper Methods ------------------------------------------------------------

    private Vector3 GetSplineDirection()
    {
        return !levelManager ? Vector3.forward : levelManager.GetDirectionOnSpline(levelManager.EnemyPosition);
    }

    #endregion Helper Methods ------------------------------------------------------------

}