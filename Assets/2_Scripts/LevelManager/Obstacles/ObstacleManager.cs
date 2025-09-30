
using System.Collections.Generic;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.Splines;
using VInspector;



public class ObstacleManager : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0,0,400f);
    
    [Header("Spline Movement Settings")]
    [SerializeField] private float boundarySizeMultiplier = 2f;
    [SerializeField] private Vector3 enemyBoundaryOffset = Vector3.zero;
    [SerializeField] private Vector3 playerBoundaryOffset = Vector3.zero;
    
    [Header("Forward Movement Settings")]
    [SerializeField] private float directionVariation = 30f;


    [Header("Obstacles")]
    [SerializeField] private ChanceList<Obstacle> splineObstaclePrefabs;
    [SerializeField] private ChanceList<Obstacle> forwardObstaclePrefabs;


    
    [Header("References")] 
    [SerializeField] private Transform obstacleHolder;
    [SerializeField] private Transform splineHolder;
    [SerializeField] private LevelManager levelManager;
    [SerializeField, Scene(Flag.Optional)] private RailPlayer player;
    
    private readonly List<Obstacle> _obstacles = new List<Obstacle>();
    private readonly List<SplineContainer> _splines = new List<SplineContainer>();
    public int ActiveObstacleCount => _obstacles.Count;
    
    private void OnValidate()
    {
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        
        if (!player)
        {
            player = FindFirstObjectByType<RailPlayer>();
        }
        
        this.ValidateRefs();
    }
    
    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
        }

        if (player)
        {
            player.Health.OnDeath += OnPlayerDeath;
        }
    }

    private void OnDisable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }
        
        if (player)
        {
            player.Health.OnDeath -= OnPlayerDeath;
        }
    }

    private void OnPlayerDeath()
    {
        RemoveAllObstacles();
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        if (stage.StageType == StageType.Store) 
        {
            RemoveAllObstacles();
        }
    }
    
    private void OnObstacleDestroyed(Obstacle obstacle)
    {
        _obstacles.Remove(obstacle);
        _splines.RemoveAll(splineContainer => !splineContainer);
    }

    private SplineContainer CreateSplineForObstacle()
    {
        GameObject splineObject = new GameObject("ObstacleSpline");
        splineObject.transform.SetParent(splineHolder);
    
        SplineContainer splineContainer = splineObject.AddComponent<SplineContainer>();
        Spline spline = splineContainer.Spline;
    
        spline.Clear();
    
        // Point 1: Spawn position
        Vector3 point1 = levelManager.EnemyPosition + spawnPosition;
    
        // Point 2: Random position in enemy boundary
        Vector2 enemySize = levelManager.EnemyBoundarySize;
        Vector3 point2 = levelManager.EnemyPosition + enemyBoundaryOffset + new Vector3(
            UnityEngine.Random.Range(-enemySize.x / boundarySizeMultiplier, enemySize.x / boundarySizeMultiplier),
            UnityEngine.Random.Range(-enemySize.y / boundarySizeMultiplier, enemySize.y / boundarySizeMultiplier),
            0f
        );
    
        // Point 3: Random position in player boundary
        Vector2 playerSize = levelManager.PlayerBoundarySize;
        Vector3 point3 = levelManager.PlayerPosition + playerBoundaryOffset + new Vector3(
            UnityEngine.Random.Range(-playerSize.x / boundarySizeMultiplier, playerSize.x / boundarySizeMultiplier),
            UnityEngine.Random.Range(-playerSize.y / boundarySizeMultiplier, playerSize.y / boundarySizeMultiplier),
            0f
        );
    
        // Point 4: Extended point from player boundary
        Vector3 directionAtPoint3 = (point3 - point2).normalized;
        Vector3 point4 = point3 + (directionAtPoint3 * 250f);
    
        spline.Add(new BezierKnot(point1));
        spline.Add(new BezierKnot(point2));
        spline.Add(new BezierKnot(point3));
        spline.Add(new BezierKnot(point4));
    
        spline.SetTangentMode(0, TangentMode.AutoSmooth);
        spline.SetTangentMode(1, TangentMode.AutoSmooth);
        spline.SetTangentMode(2, TangentMode.AutoSmooth);
        spline.SetTangentMode(3, TangentMode.AutoSmooth);
    
        _splines.Add(splineContainer);
    
        return splineContainer;
    }

    private Obstacle SpawnSplineObstacle(Obstacle obstaclePrefab)
    {
        if (!obstaclePrefab) return null;

        SplineContainer spline = CreateSplineForObstacle();
        
        Obstacle newObstacle = Instantiate(obstaclePrefab, levelManager.EnemyPosition + spawnPosition, Quaternion.identity, obstacleHolder);
        
        _obstacles.Add(newObstacle);
        newObstacle.OnObstacleDestroyed += OnObstacleDestroyed;
        
        newObstacle.Initialize(ObstacleMovementType.Spline, spline);
        
        return newObstacle;
    }

    private Obstacle SpawnForwardObstacle(Obstacle obstaclePrefab)
    {
        if (!obstaclePrefab) return null;

        Obstacle newObstacle = Instantiate(obstaclePrefab, levelManager.EnemyPosition + spawnPosition, Quaternion.identity, obstacleHolder);
        
        _obstacles.Add(newObstacle);
        newObstacle.OnObstacleDestroyed += OnObstacleDestroyed;
        
        Vector3 directionToPlayer = GetDirectionToPlayer(newObstacle.transform.position);
        newObstacle.Initialize(ObstacleMovementType.Forward, null, directionToPlayer);
        
        return newObstacle;
    }
    
    private Vector3 GetDirectionToPlayer(Vector3 fromPosition)
    {
        if (!player) return Vector3.forward;
    
        Vector3 directionToPlayer = (player.transform.position - fromPosition).normalized;
    
        float randomAngle = UnityEngine.Random.Range(-directionVariation, directionVariation);
        
        Vector3 randomAxis = new Vector3(
            UnityEngine.Random.Range(-1, 2),
            UnityEngine.Random.Range(-1, 2),
            UnityEngine.Random.Range(-1, 2)
        );
        
        if (randomAxis == Vector3.zero) randomAxis = Vector3.up;
        else randomAxis = randomAxis.normalized;
    
        Vector3 finalDirection = Quaternion.AngleAxis(randomAngle, randomAxis) * directionToPlayer;
    
        return finalDirection;
    }

    [Button]
    public void SpawnRandomSplineObstacle()
    {
        if (splineObstaclePrefabs == null || splineObstaclePrefabs.Count == 0) return;
        
        Obstacle randomObstacle = splineObstaclePrefabs.GetRandomItem();
        SpawnSplineObstacle(randomObstacle);
    }

    [Button]
    public void SpawnRandomForwardObstacle()
    {
        if (forwardObstaclePrefabs == null || forwardObstaclePrefabs.Count == 0) return;
        
        Obstacle randomObstacle = forwardObstaclePrefabs.GetRandomItem();
        SpawnForwardObstacle(randomObstacle);
    }
    
    public Obstacle SpawnSpecificSplineObstacle(Obstacle obstaclePrefab)
    {
        if (!obstaclePrefab) return null;
        return SpawnSplineObstacle(obstaclePrefab);
    }

    public Obstacle SpawnSpecificForwardObstacle(Obstacle obstaclePrefab)
    {
        if (!obstaclePrefab) return null;
        return SpawnForwardObstacle(obstaclePrefab);
    }

    [Button]
    private void RemoveAllObstacles()
    {
        if (_obstacles.Count == 0) return;

        for (int i = _obstacles.Count - 1; i >= 0; i--)
        {
            if (_obstacles[i])
            {
                Destroy(_obstacles[i].gameObject);
            }
        }
        
        _obstacles.Clear();

        // Clean up splines
        for (int i = _splines.Count - 1; i >= 0; i--)
        {
            if (_splines[i])
            {
                Destroy(_splines[i].gameObject);
            }
        }
        
        _splines.Clear();
    }
    
    [Button]
    public void SpawnSplineObstacleWave(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            SpawnRandomSplineObstacle();
        }
    }

    [Button]
    public void SpawnForwardObstacleWave(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            SpawnRandomForwardObstacle();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!levelManager) return;
    
        // Spawn point
        Gizmos.color = Color.yellow;
        Vector3 spawnPoint = levelManager.EnemyPosition + spawnPosition;
        Gizmos.DrawWireSphere(spawnPoint, 4f);
    
        // Draw 2D boundaries as rectangles
        DrawBoundaryRect(levelManager.EnemyPosition + enemyBoundaryOffset, levelManager.EnemyBoundarySize, Color.yellow);
        DrawBoundaryRect(levelManager.PlayerPosition + playerBoundaryOffset, levelManager.PlayerBoundarySize, Color.yellow);
    

        UnityEditor.Handles.Label(spawnPoint + Vector3.up * boundarySizeMultiplier, "Obstacle Spawn Point");
        UnityEditor.Handles.Label(levelManager.EnemyPosition + enemyBoundaryOffset + Vector3.up * boundarySizeMultiplier, "Obstacle Enemy Boundary");
        UnityEditor.Handles.Label(levelManager.PlayerPosition + playerBoundaryOffset + Vector3.up * boundarySizeMultiplier, "Obstacle Player Boundary");

    }

    private void DrawBoundaryRect(Vector3 center, Vector2 size, Color color)
    {
        Gizmos.color = color;
    
        float halfWidth = size.x / boundarySizeMultiplier;
        float halfHeight = size.y / boundarySizeMultiplier;
    
        Vector3 topLeft = center + new Vector3(-halfWidth, halfHeight, 0f);
        Vector3 topRight = center + new Vector3(halfWidth, halfHeight, 0f);
        Vector3 bottomRight = center + new Vector3(halfWidth, -halfHeight, 0f);
        Vector3 bottomLeft = center + new Vector3(-halfWidth, -halfHeight, 0f);
    
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
    
#endif
}