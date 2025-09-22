using System.Collections.Generic;
using System.Linq;
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


[System.Serializable]
public class RadioMessageSequenceEvent : StageEvent
{
    [Header("Messages")]
    [SerializeField] private SORadioMessage[] messages;
    
    [Header("Timing")]
    [SerializeField] private float initialDelay = 0f;
    
    [Header("Settings")]
    [SerializeField] private bool shuffle = false;
    [SerializeField] private bool loop = false;
    
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
        
        if (_radioManager)
        {
            _radioManager.OnMessageFinished += OnMessageFinished;
        }
        
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
        if (_radioManager)
        {
            _radioManager.OnMessageFinished -= OnMessageFinished;
        }
        
        StopEvent();
        _radioManager = null;
        _levelManager = null;
        _messageQueue?.Clear();
        _messageQueue = null;
        _lastSentMessage = null;
    }

    private void OnMessageFinished(SORadioMessage finishedMessage)
    {
        // if (finishedMessage == _lastSentMessage)
        // {
        //     SendNextMessage();
        // }
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
    [SerializeField] private Obstacle specificObstaclePrefab;
    [Tooltip("If true, a random obstacle from the ObstacleManager will be spawned. If false, the specificObstaclePrefab will be used.")]
    [SerializeField] private bool useRandomObstacle = true;
    [SerializeField] private int maxActiveObstacles = 3;
    [SerializeField] private float spawnInterval = 4f;
    
    private LevelManager _levelManager;
    private ObstacleManager _obstacleManager;
    private float _spawnTimer;
    
    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _obstacleManager = levelManager.ObstacleManager;
        
        _spawnTimer = 0f;
        StartEvent();
    }
    
    public override void Update(float deltaTime)
    {
        if (!isActive || !_obstacleManager) return;
        
        // If using specific obstacle, check if it's assigned
        if (!useRandomObstacle && !specificObstaclePrefab) return;
        
        _spawnTimer += deltaTime;
        
        if (_spawnTimer >= spawnInterval && GetActiveObstacleCount() < maxActiveObstacles)
        {
            SpawnObstacle();
            _spawnTimer = 0f;
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
        
        if (useRandomObstacle)
        {
            _obstacleManager.SpawnRandomObstacle();
        }
        else if (specificObstaclePrefab)
        {
            _obstacleManager.SpawnSpecificObstacle(specificObstaclePrefab);
        }
    }

    private int GetActiveObstacleCount()
    {
        return _obstacleManager ? _obstacleManager.ActiveObstacleCount : 0;
    }
}