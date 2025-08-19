using System;
using System.Collections;
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
    
    [Header("References")]
    [SerializeField] private SOGameSettings gameSettings;
    [SerializeField, Scene(Flag.EditableAnywhere)] private StoreManager storeManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private EnemySpawner enemySpawner;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;
    

    private Coroutine _stageChangeCoroutine;
    private float _currentPathSpeed;
    private bool _settingStageFlag;
    private int _bonusThresholdCounter;
    private SavePointInformation _currentSavePoint;
    private int _currentScore;
    
    public Vector3 PlayerPosition => playerPosition;
    public Vector3 EnemyPosition => enemyPosition;
    public static float WorldSpeed = 1f;
    
    public event Action<SOLevelStage> OnStageChanged;
    public event Action<int> OnScoreChanged;
    public event Action OnBonusThresholdReached;
    public event Action<SavePointInformation> OnRestartedFromSavePoint;
    


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
        
        if (!storeManager)
        {
            storeManager = FindFirstObjectByType<StoreManager>();
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
    }


    private void Start()
    {
        StartLevel();
    }

    private void OnEnable()
    {
        if (enemySpawner)
        {
            enemySpawner.OnEnemyWaveSpawned += EnemySpawned;
            enemySpawner.OnEnemyWaveCleared += EnemyCleared;
            enemySpawner.OnEnemyDeath += OnEnemyDeath;
        }

        if (player)
        {
            player.ResourceCollector.OnResourceCollected += CollectedResource;
            player.Health.OnDeath += Death;
            player.OnPause += OnPlayerPaused;
        }

        if (storeManager)
        {
            storeManager.OnStoreClosed += OnStoreClosed;
        }
    }
    

    private void OnDisable()
    {
        if (enemySpawner)
        {
            enemySpawner.OnEnemyWaveSpawned -= EnemySpawned;
            enemySpawner.OnEnemyWaveCleared -= EnemyCleared;
            enemySpawner.OnEnemyDeath -= OnEnemyDeath;
        }
        
        if (player)
        {
            player.ResourceCollector.OnResourceCollected -= CollectedResource;
            player.Health.OnDeath -= Death;
            player.OnPause -= OnPlayerPaused;
        }
        if (storeManager)
        {
            storeManager.OnStoreClosed -= OnStoreClosed;
        }
    }
    
    private void EnemyCleared(int scoreWorth)
    {
        if (!currentStage || currentStage.StageType != StageType.EnemyWave || _settingStageFlag) return;
        
        AddScore(scoreWorth);
        
        SetNextStage(currentStage.DelayBeforeNextStage);
    }
    
    private void EnemySpawned()
    {
        enemiesLeft = enemySpawner.ActiveEnemyCount;
    }

    private void OnEnemyDeath(ChickenController enemy)
    {
        enemiesLeft = enemySpawner.ActiveEnemyCount;
        AddScore(enemy.ScoreValue);
    }
    
    private void OnStoreClosed()
    {
        if (!currentStage || currentStage.StageType != StageType.Store) return;
        
        SetNextStage();
    }

    private void Death()
    {
        StartCoroutine(RestartSavePoint());
    }

    private void OnPlayerPaused()
    {
        StartCoroutine(ReturnToMainMenu(0.1f));
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
        WorldSpeed = 1f;
        
        if (levelStages == null || levelStages.Length == 0)
        {
            if (debugLog) Debug.LogError("No level stages defined!");
            return;
        }
        
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
        
        currentStageIndex = newStageIndex;
        currentStage = newStage;
        
        Tween.Custom(
            startValue: WorldSpeed,
            endValue: newStage.StageWorldSpeed, 
            duration: 0.5f, 
            onValueChange:(value) => WorldSpeed = value);
        
        UpdateReachedSavePoint(newStage);
        UpdatePlayerAndEnemyPositions();
        
        _settingStageFlag = false;
        OnStageChanged?.Invoke(newStage);

        if (newStage.IsTimeBasedStage)
        {
            if (newStage.StageType == StageType.Outro)
            {
                StartCoroutine(ReturnToMainMenu(newStage.StageDuration));
                SaveManager.UpdateLevelProgress(SceneManager.GetActiveScene().path, _currentScore);
            }
            else
            {
                if (newStage.StageType == StageType.Intro) VFXManager.Instance?.PlayVFX(level.IntroVFXSequence);
                SetNextStage(newStage.StageDuration);
            }
        }
        
    }



    private IEnumerator ChangeStageAfterDelay(int newStateIndex, float delay)
    {

        if (debugLog) Debug.Log("Setting stage: " + levelStages[newStateIndex].name + ", In " + delay);

        yield return new WaitForSeconds(delay);

        SetStage(newStateIndex);
    }

    private IEnumerator ReturnToMainMenu(float delay)
    {
        yield return new WaitForSeconds(delay);

        TransitionManager.TransitionToScene(gameSettings.MainMenuScene, level.OutroVFXSequence);

    }
    

    private IEnumerator RestartSavePoint()
    {
        if (_currentSavePoint == null) yield break;
        
        yield return new WaitForSeconds(5f);
        
        SetStage(_currentSavePoint.StageIndex);
        _currentScore = _currentSavePoint.Score;
        OnScoreChanged?.Invoke(_currentScore);
        OnRestartedFromSavePoint?.Invoke(_currentSavePoint);
    }

    private void UpdateReachedSavePoint(SOLevelStage stage)
    {
        if (!stage || !stage.IsSavePointStage || _currentSavePoint != null && _currentSavePoint.StageIndex == currentStageIndex) return;

        var playerSpecialWeapon = player.WeaponSystem.CurrentSpecialWeaponInstance?.WeaponData;
        
        var newSavePoint = new SavePointInformation(
            currentStageIndex,
            _currentScore, 
            player.ResourceCollector.CurrentCurrency, 
            player.Upgrades,
            playerSpecialWeapon
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
        _bonusThresholdCounter -= score;
        
        OnScoreChanged?.Invoke(_currentScore);
        
        if (_bonusThresholdCounter <= 0)
        {
            OnBonusThresholdReached?.Invoke();
            _bonusThresholdCounter = gameSettings.ScoreBonusThreshold;
        }

    }

    private void ResetScore()
    {
        _currentScore = 0;
        _bonusThresholdCounter = gameSettings.ScoreBonusThreshold;
        
        OnScoreChanged?.Invoke(_currentScore);
    }
    
    private void CollectedResource(Resource resource)
    {
        if (!resource) return;
        
        int score = resource.ScoreWorth;
        AddScore(score);
    }
    
    

    #endregion
    
    
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


}