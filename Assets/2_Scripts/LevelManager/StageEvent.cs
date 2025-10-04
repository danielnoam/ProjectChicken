using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using UnityEngine;

[System.Serializable]
public abstract class StageEvent
{
    [SerializeField] protected bool isActive;
    
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
    [SerializeField, Min(0)] private float initialDelay = 1;
    [SerializeField, Min(1)] private int maxActiveEnemies = 5;
    [SerializeField, MinMaxRange(1f, 30f)] private RangedFloat spawnIntervalRange = new RangedFloat(2, 2);
    
    private LevelManager _levelManager;
    private EnemySpawner _enemySpawner;
    private float _spawnTimer;
    private float _spawnInterval;
    private bool _hasStarted;
    
    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _enemySpawner = levelManager.EnemySpawner;

        _spawnTimer = -initialDelay;
        _spawnInterval = spawnIntervalRange.RandomValue;
        _hasStarted = initialDelay <= 0f;
        StartEvent();
    }
    
    public override void Update(float deltaTime)
    {
        if (!isActive || !_enemySpawner || !enemyPrefab) return;
        
        _spawnTimer += deltaTime;
        
        if (!_hasStarted)
        {
            if (_spawnTimer >= 0f)
            {
                _hasStarted = true;
                _spawnTimer = 0f;
            }
            return;
        }
        
        if (_spawnTimer >= _spawnInterval && _enemySpawner.ActiveEnemyCount < maxActiveEnemies)
        {
            _enemySpawner.SpawnEnemy(enemyPrefab);
            _spawnTimer = 0f;
            _spawnInterval = spawnIntervalRange.RandomValue;
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
    [SerializeField, Min(0)] private float initialDelay = 1;
    [SerializeField, Min(1)] private int maxActiveResources = 3;
    [SerializeField, MinMaxRange(1f, 30f)] private RangedFloat spawnIntervalRange = new RangedFloat(5, 5);
    
    private LevelManager _levelManager;
    private ResourceManager _resourceManager;
    private float _spawnTimer;
    private float _spawnInterval;
    private bool _hasStarted;
    
    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _resourceManager = levelManager.ResourceManager;
        
        _spawnTimer = -initialDelay;
        _spawnInterval = spawnIntervalRange.RandomValue;
        _hasStarted = initialDelay <= 0f;
        StartEvent();
    }
    
    public override void Update(float deltaTime)
    {
        if (!isActive || !resourcePrefab || !_resourceManager) return;
        
        _spawnTimer += deltaTime;
        
        if (!_hasStarted)
        {
            if (_spawnTimer >= 0f)
            {
                _hasStarted = true;
                _spawnTimer = 0f;
            }
            return;
        }
        
        if (_spawnTimer >= _spawnInterval && GetActiveResourceCount() < maxActiveResources)
        {
            SpawnResource();
            _spawnTimer = 0f;
            _spawnInterval = spawnIntervalRange.RandomValue;
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


[System.Serializable]
public class RadioMessageSequenceEvent : StageEvent
{
    [SerializeField, Min(0)] private float initialDelay;
    [SerializeField] private bool shuffle;
    [SerializeField] private bool loop;
    [SerializeField] private SORadioMessage[] messages;
    
    private LevelManager _levelManager;
    private RadioManager _radioManager;
    private List<SORadioMessage> _messageQueue;
    private float _timer;
    private int _currentMessageIndex;
    private bool _hasStarted;
    private SORadioMessage _lastSentMessage;

    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _radioManager = levelManager.RadioManager;
        
        if (messages == null || messages.Length == 0)
        {
            Debug.LogWarning("RadioMessageSequenceEvent: No messages assigned!");
            return;
        }
        
        _messageQueue = new List<SORadioMessage>(messages.Where(msg => msg != null));
        
        if (shuffle)
        {
            ShuffleMessages();
        }
        
        _timer = initialDelay;
        _currentMessageIndex = 0;
        _hasStarted = false;
        _lastSentMessage = null;
        
        StartEvent();
    }

    public override void Update(float deltaTime)
    {
        if (!isActive || !_radioManager || _messageQueue == null || _messageQueue.Count == 0) return;
        
        if (!_hasStarted)
        {
            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                _hasStarted = true;
                for (int i = 0; i < _messageQueue.Count; i++)
                {
                    SendNextMessage();
                }
            }
        }
    }

    public override void Cleanup()
    {
        
        StopEvent();
        _radioManager = null;
        _levelManager = null;
        _messageQueue?.Clear();
        _messageQueue = null;
        _lastSentMessage = null;
    }
    

    private void SendNextMessage()
    {
        if (_messageQueue == null || _messageQueue.Count == 0) return;

        SORadioMessage messageToSend = _messageQueue[_currentMessageIndex];
        
        if (messageToSend)
        {
            _radioManager.AddMessage(messageToSend);
            _lastSentMessage = messageToSend;
        }
        
        _currentMessageIndex++;
        
        if (_currentMessageIndex >= _messageQueue.Count)
        {
            if (loop)
            {
                _currentMessageIndex = 0;
                
                if (shuffle)
                {
                    ShuffleMessages();
                }
            }
            else
            {
                StopEvent();
            }
        }
    }

    private void ShuffleMessages()
    {
        if (_messageQueue is not { Count: > 1 }) return;
        
        for (int i = _messageQueue.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (_messageQueue[i], _messageQueue[randomIndex]) = (_messageQueue[randomIndex], _messageQueue[i]);
        }
    }
}


[System.Serializable]
public class SpawnObstacleEvent : StageEvent
{
    [SerializeField, Min(0)] private float initialDelay = 1;
    [SerializeField, Min(1)] private int maxActiveObstacles = 3;
    [SerializeField, MinMaxRange(1,10)] private RangedInt obstacleCount = new RangedInt(1, 3);
    [SerializeField, MinMaxRange(2f, 30f)] private RangedFloat spawnIntervalRange = new RangedFloat(2,4);
    
    private LevelManager _levelManager;
    private ObstacleManager _obstacleManager;
    private float _spawnTimer;
    private float _spawnInterval;
    private bool _hasStarted;
    
    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _obstacleManager = levelManager.ObstacleManager;
        
        _spawnInterval = spawnIntervalRange.RandomValue;
        _spawnTimer = -initialDelay;
        _hasStarted = initialDelay <= 0f;
        StartEvent();
    }
    
    public override void Update(float deltaTime)
    {
        if (!isActive || !_obstacleManager) return;
        
        _spawnTimer += deltaTime;
        
        if (!_hasStarted)
        {
            if (_spawnTimer >= 0f)
            {
                _hasStarted = true;
                _spawnTimer = 0f;
                SpawnObstacle();
            }
            return;
        }
        
        if (_spawnTimer >= _spawnInterval && GetActiveObstacleCount() < maxActiveObstacles)
        {
            SpawnObstacle();
            _spawnTimer = 0f;
            _spawnInterval = spawnIntervalRange.RandomValue;
        }
    }
    
    public override void Cleanup()
    {
        StopEvent();
        _obstacleManager = null;
        _levelManager = null;
    }
    
    private void SpawnObstacle()
    {
        if (!_obstacleManager) return;

        var count = obstacleCount.RandomValue;
        _obstacleManager.SpawnObstacleWave(count);
        
    }

    private int GetActiveObstacleCount()
    {
        return _obstacleManager ? _obstacleManager.ActiveNormalObstacleCount : 0;
    }
}


