using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using VInspector;

public class ResourceManager : MonoBehaviour
{
    
    [Header("References")] 
    [SerializeField] private Transform resourceHolder;
    [SerializeField] private SOLootTable debugTable;
    [SerializeField] private LevelManager levelManager;
    [SerializeField, Scene(Flag.Optional)] private EnemySpawner enemySpawner;
    [SerializeField, Scene(Flag.Optional)] private RailPlayer player;


    private readonly List<Resource> _resources = new List<Resource>();
    
    private void OnValidate()
    {
        if (!enemySpawner)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
        
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
        if (enemySpawner)
        {
            enemySpawner.OnEnemyDeath += OnEnemyDeath;
        }


        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
        }

        if (player)
        {
            player.OnDeath += OnPlayerDeath;
        }
    }

    private void OnDisable()
    {
        if (enemySpawner)
        {
            enemySpawner.OnEnemyDeath -= OnEnemyDeath;
        }
        
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }
        
        if (player)
        {
            player.OnDeath -= OnPlayerDeath;
        }
    }

    private void OnPlayerDeath()
    {
        RemoveAllResources();
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        if (stage.StageType == StageType.Store) RemoveAllResources();
    }

    private void OnEnemyDeath(ChickenController enemy)
    {
        if (!enemy  || !enemy.LootTable) return;

        SpawnResource(enemy.LootTable.RandomResource, enemy.transform.position, resourceHolder);
    }



    private Resource SpawnResource(Resource resource, Vector3 position, Transform parent)
    {
        if (!resource) return null;

        if (parent)
        {
            Resource newResource = Instantiate(resource, position, Quaternion.identity, parent);
            _resources.Add(newResource);
            newResource.OnDestroyEvent += OnResourceDestroyed;
            return newResource;
        }
        else
        {
            Resource newResource = Instantiate(resource, position, Quaternion.identity);
            _resources.Add(newResource);
            newResource.OnDestroyEvent += OnResourceDestroyed;
            return newResource;
        }
        

    }

    private void OnResourceDestroyed(Resource resource)
    {
        _resources.Remove(resource);
    }

    [Button]
    private void RemoveAllResources()
    {
        if (_resources.Count == 0) return;

        foreach (var resource in _resources)
        {
            resource.ForceDespawn();
        }
    }


    [Button]
    private void SpawnRandomResource()
    {
        if (!levelManager) return;
        SpawnResource(debugTable.RandomResource, levelManager.EnemyPosition, transform);
        
    }
}