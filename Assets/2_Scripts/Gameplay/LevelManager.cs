using System;
using System.Collections;
using System.Collections.Generic;
using Core.Attributes;
using DNExtensions.VFXManager;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;



[SelectionBase]
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
        
    [Header("Level")]
    [SerializeField, CreateEditableAsset] private SOLevel level;
    
    [Header("Debug")]
    [SerializeField] private bool debugLog;
    [SerializeField, ReadOnly] private SOLevelStage currentStage;
    [SerializeField, ReadOnly] private int currentStageIndex;
    [SerializeField, ReadOnly] private int enemiesLeft;
    [SerializeField, ReadOnly] public Vector3 playerPosition;
    [SerializeField, ReadOnly] public Vector3 enemyPosition;
    [SerializeField, ReadOnly] private SOLevelStage[] levelStages;
    
    [Header("Fullscreen FX")]
    [SerializeField] private HitFXSettings shipWarping = new HitFXSettings();
    
    [Header("References")]
    [SerializeField] private SOGameSettings gameSettings;
    [SerializeField, Scene(Flag.EditableAnywhere)] private OutroScreen outroScreen;
    [SerializeField, Scene(Flag.EditableAnywhere)] private UpgradeStore upgradeStore;
    [SerializeField, Scene(Flag.EditableAnywhere)] private EnemySpawner enemySpawner;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;
    


    private int _currentScore;
    private float _currentPathSpeed;
    private bool _settingStageFlag;
    private SavePointData _currentSavePoint;
    private SavePointData _startSavePoint;
    private Coroutine _stageChangeCoroutine;

    
    public Vector3 PlayerPosition => playerPosition;
    public Vector3 EnemyPosition => enemyPosition;
    public SOLevelStage CurrentStage => currentStage;
    public static float WorldSpeed = 1f;
    
    public event Action<SOLevelStage> OnStageChanged;
    public event Action<int> OnScoreChanged;
    public event Action<SavePointData> OnRestartedFromSavePoint;
    public event Action<RunProgressData> OnRunProgressLoaded;
    


    private void OnValidate()
    {

        this.ValidateRefs();
        
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

        UpdatePlayerAndEnemyPositions();
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
        WorldSpeed = 1f;
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
            player.OnPause += OnPlayerPaused;
        }

        if (upgradeStore)
        {
            upgradeStore.OnStoreClosed += UpgradeStoreClosed;
        }
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
            player.OnPause -= OnPlayerPaused;
        }
        if (upgradeStore)
        {
            upgradeStore.OnStoreClosed -= UpgradeStoreClosed;
        }
    }


    private void Start()
    {
        StartLevel();
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

    private void OnPlayerPaused()
    {
        ReturnToMainMenu(0);
    }
    
    private void OnPlayerCollectedResource(Resource resource)
    {
        if (!resource) return;
        
        int score = resource.ScoreWorth;
        AddScore(score);
    }
    
    
    #region Stage Management ---------------------------------------------------------------------------------

    [Button]
    private void StartLevel()
    {
        if (!level)
        {
            if (debugLog) Debug.LogError("No level defined!");
            return;
        }
        
        levelStages = level.LevelStages;
        
        if (levelStages == null || levelStages.Length == 0)
        {
            if (debugLog) Debug.LogError("No level stages defined!");
            return;
        }
        

        var runProgress = SaveManager.GetRunProgressData();
        OnRunProgressLoaded?.Invoke(runProgress);
        
        _startSavePoint = new SavePointData(currentStageIndex, 0, gameSettings.BaseHealth, 0,new Dictionary<SOUpgradeBase, int>(), player.WeaponSystem.ActiveWeaponInstance?.weaponData);
        _currentSavePoint = null;

        
        ResetScore();
        SetStage(0);
    }
    
    [Button]
    private void SetNextStage(float delay = 0)
    {
        if (_settingStageFlag) return;
        
        
        int nextStageIndex = currentStageIndex + 1;
        if (nextStageIndex < levelStages.Length)
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
        if (newStageIndex < 0 || newStageIndex >= levelStages.Length) return;
        

        SOLevelStage newStage = levelStages[newStageIndex];

        if (!newStage)
        {
            if (debugLog) Debug.LogError("No stage found at index: " + newStageIndex);
            SetNextStage();
            return;
        }
        
        if (debugLog) Debug.Log("Set stage to: " + newStage.name);
        
        
        _settingStageFlag = false;
        currentStageIndex = newStageIndex;
        currentStage = newStage;
        
        Tween.Custom(startValue: WorldSpeed, endValue: newStage.StageWorldSpeed, duration: 0.5f, onValueChange:(value) => WorldSpeed = value);
        if (currentStage.IsCheckpoint) SaveLevelProgress();
        UpdatePlayerAndEnemyPositions();
        
        OnStageChanged?.Invoke(currentStage);


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

        
    }



    private IEnumerator ChangeStageAfterDelay(int newStateIndex, float delay)
    {

        if (debugLog) Debug.Log("Setting stage: " + levelStages[newStateIndex].name + ", In " + delay);

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
            TransitionManager.TransitionToScene(gameSettings.MainMenuScene, level.OutroVFXSequence);
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

        TransitionManager.TransitionToScene(gameSettings.MainMenuScene, level.OutroVFXSequence);
        if (level.OutroVFXSequence)
        {
            FullScreenHitFXController.Instance?.TransitionTo(shipWarping);
        }
    }

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
    
    private void UpdatePlayerAndEnemyPositions()
    {
        enemyPosition = (Vector3.forward + gameSettings.EnemyBoundaryOffset) * gameSettings.EnemyPositionMultiplier;
        playerPosition = (Vector3.forward + gameSettings.PlayerBoundaryOffset) * gameSettings.PlayerPositionMultiplier;
    }



    #endregion Stage Management ---------------------------------------------------------------------------------
    
    
    
    #region Score Management ---------------------------------------------------------------------------------

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
    

    #endregion Score Management ---------------------------------------------------------------------------------
    
    
    
    
#if UNITY_EDITOR
    #region Editor -----------------------------------------------------------------------------------------------


    

    private void OnDrawGizmos()
    {
        // Draw player position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(playerPosition, 0.5f);
        Vector3 playerSplinePosition = playerPosition;
        Vector3[] localCorners = new Vector3[]
        {
            new Vector3(-gameSettings.PlayerBoundary.x, -gameSettings.PlayerBoundary.y, 0),
            new Vector3(gameSettings.PlayerBoundary.x, -gameSettings.PlayerBoundary.y, 0),  
            new Vector3(gameSettings.PlayerBoundary.x, gameSettings.PlayerBoundary.y, 0),   
            new Vector3(-gameSettings.PlayerBoundary.x, gameSettings.PlayerBoundary.y, 0)  
        };
        Vector3[] worldCorners = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            worldCorners[i] = playerSplinePosition + localCorners[i];
        }
        for (int i = 0; i < 4; i++)
        {
            int nextIndex = (i + 1) % 4;
            Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
        }
        UnityEditor.Handles.Label(playerSplinePosition + Vector3.up * (gameSettings.PlayerBoundary.y + 1f), "Player Boundaries");
        
        
        // Draw enemy position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(enemyPosition, 0.5f);
        Vector3 crosshairSplinePosition = enemyPosition;
        Vector3[] localCorners1 = new Vector3[]
        {
            new Vector3(-gameSettings.EnemyBoundary.x, -gameSettings.EnemyBoundary.y, 0),
            new Vector3(gameSettings.EnemyBoundary.x, -gameSettings.EnemyBoundary.y, 0),
            new Vector3(gameSettings.EnemyBoundary.x, gameSettings.EnemyBoundary.y, 0),
            new Vector3(-gameSettings.EnemyBoundary.x, gameSettings.EnemyBoundary.y, 0)
        };
        Vector3[] worldCorners1 = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            worldCorners1[i] = crosshairSplinePosition + localCorners1[i];
        }
        for (int i = 0; i < 4; i++)
        {
            int nextIndex = (i + 1) % 4;
            Gizmos.DrawLine(worldCorners1[i], worldCorners1[nextIndex]);
        }
        UnityEditor.Handles.Label(crosshairSplinePosition + Vector3.up * (gameSettings.EnemyBoundary.y + 1f), "Enemy Boundaries");


    }

    #endregion Editor -----------------------------------------------------------------------------------------------

#endif
}