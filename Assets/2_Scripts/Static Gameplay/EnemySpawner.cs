using System;
using System.Collections.Generic;
using DNExtensions;
using UnityEngine;


public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private SOGameSettings gameSettings;
    [SerializeField] private Transform enemySpawnPosition;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;

    [Header("Spawn Area Configuration")]
    [SerializeField] private BoxCollider spawnAreaBig;
    [SerializeField] private BoxCollider spawnAreaBlocker;
    [SerializeField] private int maxSpawnAttempts = 50;
    [SerializeField] private float minDistanceFromBlocker = 0.5f;
    [SerializeField] private bool visualizeSpawnArea = true;

    
    private readonly HashSet<ChickenStateController> _activeEnemies = new HashSet<ChickenStateController>();
    private SOLevelStage _currentStage;

    
    public int ActiveEnemyCount => _activeEnemies.Count;
    
    // Public access to spawn area colliders for other systems
    public BoxCollider SpawnAreaBig => spawnAreaBig;
    public BoxCollider SpawnAreaBlocker => spawnAreaBlocker;
    
    
    public event Action OnEnemyWaveSpawned;
    public event Action<int> OnEnemyWaveCleared;
    public event Action<ChickenStateController> OnEnemyDeath;


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

        // Auto-find box colliders if not assigned
        if (!spawnAreaBig)
        {
            BoxCollider[] colliders = GetComponentsInChildren<BoxCollider>();
            if (colliders.Length > 0)
            {
                // Find the largest collider as the big one
                BoxCollider largest = colliders[0];
                foreach (var collider in colliders)
                {
                    if (collider.size.magnitude > largest.size.magnitude)
                        largest = collider;
                }
                spawnAreaBig = largest;
            }
        }

        if (!spawnAreaBlocker && spawnAreaBig)
        {
            BoxCollider[] colliders = GetComponentsInChildren<BoxCollider>();
            foreach (var collider in colliders)
            {
                if (collider != spawnAreaBig)
                {
                    spawnAreaBlocker = collider;
                    break;
                }
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

        // Validate spawn area setup
        ValidateSpawnAreaSetup();
    }
    
    
    private void OnEnable()
    {
        levelManager.OnStageChanged += OnStageChanged;
        player.Health.OnDeath += Death;
        
        foreach (var enemy in _activeEnemies)
        {
            enemy.OnDeathEvent += UpdateEnemyCount;
        }
    }

    private void OnDisable()
    {
        levelManager.OnStageChanged -= OnStageChanged;
        player.Health.OnDeath -= Death;
        
        foreach (var enemy in _activeEnemies)
        {
            enemy.OnDeathEvent -= UpdateEnemyCount;
        }
    }
    
    
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        _currentStage = stage;
        
        if (stage.StageType == StageType.EnemyWave)
        {
            SpawnEnemyWave(stage);
        }
        else
        {
            ClearEnemies();
        }
        

    }
    
    
    private void UpdateEnemyCount(ChickenStateController enemy)
    {
        if (_currentStage && _currentStage.IsTimeBasedStage) return;
        
        _activeEnemies.Remove(enemy);
        OnEnemyDeath?.Invoke(enemy);
        
        
        if (_activeEnemies.Count <= 0)
        {
            OnEnemyWaveCleared?.Invoke(_currentStage.WaveScoreWorth);
        }
        
        enemy.OnDeathEvent -= UpdateEnemyCount;
    }
    
    private void Death()
    {
        ClearEnemies();
    }
    
    


    private void SpawnEnemyWave(SOLevelStage stage)
    {
        if (stage.EnemyWave.Count == 0) return;
        
        ClearEnemies();
        
        foreach (var enemyType in stage.EnemyWave)
        {
            for (int i = 0; i < enemyType.Value; i++)
            {
                if (!enemyType.Key || enemyType.Value <= 0) continue;
                
                SpawnEnemy(enemyType.Key);
            }
        }
        
        OnEnemyWaveSpawned?.Invoke();
    }
    
    
    
    private void SpawnEnemy(ChickenStateController enemyPrefab)
    {
        if (!enemyPrefab) return;

        // Get a valid spawn position FIRST
        Vector3 spawnPosition = GetValidSpawnPosition();
        
        // Pass the spawn position to the object pooler
        var enemyObject = ObjectPooler.GetObjectFromPool(enemyPrefab.gameObject, spawnPosition);
        
        if (enemyObject.TryGetComponent<ChickenStateController>(out var enemy))
        {
            // Force immediate positioning to prevent any visual glitches
            ForceImmediatePosition(enemy, spawnPosition);
            
            
            // Add to tracking
            enemy.OnDeathEvent += UpdateEnemyCount;
            _activeEnemies.Add(enemy);
        }
    }

    // Force immediate positioning before any frame updates
    private void ForceImmediatePosition(ChickenStateController enemy, Vector3 targetPosition)
    {
        if (!enemy) return;

        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Use the most direct approach for rigidbody positioning
            rb.position = targetPosition;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Also set transform position as backup
        enemy.transform.position = targetPosition;
    }

    // Get a valid spawn position within the big collider but outside the blocker
    private Vector3 GetValidSpawnPosition()
    {
        // Fallback to original spawn position if colliders not set up
        if (!spawnAreaBig)
        {
            return enemySpawnPosition ? enemySpawnPosition.position : transform.position;
        }

        Vector3 validPosition = Vector3.zero;
        bool foundValidPosition = false;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Generate random position within the big collider bounds
            Vector3 randomPosition = GetRandomPositionInBounds(spawnAreaBig);
            
            // Check if position is valid (not inside blocker)
            if (IsPositionValid(randomPosition))
            {
                validPosition = randomPosition;
                foundValidPosition = true;
                break;
            }
        }

        if (!foundValidPosition)
        {
            Debug.LogWarning($"EnemySpawner: Could not find valid spawn position after {maxSpawnAttempts} attempts. Using fallback position.");
            validPosition = GetFallbackPosition();
        }

        return validPosition;
    }

    // Generate random position within box collider bounds
    private Vector3 GetRandomPositionInBounds(BoxCollider boxCollider)
    {
        Bounds bounds = boxCollider.bounds;
        
        float randomX = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
        float randomY = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);
        
        return new Vector3(randomX, randomY, randomZ);
    }

    // Check if position is valid (inside big collider, outside blocker)
    private bool IsPositionValid(Vector3 position)
    {
        // Must be inside the big spawn area
        if (!spawnAreaBig.bounds.Contains(position))
            return false;

        // Must NOT be inside the blocker area (if blocker exists)
        if (spawnAreaBlocker != null)
        {
            // Add minimum distance buffer from blocker
            Bounds blockerBounds = spawnAreaBlocker.bounds;
            blockerBounds.Expand(minDistanceFromBlocker * 2f);
            
            if (blockerBounds.Contains(position))
                return false;
        }

        return true;
    }

    // Get fallback position when no valid position found
    private Vector3 GetFallbackPosition()
    {
        if (enemySpawnPosition)
            return enemySpawnPosition.position;

        // Use edge of big collider as fallback
        if (spawnAreaBig)
        {
            Bounds bounds = spawnAreaBig.bounds;
            // Position at the edge of the big collider
            return new Vector3(bounds.max.x - 1f, bounds.center.y, bounds.center.z);
        }

        return transform.position;
    }

    // Validate spawn area setup
    private void ValidateSpawnAreaSetup()
    {
        if (!spawnAreaBig)
        {
            Debug.LogError($"EnemySpawner: Big spawn area collider not assigned! Using fallback spawning.");
            return;
        }

        if (!spawnAreaBig.isTrigger)
        {
            Debug.LogWarning($"EnemySpawner: Big spawn area collider should probably be set as Trigger.");
        }

        if (spawnAreaBlocker && !spawnAreaBlocker.isTrigger)
        {
            Debug.LogWarning($"EnemySpawner: Blocker spawn area collider should probably be set as Trigger.");
        }

        if (spawnAreaBlocker)
        {
            // Check if blocker is completely inside the big area
            if (!spawnAreaBig.bounds.Contains(spawnAreaBlocker.bounds.min) || !spawnAreaBig.bounds.Contains(spawnAreaBlocker.bounds.max))
            {
                // Debug.LogWarning($"EnemySpawner: Blocker area extends outside the big spawn area. This may cause spawning issues.");
            }
        }
    }
    
    private void ClearEnemies()
    {
        var enemiesToClear = new HashSet<ChickenStateController>(_activeEnemies);
    
        foreach (var enemy in enemiesToClear)
        {
            if (enemy != null)
            {
                enemy.OnDeathEvent -= UpdateEnemyCount;
                enemy.ReturnToPool();
            }
        }
        
        _activeEnemies.Clear();
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!visualizeSpawnArea) return;

        // Draw big spawn area
        if (spawnAreaBig)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Green
            Gizmos.matrix = spawnAreaBig.transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, spawnAreaBig.size);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, spawnAreaBig.size);
        }

        // Draw blocker area
        if (spawnAreaBlocker)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red
            Gizmos.matrix = spawnAreaBlocker.transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, spawnAreaBlocker.size);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, spawnAreaBlocker.size);
        }

        // Reset matrix
        Gizmos.matrix = Matrix4x4.identity;

        // Draw spawn position reference
        if (enemySpawnPosition)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(enemySpawnPosition.position, 0.5f);
        }
    }

    // Public methods for debugging
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestSpawnPosition()
    {
        Vector3 testPosition = GetValidSpawnPosition();
        Debug.Log($"Test spawn position: {testPosition}");
        
        // Visual indicator in scene view
        Debug.DrawRay(testPosition, Vector3.up * 2f, Color.magenta, 2f);
    }
}