using UnityEngine;

[System.Serializable]
public abstract class StageEvent
{
    [SerializeField] protected string eventDescription;
    [SerializeField] protected bool isActive;
    
    public string EventDescription => eventDescription;
    public bool IsActive => isActive;
    
    public abstract void Initialize(LevelManager levelManager);
    public abstract void Update(float deltaTime);
    public abstract void Cleanup();
    
    protected virtual void StartEvent()
    {
        isActive = true;
    }
    
    protected virtual void StopEvent()
    {
        isActive = false;
    }
}

[System.Serializable]
public class SpawnEnemyEvent : StageEvent
{
    [SerializeField] private ChickenStateController enemyPrefab;
    [SerializeField] private int maxActiveEnemies = 5;
    [SerializeField] private float spawnInterval = 2f;
    
    private LevelManager _levelManager;
    private EnemySpawner _enemySpawner;
    private float _spawnTimer;
    
    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _enemySpawner = levelManager.EnemySpawner;

        _spawnTimer = 0f;
        StartEvent();
    }
    
    public override void Update(float deltaTime)
    {
        if (!isActive || !_enemySpawner || !enemyPrefab) return;
        
        _spawnTimer += deltaTime;
        
        if (_spawnTimer >= spawnInterval && _enemySpawner.ActiveEnemyCount < maxActiveEnemies)
        {
            _enemySpawner.SpawnEnemy(enemyPrefab);
            _spawnTimer = 0f;
        }
    }
    
    public override void Cleanup()
    {
        StopEvent();
        _enemySpawner = null;
        _levelManager = null;
    }
}

[System.Serializable]
public class SpawnResourceEvent : StageEvent
{
    [SerializeField] private Resource resourcePrefab;
    [SerializeField] private int maxActiveResources = 3;
    [SerializeField] private float spawnInterval = 5f;
    
    private LevelManager _levelManager;
    private ResourceManager _resourceManager;
    private float _spawnTimer;
    
    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _resourceManager = levelManager.ResourceManager;
        
        _spawnTimer = 0f;
        StartEvent();
    }
    
    public override void Update(float deltaTime)
    {
        if (!isActive || !resourcePrefab || !_resourceManager) return;
        
        _spawnTimer += deltaTime;
        
        if (_spawnTimer >= spawnInterval && GetActiveResourceCount() < maxActiveResources)
        {
            SpawnResource();
            _spawnTimer = 0f;
        }
    }
    
    public override void Cleanup()
    {
        StopEvent();
        _resourceManager = null;
        _levelManager = null;
    }
    
    private void SpawnResource()
    {
        if (!_resourceManager) return;
    
        _resourceManager.SpawnResource(resourcePrefab);
    }

    private int GetActiveResourceCount()
    {
        return _resourceManager ? _resourceManager.ActiveResourceCount : 0;
    }
}
