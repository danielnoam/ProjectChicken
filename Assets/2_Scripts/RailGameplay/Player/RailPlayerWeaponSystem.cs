using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using KBCore.Refs;
using PrimeTween;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;


[RequireComponent(typeof(RailPlayer))]
[RequireComponent(typeof(CinemachineImpulseSource))]
[RequireComponent(typeof(ControllerVibrationSource))]
public class RailPlayerWeaponSystem : MonoBehaviour
{
    [Header("Weapons Settings")]
    [Tooltip("If true, the player can use the start weapon with a special weapon, using a different button.")]
    [SerializeField] private bool allowStartWeaponWithSpecialWeapon = true;
    [Tooltip("Special weapons are permanent, and don't change after limit reached (heat, ammo, time)")]
    [SerializeField] private bool specialWeaponsArePermanent = true;
    [SerializeField] private List<WeaponInstance> weapons = new List<WeaponInstance>();
    
    [Foldout("Heat System")]
    [SerializeField, Min(0f)] private float maxHeat = 100f;
    [SerializeField, Min(0.1f)] private float timeBeforeRegen = 1f;
    [SerializeField, Min(0.1f)] private float heatRegenRate = 15f;
    [SerializeField] private bool switchingWeaponsResetsHeat = true;
    [Header("Overheat")]
    [SerializeField, Min(0.1f)] private float overHeatCooldown = 3f;
    [SerializeField] private bool overHeatMiniGame = true;
    [ShowIf("overHeatMiniGame")]
    [SerializeField] private bool overHeatResetsGame;
    [SerializeField] private float miniGameFailHeat = 50f;
    [SerializeField] private bool randomizeWindow = true;
    [EndIf]
    [ShowIf("randomizeWindow")]
    [SerializeField] private Vector2 windowPositionRange = new Vector2(0.25f, 0.75f);
    [SerializeField] private Vector2 windowSizeRange = new Vector2(0.1f, 3f);
    [EndIf]
    [HideIf("randomizeWindow")]
    [SerializeField, Range(0, 1)] private float miniGameWindowPosition = 0.23f;
    [SerializeField, Range(0, 1)] private float miniGameWindow = 0.415f; 
    [EndIf]
    [SerializeField] private ControllerVibrationEffectSettings vibrationOnOverheatSettings = new ControllerVibrationEffectSettings();
    [Header("Dodge")]
    [SerializeField] private bool dodgeReleasesHeat = true;
    [ShowIf("dodgeReleasesHeat")]
    [SerializeField, Min(0f)] private float heatReleased = 25f;
    [EndIf]
    [EndFoldout]
    
    [Foldout("Reticles")]
    [SerializeField] private float targetReticleAimLockSize = 2.5f;
    [SerializeField] private float targetReticleAimLockDuration = 0.6f;
    [SerializeField] private float targetReticleOverheatPunchStrength = 1f;
    [SerializeField] private float targetReticlePunchStrength = 0.2f;
    [SerializeField] private float targetReticlePunchDuration = 0.3f;
    [EndFoldout]
    


    
    [Header("References")]
    [SerializeField, Child(Flag.Editable)] private AudioSource audioSource;
    [SerializeField] private Transform reticleHolder;
    [SerializeField] private WeaponReticle targetReticle;
    [SerializeField] private SOAudioEvent weaponSwitchSfx;
    [SerializeField] private SOAudioEvent weaponOverheatSfx;
    [SerializeField] private SOAudioEvent weaponHeatResetSfx;
    [SerializeField] private SOAudioEvent weaponHeatMiniGameSuccess;
    [SerializeField] private SOAudioEvent weaponHeatMiniGameFail;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming playerAiming;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;
    [SerializeField, Self, HideInInspector] private CinemachineImpulseSource impulseSource;



