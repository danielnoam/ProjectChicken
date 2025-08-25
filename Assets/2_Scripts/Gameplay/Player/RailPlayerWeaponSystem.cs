using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions;
using KBCore.Refs;
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
    [SerializeField] private bool allowStartWeaponWithSpecialWeapon;
    [Tooltip("Special weapons are permanent, and don't change after limit reached (heat, ammo, time)")]
    [SerializeField] private bool specialWeaponsArePermanent = true;
    [SerializeField] private List<WeaponInstance> weapons = new List<WeaponInstance>();
    
    [Foldout("Heat System")]
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
    
    
    [Header("References")]
    [SerializeField, Child(Flag.Editable)] private AudioSource audioSource;
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


    private bool _attackInputHeld;
    private bool _attack2InputHeld;
    private WeaponInstance _activeWeaponInstance;
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


    public bool IsInOverheat => _overHeated || _overHeatedCooldown;
    public bool AllowShooting { get; private set; }

    public float MaxWeaponHeat { get; private set; }

    public WeaponInstance BaseWeaponInstance { get; private set; }

    public WeaponInstance CurrentSpecialWeaponInstance { get; private set; }


    public event Action OnWeaponUsed;
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
    

    private void OnEnable()
    {
        OnWeaponHeatUpdatedEvent += OnHeatUpdated;
        playerInput.OnAttackEvent += OnAttack;
        playerInput.OnAttack2Event += OnAttack2;
        playerMovement.OnDodge += OnDodge;
        playerAiming.OnAimLockStateChange += OnAimLock;
        player.Health.OnDeath += OnDeath;
        
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged += OnStageChanged;
        }
    }
    

    private void OnDisable()
    {
        OnWeaponHeatUpdatedEvent -= OnHeatUpdated;
        playerInput.OnAttackEvent -= OnAttack;
        playerInput.OnAttack2Event -= OnAttack2;
        playerMovement.OnDodge -= OnDodge;
        playerAiming.OnAimLockStateChange -= OnAimLock;
        player.Health.OnDeath -= OnDeath;
        
        
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged -= OnStageChanged;
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
        
        AllowShooting = stage.AllowPlayerShootingAndAiming;
        OnAllowShootingChangedEvent?.Invoke(AllowShooting);

        if (AllowShooting)
        {
            if (_activeWeaponInstance == null)
            {
                if (CurrentSpecialWeaponInstance != null) _activeWeaponInstance = CurrentSpecialWeaponInstance;
                else if (BaseWeaponInstance != null) _activeWeaponInstance = BaseWeaponInstance;
            }
            _activeWeaponInstance?.OnWeaponSelected(AllowShooting);

        }
        else
        {
            _activeWeaponInstance?.OnWeaponDeselected();
        }
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


    
    private void OnAimLock(bool state, ChickenController target)
    {
        if (!AllowShooting) return;

        if (state)
        {
            _activeWeaponInstance?.OnAimLocked();
        }
        else
        {
            _activeWeaponInstance?.OnAimUnlocked();
        }
    }
    
    private void OnDeath()
    {
        AllowShooting = false;
        _activeWeaponInstance?.OnWeaponDeselected();
    }
    
    private void OnHeatUpdated(float heat)
    {
        var normalizedHeat = heat / MaxWeaponHeat;
        _activeWeaponInstance?.OnHeatChanged(normalizedHeat);
    }
    
    
    public void SetUp(WeaponInstance activeWeapon = null)
    {
        AllowShooting = true;
        _overHeated = false;
        _overHeatedCooldown = false;
        _inMiniGameWindow = false;
        _miniGameAttempted = false;
        _attackInputHeld = false;
        MaxWeaponHeat = player.GameSettings.BaseMaxHeat;
        _currentHeat = 0f;
        _lastFireTimer = 0f;
        
        
        foreach (var weapon in weapons)
        {
            weapon.SetUpWeaponInstance(player, controllerVibrationSource, impulseSource);
        }

        SetBaseWeapon(weapons[0]);


        // Set active weapon
        if (activeWeapon != null)
        {
            _activeWeaponInstance = activeWeapon;
            _activeWeaponInstance?.OnWeaponSelected(AllowShooting);
        }
        else
        {
            if (_activeWeaponInstance == null)
            {
                if (CurrentSpecialWeaponInstance != null) _activeWeaponInstance = CurrentSpecialWeaponInstance;
                else if (BaseWeaponInstance != null) _activeWeaponInstance = BaseWeaponInstance;
                
                _activeWeaponInstance?.OnWeaponDeselected();
            }
        }
        
        
        OnBaseWeaponSwitchedEvent?.Invoke(BaseWeaponInstance);
        OnSpecialWeaponSwitchedEvent?.Invoke(_previousSpecialWeaponInstance, CurrentSpecialWeaponInstance);
        OnWeaponHeatResetEvent?.Invoke();
        
    }


    #region Weapon Usage ----------------------------------------------------------------------------------------------------

    private void FireActiveWeapon()
    {
        if (!AllowShooting) return;
        
        if (CurrentSpecialWeaponInstance != null)
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
        if (BaseWeaponInstance == null || !(_baseWeaponFireRateCooldown <= 0)) return;
        
        if (BaseWeaponInstance.WeaponData.WeaponLimitation == WeaponLimitation.HeatBased)
        {
            if (IsInOverheat)
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
            
            _currentHeat += BaseWeaponInstance.WeaponData.HeatPerShot;
            _lastFireTimer = timeBeforeRegen;
            if (_currentHeat >= MaxWeaponHeat)
            {
                SetOverheating();
            }
            
            OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
        }

        if (_activeWeaponInstance != BaseWeaponInstance)
        {
            _activeWeaponInstance?.OnWeaponDeselected();
            _activeWeaponInstance = BaseWeaponInstance;
            _activeWeaponInstance.OnWeaponSelected(AllowShooting);
        }
        UseWeapon(_activeWeaponInstance);
        _baseWeaponFireRateCooldown = BaseWeaponInstance.WeaponData.FireRate;
        OnBaseWeaponCooldownUpdatedEvent?.Invoke(BaseWeaponInstance,_baseWeaponFireRateCooldown);
    }

    private void FireSpecialWeapon()
    {
        if (CurrentSpecialWeaponInstance == null || !(_specialWeaponFireRateCooldown <= 0)) return;
        
        
        switch (CurrentSpecialWeaponInstance.WeaponData.WeaponLimitation)
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

                if (IsInOverheat)
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
                    }

                    return;
                }

                _currentHeat += CurrentSpecialWeaponInstance.WeaponData.HeatPerShot;
                _lastFireTimer = timeBeforeRegen;
                if (_currentHeat >= MaxWeaponHeat)
                {
                    SetOverheating(); 
                }
                    
                OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
            }
                break;
        }

        if (_activeWeaponInstance != CurrentSpecialWeaponInstance)
        {
            _activeWeaponInstance?.OnWeaponDeselected();
            _activeWeaponInstance = CurrentSpecialWeaponInstance;
            _activeWeaponInstance.OnWeaponSelected(AllowShooting);
        }
        UseWeapon(_activeWeaponInstance);
        _specialWeaponFireRateCooldown = CurrentSpecialWeaponInstance.WeaponData.FireRate;
        OnSpecialWeaponCooldownUpdatedEvent?.Invoke(CurrentSpecialWeaponInstance,_specialWeaponFireRateCooldown);
    }
    
    

    private void UseWeapon(WeaponInstance weaponInstance)
    {
        if (weaponInstance == null) return;
        
        weaponInstance.OnWeaponUsed(player);
        OnWeaponUsed?.Invoke();
    }

    #endregion Weapon Usage ----------------------------------------------------------------------------------------------------
    

    #region Weapon Limiters ----------------------------------------------------------------------------------------------------

    private void UpdateFireRateCooldown()
    {
        if (_baseWeaponFireRateCooldown > 0)
        {
            _baseWeaponFireRateCooldown -= Time.deltaTime;
            OnBaseWeaponCooldownUpdatedEvent?.Invoke(BaseWeaponInstance,_baseWeaponFireRateCooldown);
            
        }

        if (_specialWeaponFireRateCooldown > 0)
        {
            _specialWeaponFireRateCooldown -= Time.deltaTime;
            OnSpecialWeaponCooldownUpdatedEvent?.Invoke(CurrentSpecialWeaponInstance,_specialWeaponFireRateCooldown);
        }
    }

    
    private void UpdateHeatRegeneration()
    {
        // Heat regeneration
        if (!IsInOverheat && _currentHeat > 0)
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
        if (CurrentSpecialWeaponInstance != null && CurrentSpecialWeaponInstance.WeaponData.WeaponLimitation == WeaponLimitation.TimeBased)
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
        _currentHeat = MaxWeaponHeat;
        _lastFireTimer = timeBeforeRegen;
        _overHeated = true;
        _overHeatedCooldown = false;
        _inMiniGameWindow = false;
        if (overHeatResetsGame) _miniGameAttempted = false;
        weaponOverheatSfx?.Play(audioSource);
        CurrentSpecialWeaponInstance?.OnWeaponOverheat();
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
        if (_currentHeat >= MaxWeaponHeat)
        {
            SetOverheating();
        }
        OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
    }
    
    private IEnumerator OverHeatCooldownRoutine()
    {
        float cooldownTime = overHeatCooldown * 0.4f;
        float baseRegenTime = overHeatCooldown * 0.6f;
        _currentHeat = MaxWeaponHeat;
        float heatToRegenerate = _currentHeat;
        if (heatToRegenerate <= 0.1f)
        {
            ResetHeat();
            yield break;
        }
        float actualRegenTime = Mathf.Max(0.1f, (heatToRegenerate / MaxWeaponHeat) * baseRegenTime);
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
    
    
    public void AddMaxHeatUpgrade(float amount)
    {
        MaxWeaponHeat += amount;
        if (MaxWeaponHeat > player.GameSettings.MaxHeat)
        {
            MaxWeaponHeat = player.GameSettings.MaxHeat;
        }
        
        ResetHeat();
    }


    


    #endregion Weapon Limiters ----------------------------------------------------------------------------------------------------
    

    #region Weapon Management --------------------------------------------------------------------------------------
    
    
    private void SetSpecialWeapon(WeaponInstance newWeapon)
    {
        if (newWeapon == null || CurrentSpecialWeaponInstance == newWeapon) return;
        
        // Disable the previous special Weapon if it is active
        if (CurrentSpecialWeaponInstance != null)
        {
            CurrentSpecialWeaponInstance.OnWeaponDeselected();
            _previousSpecialWeaponInstance = CurrentSpecialWeaponInstance;
        }

        // Set the new Weapon
        CurrentSpecialWeaponInstance = newWeapon;
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
        _activeWeaponInstance.OnWeaponSelected(AllowShooting);
        weaponSwitchSfx?.Play(audioSource);
        OnSpecialWeaponCooldownUpdatedEvent?.Invoke(_activeWeaponInstance,_specialWeaponFireRateCooldown);
        OnSpecialWeaponSwitchedEvent?.Invoke(_previousSpecialWeaponInstance, _activeWeaponInstance);
    }
    
    
    public void SetSpecialWeapon(SOWeaponData weaponData)
    {
        if (!weaponData) return;

        foreach (var weaponInfo in weapons)
        {
            if (weaponInfo.weaponData == weaponData)
            {
                SetSpecialWeapon(weaponInfo);
                break;
            }
        }
    }
    
    private void SetBaseWeapon(WeaponInstance weapon)
    {
        _baseWeaponFireRateCooldown = 0;
        BaseWeaponInstance = weapon;
        OnBaseWeaponSwitchedEvent?.Invoke(BaseWeaponInstance);
    }
    
    
    private void DisableSpecialWeapon()
    {
        if (!Application.isPlaying || CurrentSpecialWeaponInstance == null) return;
        
        CurrentSpecialWeaponInstance.OnWeaponDeselected();
        _previousSpecialWeaponInstance = CurrentSpecialWeaponInstance;
        CurrentSpecialWeaponInstance = null;
        OnSpecialWeaponDisabledEvent?.Invoke(_previousSpecialWeaponInstance);

        if (BaseWeaponInstance != null)
        {
            _activeWeaponInstance = BaseWeaponInstance;
            _activeWeaponInstance.OnWeaponSelected(AllowShooting);
        }
    }

    public void AddWeaponUpgrade(SOWeaponUpgrade weaponUpgrade)
    {
        foreach (var weaponInstance in weapons)
        {
            if (weaponInstance.weaponData == weaponUpgrade.BaseWeapon)
            {
                weaponInstance.ApplyWeaponUpgrade(player);
            }
        }
    }
    
    private void UpgradeCurrentWeapon()
    {
        if (CurrentSpecialWeaponInstance != null)
        {
            // Get the next upgrade for the special weapon
            SOWeaponUpgrade nextUpgrade = GetNextWeaponUpgrade(CurrentSpecialWeaponInstance.WeaponData);
            if (nextUpgrade)
            {
                // Add the upgrade to player's upgrade list first
                player.Upgrades.Add(nextUpgrade);
                // Then apply the upgrade
                CurrentSpecialWeaponInstance.ApplyWeaponUpgrade(player);
            }
        }
        else if (BaseWeaponInstance != null)
        {
            // Get the next upgrade for the base weapon
            SOWeaponUpgrade nextUpgrade = GetNextWeaponUpgrade(BaseWeaponInstance.WeaponData);
            if (nextUpgrade)
            {
                // Add the upgrade to player's upgrade list first
                player.Upgrades.Add(nextUpgrade);
                // Then apply the upgrade
                BaseWeaponInstance.ApplyWeaponUpgrade(player);
            }
        }
    }
    
    private SOWeaponUpgrade GetNextWeaponUpgrade(SOWeaponData weaponData)
    {
        if (!weaponData || weaponData.WeaponUpgrades == null || weaponData.WeaponUpgrades.Count == 0) return null;

        // Find the highest upgrade the player currently has
        SOWeaponUpgrade currentHighest = player.GetHighestWeaponUpgrade(weaponData);
    
        if (!currentHighest)
        {
            // Player has no upgrades for this weapon, return the first one
            return weaponData.WeaponUpgrades[0];
        }
    
        // Find the index of the current highest upgrade
        int currentIndex = weaponData.WeaponUpgrades.IndexOf(currentHighest);
    
        // Return the next upgrade if it exists
        if (currentIndex >= 0 && currentIndex < weaponData.WeaponUpgrades.Count - 1)
        {
            return weaponData.WeaponUpgrades[currentIndex + 1];
        }
    
        // No more upgrades available
        return null;
    }

    #endregion Weapon Management --------------------------------------------------------------------------------------


    #region Input Handling --------------------------------------------------------------------------------------

    
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!AllowShooting || !player.Health.IsAlive())
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
        
        if (!AllowShooting || !player.Health.IsAlive())
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
        
        if (_attack2InputHeld && allowStartWeaponWithSpecialWeapon && CurrentSpecialWeaponInstance != null)
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
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            UpgradeCurrentWeapon();
            ResetHeat();
        }
    }

    #endregion Input Handling --------------------------------------------------------------------------------------



}


