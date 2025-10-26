using System;
using System.Collections.Generic;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.Splines;
using VInspector;
using Random = UnityEngine.Random;


public class ObstacleManager : MonoBehaviour
{
    [Header("Normal Obstacle Spline Settings")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0,0,400f);
    [SerializeField] private float enemyBoundarySizeMultiplier = 1f;
    [SerializeField] private float playerBoundarySizeMultiplier = 1.3f;
    [SerializeField] private Vector3 enemyBoundaryOffset = Vector3.zero;
    [SerializeField] private Vector3 playerBoundaryOffset = Vector3.zero;
    [SerializeField, Range(0f,100f)] private float chanceToAimAtPlayer = 20f;
    
    [Header("Passthrough Obstacle Settings")]
    [SerializeField] private float passthroughStartDistance = 1500f;
    [SerializeField] private float passthroughEndDistance = -200f;
    [SerializeField] private float passthroughBoundarySizeMultiplier = 1.3f;

    [Header("Obstacles")]
    [SerializeField] private ChanceList<NormalObstacle> obstaclePrefabs;
    [SerializeField] private ChanceList<PassthroughObstacle> passthroughObstaclePrefabs;
    
    [Header("References")] 
    [SerializeField] private Transform obstacleHolder;
    [SerializeField] private Transform splineHolder;
    [SerializeField] private LevelManager levelManager;
    [SerializeField, Scene(Flag.Optional)] private RailPlayer player;
    
    private readonly List<NormalObstacle> _normalObstacles = new List<NormalObstacle>();
    private readonly List<PassthroughObstacle> _passthroughObstacles = new List<PassthroughObstacle>();
    private readonly List<SplineContainer> _splines = new List<SplineContainer>();
    private PassthroughObstacle _lastPassthroughObstacle;
    
    
    
    public int ActiveNormalObstacleCount => _normalObstacles.Count;
    public int ActivePassthroughObstacleCount => _passthroughObstacles.Count;
    public int TotalActiveObstacleCount => _normalObstacles.Count + _passthroughObstacles.Count;
    
    public event Action<NormalObstacle> OnObstacleBroke;
    public event Action<PassthroughObstacle> OnPlayerEnteredPassThroughObstacle; 
    public event Action<PassthroughObstacle> OnPlayerPassedThroughObstacle;

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
    
    private void OnObstacleDestroyed(BaseObstacle obstacle)
    {
        obstacle.OnObstacleDestroyed -= OnObstacleDestroyed;
        
        if (obstacle is PassthroughObstacle passthroughObstacle)
        {
            passthroughObstacle.OnPlayerPassedThrough -= HandleObstaclePassedThrough;
            passthroughObstacle.OnPlayerEnteredPassthrough -= HandlePlayerEnteredPassThrough;
            _passthroughObstacles.Remove(passthroughObstacle);
        }
        else if (obstacle is NormalObstacle normalObstacle)
        {
            normalObstacle.OnObstacleBroke -= HandleObstacleBroke;
            _normalObstacles.Remove(normalObstacle);
        }
        
        _splines.RemoveAll(splineContainer => !splineContainer);
    }
    

    private void HandleObstacleBroke(NormalObstacle normalObstacle)
    {
        OnObstacleBroke?.Invoke(normalObstacle);
    }
    
    private void HandlePlayerEnteredPassThrough(PassthroughObstacle passthroughObstacle)
    {
        OnPlayerEnteredPassThroughObstacle?.Invoke(passthroughObstacle);
    }
    
    private void HandleObstaclePassedThrough(PassthroughObstacle passthroughObstacle)
    {
        OnPlayerPassedThroughObstacle?.Invoke(passthroughObstacle);
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
            Random.Range(-enemySize.x / enemyBoundarySizeMultiplier, enemySize.x / enemyBoundarySizeMultiplier),
            Random.Range(-enemySize.y / enemyBoundarySizeMultiplier, enemySize.y / enemyBoundarySizeMultiplier),
            0f
        );
    
        
        
