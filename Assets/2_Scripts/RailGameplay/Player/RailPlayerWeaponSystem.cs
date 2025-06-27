using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

[Serializable]
public class WeaponInfo
{
    public SOWeapon weaponData;
    public Transform weaponGfx;
    public Transform weaponReticle;
    public Transform[] weaponBarrels;


    public void OnWeaponSelected()
    {
        weaponGfx?.gameObject.SetActive(true);
        weaponReticle?.gameObject.SetActive(true);
    }

    public void OnWeaponDeselected()
    {
        weaponGfx?.gameObject.SetActive(false);
        weaponReticle?.gameObject.SetActive(false);
    }
}

public class RailPlayerWeaponSystem : MonoBehaviour
{
    [Header("Weapons Settings")]
    [Tooltip("If true, the player can use the base weapon even with a special weapon, using a different button.")]
    [SerializeField] private bool allowBaseWeaponWithSpecialWeapon = true;
    [Tooltip("Special weapons are permanent, and don't change after limit reached (heat, ammo, time)")]
    [SerializeField] private bool specialWeaponsArePermanent = true;
    [SerializeField] private List<WeaponInfo> weapons = new List<WeaponInfo>();
    
    [Foldout("Heat System")]
    [SerializeField, Min(0f)] private float maxHeat = 100f;
    [SerializeField, Min(0.1f)] private float timeBeforeRegen = 1f;
    [SerializeField, Min(0.1f)] private float heatRegenRate = 15f;
    [SerializeField] private bool switchingWeaponsResetsHeat = true;
    [Header("Overheat")]
    [SerializeField, Min(0.1f)] private float overHeatCooldown = 3f;
    [SerializeField] private bool overHeatMiniGame = true;
    [ShowIf("overHeatMiniGame")]
    [SerializeField] private float failHeat = 50f;
    [SerializeField] private bool randomizeWindow = true;
    [EndIf]
    [ShowIf("randomizeWindow")]
    [SerializeField] private Vector2 windowPositionRange = new Vector2(0.25f, 0.75f);
    [SerializeField] private Vector2 windowSizeRange = new Vector2(0.1f, 3f);
    [EndIf]
    [HideIf("randomizeWindow")]
    [SerializeField, Range(0, 1)] private float miniGameWindowPosition = 0.25f;
    [SerializeField, Range(0, 1)] private float miniGameWindow = 0.25f; 
    [EndIf]
    [Header("Dodge")]
    [SerializeField] private bool dodgeReleasesHeat = true;
    [ShowIf("dodgeReleasesHeat")]
    [SerializeField, Min(0f)] private float heatReleased = 25f;
    [EndIf]
    [EndFoldout]
    
    [Header("Reticles")]
    [SerializeField, Tooltip("How fast the reticle position smoothly moves to its target position")] private float reticleFollowSpeed = 25f;
    [SerializeField] private float reticleGrowSpeed = 5f;
    [SerializeField] private float reticleSizeMultiplier = 2f;
    [SerializeField, Range(0f, 1f)] private float smallReticleRange = 0.8f;

    
    [Header("References")]
    [SerializeField] private Transform smallReticle;
    [SerializeField] private Transform targetReticle;
    [SerializeField] private SOAudioEvent weaponSwitchSfx;
    [SerializeField] private SOAudioEvent weaponOverheatSfx;
    [SerializeField] private SOAudioEvent weaponHeatResetSfx;
    [SerializeField] private SOAudioEvent weaponHeatMiniGameSuccess;
    [SerializeField] private SOAudioEvent weaponHeatMiniGameFail;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming playerAiming;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;


    private bool _allowShooting;
    private bool _attackInputHeld;
    private bool _attack2InputHeld;
    private WeaponInfo _baseWeaponInfo;
    private WeaponInfo _currentSpecialWeaponInfo;
    private WeaponInfo _previousSpecialWeaponInfo;
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
    
    
    public bool IsOverHeated => _overHeated || _overHeatedCooldown;
    public float MaxWeaponHeat => maxHeat;
    public WeaponInfo BaseWeaponInfo => _baseWeaponInfo;
    public WeaponInfo CurrentSpecialWeaponInfo => _currentSpecialWeaponInfo;


