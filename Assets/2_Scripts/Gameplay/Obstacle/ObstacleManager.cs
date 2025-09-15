using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using VInspector;

public class ObstacleManager : MonoBehaviour
{

    
    [Header("Obstacle Spawning")]
    [SerializeField] private float spawnDistance = 50f;
    [SerializeField] private Obstacle[] obstaclePrefabs;

    
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 15f;
    [SerializeField] private float directionVariation = 30f;
    
    
    [Header("References")] 
    [SerializeField] private Transform obstacleHolder;
    [SerializeField] private LevelManager levelManager;
    [SerializeField, Scene(Flag.Optional)] private RailPlayer player;
    
    private readonly List<Obstacle> _obstacles = new List<Obstacle>();
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
    
    private void Update()
    {
        // CleanupDistantObstacles();
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
    }

    private Obstacle SpawnObstacle(Obstacle obstaclePrefab)
    {
        if (!obstaclePrefab) return null;

        Obstacle newObstacle = Instantiate(obstaclePrefab, levelManager.EnemyPosition + (Vector3.forward * spawnDistance), Quaternion.identity, obstacleHolder);
        
        _obstacles.Add(newObstacle);
        newObstacle.OnObstacleDestroyed += OnObstacleDestroyed;
        
        Vector3 directionToPlayer = GetDirectionToPlayer(newObstacle.transform.position);
        newObstacle.Initialize(directionToPlayer, baseSpeed);
        
        Debug.Log("Spawned Obstacle: " + newObstacle.name);
        return newObstacle;
    }
    
    private Vector3 GetDirectionToPlayer(Vector3 fromPosition)
    {
        if (!player) return Vector3.forward;
        
        Vector3 directionToPlayer = (player.transform.position - fromPosition).normalized;
        
        float randomAngle = UnityEngine.Random.Range(-directionVariation, directionVariation);
        Vector3 finalDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * directionToPlayer;
        
        return finalDirection;
    }
    
    
    [Button]
    public void SpawnRandomObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;
        
        Obstacle randomObstacle = obstaclePrefabs[UnityEngine.Random.Range(0, obstaclePrefabs.Length)];
        
        SpawnObstacle(randomObstacle);
    }
    
    public Obstacle SpawnSpecificObstacle(Obstacle obstaclePrefab)
    {
        if (!obstaclePrefab) return null;
        
        return SpawnObstacle(obstaclePrefab);
    }
    
    
    private void CleanupDistantObstacles()
    {
        if (!player) return;
        
        for (int i = _obstacles.Count - 1; i >= 0; i--)
        {
            if (!_obstacles[i])
            {
                _obstacles.RemoveAt(i);
                continue;
            }
            
            float distanceToPlayer = Vector3.Distance(_obstacles[i].transform.position, player.transform.position);
            
            if (distanceToPlayer > spawnDistance * 2f)
            {
                Obstacle obstacle = _obstacles[i];
                _obstacles.RemoveAt(i);
                
                if (obstacle)
                {
                    Destroy(obstacle.gameObject);
                }
            }
        }
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
    }
    
    [Button]
    private void SpawnObstacleWave()
    {
        int waveSize = UnityEngine.Random.Range(3, 6);
        
        for (int i = 0; i < waveSize; i++)
        {
            SpawnRandomObstacle();
        }
    }

}