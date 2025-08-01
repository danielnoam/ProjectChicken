using System;
using System.Collections.Generic;
using DNExtensions;
using UnityEngine;


public class EnemyWaveSpawner : MonoBehaviour
{
    public static EnemyWaveSpawner Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;

    
    private SOLevelStage _currentStage;
    private readonly HashSet<ChickenController> _activeEnemies = new HashSet<ChickenController>();


    public int ActiveEnemyCount => _activeEnemies.Count;
    public event Action OnEnemyWaveSpawned;
    public event Action<int> OnEnemyWaveCleared;
    public event Action<ChickenController> OnEnemyDeath;


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
    }
    
    
    private void OnEnable()
    {
        levelManager.OnStageChanged += OnStageChanged;
        player.OnDeath += OnPlayerDeath;
        
        foreach (var enemy in _activeEnemies)
        {
            enemy.OnDeath += UpdateEnemyCount;
        }
    }

    private void OnDisable()
    {
        levelManager.OnStageChanged -= OnStageChanged;
        player.OnDeath -= OnPlayerDeath;
        
        foreach (var enemy in _activeEnemies)
        {
            enemy.OnDeath -= UpdateEnemyCount;
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
    
    
    private void UpdateEnemyCount(ChickenController enemy)
    {
        if (_currentStage && _currentStage.IsTimeBasedStage) return;
        
        _activeEnemies.Remove(enemy);
        OnEnemyDeath?.Invoke(enemy);
        
        if (_activeEnemies.Count <= 0)
        {
            OnEnemyWaveCleared?.Invoke(_currentStage.WaveScore);
        }
    }
    
    private void OnPlayerDeath()
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
    
    
    
    private void SpawnEnemy(ChickenController enemyPrefab)
    {
        if (!enemyPrefab) return;

        var enemyObject = ObjectPooler.GetObjectFromPool(enemyPrefab.gameObject);
        if (enemyObject.TryGetComponent<ChickenController>(out var enemy))
        {
            enemy.transform.localPosition = Vector3.zero;
            enemy.transform.localRotation = Quaternion.identity;
            enemy.OnDeath += UpdateEnemyCount;
            _activeEnemies.Add(enemy);
        }

    }
    
    private void ClearEnemies()
    {
        var enemiesToClear = new HashSet<ChickenController>(_activeEnemies);
    
        foreach (var enemy in enemiesToClear)
        {
            enemy.OnDeath -= UpdateEnemyCount;
            enemy.ReturnToPool();
        }
        
        _activeEnemies.Clear();
    }
    
    

    

}