    public event Action<WeaponInfo> OnWeaponFired;
    public event Action<WeaponInfo,WeaponInfo> OnSpecialWeaponSwitched;
    public event Action<WeaponInfo> OnSpecialWeaponDisabled;
    public event Action<WeaponInfo> OnBaseWeaponSwitched;
    public event Action<WeaponInfo,float> OnBaseWeaponCooldownUpdated;
    public event Action<WeaponInfo,float> OnSpecialWeaponCooldownUpdated;
    public event Action<float> OnWeaponHeatUpdated;
    public event Action OnWeaponOverheated;
    public event Action OnWeaponHeatReset;
    public event Action<float,float, float> OnWeaponHeatMiniGameWindowCreated;
    public event Action OnWeaponHeatMiniGameSucceeded;
    public event Action OnWeaponHeatMiniGameFailed;

    
    private void OnValidate() 
    { 
        this.ValidateRefs();
    
        // Clamp window ranges between 0 and 1
        windowPositionRange = new Vector2(
            Mathf.Clamp01(windowPositionRange.x), 
            Mathf.Clamp01(windowPositionRange.y)
        );
    
        windowSizeRange = new Vector2(
            Mathf.Clamp01(windowSizeRange.x), 
            Mathf.Clamp01(windowSizeRange.y)
        );
    
        // Ensure x is always less than or equal to y
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
        if (weapons.Count >= 0)
        {
            _baseWeaponFireRateCooldown = 0;
            _baseWeaponInfo = weapons[0];
            _baseWeaponInfo.OnWeaponSelected();
            OnBaseWeaponSwitched?.Invoke(_baseWeaponInfo);
        }
        
        _allowShooting = true;
    }
    

