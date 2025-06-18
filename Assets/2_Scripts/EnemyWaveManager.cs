using System;
using KBCore.Refs;
using UnityEngine;





public class EnemyWaveManager : MonoBehaviour
{
    public static EnemyWaveManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField, Scene(Flag.Editable)] private LevelManager levelManager;
    [SerializeField] private Transform enemyHolder;


    private SOLevelStage _currentStage;
    private int _enemyCount;
    public event Action<int> OnEnemyWaveCleared;
    public event Action<int> OnEnemyDeath;

    private void OnValidate()
    {
        this.ValidateRefs();
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
    }

    private void OnDisable()
    {
        levelManager.OnStageChanged -= OnStageChanged;
        foreach (Transform child in enemyHolder)
        {
            if (child.TryGetComponent<ChickenController>(out var enemy))
            {
                enemy.OnDeath -= OnEnemyDeath;
                enemy.OnDeath -= UpdateEnemyCount;
            }
        }
    }




    #region Events --------------------------------------------------------------------------------------
    
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        _currentStage = stage;
        
        switch (stage.StageType)
        {
            case StageType.EnemyWave:
                SpawnEnemyWave(stage);
                _currentStage = stage;
                break;
            case StageType.Checkpoint:
                ClearEnemies();
                break;
        }
    }
    
    
    private void UpdateEnemyCount(int enemyScore)
    {
        _enemyCount--;
        
        if (_enemyCount <= 0)
        {
            OnEnemyWaveCleared?.Invoke(_currentStage.WaveScore);
        }
    }
    

    #endregion Events --------------------------------------------------------------------------------------
    
    

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
        
        _enemyCount = totalEnemiesSpawned;
    }
    
    
    
    private void SpawnEnemy(ChickenController enemyPrefab)
    {
        if (!enemyPrefab) return;

        var enemyInstance = Instantiate(enemyPrefab, enemyHolder);
        enemyInstance.transform.localPosition = Vector3.zero;
        enemyInstance.transform.localRotation = Quaternion.identity;
        enemyInstance.OnDeath += OnEnemyDeath;
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
        
        _enemyCount = 0;
    }

    #endregion Enemy Spawning --------------------------------------------------------------------------------------
    

    

}