[System.Serializable]
public class SpawnPassthroughObstacleEvent : StageEvent
{
    [SerializeField, Min(0)] private float initialDelay = 1;
    [SerializeField, Min(1)] private int maxActiveObstacles = 1;
    [SerializeField, MinMaxRange(5f, 30f)] private RangedFloat spawnIntervalRange = new RangedFloat(0, 30);

    
    private LevelManager _levelManager;
    private ObstacleManager _obstacleManager;
    private float _spawnTimer;
    private float _spawnInterval;
    private bool _hasStarted;
    
    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _obstacleManager = levelManager.ObstacleManager;
        _spawnTimer = -initialDelay;
        _spawnInterval = spawnIntervalRange.RandomValue;
        _hasStarted = initialDelay <= 0f;
        StartEvent();
    }
    
    public override void Update(float deltaTime)
    {
        if (!isActive || !_obstacleManager) return;
        
        _spawnTimer += deltaTime;
        
        if (!_hasStarted)
        {
            if (_spawnTimer >= 0f)
            {
                _hasStarted = true;
                _spawnTimer = 0f;
                SpawnObstacle();
            }
            return;
        }
        
        if (_spawnTimer >= _spawnInterval && GetActiveObstacleCount() < maxActiveObstacles)
        {
            SpawnObstacle();
            _spawnTimer = 0f;
            _spawnInterval = spawnIntervalRange.RandomValue;
        }
    }
    
    public override void Cleanup()
    {
        StopEvent();
        _obstacleManager = null;
        _levelManager = null;
    }
    
    private void SpawnObstacle()
    {
        if (!_obstacleManager) return;
        
        _obstacleManager.SpawnRandomPassthroughObstacle();
        
    }
    
    private int GetActiveObstacleCount()
    {
        return _obstacleManager ? _obstacleManager.ActivePassthroughObstacleCount : 0;
    }
}