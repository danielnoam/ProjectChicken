using System;
using KBCore.Refs;
using UnityEngine;
using VInspector;


public class EnemyWaveManager : MonoBehaviour
{
    public static EnemyWaveManager Instance { get; private set; }
    
    [Header("Debug")]
    [SerializeField,ReadOnly] private int enemyCount;
    [SerializeField,ReadOnly] private SOLevelStage currentStage;
    
    [Header("References")]
    [SerializeField] private Transform enemyHolder;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;



    public event Action<int> OnEnemyWaveCleared;
    public event Action<int> OnEnemyDeath;


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
    }

    private void OnDisable()
    {
        levelManager.OnStageChanged -= OnStageChanged;
        player.OnDeath -= OnPlayerDeath;
        foreach (Transform child in enemyHolder)
        {
            if (child.TryGetComponent<ChickenController>(out var enemy))
            {
                enemy.OnDeath -= UpdateEnemyCount;
            }
        }
    }
    
    
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        currentStage = stage;
        
        if (stage.StageType == StageType.EnemyWave)
        {
            SpawnEnemyWave(stage);
        }
        else
        {
            ClearEnemies();
        }
        

    }
    
    
    private void UpdateEnemyCount(int enemyScore)
    {
        if (currentStage && currentStage.IsTimeBasedStage) return;
        
        enemyCount--;
        OnEnemyDeath?.Invoke(enemyScore);
        
        if (enemyCount <= 0)
        {
            OnEnemyWaveCleared?.Invoke(currentStage.WaveScore);
        }
    }
    
    private void OnPlayerDeath()
    {
        ClearEnemies();
    }
    
    
    

    #region Enemy Spawning --------------------------------------------------------------------------------------

    private void SpawnEnemyWave(SOLevelStage stage)
    {
        if (stage.EnemyWave.Count == 0) return;
        
        
        // Clear previous enemies
        ClearEnemies();

        
        // Spawn new enemies
        int totalEnemiesSpawned = 0;

        foreach (var enemyType in stage.EnemyWave)
        {
            for (int i = 0; i < enemyType.Value; i++)
            {
                if (!enemyType.Key || enemyType.Value <= 0) continue;
                
                SpawnEnemy(enemyType.Key);
                totalEnemiesSpawned++;
            }
        }
        
        enemyCount = totalEnemiesSpawned;
    }
    
    
    
    private void SpawnEnemy(ChickenController enemyPrefab)
    {
        if (!enemyPrefab) return;

        var enemyInstance = Instantiate(enemyPrefab, enemyHolder);
        enemyInstance.transform.localPosition = Vector3.zero;
        enemyInstance.transform.localRotation = Quaternion.identity;
        enemyInstance.OnDeath += UpdateEnemyCount;
    }
    
    private void ClearEnemies()
    {
        foreach (Transform child in enemyHolder)
        {
            if (child.TryGetComponent<ChickenController>(out var enemy))
            {
                Destroy(child.gameObject);
            }
        }
        
        enemyCount = 0;
    }

    #endregion Enemy Spawning --------------------------------------------------------------------------------------
    

    

}
