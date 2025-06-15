using System;
using KBCore.Refs;
using UnityEngine;





public class EnemyWaveSpawnerTest : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField, Scene(Flag.Editable)] private LevelManager levelManager;
    [SerializeField] private Transform enemyHolder;


    private int enemyCount;
    public event Action OnEnemyWaveCleared; 

    private void OnValidate()
    {
        this.ValidateRefs();
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
            }
        }
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;


        if (stage.StageType == StageType.EnemyWave)
        {
            SpawnEnemyWave(stage);
        } 
    }
    
    
    private void OnEnemyDeath()
    {
        enemyCount--;
        
        if (enemyCount <= 0)
        {
            OnEnemyWaveCleared?.Invoke();
        }
    }



    #region Enemy Spawning --------------------------------------------------------------------------------------

    private void SpawnEnemyWave(SOLevelStage stage)
    {
        if (stage.EnemyWave.Count == 0) return;

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
        enemyInstance.OnDeath += OnEnemyDeath;
    }

    #endregion Enemy Spawning --------------------------------------------------------------------------------------
    

    

}
