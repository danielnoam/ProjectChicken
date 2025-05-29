
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;

public class TestEnemyWaveHandler : MonoBehaviour
{
    [Header("Spawning Settings")] 
    [SerializeField] private Vector3 spawnPositionOffset = new Vector3(0, 0, 12f);
    [SerializeField] private Vector2 xSpawnPositionRange = new Vector2(-10f, 10f);
    [SerializeField] private Vector2 ySpawnPositionRange = new Vector2(-10f, 10f);
    
    
    [Header("Follow Point Settings")]
    [SerializeField] private Vector3 followPositionOffset = new Vector3(0, 0, 12f);
    [SerializeField] private Vector2 xFollowPositionRange = new Vector2(-10f, 10f);
    [SerializeField] private Vector2 yFollowPositionRange = new Vector2(-10f, 10f);

    
    [Header("References")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Transform followPointHolder;
    [SerializeField] private Transform enemyHolder;
    [SerializeField] private EnemyBase enemyPrefab;
    
    
     private readonly Dictionary<Transform, Vector3> _followPoints = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        ClearPoints();
    }


    private void Update()
    {
        MovePointsAlongPath();
        
        
        // If there are no enemies, spawn a new wave
        if (FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length <= 0)
        {
            SpawnEnemyWave(Random.Range(1, 7));
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
        if (!levelManager || !enemyPrefab) return;
        
        Vector3 spawnPoint = levelManager.CurrentPositionOnPath.position  + spawnPositionOffset + new Vector3(Random.Range(xSpawnPositionRange.x,xSpawnPositionRange.y), Random.Range(ySpawnPositionRange.x,ySpawnPositionRange.y), 0);
        
        EnemyBase newEnemy = Instantiate(enemyPrefab, spawnPoint, Quaternion.identity, enemyHolder);
        
        newEnemy.SetUp(followPoint);
    }
    
    private void ClearEnemies()
    {

        var enemies = FindObjectsByType<EnemyBase>( FindObjectsSortMode.None);
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
            Vector3 offset = followPositionOffset + new Vector3(Random.Range(xFollowPositionRange.x, xFollowPositionRange.y), Random.Range(yFollowPositionRange.x, yFollowPositionRange.y), 0);
            _followPoints.Add(newPoint, offset);
        }
    }

    private void MovePointsAlongPath()
    {
        if (_followPoints.Count <= 0) return;
            
        
        foreach (var point in _followPoints)
        {
            if (point.Key)
            {
                point.Key.position = levelManager.CurrentPositionOnPath.position + point.Value;
            }
            
        }
    }

    #endregion Follow Points ------------------------------------------------------------
}