        Vector3 point3;
        if (Random.Range(0f, 100f) <= chanceToAimAtPlayer)
        {
            // Point 3: Aim at player
            point3 = levelManager.Player.transform.position;
        }
        else
        {
            // Point 3: Random position in player boundary
            Vector2 playerSize = levelManager.PlayerBoundarySize;
             point3 = levelManager.PlayerPosition + playerBoundaryOffset + new Vector3(
                Random.Range(-playerSize.x / playerBoundarySizeMultiplier, playerSize.x / playerBoundarySizeMultiplier),
                Random.Range(-playerSize.y / playerBoundarySizeMultiplier, playerSize.y / playerBoundarySizeMultiplier),
                0f
            );
        }


    
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

    private SplineContainer CreateForwardSplineForObstacle()
    {
        GameObject splineObject = new GameObject("ForwardObstacleSpline");
        splineObject.transform.SetParent(splineHolder);
    
        SplineContainer splineContainer = splineObject.AddComponent<SplineContainer>();
        Spline spline = splineContainer.Spline;
    
        spline.Clear();
    
        // Generate random X/Y position within player boundary size
        Vector2 playerSize = levelManager.PlayerBoundarySize;
        float randomX = Random.Range(-playerSize.x / passthroughBoundarySizeMultiplier, playerSize.x / passthroughBoundarySizeMultiplier);
        float randomY = Random.Range(-playerSize.y / passthroughBoundarySizeMultiplier, playerSize.y / passthroughBoundarySizeMultiplier);
    
        // Point 1: Start position with random offset at configured distance
        Vector3 point1 = new Vector3(randomX, randomY, levelManager.EnemyPosition.z + passthroughStartDistance);
    
        // Point 2: End position with same X/Y but at configured end distance
        Vector3 point2 = new Vector3(randomX, randomY, levelManager.PlayerPosition.z + passthroughEndDistance);
    
        spline.Add(new BezierKnot(point1));
        spline.Add(new BezierKnot(point2));
    
        spline.SetTangentMode(0, TangentMode.Linear);
        spline.SetTangentMode(1, TangentMode.Linear);
    
        _splines.Add(splineContainer);
    
        return splineContainer;
    }

    private NormalObstacle SpawnSplineObstacle(NormalObstacle normalObstaclePrefab)
    {
        if (!normalObstaclePrefab) return null;

        SplineContainer spline = CreateSplineForObstacle();
        
        NormalObstacle newNormalObstacle = Instantiate(normalObstaclePrefab, levelManager.EnemyPosition + spawnPosition, Quaternion.identity, obstacleHolder);
        
        _normalObstacles.Add(newNormalObstacle);
        newNormalObstacle.OnObstacleDestroyed += OnObstacleDestroyed;
        newNormalObstacle.OnObstacleBroke += HandleObstacleBroke;
        
        newNormalObstacle.Initialize(spline);
        
        return newNormalObstacle;
    }
    


    private PassthroughObstacle SpawnPassthroughObstacle(PassthroughObstacle obstaclePrefab)
    {
        if (!obstaclePrefab) return null;
        
        SplineContainer spline = CreateForwardSplineForObstacle();

        PassthroughObstacle newObstacle = Instantiate(obstaclePrefab, levelManager.EnemyPosition + spawnPosition, Quaternion.identity, obstacleHolder);
        
        _passthroughObstacles.Add(newObstacle);
        newObstacle.OnObstacleDestroyed += OnObstacleDestroyed;
        newObstacle.OnPlayerPassedThrough += HandleObstaclePassedThrough;
        newObstacle.OnPlayerEnteredPassthrough += HandlePlayerEnteredPassThrough;
        
        newObstacle.Initialize(spline);
        
        _lastPassthroughObstacle = newObstacle;
        return newObstacle;
    }



