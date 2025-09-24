using System;
using System.Collections;
using System.Collections.Generic;
using Core.Attributes;
using DNExtensions;
using DNExtensions.VFXManager;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VInspector;



[SelectionBase]
[RequireComponent(typeof(LevelManagerInput))]
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("General")]
    [SerializeField, Min(0)] private Vector2 enemyBoundarySize = new Vector2(45f,30f);
    [SerializeField, Min(0)] private Vector2 playerBoundarySize = new Vector2(40f,25f);
    [SerializeField] private Vector3 playerBoundaryOffset;
    [SerializeField] private Vector3 enemyBoundaryOffset;
    
    [Header("Level")]
    [SerializeField, CreateEditableAsset] private SOLevel level;
    [SerializeField] private bool debugLog;
    [SerializeField, VInspector.ReadOnly] private SOLevelStage currentStage;
    [SerializeField, VInspector.ReadOnly] private int currentStageIndex;
    [SerializeField, VInspector.ReadOnly] private int enemiesLeft;
    

    [Header("References")]
    [SerializeField] private SOPlayerStats playerStats;
    [SerializeField] private SceneField mainMenuScene;
    [SerializeField, Scene(Flag.EditableAnywhere)] private OutroScreen outroScreen;
    [SerializeField, Scene(Flag.EditableAnywhere)] private UpgradeStore upgradeStore;
    [SerializeField, Scene(Flag.EditableAnywhere)] private EnemySpawner enemySpawner;
    [SerializeField, Self(Flag.EditableAnywhere)] private FormationBoundaryManager boundaryManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private ResourceManager resourceManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private ObstacleManager obstacleManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RadioManager radioManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private TaskVisualizer taskVisualizer;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;
    [SerializeField] private HitFXSettings shipWarping = new HitFXSettings();
    [SerializeField,Self,HideInInspector] private LevelManagerInput input;
    


    private bool _isGamePaused;
    private int _currentScore;
    private SOLevelStage[] _levelStages;
    private StageTask[] _currentStageTasks;
    private StageEvent[] _currentStageEvents;
    private int _completedTaskCount;
    private float _currentPathSpeed;
    private bool _settingStageFlag;
    private SavePointData _currentSavePoint;
    private SavePointData _startSavePoint;
    private Coroutine _stageChangeCoroutine;


    public LevelManagerInput LevelManagerInput => input;
    public Vector3 PlayerPosition => Vector3.forward + playerBoundaryOffset;
    public Vector3 EnemyPosition => Vector3.forward + enemyBoundaryOffset;
    public Vector2 PlayerBoundarySize => playerBoundarySize;
    public Vector2 EnemyBoundarySize => enemyBoundarySize;
    public RailPlayer Player => player;
    public EnemySpawner EnemySpawner => enemySpawner;
    public ResourceManager ResourceManager => resourceManager;
    public ObstacleManager ObstacleManager => obstacleManager;
    public RadioManager RadioManager => radioManager;
    public TaskVisualizer TaskVisualizer => taskVisualizer;
    public int CurrentScore => _currentScore;
    public int EnemiesLeft => enemiesLeft;
    public bool IsGamePaused => _isGamePaused;
    public SOLevelStage CurrentStage => currentStage;


    public event Action<SOLevel> OnLevelSet; 
    public event Action<SOLevelStage> OnStageChanged;
    public event Action<int> OnScoreChanged;
    public event Action<SavePointData> OnRestartedFromSavePoint;
    public event Action<RunProgressData> OnRunProgressLoaded;
    public event Action<bool> OnPause;
    
    
    public static float WorldSpeed = 1f;


    private void OnValidate()
    {
        
        if (!player)
        {
            player = FindFirstObjectByType<RailPlayer>();
        }
        
        if (!enemySpawner)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
        
        if (!upgradeStore)
        {
            upgradeStore = FindFirstObjectByType<UpgradeStore>();
        }
        
        if (!outroScreen)
        {
            outroScreen = FindFirstObjectByType<OutroScreen>();
        }
        
        if (!boundaryManager)
        {
            boundaryManager = FindFirstObjectByType<FormationBoundaryManager>();
        }
        
        if (!resourceManager)
        {
            resourceManager = FindFirstObjectByType<ResourceManager>();
        }

        if (!radioManager)
        {
            radioManager = FindFirstObjectByType<RadioManager>();
        }
        
        if (!obstacleManager)
        {
            obstacleManager = FindFirstObjectByType<ObstacleManager>();
        }
        
        
        if (!taskVisualizer)
        {
            taskVisualizer = FindFirstObjectByType<TaskVisualizer>();
        }
        

        this.ValidateRefs();
        
        if (boundaryManager) boundaryManager.UpdateBoundary();
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
        }
        
        PrimeTweenConfig.warnTweenOnDisabledTarget = false;
        PrimeTweenConfig.warnZeroDuration = false;
    }
    
    private void OnEnable()
    {
        if (enemySpawner)
        {
            enemySpawner.OnEnemyWaveSpawned += OnEnemySpawned;
            enemySpawner.OnEnemyWaveCleared += OnEnemiesCleared;
            enemySpawner.OnEnemyDeath += OnEnemyDeath;
        }

        if (player)
        {
            player.ResourceCollector.OnResourceCollected += OnPlayerCollectedResource;
            player.Health.OnDeath += OnPlayerDeath;
        }

        if (upgradeStore)
        {
            upgradeStore.OnStoreClosed += UpgradeStoreClosed;
        }
        
        input.OnPauseActionEvent += OnPauseAction;
    }
    

    private void OnDisable()
    {
        if (enemySpawner)
        {
            enemySpawner.OnEnemyWaveSpawned -= OnEnemySpawned;
            enemySpawner.OnEnemyWaveCleared -= OnEnemiesCleared;
            enemySpawner.OnEnemyDeath -= OnEnemyDeath;
        }
        
        if (player)
        {
            player.ResourceCollector.OnResourceCollected -= OnPlayerCollectedResource;
            player.Health.OnDeath -= OnPlayerDeath;
        }
        if (upgradeStore)
        {
            upgradeStore.OnStoreClosed -= UpgradeStoreClosed;
        }
        
        
        input.OnPauseActionEvent -= OnPauseAction;
    }


    private void OnDestroy()
    {
        CleanupCurrentTasks();
    }


    private void Start()
    {
        StartLevel();
    }

    private void Update()
    {
        UpdateStageEvents();

        if (Input.GetKeyDown(KeyCode.F12))
        {
            SetNextStage();
        }
    }
    

    private void OnEnemiesCleared(int scoreWorth)
    {
        if (!currentStage || currentStage.StageType != StageType.EnemyWave || _settingStageFlag) return;
        
        AddScore(scoreWorth);
        
        SetNextStage(currentStage.DelayBeforeNextStage);
    }
    
    private void OnEnemySpawned()
    {
        enemiesLeft = enemySpawner.ActiveEnemyCount;
    }

    private void OnEnemyDeath(ChickenStateController enemy)
    {
        enemiesLeft = enemySpawner.ActiveEnemyCount;
        AddScore(enemy.ScoreWorth);
    }
    
    private void UpgradeStoreClosed()
    {
        if (!currentStage || currentStage.StageType != StageType.Store) return;
        
        SetNextStage();
    }

    private void OnPlayerDeath()
    {
        StartCoroutine(RestartFromSavePointRoutine());
    }
    
    
    private void OnPlayerCollectedResource(Resource resource)
    {
        if (!resource) return;
        
        int score = resource.ScoreWorth;
        AddScore(score);
    }

    private void OnPauseAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SetPausedState(!_isGamePaused);
        }
    }
    


    public void SetPausedState(bool paused)
    {
        _isGamePaused = paused;
        Time.timeScale = _isGamePaused ? 0 : 1;
        OnPause?.Invoke(_isGamePaused);
    }

    
    #region Stage Management

    [Button]
    private void StartLevel()
    {
        if (!level)
        {
            if (debugLog) Debug.LogError("No level defined!");
            return;
        }
        
        _levelStages = level.LevelStages;
        _isGamePaused = false;
        WorldSpeed = 1f;
        
        if (_levelStages == null || _levelStages.Length == 0)
        {
            if (debugLog) Debug.LogError("No level stages defined!");
            return;
        }
        
        OnLevelSet?.Invoke(level);
        

        var runProgress = SaveManager.GetRunProgressData();
        OnRunProgressLoaded?.Invoke(runProgress);
        
        _startSavePoint = new SavePointData(currentStageIndex, 0, playerStats.BaseHealth, 0,new Dictionary<SOUpgradeBase, int>(), player.WeaponSystem.ActiveWeaponInstance?.weaponData);
        _currentSavePoint = null;

        
        ResetScore();
        SetStage(0);
    }
    
    [Button]
    private void SetNextStage(float delay = 0)
    {
        if (_settingStageFlag) return;
        
        
        if (_stageChangeCoroutine != null)
        {
            StopCoroutine(_stageChangeCoroutine);
            _stageChangeCoroutine = null;
        }
        
        int nextStageIndex = currentStageIndex + 1;
        if (nextStageIndex < _levelStages.Length)
        {
            
            _settingStageFlag = true;
            
            if (delay <= 0)
            {
                SetStage(nextStageIndex);
            }
            else
            {
                if (_stageChangeCoroutine != null)
                {
                    StopCoroutine(_stageChangeCoroutine);
                }
        
                _stageChangeCoroutine = StartCoroutine(ChangeStageAfterDelay(nextStageIndex, delay));
            }

        }
        else
        {
            if (debugLog) Debug.Log("No more stages available");
        }
    }
    
    private void SetStage(int newStageIndex)
    {
        if (newStageIndex < 0 || newStageIndex >= _levelStages.Length) return;
        

        SOLevelStage newStage = _levelStages[newStageIndex];

        if (!newStage)
        {
            if (debugLog) Debug.LogError("No stage found at index: " + newStageIndex);
            SetNextStage();
            return;
        }
        
        if (debugLog) Debug.Log("Set stage to: " + newStage.name);
        
        CleanupCurrentTasks();
        CleanupCurrentEvents();
        _settingStageFlag = false;
        currentStageIndex = newStageIndex;
        currentStage = newStage;
        
        Tween.Custom(startValue: WorldSpeed, endValue: newStage.WorldSpeed, duration: 0.5f, onValueChange:(value) => WorldSpeed = value);
        if (currentStage.IsCheckpoint) SaveLevelProgress();
        

        
        OnStageChanged?.Invoke(currentStage);
        
        InitializeStageEvents();


        if (currentStage.IsTimeBasedStage)
        {
            if (currentStage.StageType == StageType.Outro)
            {
                
                if (currentStage.ShowOutroMenu)
                {
                    outroScreen.Show(currentStage.NextLevel.IsSceneValid());
                }
                else
                {
                    ReturnToMainMenu(newStage.StageDuration);
                }
            }
            else
            {
                if (currentStage.StageType == StageType.Intro)
                {
                    VFXManager.Instance?.PlayVFX(level.IntroVFXSequence);
                    FullScreenHitFXController.Instance?.TransitionFrom(shipWarping, currentStage.StageDuration/2f);
                }
                SetNextStage(currentStage.StageDuration);
            }
        }
        else if (currentStage.StageType == StageType.Task)
        {
            InitializeTaskStage();
        }

        
    }
    

    private IEnumerator ChangeStageAfterDelay(int newStateIndex, float delay)
    {

        if (debugLog) Debug.Log("Setting stage: " + _levelStages[newStateIndex].name + ", In " + delay);

        yield return new WaitForSeconds(delay);

        SetStage(newStateIndex);
    }



    public void ReturnToMainMenu(float delay = 2)
    {
        SaveManager.UpdateLevelProgress(SceneManager.GetActiveScene().path, _currentScore);
        SaveManager.ResetRunProgressData();
        if (delay > 0)
        {
            StartCoroutine(DelayReturnToMainMenu(delay));
        }
        else
        {
            if (level.OutroVFXSequence)
            {
                FullScreenHitFXController.Instance?.TransitionTo(shipWarping);
            }
            TransitionManager.TransitionToScene(mainMenuScene, level.OutroVFXSequence);
        }
    }


    public void LoadNextLevel()
    {
        if (!level || !currentStage || currentStage.NextLevel == null) return;
        
        var runProgress = new RunProgressData(player.Health.CurrentHealth, player.ResourceCollector.CurrentCurrency, player.Upgrades, player.WeaponSystem.ActiveWeaponInstance?.weaponData);
        SaveManager.UpdateRunProgress(runProgress);
        TransitionManager.TransitionToScene(currentStage.NextLevel, level.OutroVFXSequence);

        if (level.OutroVFXSequence)
        {
            FullScreenHitFXController.Instance?.TransitionTo(shipWarping);
        }

    }
    
    private IEnumerator DelayReturnToMainMenu(float delay)
    {
        yield return new WaitForSeconds(delay);

        TransitionManager.TransitionToScene(mainMenuScene, level.OutroVFXSequence);
        if (level.OutroVFXSequence)
        {
            FullScreenHitFXController.Instance?.TransitionTo(shipWarping);
        }
    }
    
    


    #endregion


    #region Tasks

    
    private void InitializeTaskStage()
    {
        if (currentStage.Tasks == null || currentStage.Tasks.Length == 0)
        {
            if (debugLog) Debug.LogWarning("Task stage has no tasks defined!");
            SetNextStage();
            return;
        }
    
        _currentStageTasks = currentStage.Tasks;
        _completedTaskCount = 0;
    
        foreach (var task in _currentStageTasks)
        {
            task.OnTaskCompleted += OnTaskCompleted;
            task.Initialize(this);
        }
    
        if (debugLog) Debug.Log($"Initialized {_currentStageTasks.Length} tasks for stage: {currentStage.name}");
    }

    private void OnTaskCompleted(StageTask completedTask)
    {
        _completedTaskCount++;
    
        if (debugLog) Debug.Log($"Task completed: ({_completedTaskCount}/{_currentStageTasks.Length})");
        
        
        bool shouldAdvance = currentStage.RequireAllTasks ? _completedTaskCount >= _currentStageTasks.Length : true;
    
        if (shouldAdvance)
        {
            if (debugLog) Debug.Log("All required tasks completed, advancing stage");
            SetNextStage(currentStage.DelayBeforeNextStage);
        }
    }

    private void CleanupCurrentTasks()
    {
        if (_currentStageTasks == null) return;
        
        
        foreach (var task in _currentStageTasks)
        {
            task.OnTaskCompleted -= OnTaskCompleted;
            task.Cleanup();
        }
        _currentStageTasks = null;
    }

    #endregion

    
    #region Events

    
    private void InitializeStageEvents()
    {
        if (currentStage.Events == null || currentStage.Events.Length == 0) return;
    
        _currentStageEvents = currentStage.Events;
    
        foreach (var stageEvent in _currentStageEvents)
        {
            stageEvent?.Initialize(this);
        }
    
        if (debugLog) Debug.Log($"Initialized {_currentStageEvents.Length} events for stage: {currentStage.name}");
    }

    private void UpdateStageEvents()
    {
        if (_currentStageEvents == null) return;
    
        foreach (var stageEvent in _currentStageEvents)
        {
            if (stageEvent == null || !stageEvent.IsActive) continue;
            stageEvent.Update(Time.deltaTime);
        }
    }

    private void CleanupCurrentEvents()
    {
        if (_currentStageEvents == null) return;
    
        foreach (var stageEvent in _currentStageEvents)
        {
            stageEvent?.Cleanup();
        }
        _currentStageEvents = null;
    }
    

    #endregion
    
    
    #region Save/Load

    private IEnumerator RestartFromSavePointRoutine()
    {
        yield return new WaitForSeconds(5f);
    
        if (_currentSavePoint == null)
        {
            StartLevel();
            OnRestartedFromSavePoint?.Invoke(_startSavePoint);
        }
        else
        {
            SetStage(_currentSavePoint.StageIndex);
            _currentScore = _currentSavePoint.Score;
            OnScoreChanged?.Invoke(_currentScore);
            OnRestartedFromSavePoint?.Invoke(_currentSavePoint);
        }
    }

    private void SaveLevelProgress()
    {
        if (!currentStage || _currentSavePoint != null && _currentSavePoint.StageIndex == currentStageIndex) return;
        
        var newSavePoint = new SavePointData(
            currentStageIndex,
            _currentScore, 
            player.Health.CurrentHealth,
            player.ResourceCollector.CurrentCurrency, 
            player.Upgrades,
            player.WeaponSystem.ActiveWeaponInstance?.weaponData
        );
            
        _currentSavePoint = newSavePoint;
    }

    #endregion
    
    
    #region Score Management

    private void AddScore(int score)
    {
        _currentScore += score;
        OnScoreChanged?.Invoke(_currentScore);
    }

    private void ResetScore()
    {
        _currentScore = 0;
        OnScoreChanged?.Invoke(_currentScore);
    }
    

    #endregion
}