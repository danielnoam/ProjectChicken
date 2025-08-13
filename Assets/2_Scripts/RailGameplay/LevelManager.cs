using System;
using System.Collections;
using DNExtensions;
using DNExtensions.VFXManager;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;



[SelectionBase]
public class LevelManager : MonoBehaviour
{
    
    public static LevelManager Instance { get; private set; }
    
        
    [Header("Level Settings")]
    [SerializeField] private SOLevelStage[] levelStages;
    
    [Header("Debug")]
    [SerializeField] private bool debugLog;
    [SerializeField, VInspector.ReadOnly] private SOLevelStage currentStage;
    [SerializeField, VInspector.ReadOnly] private int currentStageIndex;
    [SerializeField, VInspector.ReadOnly] private int enemiesLeft;
    [SerializeField, VInspector.ReadOnly] public Vector3 playerPosition;
    [SerializeField, VInspector.ReadOnly] public Vector3 enemyPosition;
    
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
            return;
        }

        UpdatePlayerAndEnemyPositions();
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
            player.ResourceCollector.OnResourceCollected += OnPlayerCollectedResource;
            player.OnDeath += OnPlayerDeath;
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
            player.ResourceCollector.OnResourceCollected -= OnPlayerCollectedResource;
            player.OnDeath -= OnPlayerDeath;
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

    private void OnPlayerDeath()
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
        UpdateReachedSavePoint(newStage);
        UpdatePlayerAndEnemyPositions();

        if (newStage.IsTimeBasedStage)
        {
            if (newStage.StageType == StageType.Outro)
            {
                StartCoroutine(ReturnToMainMenu(newStage.StageDuration, newStage.StageVFXSequence));
                SaveManager.UpdateLevelProgress(SceneManager.GetActiveScene().path, _currentScore);
            }
            else
            {
                SetNextStage(newStage.StageDuration);
                VFXManager.Instance?.PlayVFX(newStage.StageVFXSequence);
            }
        }
        else
        {
            VFXManager.Instance?.PlayVFX(newStage.StageVFXSequence);
        }  
        
        _settingStageFlag = false;
        OnStageChanged?.Invoke(newStage);

        


    }



    private IEnumerator ChangeStageAfterDelay(int newStateIndex, float delay)
    {

        if (debugLog) Debug.Log("Setting stage: " + levelStages[newStateIndex].name + ", In " + delay);

        yield return new WaitForSeconds(delay);

        SetStage(newStateIndex);
    }

    private IEnumerator ReturnToMainMenu(float delay, SOVFEffectsSequence outroSequence = null)
    {
        yield return new WaitForSeconds(delay);

        if (outroSequence)
        {
            TransitionManager.TransitionToScene(gameSettings.MainMenuScene, outroSequence);
        }
        else
        {
            gameSettings.MainMenuScene?.LoadScene();
        }

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

        var playerSpecialWeapon = player.PlayerWeapon.CurrentSpecialWeaponInstance?.WeaponData;
        
        var newSavePoint = new SavePointInformation(
            currentStageIndex,
            _currentScore, 
            player.CurrentHealth, 
            player.CurrentShieldHealth, 
            player.CurrentCurrency, 
            playerSpecialWeapon
            );
            
        _currentSavePoint = newSavePoint;
    }
    
    private void UpdatePlayerAndEnemyPositions()
    {
        if (!currentStage)
        {
            enemyPosition = Vector3.forward * gameSettings.EnemyPositionMultiplier;
            playerPosition = Vector3.forward * gameSettings.PlayerPositionMultiplier;
            
        }
        else
        {
            enemyPosition = (Vector3.forward + currentStage.EnemyPositionOffset) * gameSettings.EnemyPositionMultiplier;
            playerPosition = (Vector3.forward + currentStage.PlayerPositionOffset) * gameSettings.PlayerPositionMultiplier;
        }
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
            _bonusThresholdCounter = gameSettings.BonusThreshold;
        }

    }

    private void ResetScore()
    {
        _currentScore = 0;
        _bonusThresholdCounter = gameSettings.BonusThreshold;
        
        OnScoreChanged?.Invoke(_currentScore);
    }
    
    private void OnPlayerCollectedResource(Resource resource)
    {
        if (!resource) return;
        
        int score = resource.ScoreWorth;
        AddScore(score);
    }
    
    

    #endregion
    

    
    
    #region Editor -----------------------------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        
        
        
        if (!Application.isPlaying) return;
        // Draw player position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(playerPosition, 0.3f);
            
        // Draw enemy position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(enemyPosition, 0.3f);

    }

    #endregion Editor -----------------------------------------------------------------------------------------------


}