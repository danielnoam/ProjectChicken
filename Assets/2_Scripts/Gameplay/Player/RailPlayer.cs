using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField, Min(0)] private int baseHealth = 2;
    [SerializeField] private bool dodgingGivesInvincibility = true;
    
    [Header("Shield")]
    [SerializeField, Min(0)] private float baseShield = 100f;
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
    [SerializeField] private Transform storeCameraLookAtTarget;
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
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem weaponSystem;
    [SerializeField, Self, HideInInspector] private RailPlayerResourceCollector resourceCollector;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;
    [SerializeField, Self, HideInInspector] private CinemachineImpulseSource cinemachineImpulseSource;

    private float _damagedCooldown;
    private float _pauseTimer;
    private bool _pauseInputHeld;
    private Coroutine _regenShieldCoroutine;


    
    public int CurrentHealth { get; private set; }

    public float CurrentShield { get; private set; }

    public int CurrentCurrency { get; private set; }

    public float MaxShield { get; private set; }
    public List<SOUpgradeBase> Upgrades { get; private set; }  = new List<SOUpgradeBase>();
    
    public SOGameSettings GameSettings => gameSettings;
    public RailPlayerAiming PlayerAiming => playerAiming;
    public RailPlayerWeaponSystem WeaponSystem => weaponSystem;
    public RailPlayerMovement PlayerMovement => playerMovement;
    public RailPlayerResourceCollector ResourceCollector => resourceCollector;
    public LevelManager LevelManager => levelManager;

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
        CurrentCurrency = 0;
        CurrentHealth = baseHealth;
        CurrentShield = baseShield;
        MaxShield = baseShield;
    }

    private void Start()
    {
        OnCurrencyChanged?.Invoke(CurrentCurrency);
        OnHealthChanged?.Invoke(CurrentHealth);
        OnShieldChanged?.Invoke(CurrentShield);
    }

    private void OnEnable()
    {
        levelManager.OnStageChanged += OnStageChanged;
        levelManager.OnRestartedFromSavePoint += RestartedFromSavePoint;
        playerInput.OnPauseActionEvent += OnPauseAction;
    }

    private void OnDisable()
    {
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
            SaveManager.UpdatePlayerCurrency(CurrentCurrency);
        }
    }

    private void RestartedFromSavePoint(SavePointInformation savePoint)
    {
        if (savePoint == null) return;
        
        CurrentCurrency = savePoint.PlayerCurrency;
        CurrentHealth = savePoint.PlayerHealth;
        CurrentShield = savePoint.PlayerShield;
        Upgrades = savePoint.PlayerUpgrades;
        
        OnCurrencyChanged?.Invoke(CurrentCurrency);
        OnHealthChanged?.Invoke(CurrentHealth);
        OnShieldChanged?.Invoke(CurrentShield);
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


    public void TakeDamage(float damage)
    {
        if (damage <= 0 || !IsAlive() || (dodgingGivesInvincibility && IsDodging())) return;
        
        StopShieldRegen();
        
        if (ShieldActive())
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
        
        if (_damagedCooldown <= 0 &&  _regenShieldCoroutine == null && CurrentShield < MaxShield)
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

        CurrentHealth -= 1;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
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
        
        OnHealthChanged?.Invoke(CurrentHealth);
    }
    
    public void HealHealth(int amount = 1)
    {
        if (amount <= 0) return;
        
        CurrentHealth += amount;
        if (CurrentHealth > gameSettings.MaxPlayerHealth)
        {
            CurrentHealth = gameSettings.MaxPlayerHealth;
        }
        healthHealedSfx?.Play(audioSource);
        OnHealthChanged?.Invoke(CurrentHealth);
    }
    

    #endregion Health --------------------------------------------------------------------------
    
    
    #region Shield --------------------------------------------------------------------------------------
    
    private IEnumerator RegenShieldRoutine()
    {
        shieldStartRegenSfx?.Play(audioSource);
        
        while (CurrentShield < MaxShield)
        {
            CurrentShield += shieldRegenRate * Time.deltaTime;
            if (CurrentShield >= MaxShield)
            {
                CurrentShield = MaxShield;
                shieldRegeneratedSfx?.Play(audioSource);
                yield break;
            }
            
            OnShieldChanged?.Invoke(CurrentShield);
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
        _regenShieldCoroutine = StartCoroutine(RegenShieldRoutine());
        _damagedCooldown = 0;
    }
    
    private void DamageShield(float damage)
    {
        if (damage <= 0 || !ShieldActive()) return;
        
        CurrentShield -= damage;

        if (CurrentShield < 0)
        {
            CurrentShield = 0;
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
        
        OnShieldChanged?.Invoke(CurrentShield);
    }
    
    public void HealShield(float amount = 25f)
    {
        if (CurrentShield >= MaxShield) return;
        
        CurrentShield += amount;
        if (CurrentShield >= MaxShield)
        {
            CurrentShield = MaxShield;
            shieldRegeneratedSfx?.Play(audioSource);
        }
        else
        {
            StartShieldRegen();
        }
        
        OnShieldChanged?.Invoke(CurrentShield);
    }

    #endregion Shield --------------------------------------------------------------------------------------
    

    #region Currency --------------------------------------------------------------------------------
    
    [Button]
    public void UpdateCurrency(int amount)
    {
        CurrentCurrency += amount;
        OnCurrencyChanged?.Invoke(CurrentCurrency);
    }
    

    #endregion Currency --------------------------------------------------------------------------------

    
    #region Upgrades -----------------------------------------------------------------------------
    
    public void AddHealthUpgrade(SOUpgradeBase upgrade, int amount)
    {
        Upgrades.Add(upgrade);
        HealHealth(amount);
    }
    
    public void AddMaxShieldUpgrade(SOUpgradeBase upgrade, float amount)
    {
        Upgrades.Add(upgrade);
        MaxShield += amount;
        if (MaxShield > gameSettings.MaxPlayerShield)
        {
            MaxShield = gameSettings.MaxPlayerShield;
        }

        StartShieldRegen();
    }
    
    public void AddMagnetSizeUpgrade(SOUpgradeBase upgrade, float amount)
    {
        Upgrades.Add(upgrade);
        ResourceCollector.AddMagnetSizeUpgrade(amount);
    }

    public void AddWeaponUpgrade(SOUpgradeBase upgrade, SOWeaponUpgrade weaponUpgrade)
    {
        Upgrades.Add(upgrade);
        weaponSystem.AddWeaponUpgrade(weaponUpgrade);
    }
    
    public void AddMaxHeatUpgrade(SOUpgradeBase upgrade, float amount)
    {
        Upgrades.Add(upgrade);
        weaponSystem.AddMaxHeatUpgrade(amount);
    }

    public void AddDodgeUpgrade(SOUpgradeBase upgrade, int amount)
    {
        Upgrades.Add(upgrade);
        playerMovement.AddDodgeAccumulationUpgrade(amount);
    }
    
    public bool HasUpgrade(int itemID)
    {
        if (itemID == 0 || Upgrades.Count <= 0) return false;
        return Upgrades.Any(upgrade => upgrade.ItemID == itemID);
    }
    
    public SOWeaponUpgrade GetHighestWeaponUpgrade(SOWeaponData weaponData)
    {
        SOWeaponUpgrade highestUpgrade = null;
        int highestIndex = -1;
    
        for (int i = 0; i < weaponData.WeaponUpgrades.Count; i++)
        {
            var weaponUpgrade = weaponData.WeaponUpgrades[i];
            if (HasUpgrade(weaponUpgrade.ItemID))
            {
                if (i > highestIndex)
                {
                    highestIndex = i;
                    highestUpgrade = weaponUpgrade;
                }
            }
        }
    
        return highestUpgrade;
    }


    #endregion Upgrades -----------------------------------------------------------------------------
    
    
    #region Helper Methods --------------------------------------------------------------------------------------

    public bool ShieldActive()
    {
        return CurrentShield > 0;
    }
    
    public bool IsAlive()
    {
        return CurrentHealth > 0;
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
    
    
    public Transform GetStoreCameraLookAtTarget()
    {
        return storeCameraLookAtTarget ? storeCameraLookAtTarget : transform;
    }
    
    public Transform GetRandomCameraPosition()
    {
        if (!cameraPositions) return transform;
        
        int randomIndex = UnityEngine.Random.Range(0, cameraPositions.childCount);
        return cameraPositions.GetChild(randomIndex);
    }
    
    
    #endregion Helper Methods --------------------------------------------------------------------------------------
}