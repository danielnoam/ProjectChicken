using KBCore.Refs;
using UnityEngine;
using VInspector;

public class ResourceManager : MonoBehaviour
{
    
    [Header("References")] 
    [SerializeField] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private EnemySpawner enemySpawner;
    [SerializeField] private Transform resourceHolder;
    [SerializeField] private SOLootTable debugTable;
    
    private void OnValidate()
    {
        if (!enemySpawner)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
        this.ValidateRefs();
    }
    
    
    private void OnEnable()
    {
        if (enemySpawner)
        {
            enemySpawner.OnEnemyDeath += OnEnemyDeath;
        }
    }

    private void OnDisable()
    {
        if (enemySpawner)
        {
            enemySpawner.OnEnemyDeath -= OnEnemyDeath;
        }
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
            return newResource;
        }
        else
        {
            Resource newResource = Instantiate(resource, position, Quaternion.identity);
            return newResource;
        }
    }

    [Button]
    private void SpawnRandomResourceOnPath()
    {
        if (!levelManager) return;
        SpawnResource(debugTable.RandomResource, levelManager.EnemyPosition, transform);
        
    }
}