    private bool _allowShooting;
    private bool _attackInputHeld;
    private bool _attack2InputHeld;
    private WeaponInstance _activeWeaponInstance;
    private WeaponInstance _baseWeaponInstance;
    private WeaponInstance _currentSpecialWeaponInstance;
    private WeaponInstance _previousSpecialWeaponInstance;
    private Coroutine _overHeatCooldownRoutine;
    private bool _overHeated; 
    private bool _overHeatedCooldown;
    private bool _inMiniGameWindow;
    private bool _miniGameAttempted;
    private float _lastFireTimer;
    private float _currentHeat;
    private float _baseWeaponFireRateCooldown;
    private float _specialWeaponFireRateCooldown;
    private float _specialWeaponTime;
    private float _specialWeaponAmmo;
    private readonly List<WeaponReticle> _rangingReticles = new List<WeaponReticle>();
    
    
    public bool IsOverHeated => _overHeated || _overHeatedCooldown;
    public float MaxWeaponHeat => maxHeat;
    public WeaponInstance BaseWeaponInstance => _baseWeaponInstance;
    public WeaponInstance CurrentSpecialWeaponInstance => _currentSpecialWeaponInstance;


    public event Action<WeaponInstance> OnWeaponUsedEvent;
    public event Action<WeaponInstance,WeaponInstance> OnSpecialWeaponSwitchedEvent;
    public event Action<WeaponInstance> OnSpecialWeaponDisabledEvent;
    public event Action<WeaponInstance> OnBaseWeaponSwitchedEvent;
    public event Action<WeaponInstance,float> OnBaseWeaponCooldownUpdatedEvent;
    public event Action<WeaponInstance,float> OnSpecialWeaponCooldownUpdatedEvent;
    public event Action<float> OnWeaponHeatUpdatedEvent;
    public event Action OnWeaponOverheatedEvent;
    public event Action OnWeaponHeatResetEvent;
    public event Action<float,float, float> OnWeaponHeatMiniGameWindowCreatedEvent;
    public event Action OnWeaponHeatMiniGameSucceededEvent;
    public event Action OnWeaponHeatMiniGameFailedEvent;
    public event Action<bool> OnAllowShootingChangedEvent;
    

    
    private void OnValidate() 
    { 
        this.ValidateRefs();
        
        windowPositionRange = new Vector2(
            Mathf.Clamp01(windowPositionRange.x), 
            Mathf.Clamp01(windowPositionRange.y)
        );
    
        windowSizeRange = new Vector2(
            Mathf.Clamp01(windowSizeRange.x), 
            Mathf.Clamp01(windowSizeRange.y)
        );
        
        if (windowPositionRange.x > windowPositionRange.y)
        {
            windowPositionRange = new Vector2(windowPositionRange.y, windowPositionRange.x);
        }
    
        if (windowSizeRange.x > windowSizeRange.y)
        {
            windowSizeRange = new Vector2(windowSizeRange.y, windowSizeRange.x);
        }
    }

    private void Awake()
    {
        foreach (var weapon in weapons)
        {
            weapon.SetUpWeaponInstance(controllerVibrationSource, impulseSource);
        }
        
        _allowShooting = true;

    }

    private void Start()
    {
        if (weapons.Count >= 0)
        {
            SetUpBaseWeapon(weapons[0]);
        }

        
        // Setup in a case there is no active level manager to set stage
        if (!player.LevelManager)
        {
            targetReticle?.Show();
            targetReticle?.ForceChangeAimLockSize(targetReticleAimLockSize);
            if (_activeWeaponInstance == null)
            {
                if (_currentSpecialWeaponInstance != null) _activeWeaponInstance = _currentSpecialWeaponInstance;
                else if (_baseWeaponInstance != null) _activeWeaponInstance = _baseWeaponInstance;
                
                _activeWeaponInstance?.OnWeaponSelected(_allowShooting);
            }
        }
    }