    private void SpawnRandomObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Count == 0) return;
        
        NormalObstacle randomNormalObstacle = obstaclePrefabs.GetRandomItem();
        SpawnSplineObstacle(randomNormalObstacle);
    }
    
    [Button]
    public void SpawnRandomPassthroughObstacle()
    {
        if (passthroughObstaclePrefabs == null || passthroughObstaclePrefabs.Count == 0) return;

        PassthroughObstacle randomObstacle;
    
        if (!_lastPassthroughObstacle || passthroughObstaclePrefabs.Count == 1)
        {
            randomObstacle = passthroughObstaclePrefabs.GetRandomItem();
        }
        else
        {
            // Keep trying until we get a different obstacle
            do
            {
                randomObstacle = passthroughObstaclePrefabs.GetRandomItem();
            }
            while (randomObstacle == _lastPassthroughObstacle);
        }
    
        SpawnPassthroughObstacle(randomObstacle);
    }
    
        
    [Button]
    public void SpawnObstacleWave(int amount = 3)
    {
        for (int i = 0; i < amount; i++)
        {
            SpawnRandomObstacle();
        }
    }

    

    [Button]
    private void RemoveAllObstacles()
    {
        // Remove normal obstacles
        for (int i = _normalObstacles.Count - 1; i >= 0; i--)
        {
            if (_normalObstacles[i])
            {
                Destroy(_normalObstacles[i].gameObject);
            }
        }
        _normalObstacles.Clear();

        // Remove passthrough obstacles
        for (int i = _passthroughObstacles.Count - 1; i >= 0; i--)
        {
            if (_passthroughObstacles[i])
            {
                Destroy(_passthroughObstacles[i].gameObject);
            }
        }
        _passthroughObstacles.Clear();

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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!levelManager) return;
    
        // Normal Obstacle Spawn point
        Gizmos.color = Color.yellow;
        Vector3 spawnPoint = levelManager.EnemyPosition + spawnPosition;
        Gizmos.DrawWireSphere(spawnPoint, 4f);
    
        // Normal Obstacle boundaries
        Gizmos.color = Color.yellow;
        DrawBoundaryRect(levelManager.EnemyPosition + enemyBoundaryOffset, levelManager.EnemyBoundarySize, enemyBoundarySizeMultiplier);
        DrawBoundaryRect(levelManager.PlayerPosition + playerBoundaryOffset, levelManager.PlayerBoundarySize, playerBoundarySizeMultiplier);
    
        // Passthrough Obstacle spawn point
        Gizmos.color = Color.cyan;
        Vector3 passthroughSpawnPoint = new Vector3(0f, 0f, levelManager.EnemyPosition.z + passthroughStartDistance);
        Gizmos.DrawWireSphere(passthroughSpawnPoint, 4f);
        
        // Passthrough Obstacle end point
        Vector3 passthroughEndPoint = new Vector3(0f, 0f, levelManager.PlayerPosition.z + passthroughEndDistance);
        Gizmos.DrawWireSphere(passthroughEndPoint, 4f);
        
        // Passthrough boundary area at start
        Gizmos.color = Color.cyan;
        DrawBoundaryRect(passthroughSpawnPoint, levelManager.PlayerBoundarySize, passthroughBoundarySizeMultiplier);

        UnityEditor.Handles.Label(spawnPoint + Vector3.up * 5f, "Normal Obstacle Spawn");
        UnityEditor.Handles.Label(levelManager.EnemyPosition + enemyBoundaryOffset + Vector3.up * 5f, "Normal Obstacle Enemy Boundary");
        UnityEditor.Handles.Label(levelManager.PlayerPosition + playerBoundaryOffset + Vector3.up * 5f, "Normal Obstacle Player Boundary");
        UnityEditor.Handles.Label(passthroughSpawnPoint + Vector3.up * 5f, "Passthrough Start");
        UnityEditor.Handles.Label(passthroughEndPoint + Vector3.up * 5f, "Passthrough End");
    }

    private void DrawBoundaryRect(Vector3 center, Vector2 size, float multiplier)
    {
        float halfWidth = size.x / multiplier;
        float halfHeight = size.y / multiplier;
    
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