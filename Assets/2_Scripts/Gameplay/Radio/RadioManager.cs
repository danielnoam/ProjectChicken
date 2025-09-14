using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class RadioManager : MonoBehaviour
{
    public static RadioManager Instance { get; private set; }
    
    
    [Header("Messages")]
    [SerializeField] private SORadioMessage[] playerDamagedMessages;
    [SerializeField] private SORadioMessage[] playerDeathMessages;
    [SerializeField] private SORadioMessage[] enemyDeathMessages;
    [SerializeField] private SORadioMessage[] currencyPickUpMessages;
    [SerializeField, Range(0,1)] private float enemyDeathMessageChance = 0.05f;
    [SerializeField, Range(0,1)] private float currencyPickUpMessageChance = 0.05f;
    
    [Header("References")]
    [SerializeField] private RadioMessageUI radioMessageUI;
    [SerializeField, Self(Flag.Editable)] private AudioSource audioSource;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private RailPlayer player;
    [SerializeField] private SORadioMessage testMessage;

    private readonly List<SORadioMessage> _messages = new List<SORadioMessage>();
    private SORadioMessage _currentMessage;
    private bool _messagePlaying;
    private int _playerHealth = 100;
    private SOCharacter _currentSender;
    
    public event Action<SORadioMessage> OnMessageFinished;
    public event Action<SORadioMessage> OnMessageStarted;
    
    private void OnValidate()
    {
        if (!levelManager) levelManager = FindFirstObjectByType<LevelManager>();
        if (!player) player = FindFirstObjectByType<RailPlayer>();
        if (!enemySpawner) enemySpawner = FindFirstObjectByType<EnemySpawner>();
        this.ValidateRefs();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (player) _playerHealth = player.PlayerStats.BaseHealth;
    }

    private void OnEnable()
    {
        radioMessageUI.OnMessageHidden += OnMessageHidden;
        radioMessageUI.OnMessageCompleted += OnMessageCompleted;
        if (levelManager) levelManager.OnStageChanged += OnStageChanged;
        if (enemySpawner) enemySpawner.OnEnemyDeath += OnEnemyDeath;
        if (player)
        {
            player.ResourceCollector.OnResourceCollected += OnResourceCollected;
            player.Health.OnDeath += OnPlayerDeath;
            player.Health.OnHealthChanged += OnPlayerHealthChanged;
        }

    }
    
    private void OnDisable()
    {
        radioMessageUI.OnMessageHidden -= OnMessageHidden;
        radioMessageUI.OnMessageCompleted -= OnMessageCompleted;
        if (levelManager) levelManager.OnStageChanged -= OnStageChanged;
        if (enemySpawner) enemySpawner.OnEnemyDeath -= OnEnemyDeath;
        if (player)
        {
            player.ResourceCollector.OnResourceCollected -= OnResourceCollected;
            player.Health.OnDeath -= OnPlayerDeath;
            player.Health.OnHealthChanged -= OnPlayerHealthChanged;
        }

    }
    

    private void Update()
    {
        if (_messagePlaying || _messages.Count == 0) return;

        PlayNextMessage();
    }
    
    private void OnMessageHidden()
    {
        _messagePlaying = false;
        _currentSender = null;
        
        // Process next message in queue if available
        if (_messages.Count > 0)
        {
            // Add a small delay to ensure smooth transition
            Invoke(nameof(PlayNextMessage), 0.1f);
        }
    }
    
    private void OnMessageCompleted()
    {
        if (_currentMessage && _currentMessage.IsPersistent)
        {
            _messagePlaying = false;
            return;
        }

        if (_messages.Count == 0 || (_messages.Count > 0 && _messages[0].Sender != _currentSender))
        {
            _messagePlaying = false;
            _currentSender = null; 
            radioMessageUI.HideMessage();
        }
        else
        {
            _messagePlaying = false;
        }
        
        OnMessageFinished?.Invoke(_currentMessage);
    }

    
    private void OnResourceCollected(Resource resource)
    {
        if (currencyPickUpMessages.Length <= 0 || !resource || resource.ResourceType != ResourceType.Currency) return;

        if (Random.value > currencyPickUpMessageChance) return;
        
        var message = currencyPickUpMessages[Random.Range(0, currencyPickUpMessages.Length)];
        AddMessage(message);
    }
    
    private void OnPlayerHealthChanged(int health)
    {
        if (playerDamagedMessages.Length <= 0) return;

        if (health < _playerHealth && health > 0)
        {
            var message = playerDamagedMessages[Random.Range(0, playerDamagedMessages.Length)];
            AddMessage(message);
        }

        _playerHealth = health;
    }

    private void OnPlayerDeath()
    {
        if (playerDeathMessages.Length <= 0) return;
        
        var message = playerDeathMessages[Random.Range(0, playerDeathMessages.Length)];
        AddMessage(message);
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        if (stage.StageType == StageType.Intro)
        {
            ClearMessages();
        }
        
        AddMessage(stage.StartRadioMessage);
    }
    
    private void OnEnemyDeath(ChickenStateController enemy)
    {
        if (!enemy || enemyDeathMessages.Length <= 0) return;
        
        if (Random.value > enemyDeathMessageChance) return;
        var message = enemyDeathMessages[Random.Range(0, enemyDeathMessages.Length)];
        AddMessage(message);
    }
    

    public void AddMessage(SORadioMessage message)
    {
        if (!message) return;
        
        if (message.IsImportant)
        {
            if (_messagePlaying)
            {
                _currentMessage?.AudioEvent?.Stop(audioSource);
                _messages.Insert(0, _currentMessage);
                radioMessageUI.HideMessage();
            }
            PlayMessage(message);
        }
        else
        {
            _messages.Add(message);
        }
    }


    private void PlayNextMessage()
    {
        if (_messages.Count == 0) return;

        var message = _messages[0];
        _messages.RemoveAt(0);
        PlayMessage(message);
    }

    private void PlayMessage(SORadioMessage message)
    {
        bool isSameSender = _currentSender == message.Sender && _currentSender != null;
        bool shouldReplace = (_currentMessage && _currentMessage.IsPersistent && !isSameSender) || message.IsImportant;
        
        if (_messagePlaying && _currentMessage != null)
        {
            _currentMessage.AudioEvent?.Stop(audioSource);
        }

        if (isSameSender && !shouldReplace && _messagePlaying)
        {
            _currentMessage = message;
            message.AudioEvent?.Play(audioSource);
            radioMessageUI.UpdateMessageOnly(message);
        }
        else
        {
            if (_messagePlaying)
            {
                radioMessageUI.HideMessage();
            }

            _messagePlaying = true;
            _currentMessage = message;
            _currentSender = message.Sender;
            message.AudioEvent?.Play(audioSource);
            radioMessageUI.ShowMessage(message);
        }
        
        OnMessageStarted?.Invoke(message);
    }

    private void ClearMessages()
    {
        _messages.Clear();
        _currentMessage = null;
        _currentSender = null; 
        radioMessageUI.HideMessage();
    }
    
    [Button]
    private void AddTestMessage()
    {
        AddMessage(testMessage);
    }
    
    [Button]
    private void PlayTestMessage()
    {
        PlayMessage(testMessage);
    }
}