    private void OnEnable()
    {
        playerInput.OnAttackEvent += OnAttack;
        playerInput.OnAttack2Event += OnAttack2;
        playerMovement.OnDodge += OnDodge;
        
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged += OnStageChanged;
        }
    }
    
    private void OnDisable()
    {
        playerInput.OnAttackEvent -= OnAttack;
        playerInput.OnAttack2Event -= OnAttack2;
        playerMovement.OnDodge -= OnDodge;
        
        
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
        UpdateReticlesPosition();
    }
    

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        _allowShooting = stage.AllowPlayerShooting;
    }


    #region Weapon Usage ----------------------------------------------------------------------------------------------------

    private void FireActiveWeapon()
    {
        // If there is a special weapon selected, use it
        if (_currentSpecialWeaponInfo != null)
        {
            FireSpecialWeapon();
        }
        else
        {
            // If not, use the base weapon
            FireBaseWeapon();
        }
    }
    
    private void FireBaseWeapon()
    {
        if (_baseWeaponInfo == null || !(_baseWeaponFireRateCooldown <= 0)) return;
        
        if (_baseWeaponInfo.weaponData.WeaponLimitationType == WeaponLimitationType.HeatBased)
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
            
            _currentHeat += _baseWeaponInfo.weaponData.HeatPerShot;
            _lastFireTimer = timeBeforeRegen;
            if (_currentHeat >= maxHeat)
            {
                SetOverheating();
            }
            
            OnWeaponHeatUpdated?.Invoke(_currentHeat);
        }
            
            
        UseWeapon(_baseWeaponInfo);
        _baseWeaponFireRateCooldown = _baseWeaponInfo.weaponData.FireRate;
        OnBaseWeaponCooldownUpdated?.Invoke(_baseWeaponInfo,_baseWeaponFireRateCooldown);
    }

    private void FireSpecialWeapon()
    {
        if (_currentSpecialWeaponInfo == null || !(_specialWeaponFireRateCooldown <= 0)) return;
        
        
        switch (_currentSpecialWeaponInfo.weaponData.WeaponLimitationType)
        {
            case WeaponLimitationType.AmmoBased when _specialWeaponAmmo > 0:
                _specialWeaponAmmo -= 1;
                break;
            case WeaponLimitationType.AmmoBased when _specialWeaponAmmo <= 0:
            {
                if (!specialWeaponsArePermanent)
                {
                    DisableSpecialWeapon();
                }
                return;
            }
            case WeaponLimitationType.TimeBased when _specialWeaponTime <= 0:
                    
                if (!specialWeaponsArePermanent)
                {
                    DisableSpecialWeapon();
                }
                return;
            case WeaponLimitationType.HeatBased:
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

                _currentHeat += _currentSpecialWeaponInfo.weaponData.HeatPerShot;
                _lastFireTimer = timeBeforeRegen;
                if (_currentHeat >= maxHeat)
                {
                    SetOverheating();
                }
                    
                OnWeaponHeatUpdated?.Invoke(_currentHeat);
            }
                break;
        }
            
        UseWeapon(_currentSpecialWeaponInfo);
        _specialWeaponFireRateCooldown = _currentSpecialWeaponInfo.weaponData.FireRate;
        OnSpecialWeaponCooldownUpdated?.Invoke(CurrentSpecialWeaponInfo,_specialWeaponFireRateCooldown);
    }
    
    
    private void UseWeapon(WeaponInfo weaponInfo)
    {
        if (weaponInfo == null) return;
        
        
        weaponInfo.weaponData.Fire(player, weaponInfo.weaponBarrels);
        OnWeaponFired?.Invoke(weaponInfo);
        
    }

    #endregion Weapon Usage ----------------------------------------------------------------------------------------------------
    

    #region Weapon Limiters ----------------------------------------------------------------------------------------------------

    private void UpdateFireRateCooldown()
    {
        if (_baseWeaponFireRateCooldown > 0)
        {
            _baseWeaponFireRateCooldown -= Time.deltaTime;
            OnBaseWeaponCooldownUpdated?.Invoke(_baseWeaponInfo,_baseWeaponFireRateCooldown);
            
        }

        if (_specialWeaponFireRateCooldown > 0)
        {
            _specialWeaponFireRateCooldown -= Time.deltaTime;
            OnSpecialWeaponCooldownUpdated?.Invoke(_currentSpecialWeaponInfo,_specialWeaponFireRateCooldown);
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
                    OnWeaponHeatReset?.Invoke();
                }
                
                OnWeaponHeatUpdated?.Invoke(_currentHeat);
            }
            else
            {
                _lastFireTimer -= Time.deltaTime;
            }
        }
    }
    
    
    private void UpdateWeaponTime()
    {
        if (_currentSpecialWeaponInfo != null && _currentSpecialWeaponInfo.weaponData.WeaponLimitationType == WeaponLimitationType.TimeBased)
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
        _miniGameAttempted = false;
        weaponOverheatSfx?.Play(audioSource);
        OnWeaponOverheated?.Invoke();
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
        _currentHeat = 0;
        _lastFireTimer = 0;
        weaponHeatResetSfx?.Play(audioSource);
        OnWeaponHeatUpdated?.Invoke(_currentHeat);
        OnWeaponHeatReset?.Invoke();
    }
    
    private void HeatMiniGameSucceeded()
    {
        if (!overHeatMiniGame || _miniGameAttempted) return;
        
        _miniGameAttempted = true;
        _attackInputHeld = false;
        weaponHeatMiniGameSuccess?.Play(audioSource);
        OnWeaponHeatMiniGameSucceeded?.Invoke();
        ResetHeat();

    }
    
    private void HeatMiniGameFailed()
    {
        if (!overHeatMiniGame || _miniGameAttempted) return;

        _miniGameAttempted = true;
        _attackInputHeld = false;
        weaponHeatMiniGameFail?.Play(audioSource);
        _currentHeat += failHeat;
        OnWeaponHeatMiniGameFailed?.Invoke();
        if (_currentHeat >= maxHeat)
        {
            SetOverheating();
        }
        OnWeaponHeatUpdated?.Invoke(_currentHeat);
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
            OnWeaponHeatMiniGameWindowCreated?.Invoke(actualRegenTime, miniGameDuration, miniGameStartTime);
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
            OnWeaponHeatUpdated?.Invoke(_currentHeat);
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
        OnWeaponHeatUpdated?.Invoke(_currentHeat);
        weaponHeatResetSfx?.Play(audioSource);
    }
    


    #endregion Weapon Limiters ----------------------------------------------------------------------------------------------------
    
    
    #region Weapon Reticle -----------------------------------------------------------------------------------------------

    private void UpdateReticlesPosition()
    {
        if (!_allowShooting) return;

        if (_currentSpecialWeaponInfo != null && _currentSpecialWeaponInfo.weaponReticle)
        {
            _currentSpecialWeaponInfo.weaponReticle.position = Vector3.Lerp(_currentSpecialWeaponInfo.weaponReticle.position, playerAiming.AimWorldPosition.position, reticleFollowSpeed * Time.deltaTime);
            _currentSpecialWeaponInfo.weaponReticle.rotation = player.AlignToSplineDirection ? player.AimSplineRotation : Quaternion.identity;
        }
        else
        {
            if (_baseWeaponInfo != null && _baseWeaponInfo.weaponReticle)
            {
                _baseWeaponInfo.weaponReticle.position = Vector3.Lerp(_baseWeaponInfo.weaponReticle.position, playerAiming.AimWorldPosition.position, reticleFollowSpeed * Time.deltaTime);
                _baseWeaponInfo.weaponReticle.rotation = player.AlignToSplineDirection ? player.AimSplineRotation : Quaternion.identity;
            }
        }

        
        if (smallReticle)
        {
            Vector3 smallReticlePosition = Vector3.Lerp(transform.position, playerAiming.AimWorldPosition.position, smallReticleRange);
            smallReticle.position = Vector3.Lerp(smallReticle.position, smallReticlePosition, reticleFollowSpeed * Time.deltaTime);
            smallReticle.rotation = player.AlignToSplineDirection ? player.AimSplineRotation : Quaternion.identity;
        }

        if (targetReticle)
        {
            targetReticle.position = Vector3.Lerp(targetReticle.position, playerAiming.AimWorldPosition.position, reticleFollowSpeed * Time.deltaTime);
            targetReticle.rotation = player.AlignToSplineDirection ? player.AimSplineRotation : Quaternion.identity;
            Vector3 targetReticleSize = playerAiming.CurrentAimLockTarget ? (Vector3.one * reticleSizeMultiplier) : Vector3.one; 
            targetReticle.localScale = Vector3.Lerp(targetReticle.localScale, targetReticleSize, reticleGrowSpeed * Time.deltaTime);
        }
    }
    

    #endregion Weapon Reticle -----------------------------------------------------------------------------------------------
    
    
    #region Special Weapon Management --------------------------------------------------------------------------------------
    
    
    private void SetSpecialWeapon(WeaponInfo newWeapon)
    {
        if (newWeapon == null) return;
        
        // Disable the previous special Weapon if it is active
        if (_currentSpecialWeaponInfo != null)
        {
            _currentSpecialWeaponInfo.OnWeaponDeselected();
            _previousSpecialWeaponInfo = _currentSpecialWeaponInfo;
        }

        // Set the new Weapon
        _currentSpecialWeaponInfo = newWeapon;
        _specialWeaponFireRateCooldown = 0;
        switch (newWeapon.weaponData.WeaponLimitationType)
        {
            case WeaponLimitationType.AmmoBased:
                _specialWeaponAmmo = newWeapon.weaponData.AmmoLimit;
                break;
            case WeaponLimitationType.TimeBased:
                _specialWeaponTime = newWeapon.weaponData.TimeLimit;
                break;
        }
        if (switchingWeaponsResetsHeat) ResetHeat();
        newWeapon.OnWeaponSelected();
            
        weaponSwitchSfx?.Play(audioSource);
        OnSpecialWeaponCooldownUpdated?.Invoke(newWeapon,_specialWeaponFireRateCooldown);
        OnSpecialWeaponSwitched?.Invoke(_previousSpecialWeaponInfo, newWeapon);
    }
    
    
    public void SetSpecialWeapon(SOWeapon newWeapon)
    {
        if (!newWeapon) return;

        foreach (var weaponInfo in weapons.Where(weaponInfo => weaponInfo.weaponData == newWeapon))
        {
            SetSpecialWeapon(weaponInfo);
            break;
        }
    }

    
    [Button]
    private void DisableSpecialWeapon()
    {
        if (!Application.isPlaying || _currentSpecialWeaponInfo == null) return;
        
        _currentSpecialWeaponInfo.OnWeaponDeselected();
        _previousSpecialWeaponInfo = _currentSpecialWeaponInfo;
        _currentSpecialWeaponInfo = null;
        OnSpecialWeaponDisabled?.Invoke(_previousSpecialWeaponInfo);
    }
    

    #endregion Special Weapon Management --------------------------------------------------------------------------------------


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
        if (!allowBaseWeaponWithSpecialWeapon) return;
        
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
        
        if (_attack2InputHeld && allowBaseWeaponWithSpecialWeapon && _currentSpecialWeaponInfo != null)
        {
            FireBaseWeapon();
        }
        
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var weapon = weapons[1];
            if (weapon.weaponData)
            {
                SetSpecialWeapon(weapon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            var weapon = weapons[2];
            if (weapon.weaponData)
            {
                SetSpecialWeapon(weapon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            var weapon = weapons[3];
            if (weapon.weaponData)
            {
                SetSpecialWeapon(weapon);
            }
        }
    }

    #endregion Input Handling --------------------------------------------------------------------------------------

    
}
