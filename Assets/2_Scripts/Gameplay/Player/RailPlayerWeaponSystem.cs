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
    [SerializeField] private List<WeaponInstance> weapons = new List<WeaponInstance>();
    
    [Foldout("Heat System")]
    [SerializeField, Min(0.1f)] private float timeBeforeRegen = 1f;
    [SerializeField, Min(0.1f)] private float heatRegenRate = 15f;
    [SerializeField, Min(0f)] private float heatReleasedOnDodge = 25f;
    [Header("Overheat")]
    [SerializeField, Min(0.1f)] private float overHeatCooldown = 3f;
    [SerializeField] private ControllerVibrationEffectSettings vibrationOnOverheatSettings = new ControllerVibrationEffectSettings();
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
    private WeaponInstance _previousActiveWeaponInstance;
    private Coroutine _overHeatCooldownRoutine;
    private bool _overHeated; 
    private bool _overHeatedCooldown;
    private bool _inMiniGameWindow;
    private bool _miniGameAttempted;
    private float _lastFireTimer;
    private float _currentHeat;
    private float _weaponFireRateCooldown;
    private float _weaponTime;
    private float _weaponAmmo;


    public bool IsInOverheat => _overHeated || _overHeatedCooldown;
    public bool AllowShooting { get; private set; }

    public float MaxWeaponHeat { get; private set; }

    public WeaponInstance ActiveWeaponInstance { get; private set; }


    public event Action<WeaponInstance> OnWeaponUsed;
    public event Action<WeaponInstance,WeaponInstance> OnActiveWeaponSwitchedEvent;
    public event Action<WeaponInstance,float> OnActiveWeaponCooldownUpdatedEvent;
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
            if (ActiveWeaponInstance == null && weapons.Count > 0)
            {
                ActiveWeaponInstance = weapons[0];
            }
            ActiveWeaponInstance?.OnWeaponSelected(AllowShooting);
        }
        else
        {
            ActiveWeaponInstance?.OnWeaponDeselected();
        }
    }
    
    private void OnDodge()
    {
        if (heatReleasedOnDodge <= 0 || _currentHeat <= 0 || _overHeated) return;
        
        _currentHeat -= heatReleasedOnDodge;
        
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
            ActiveWeaponInstance?.OnAimLocked();
        }
        else
        {
            ActiveWeaponInstance?.OnAimUnlocked();
        }
    }
    
    private void OnDeath()
    {
        AllowShooting = false;
        ActiveWeaponInstance?.OnWeaponDeselected();
    }
    
    private void OnHeatUpdated(float heat)
    {
        var normalizedHeat = heat / MaxWeaponHeat;
        ActiveWeaponInstance?.OnHeatChanged(normalizedHeat);
    }
    
    
    public void SetUp(SOWeaponData activeWeapon = null)
    {
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


        if (activeWeapon)
        {
            SetActiveWeapon(activeWeapon);
        }
        else if (weapons.Count > 0)
        {
            SetActiveWeapon(weapons[0]);
        }
        
        ActiveWeaponInstance?.OnWeaponSelected(AllowShooting);
        OnActiveWeaponSwitchedEvent?.Invoke(_previousActiveWeaponInstance, ActiveWeaponInstance);
        OnWeaponHeatResetEvent?.Invoke();
    }


    #region Weapon Usage ----------------------------------------------------------------------------------------------------

    private void FireActiveWeapon()
    {
        if (!AllowShooting || ActiveWeaponInstance == null || !(_weaponFireRateCooldown <= 0)) return;
        
        switch (ActiveWeaponInstance.CurrentWeaponData.WeaponLimitation)
        {
            case WeaponLimitation.AmmoBased:
                if (_weaponAmmo <= 0) return;
                _weaponAmmo -= 1;
                break;
                
            case WeaponLimitation.TimeBased:
                if (_weaponTime <= 0) return;
                break;
                
            case WeaponLimitation.HeatBased:
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
                
                _currentHeat += ActiveWeaponInstance.CurrentWeaponData.HeatPerShot;
                _lastFireTimer = timeBeforeRegen;
                if (_currentHeat >= MaxWeaponHeat)
                {
                    SetOverheating();
                }
                
                OnWeaponHeatUpdatedEvent?.Invoke(_currentHeat);
                break;
        }
        
        ActiveWeaponInstance.OnWeaponUsed(player);
        OnWeaponUsed?.Invoke(ActiveWeaponInstance);
        
        _weaponFireRateCooldown = ActiveWeaponInstance.CurrentWeaponData.FireRate;
        OnActiveWeaponCooldownUpdatedEvent?.Invoke(ActiveWeaponInstance, _weaponFireRateCooldown);
    }
    

    #endregion Weapon Usage ----------------------------------------------------------------------------------------------------
    

    #region Weapon Limiters ----------------------------------------------------------------------------------------------------

    private void UpdateFireRateCooldown()
    {
        if (_weaponFireRateCooldown > 0)
        {
            _weaponFireRateCooldown -= Time.deltaTime;
            OnActiveWeaponCooldownUpdatedEvent?.Invoke(ActiveWeaponInstance, _weaponFireRateCooldown);
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
        if (ActiveWeaponInstance != null && ActiveWeaponInstance.CurrentWeaponData.WeaponLimitation == WeaponLimitation.TimeBased)
        {
            if (_weaponTime > 0)
            {
                _weaponTime -= Time.deltaTime;
            }
            else if (_weaponTime <= 0)
            {
                // Switch back to start weapon (weapons[0]) when time runs out
                if (weapons.Count > 0)
                {
                    SetActiveWeapon(weapons[0]);
                }
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
        ActiveWeaponInstance?.OnWeaponOverheat();
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
    
    
    private void SetActiveWeapon(WeaponInstance newWeapon)
    {
        if (newWeapon == null) return;
        
        if (ActiveWeaponInstance == newWeapon)
        {
            _weaponFireRateCooldown = 0;
            switch (newWeapon.CurrentWeaponData.WeaponLimitation)
            {
                case WeaponLimitation.AmmoBased:
                    _weaponAmmo = newWeapon.CurrentWeaponData.AmmoLimit;
                    break;
                case WeaponLimitation.TimeBased:
                    _weaponTime = newWeapon.CurrentWeaponData.TimeLimit;
                    break;
            }
            ResetHeat();
            return;
        }
        
        // Disable the previous weapon if it is active
        if (ActiveWeaponInstance != null)
        {
            ActiveWeaponInstance.OnWeaponDeselected();
            _previousActiveWeaponInstance = ActiveWeaponInstance;
        }

        // Set the new Weapon
        ActiveWeaponInstance = newWeapon;
        _weaponFireRateCooldown = 0;
        switch (newWeapon.CurrentWeaponData.WeaponLimitation)
        {
            case WeaponLimitation.AmmoBased:
                _weaponAmmo = newWeapon.CurrentWeaponData.AmmoLimit;
                break;
            case WeaponLimitation.TimeBased:
                _weaponTime = newWeapon.CurrentWeaponData.TimeLimit;
                break;
        }
        ResetHeat();
        ActiveWeaponInstance.OnWeaponSelected(AllowShooting);
        weaponSwitchSfx?.Play(audioSource);
        OnActiveWeaponCooldownUpdatedEvent?.Invoke(ActiveWeaponInstance, _weaponFireRateCooldown);
        OnActiveWeaponSwitchedEvent?.Invoke(_previousActiveWeaponInstance, ActiveWeaponInstance);
    }
    
    
    public void SetActiveWeapon(SOWeaponData weaponData)
    {
        if (!weaponData) return;

        foreach (var weaponInfo in weapons)
        {
            if (weaponInfo.weaponData == weaponData)
            {
                SetActiveWeapon(weaponInfo);
                break;
            }
        }
    }

    public void AddWeaponUpgrade(SOWeaponUpgrade weaponUpgrade)
    {
        foreach (var weaponInstance in weapons)
        {
            if (weaponInstance.weaponData == weaponUpgrade.BaseWeapon)
            {
                weaponInstance.ApplyWeaponUpgrade(player);
                SetActiveWeapon(weaponInstance);
            }
        }
    }
    
    private void UpgradeCurrentWeapon()
    {
        if (ActiveWeaponInstance != null)
        {
            // Get the next upgrade for the active weapon
            SOWeaponUpgrade nextUpgrade = GetNextWeaponUpgrade(ActiveWeaponInstance.CurrentWeaponData);
            if (nextUpgrade)
            {
                // Add the upgrade to player's upgrade list first
                player.Upgrades.Add(nextUpgrade, 1);
                // Then apply the upgrade
                ActiveWeaponInstance.ApplyWeaponUpgrade(player);
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

    private void CheckAttackInputs()
    {
        if (_attackInputHeld)
        {
            FireActiveWeapon();
        }
        
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (weapons.Count > 1)
            {
                var weapon = weapons[1];
                if (weapon.CurrentWeaponData)
                {
                    SetActiveWeapon(weapon);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            if (weapons.Count > 2)
            {
                var weapon = weapons[2];
                if (weapon.CurrentWeaponData)
                {
                    SetActiveWeapon(weapon);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            if (weapons.Count > 3)
            {
                var weapon = weapons[3];
                if (weapon.CurrentWeaponData)
                {
                    SetActiveWeapon(weapon);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            // Switch back to start weapon (weapons[0])
            if (weapons.Count > 0)
            {
                SetActiveWeapon(weapons[0]);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            UpgradeCurrentWeapon();
            ResetHeat();
        }
    }

    #endregion Input Handling --------------------------------------------------------------------------------------
}