using System;
using System.Collections;
using System.Collections.Generic;
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
    [Header("General")]
    [SerializeField, Min(0)] private float timeToPause = 3f;
    [SerializeField] private bool alignToSplineDirection = true;
    [SerializeField, Min(0), ShowIf("alignToSplineDirection")] private float splineRotationSpeed = 5f; [EndIf]
    
    [Header("Health")]
    [SerializeField, Min(0)] private int baseHealth = 3;
    [SerializeField] private bool dodgingGivesInvincibility = true;
    [SerializeField] private bool receiveHealthOnBonusThreshold = true;
    [SerializeField] private SOHealthUpgrade[] healthUpgrades = Array.Empty<SOHealthUpgrade>();
    
    [Header("Shield")]
    [SerializeField, Min(0)] private float baseShieldHealth = 100f;
    [SerializeField, Min(0)] private float shieldRegenCooldown = 3f;
    [SerializeField, Min(0)] private float shieldRegenRate = 15f;
    [SerializeField] private SOShieldUpgrade[] shieldUpgrades = Array.Empty<SOShieldUpgrade>();
    
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
    [SerializeField] private SOAudioEvent healthDamageSfx;
    [SerializeField] private SOAudioEvent healthHealedSfx;
    [SerializeField] private SOAudioEvent shieldDamageSfx;
    [SerializeField] private SOAudioEvent shieldStartRegenSfx;
    [SerializeField] private SOAudioEvent shieldRegeneratedSfx;
    [SerializeField] private SOAudioEvent shieldDepletedSfx;
    [SerializeField] private SOAudioEvent deathSfx;
    [SerializeField] private LevelManager levelManager;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming playerAiming;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem playerWeapon;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private RailPlayerResourceCollector resourceCollector;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;
    [SerializeField, Self, HideInInspector] private CinemachineImpulseSource cinemachineImpulseSource;

    private int _currentHealth;
    private int _maxHealth;
    private int _currentCurrency;
    private float _currentShieldHealth;
    private float _maxShieldHealth;
    private float _damagedCooldown;
    private float _pauseTimer;
    private bool _pauseInputHeld;
    private Coroutine _regenShieldCoroutine;
    private Quaternion _splineRotation = Quaternion.identity;

    public RailPlayerAiming PlayerAiming => playerAiming;
    public RailPlayerWeaponSystem PlayerWeapon => playerWeapon;
    public RailPlayerMovement PlayerMovement => playerMovement;
    public RailPlayerResourceCollector ResourceCollector => resourceCollector;
    public LevelManager LevelManager => levelManager;
    public Quaternion SplineRotation => _splineRotation;
    public bool AlignToSplineDirection => alignToSplineDirection;
    public int MaxHealth => _maxHealth;
    public float MaxShieldHealth => _maxShieldHealth;
    public int CurrentHealth => _currentHealth;
    public float CurrentShieldHealth => _currentShieldHealth;
    public int CurrentCurrency => _currentCurrency;
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
        _maxHealth = baseHealth + TotalHealthUpgrades();
        _maxShieldHealth = baseShieldHealth + TotalShieldUpgrades();
        
        _currentCurrency = SaveManager.GetCurrency();
        _currentHealth = _maxHealth;
        _currentShieldHealth = _maxShieldHealth;
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
        GetSplineRotations();
        CheckDamageCooldown();
        
        if (_pauseInputHeld)
        {
            _pauseTimer += Time.deltaTime;
            OnPauseTimerChanged?.Invoke(_pauseTimer/timeToPause);
            if (_pauseTimer >= timeToPause)
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
            OnPauseTimerChanged?.Invoke(_pauseTimer/timeToPause);
        } 
        else if (context.canceled)
        {
            _pauseInputHeld = false;
            _pauseTimer = 0f;
            OnPauseTimerChanged?.Invoke(_pauseTimer/timeToPause);
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
        
        if (_damagedCooldown <= 0 &&  _regenShieldCoroutine == null && _currentShieldHealth < _maxShieldHealth)
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
        if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }
        healthHealedSfx?.Play(audioSource);
        
        OnHealthChanged?.Invoke(_currentHealth);
    }

    #endregion Health --------------------------------------------------------------------------
    
    
    #region Shield --------------------------------------------------------------------------------------
    
    private IEnumerator RegenShieldRoutine()
    {
        shieldStartRegenSfx?.Play(audioSource);
        
        while (_currentShieldHealth < _maxShieldHealth)
        {
            _currentShieldHealth += shieldRegenRate * Time.deltaTime;
            if (_currentShieldHealth >= _maxShieldHealth)
            {
                _currentShieldHealth = _maxShieldHealth;
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
        if (_currentShieldHealth >= _maxShieldHealth) return;
        
        _currentShieldHealth += amount;
        if (_currentShieldHealth >= _maxShieldHealth)
        {
            _currentShieldHealth = _maxShieldHealth;
            shieldRegeneratedSfx?.Play(audioSource);
        }
        else
        {
            StartShieldRegen();
        }
        
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }

    #endregion Shield --------------------------------------------------------------------------------------
    

    #region Currency --------------------------------------------------------------------------------

    [Button]
    public void UpdateCurrency(int amount)
    {
        _currentCurrency += amount;
        OnCurrencyChanged?.Invoke(_currentCurrency);
    }

    #endregion Currency --------------------------------------------------------------------------------
    
    
    #region Upgrades -----------------------------------------------------------------------------------------

    private int TotalHealthUpgrades()
    {
        var health = 0;
        
        foreach (var upgrade in healthUpgrades)
        {
            if (SaveManager.HasStoreItem(upgrade.ItemID))
            {
                health += upgrade.HealthUpgradeAmount;
            }
        }
        
        return health;
    }
    
    private float TotalShieldUpgrades()
    {
        var shield = 0f;

        foreach (var upgrade in shieldUpgrades)
        {
            if (SaveManager.HasStoreItem(upgrade.ItemID))
            {
                shield += upgrade.ShieldUpgradeAmount;
            }
        }

        return shield;
    }

    #endregion Upgrades -----------------------------------------------------------------------------------------

    
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
        return followCameraTarget;
    }
    
    public Transform GetRandomCameraPosition()
    {
        switch (cameraPositions.childCount)
        {
            case 0:
                return null;
            case 1:
                cameraPositions.GetChild(1);
                break;
        }

        int randomIndex = UnityEngine.Random.Range(2, cameraPositions.childCount);
        return cameraPositions.GetChild(randomIndex);
    }
    
    private void GetSplineRotations()
    {
        if (!alignToSplineDirection || !levelManager)
        {
            _splineRotation = Quaternion.identity;
            return;
        }
        
        Vector3 splineForward = levelManager.GetSplineTangentAtPosition(levelManager.CurrentPositionOnPath.position);
        
        if (splineForward != Vector3.zero)
        {
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineForward, Vector3.up);
            _splineRotation = Quaternion.Slerp(_splineRotation, targetSplineRotation, splineRotationSpeed * Time.deltaTime);
        }
    }

    #endregion Helper Methods --------------------------------------------------------------------------------------
}