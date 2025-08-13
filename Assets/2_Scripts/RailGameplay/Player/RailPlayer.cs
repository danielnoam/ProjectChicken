using System;
using System.Collections;
using DNExtensions;
using KBCore.Refs;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(RailPlayerInput))]
[RequireComponent(typeof(RailPlayerMovement))]
[RequireComponent(typeof(RailPlayerAiming))]
[RequireComponent(typeof(RailPlayerWeaponSystem))]
[RequireComponent(typeof(RailPlayerResourceCollector))]
[RequireComponent(typeof(ControllerVibrationSource))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class RailPlayer : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(0)] private int baseHealth = 3;
    [SerializeField] private bool dodgingGivesInvincibility = true;
    [SerializeField] private bool receiveHealthOnBonusThreshold = true;
    
    [Header("Shield")]
    [SerializeField, Min(0)] private float baseShieldHealth = 100f;
    [SerializeField, Min(0)] private float shieldRegenCooldown = 3f;
    [SerializeField, Min(0)] private float shieldRegenRate = 15f;
    
    [Header("Camera Shake")]
    [SerializeField] private CameraShakeSettings shieldDamagedShakeSettings;
    [SerializeField] private CameraShakeSettings healthDamagedShakeSettings;
    [SerializeField] private CameraShakeSettings deathShakeSettings;
    
    [Header("Controller Rumble")]
    [SerializeField] private ControllerVibrationEffectSettings shieldDamagedVibrationSettings;
    [SerializeField] private ControllerVibrationEffectSettings healthDamagedVibrationSettings;
    [SerializeField] private ControllerVibrationEffectSettings deathVibrationSettings;
    
    [Header("References")]
    [SerializeField, Child(Flag.Editable)] private AudioSource audioSource;
    [SerializeField] private Transform cameraPositions;
    [SerializeField] private Transform followCameraTarget;
    [SerializeField] private Transform storeCameraTarget;
    [SerializeField] private SOAudioEvent healthDamageSfx;
    [SerializeField] private SOAudioEvent healthHealedSfx;
    [SerializeField] private SOAudioEvent shieldDamageSfx;
    [SerializeField] private SOAudioEvent shieldStartRegenSfx;
    [SerializeField] private SOAudioEvent shieldRegeneratedSfx;
    [SerializeField] private SOAudioEvent shieldDepletedSfx;
    [SerializeField] private SOAudioEvent deathSfx;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private SOGameSettings gameSettings;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming playerAiming;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem playerWeapon;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private RailPlayerResourceCollector resourceCollector;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;
    [SerializeField, Self, HideInInspector] private CinemachineImpulseSource cinemachineImpulseSource;

    private int _currentHealth;
    private int _currentCurrency;
    private float _currentShieldHealth;
    private float _baseMaxShieldHealth;
    private float _damagedCooldown;
    private float _pauseTimer;
    private bool _pauseInputHeld;
    private Coroutine _regenShieldCoroutine;

    public SOGameSettings GameSettings => gameSettings;
    public RailPlayerAiming PlayerAiming => playerAiming;
    public RailPlayerWeaponSystem PlayerWeapon => playerWeapon;
    public RailPlayerMovement PlayerMovement => playerMovement;
    public RailPlayerResourceCollector ResourceCollector => resourceCollector;
    public LevelManager LevelManager => levelManager;
    public int CurrentHealth => _currentHealth;
    public float CurrentShieldHealth => _currentShieldHealth;
    public int CurrentCurrency => _currentCurrency;
    public float BaseMaxShieldHealth => _baseMaxShieldHealth;
    public event Action OnDeath;
    public event Action<int> OnHealthChanged;
    public event Action<float> OnShieldChanged;
    public event Action<int> OnCurrencyChanged;
    public event Action<float> OnPauseTimerChanged;
    public event Action OnPause;

    private void OnValidate()
    {
        this.ValidateRefs();
        
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
    }

    private void Awake()
    {
        _currentCurrency = SaveManager.GetCurrency();
        _currentHealth = baseHealth;
        _currentShieldHealth = baseShieldHealth;
    }

    private void Start()
    {
        OnCurrencyChanged?.Invoke(_currentCurrency);
        OnHealthChanged?.Invoke(_currentHealth);
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }

    private void OnEnable()
    {
        levelManager.OnBonusThresholdReached += OnScoreReachedBonusThreshold;
        levelManager.OnStageChanged += OnStageChanged;
        levelManager.OnRestartedFromSavePoint += RestartedFromSavePoint;
        playerInput.OnPauseActionEvent += OnPauseAction;
    }

    private void OnDisable()
    {
        levelManager.OnBonusThresholdReached -= OnScoreReachedBonusThreshold;
        levelManager.OnStageChanged -= OnStageChanged;
        levelManager.OnRestartedFromSavePoint -= RestartedFromSavePoint;
        playerInput.OnPauseActionEvent -= OnPauseAction;
    }

    private void Update()
    {
        CheckDamageCooldown();
        
        if (_pauseInputHeld)
        {
            _pauseTimer += Time.deltaTime;
            OnPauseTimerChanged?.Invoke(_pauseTimer/gameSettings.TimeToPause);
            if (_pauseTimer >= gameSettings.TimeToPause)
            {
                OnPause?.Invoke();
            }
        }
    }
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        if (stage.StageType == StageType.Outro)
        {
            SaveManager.UpdatePlayerCurrency(_currentCurrency);
        }
    }

    private void RestartedFromSavePoint(SavePointInformation savePoint)
    {
        if (savePoint == null) return;
        
        _currentCurrency = savePoint.PlayerCurrency;
        _currentHealth = savePoint.PlayerHealth;
        _currentShieldHealth = savePoint.PlayerShield;
        
        OnCurrencyChanged?.Invoke(_currentCurrency);
        OnHealthChanged?.Invoke(_currentHealth);
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }
    
    private void OnScoreReachedBonusThreshold()
    {
        if (!receiveHealthOnBonusThreshold) return;
        
        HealHealth(1);
    }
    
    private void OnPauseAction(InputAction.CallbackContext context)
    {
        if (!IsAlive()) return;

        if (context.started)
        {
            _pauseInputHeld = true;
            _pauseTimer = 0f;
            OnPauseTimerChanged?.Invoke(_pauseTimer/gameSettings.TimeToPause);
        } 
        else if (context.canceled)
        {
            _pauseInputHeld = false;
            _pauseTimer = 0f;
            OnPauseTimerChanged?.Invoke(_pauseTimer/gameSettings.TimeToPause);
        }
    }
    

    #region Damage ----------------------------------------------------------------------

    [Button]
    public void TakeDamage(float damage)
    {
        if (damage <= 0 || !IsAlive() || (dodgingGivesInvincibility && IsDodging())) return;
        
        StopShieldRegen();
        
        if (HasShield())
        {
            DamageShield(damage);
            return;
        }
        
        DamageHealth();
    }
    
    private void CheckDamageCooldown()
    {
        if (!IsAlive()) return;
        
        if (_damagedCooldown > 0)
        {
            _damagedCooldown -= Time.deltaTime;
        }
        
        if (_damagedCooldown <= 0 &&  _regenShieldCoroutine == null && _currentShieldHealth < _baseMaxShieldHealth)
        {
            StartShieldRegen();
        }
    }

    private void Die()
    {
        deathSfx?.Play(audioSource);
        controllerVibrationSource.Vibrate(deathVibrationSettings);
        if (cinemachineImpulseSource)
        {
            cinemachineImpulseSource.ImpulseDefinition.ImpulseShape = deathShakeSettings.impulseShape;
            cinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = deathShakeSettings.duration;
            cinemachineImpulseSource.DefaultVelocity = new Vector3(UnityEngine.Random.Range(-1f,1f),UnityEngine.Random.Range(-1f,1f),UnityEngine.Random.Range(-1f,1f));
            cinemachineImpulseSource.GenerateImpulseWithForce(deathShakeSettings.intensity);
        }

        OnDeath?.Invoke();
    }
    
    #endregion Damage ----------------------------------------------------------------------

    
    #region Health --------------------------------------------------------------------------

    private void DamageHealth()
    {
        if (!IsAlive()) return;

        _currentHealth -= 1;
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }
        else
        {
            if (cinemachineImpulseSource)
            {
                cinemachineImpulseSource.ImpulseDefinition.ImpulseShape = healthDamagedShakeSettings.impulseShape;
                cinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = healthDamagedShakeSettings.duration;
                cinemachineImpulseSource.DefaultVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
                cinemachineImpulseSource.GenerateImpulseWithForce(healthDamagedShakeSettings.intensity);
            }
            controllerVibrationSource.Vibrate(healthDamagedVibrationSettings);
            healthDamageSfx?.Play(audioSource);
        }
        
        OnHealthChanged?.Invoke(_currentHealth);
    }
    
    
    [Button]
    public void HealHealth(int amount = 1)
    {
        if (amount <= 0) return;
        
        _currentHealth += amount;
        if (_currentHealth > gameSettings.MaxPlayerHealth)
        {
            _currentHealth = gameSettings.MaxPlayerHealth;
        }
        healthHealedSfx?.Play(audioSource);
        
        OnHealthChanged?.Invoke(_currentHealth);
    }

    #endregion Health --------------------------------------------------------------------------
    
    
    #region Shield --------------------------------------------------------------------------------------
    
    private IEnumerator RegenShieldRoutine()
    {
        shieldStartRegenSfx?.Play(audioSource);
        
        while (_currentShieldHealth < _baseMaxShieldHealth)
        {
            _currentShieldHealth += shieldRegenRate * Time.deltaTime;
            if (_currentShieldHealth >= _baseMaxShieldHealth)
            {
                _currentShieldHealth = _baseMaxShieldHealth;
                shieldRegeneratedSfx?.Play(audioSource);
                yield break;
            }
            
            OnShieldChanged?.Invoke(_currentShieldHealth);
            yield return null;
        }
    }
    
    private void StopShieldRegen()
    {
        if (_regenShieldCoroutine != null)
        {
            StopCoroutine(_regenShieldCoroutine);
            _regenShieldCoroutine = null;
        }
        
        _damagedCooldown = shieldRegenCooldown;
    }

    private void StartShieldRegen()
    {
        _regenShieldCoroutine ??= StartCoroutine(RegenShieldRoutine());
        _damagedCooldown = 0;
    }
    
    private void DamageShield(float damage)
    {
        if (damage <= 0 || !HasShield()) return;
        
        _currentShieldHealth -= damage;

        if (_currentShieldHealth < 0)
        {
            _currentShieldHealth = 0;
            DamageHealth();
            shieldDepletedSfx?.Play(audioSource);
        }
        else
        {
            if (cinemachineImpulseSource)
            {
                cinemachineImpulseSource.ImpulseDefinition.ImpulseShape = shieldDamagedShakeSettings.impulseShape;
                cinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = shieldDamagedShakeSettings.duration;
                cinemachineImpulseSource.DefaultVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
                cinemachineImpulseSource.GenerateImpulseWithForce(shieldDamagedShakeSettings.intensity);
            }
            controllerVibrationSource.Vibrate(shieldDamagedVibrationSettings);
            shieldDamageSfx?.Play(audioSource);
        }
        
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }
    
    [Button]
    public void HealShield(float amount = 25f)
    {
        if (_currentShieldHealth >= _baseMaxShieldHealth) return;
        
        _currentShieldHealth += amount;
        if (_currentShieldHealth >= _baseMaxShieldHealth)
        {
            _currentShieldHealth = _baseMaxShieldHealth;
            shieldRegeneratedSfx?.Play(audioSource);
        }
        else
        {
            StartShieldRegen();
        }
        
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }

    public void AddBaseShield(float amount)
    {
        if (amount <= 0) return;
        
        _baseMaxShieldHealth += amount;
        if (_baseMaxShieldHealth > gameSettings.MaxPlayerShield)
        {
            _baseMaxShieldHealth = gameSettings.MaxPlayerShield;
        }

        _currentShieldHealth = _baseMaxShieldHealth;
        shieldRegeneratedSfx?.Play(audioSource);
        OnShieldChanged?.Invoke(_currentShieldHealth);
    
    }

    #endregion Shield --------------------------------------------------------------------------------------
    

    #region Currency --------------------------------------------------------------------------------
    
    public void UpdateCurrency(int amount)
    {
        _currentCurrency += amount;
        OnCurrencyChanged?.Invoke(_currentCurrency);
    }
    

    #endregion Currency --------------------------------------------------------------------------------
    
    
    #region Helper Methods --------------------------------------------------------------------------------------

    public bool HasShield()
    {
        return _currentShieldHealth > 0;
    }
    
    public bool IsAlive()
    {
        return _currentHealth > 0;
    }
    
    public bool IsDodging()
    {
        return playerMovement.IsDodging;
    }
    
    public ChickenController GetTarget(float radius)
    {
        return playerAiming.CurrentAimLockTarget ? playerAiming.CurrentAimLockTarget : playerAiming.GetEnemyTarget(radius);
    }
    
    public ChickenController[] GetAllTargets(int maxTargets, float radius)
    {
        return playerAiming.GetEnemyTargets(maxTargets, radius);
    }
    
    public Transform GetFollowCameraTarget()
    {
        return followCameraTarget ? followCameraTarget : transform;;
    }

    public Transform GetStoreCameraTarget()
    {
        return storeCameraTarget ? storeCameraTarget : transform;
    }
    
    public Transform GetRandomCameraPosition()
    {
        if (!cameraPositions) return transform;
        
        int randomIndex = UnityEngine.Random.Range(0, cameraPositions.childCount);
        return cameraPositions.GetChild(randomIndex);
    }
    
    
    #endregion Helper Methods --------------------------------------------------------------------------------------
}