    private void OnEnable()
    {
        OnWeaponHeatUpdatedEvent += OnHeatUpdated;
        playerInput.OnAttackEvent += OnAttack;
        playerInput.OnAttack2Event += OnAttack2;
        playerMovement.OnDodge += OnDodge;
        playerAiming.OnAimLockStateChange += OnAimLock;
        player.OnDeath += OnDeath;
        
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged += OnStageChanged;
            player.LevelManager.OnRestartedFromSavePoint += RestartedFromSavePoint;
        }
    }
    

    private void OnDisable()
    {
        OnWeaponHeatUpdatedEvent -= OnHeatUpdated;
        playerInput.OnAttackEvent -= OnAttack;
        playerInput.OnAttack2Event -= OnAttack2;
        playerMovement.OnDodge -= OnDodge;
        playerAiming.OnAimLockStateChange -= OnAimLock;
        player.OnDeath -= OnDeath;
        
        
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged -= OnStageChanged;
            player.LevelManager.OnRestartedFromSavePoint -= RestartedFromSavePoint;
        }
    }


    private void Update()
    {
        CheckAttackInputs();
        UpdateFireRateCooldown();
        UpdateHeatRegeneration();
        UpdateWeaponTime();
    }
    

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        _allowShooting = stage.AllowPlayerShooting;
        OnAllowShootingChangedEvent?.Invoke(_allowShooting);

        if (_allowShooting)
        {
            targetReticle?.Show();
            targetReticle?.ForceChangeAimLockSize(targetReticleAimLockSize);
            
            if (_activeWeaponInstance == null)
            {
                if (_currentSpecialWeaponInstance != null) _activeWeaponInstance = _currentSpecialWeaponInstance;
                else if (_baseWeaponInstance != null) _activeWeaponInstance = _baseWeaponInstance;
            }
            _activeWeaponInstance?.OnWeaponSelected(_allowShooting);

        }
        else
        {
            targetReticle.Hide();
            _activeWeaponInstance?.OnWeaponDeselected();
        }
    }
    
    private void RestartedFromSavePoint(SavePointInformation savePoint)
    {
        if (savePoint == null) return;

        if (savePoint.PlayerSpecialWeapon)
        {
            SetSpecialWeapon(savePoint.PlayerSpecialWeapon);
        }
        else
        {
            ResetHeat();
        }
    }
    
    private void OnAimLock(bool state, ChickenController target)
    {
        if (!_allowShooting) return;

        if (state)
        {
            targetReticle?.EnableAimLockSize(targetReticleAimLockDuration);
            _activeWeaponInstance?.OnAimLocked();
        }
        else
        {
            targetReticle?.DisableAimLockSize(targetReticleAimLockDuration);
            _activeWeaponInstance?.OnAimUnlocked();
        }
    }
    
    private void OnDeath()
    {
        _allowShooting = false;
        targetReticle?.Hide();
        _activeWeaponInstance?.OnWeaponDeselected();
    }
    
    private void OnHeatUpdated(float heat)
    {
        var normalizedHeat = heat / maxHeat;
        targetReticle?.SetEmissionStrength(normalizedHeat);
        _activeWeaponInstance?.OnHeatChanged(normalizedHeat);
    }


    #region Weapon Usage ----------------------------------------------------------------------------------------------------

    private void FireActiveWeapon()
    {
        if (!_allowShooting) return;
        
        if (_currentSpecialWeaponInstance != null)
        {
            FireSpecialWeapon();
        }
        else
        {
            FireBaseWeapon();
        }
    }
    
    private void FireBaseWeapon()
    {
        if (_baseWeaponInstance == null || !(_baseWeaponFireRateCooldown <= 0)) return;
        
        if (_baseWeaponInstance.WeaponData.WeaponLimitation == WeaponLimitation.HeatBased)
        {
            if (IsOverHeated)
            { 
                if (overHeatMiniGame)
                {
                    if (_inMiniGameWindow)
                    {
                        HeatMiniGameSucceeded();
                    }
                    else if (_overHeatedCooldown)
                    {
                        HeatMiniGameFailed();
                    }
                }
                
                return;
            }
            
            _currentHeat += _baseWeaponInstance.WeaponData.HeatPerShot;
            _lastFireTimer = timeBeforeRegen;
            if (_currentHeat >= maxHeat)
            {
                SetOverheating();
            }
            
            OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
        }

        if (_activeWeaponInstance != _baseWeaponInstance)
        {
            _activeWeaponInstance?.OnWeaponDeselected();
            _activeWeaponInstance = _baseWeaponInstance;
            _activeWeaponInstance.OnWeaponSelected(_allowShooting);
        }
        UseWeapon(_activeWeaponInstance);
        _baseWeaponFireRateCooldown = _baseWeaponInstance.WeaponData.FireRate;
        OnBaseWeaponCooldownUpdatedEvent?.Invoke(_baseWeaponInstance,_baseWeaponFireRateCooldown);
    }

    private void FireSpecialWeapon()
    {
        if (_currentSpecialWeaponInstance == null || !(_specialWeaponFireRateCooldown <= 0)) return;
        
        
        switch (_currentSpecialWeaponInstance.WeaponData.WeaponLimitation)
        {
            case WeaponLimitation.AmmoBased when _specialWeaponAmmo > 0:
                _specialWeaponAmmo -= 1;
                break;
            case WeaponLimitation.AmmoBased when _specialWeaponAmmo <= 0:
            {
                if (!specialWeaponsArePermanent)
                {
                    DisableSpecialWeapon();
                }
                return;
            }
            case WeaponLimitation.TimeBased when _specialWeaponTime <= 0:
                    
                if (!specialWeaponsArePermanent)
                {
                    DisableSpecialWeapon();
                }
                return;
            case WeaponLimitation.HeatBased:
            {

                if (IsOverHeated)
                {
                    if (!specialWeaponsArePermanent)
                    {
                        DisableSpecialWeapon();
                        return;
                    } 
                    
                    if (overHeatMiniGame)
                    {
                        if (_inMiniGameWindow)
                        {
                            HeatMiniGameSucceeded();
                        }
                        else if (_overHeatedCooldown)
                        {
                            HeatMiniGameFailed();
                        }
                        return;
                    }

                    return;
                }

                _currentHeat += _currentSpecialWeaponInstance.WeaponData.HeatPerShot;
                _lastFireTimer = timeBeforeRegen;
                if (_currentHeat >= maxHeat)
                {
                    SetOverheating(); 
                }
                    
                OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
            }
                break;
        }

        if (_activeWeaponInstance != _currentSpecialWeaponInstance)
        {
            _activeWeaponInstance?.OnWeaponDeselected();
            _activeWeaponInstance = _currentSpecialWeaponInstance;
            _activeWeaponInstance.OnWeaponSelected(_allowShooting);
        }
        UseWeapon(_activeWeaponInstance);
        _specialWeaponFireRateCooldown = _currentSpecialWeaponInstance.WeaponData.FireRate;
        OnSpecialWeaponCooldownUpdatedEvent?.Invoke(CurrentSpecialWeaponInstance,_specialWeaponFireRateCooldown);
    }
    
    

    private void UseWeapon(WeaponInstance weaponInstance)
    {
        if (weaponInstance == null) return;
        
        weaponInstance.OnWeaponUsed(player);
        targetReticle?.PunchReticleSize(targetReticlePunchStrength, targetReticlePunchDuration);
    
        OnWeaponUsedEvent?.Invoke(weaponInstance);
    }

    #endregion Weapon Usage ----------------------------------------------------------------------------------------------------
    

    #region Weapon Limiters ----------------------------------------------------------------------------------------------------

    private void UpdateFireRateCooldown()
    {
        if (_baseWeaponFireRateCooldown > 0)
        {
            _baseWeaponFireRateCooldown -= Time.deltaTime;
            OnBaseWeaponCooldownUpdatedEvent?.Invoke(_baseWeaponInstance,_baseWeaponFireRateCooldown);
            
        }

        if (_specialWeaponFireRateCooldown > 0)
        {
            _specialWeaponFireRateCooldown -= Time.deltaTime;
            OnSpecialWeaponCooldownUpdatedEvent?.Invoke(_currentSpecialWeaponInstance,_specialWeaponFireRateCooldown);
        }
    }

    
    private void UpdateHeatRegeneration()
    {
        // Heat regeneration
        if (!IsOverHeated && _currentHeat > 0)
        {

            if (_lastFireTimer <= 0)
            {
                _currentHeat -= heatRegenRate * Time.deltaTime;
                
                if (_currentHeat <= 0)
                {
                    _currentHeat = 0;
                    weaponHeatResetSfx?.Play(audioSource);
                    OnWeaponHeatResetEvent?.Invoke();
                }
                
                OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
            }
            else
            {
                _lastFireTimer -= Time.deltaTime;
            }
        }
    }
    
    
    private void UpdateWeaponTime()
    {
        if (_currentSpecialWeaponInstance != null && _currentSpecialWeaponInstance.WeaponData.WeaponLimitation == WeaponLimitation.TimeBased)
        {

            if (_specialWeaponTime > 0)
            {
                _specialWeaponTime -= Time.deltaTime;
            }
            else if (_specialWeaponTime <= 0 && !specialWeaponsArePermanent)
            {
                DisableSpecialWeapon();
            }
        }
    }
    
    
    
    private void SetOverheating()
    {
        if (_overHeatCooldownRoutine != null)
        {
            StopCoroutine(_overHeatCooldownRoutine);
        }
    
        _overHeatCooldownRoutine = StartCoroutine(OverHeatCooldownRoutine());
        _currentHeat = maxHeat;
        _lastFireTimer = timeBeforeRegen;
        _overHeated = true;
        _overHeatedCooldown = false;
        _inMiniGameWindow = false;
        if (overHeatResetsGame) _miniGameAttempted = false;
        weaponOverheatSfx?.Play(audioSource);
        _currentSpecialWeaponInstance?.OnWeaponOverheat();
        targetReticle?.PunchReticleSize(targetReticleOverheatPunchStrength, targetReticlePunchDuration);
        targetReticle?.SetEmissionStrength(_currentHeat);
        controllerVibrationSource.Vibrate(vibrationOnOverheatSettings);
        
        OnWeaponOverheatedEvent?.Invoke();
    }

    private void ResetHeat()
    {
        if (_overHeatCooldownRoutine != null)
        {
            StopCoroutine(_overHeatCooldownRoutine);
            _overHeatCooldownRoutine = null;
        }
        
        _overHeated = false;
        _overHeatedCooldown = false;
        _inMiniGameWindow = false;
        _miniGameAttempted = false;
        if (_currentHeat > 0) weaponHeatResetSfx?.Play(audioSource);
        _currentHeat = 0;
        _lastFireTimer = 0;
        targetReticle?.SetEmissionStrength(_currentHeat);
        OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
        OnWeaponHeatResetEvent?.Invoke();
    }
    
    private void HeatMiniGameSucceeded()
    {
        if (!overHeatMiniGame || _miniGameAttempted) return;
        
        _miniGameAttempted = true;
        _attackInputHeld = false;
        weaponHeatMiniGameSuccess?.Play(audioSource);
        OnWeaponHeatMiniGameSucceededEvent?.Invoke();
        ResetHeat();

    }
    
    private void HeatMiniGameFailed()
    {
        if (!overHeatMiniGame || _miniGameAttempted) return;

        _miniGameAttempted = true;
        _attackInputHeld = false;
        weaponHeatMiniGameFail?.Play(audioSource);
        _currentHeat += miniGameFailHeat;
        OnWeaponHeatMiniGameFailedEvent?.Invoke();
        if (_currentHeat >= maxHeat)
        {
            SetOverheating();
        }
        OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
    }
    
    private IEnumerator OverHeatCooldownRoutine()
    {
        float cooldownTime = overHeatCooldown * 0.4f;
        float baseRegenTime = overHeatCooldown * 0.6f;
        _currentHeat = maxHeat;
        float heatToRegenerate = _currentHeat;
        if (heatToRegenerate <= 0.1f)
        {
            ResetHeat();
            yield break;
        }
        float actualRegenTime = Mathf.Max(0.1f, (heatToRegenerate / maxHeat) * baseRegenTime);
        float regenRate = heatToRegenerate / actualRegenTime;
        
        float miniGameDuration;
        float miniGameStartTime;

        if (!randomizeWindow)
        {
            miniGameDuration = actualRegenTime * miniGameWindow;
            miniGameStartTime = actualRegenTime * (1f - miniGameWindowPosition);
        }
        else
        {
            float randomWindowSize = UnityEngine.Random.Range(windowSizeRange.x, windowSizeRange.y);
            float randomWindowPosition = UnityEngine.Random.Range(windowPositionRange.x, windowPositionRange.y);

            miniGameDuration = actualRegenTime * randomWindowSize;
            miniGameStartTime = actualRegenTime * (1f - randomWindowPosition);
        }
        
        float miniGameEndTime = miniGameStartTime - miniGameDuration;
        if (miniGameEndTime < 0)
        {
            miniGameDuration = miniGameStartTime;
        }

        if (overHeatMiniGame)
        {
            if (overHeatResetsGame || !_miniGameAttempted)
            {
                OnWeaponHeatMiniGameWindowCreatedEvent?.Invoke(actualRegenTime, miniGameDuration, miniGameStartTime);
            }
        }
        
        // Cooldown phase
        while (cooldownTime > 0)
        {
            cooldownTime -= Time.deltaTime;
            yield return null;
        }

        // Regen phase
        while (_currentHeat > 0)
        {
            _overHeated = false;
            _overHeatedCooldown = true;

            if (overHeatMiniGame && _overHeatedCooldown)
            {
                // Prevent division by zero
                if (heatToRegenerate > 0)
                {
                    float heatPercentage = _currentHeat / heatToRegenerate;
                    float currentTimeEquivalent = heatPercentage * actualRegenTime;
        
                    bool miniGameActive = currentTimeEquivalent <= miniGameStartTime && 
                                          currentTimeEquivalent > (miniGameStartTime - miniGameDuration);
                    _inMiniGameWindow = miniGameActive;
                }
            }

            _currentHeat -= regenRate * Time.deltaTime;
            OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
            yield return null;
        }

        ResetHeat();
    }

    private void OnDodge()
    {
        if (!dodgeReleasesHeat || _currentHeat <= 0 || _overHeated) return;
        
        _currentHeat -= heatReleased;
        
        if (_currentHeat < 0)
        {
            _currentHeat = 0;
        }
        OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
        weaponHeatResetSfx?.Play(audioSource);
    }
    
    
    


    #endregion Weapon Limiters ----------------------------------------------------------------------------------------------------
    

    #region Weapon Management --------------------------------------------------------------------------------------
    
    
    private void SetSpecialWeapon(WeaponInstance newWeapon)
    {
        if (newWeapon == null || _currentSpecialWeaponInstance == newWeapon) return;
        
        // Disable the previous special Weapon if it is active
        if (_currentSpecialWeaponInstance != null)
        {
            _currentSpecialWeaponInstance.OnWeaponDeselected();
            _previousSpecialWeaponInstance = _currentSpecialWeaponInstance;
        }

        // Set the new Weapon
        _currentSpecialWeaponInstance = newWeapon;
        _specialWeaponFireRateCooldown = 0;
        switch (newWeapon.WeaponData.WeaponLimitation)
        {
            case WeaponLimitation.AmmoBased:
                _specialWeaponAmmo = newWeapon.WeaponData.AmmoLimit;
                break;
            case WeaponLimitation.TimeBased:
                _specialWeaponTime = newWeapon.WeaponData.TimeLimit;
                break;
        }
        if (switchingWeaponsResetsHeat) ResetHeat();
        _activeWeaponInstance?.OnWeaponDeselected();
        _activeWeaponInstance = newWeapon;
        _activeWeaponInstance.OnWeaponSelected(_allowShooting);
        weaponSwitchSfx?.Play(audioSource);
        OnSpecialWeaponCooldownUpdatedEvent?.Invoke(newWeapon,_specialWeaponFireRateCooldown);
        OnSpecialWeaponSwitchedEvent?.Invoke(_previousSpecialWeaponInstance, newWeapon);
    }
    
    
    public void SetSpecialWeapon(SOWeaponData weaponData)
    {
        if (!weaponData) return;

        foreach (var weaponInfo in weapons)
        {
            if (weaponInfo.baseWeaponData == weaponData)
            {
                SetSpecialWeapon(weaponInfo);
                break;
            }
        }
    }
    
    private void SetUpBaseWeapon(WeaponInstance weapon)
    {
        _baseWeaponFireRateCooldown = 0;
        _baseWeaponInstance = weapon;
        OnBaseWeaponSwitchedEvent?.Invoke(_baseWeaponInstance);
    }

    
    private void DisableSpecialWeapon()
    {
        if (!Application.isPlaying || _currentSpecialWeaponInstance == null) return;
        
        _currentSpecialWeaponInstance.OnWeaponDeselected();
        _previousSpecialWeaponInstance = _currentSpecialWeaponInstance;
        _currentSpecialWeaponInstance = null;
        OnSpecialWeaponDisabledEvent?.Invoke(_previousSpecialWeaponInstance);

        if (_baseWeaponInstance != null)
        {
            _activeWeaponInstance = _baseWeaponInstance;
            _activeWeaponInstance.OnWeaponSelected(_allowShooting);
        }
    }
    

    #endregion Weapon Management --------------------------------------------------------------------------------------


    #region Input Handling --------------------------------------------------------------------------------------

    
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!_allowShooting || !player.IsAlive())
        {
            _attackInputHeld = false;
            return;
        }
        
        if (context.started)
        {
            _attackInputHeld = true;
            FireActiveWeapon();
        }
        else if (context.canceled)
        {
            _attackInputHeld = false;
        }
    }
    
    private void OnAttack2(InputAction.CallbackContext context)
    {
        if (!allowStartWeaponWithSpecialWeapon) return;
        
        if (!_allowShooting || !player.IsAlive())
        {
            _attack2InputHeld = false;
            return;
        }
        
        if (context.started)
        {
            _attack2InputHeld = true;
        }
        else if (context.canceled)
        {
            _attack2InputHeld = false;
        }
    }

    private void CheckAttackInputs()
    {
        if (_attackInputHeld)
        {
            FireActiveWeapon();
        }
        
        if (_attack2InputHeld && allowStartWeaponWithSpecialWeapon && _currentSpecialWeaponInstance != null)
        {
            FireBaseWeapon();
        }
        
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var weapon = weapons[1];
            if (weapon.WeaponData)
            {
                SetSpecialWeapon(weapon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            var weapon = weapons[2];
            if (weapon.WeaponData)
            {
                SetSpecialWeapon(weapon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            var weapon = weapons[3];
            if (weapon.WeaponData)
            {
                SetSpecialWeapon(weapon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            DisableSpecialWeapon();
        }
    }

    #endregion Input Handling --------------------------------------------------------------------------------------

    
}


