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
    public Transform weaponGfx;
    public Transform weaponReticle;
    public Transform[] projectileSpawnPoints;
}

public class RailPlayerWeaponSystem : MonoBehaviour
{
    
    [Header("Weapon System Settings")]
    [Tooltip("If true, the player can use the base weapon even with a special weapon, using a different button.")]
    [SerializeField] private bool allowBaseWeaponWithSpecialWeapon = true;
    [SerializeField] private bool specialWeaponsArePermanent = true;
    
    [Header("Heat System")]
    [SerializeField, Min(0f)] private float maxHeat = 100f;
    [SerializeField, Min(0.1f)] private float overHeatCooldown = 3f;
    [SerializeField, Min(0.1f)] private float timeBeforeRegen = 1f;
    [SerializeField, Min(0.1f)] private float heatRegenRate = 4f;
    [SerializeField] private bool switchingWeaponsResetsHeat = true;
    [SerializeField] private bool dodgeReleasesHeat = true;
    [SerializeField, Min(0f), ShowIf("dodgeReleasesHeat")] private float heatReleased = 25f;[EndIf]
    
    [Header("Weapons")]
    [SerializeField] private SOWeapon baseWeapon;
    [SerializeField] private SerializedDictionary<SOWeapon, WeaponInfo> weapons = new SerializedDictionary<SOWeapon, WeaponInfo>();
    
    [Header("SFXs")]
    [SerializeField] private SOAudioEvent weaponSwitchSfx;
    [SerializeField] private SOAudioEvent weaponOverheatSfx;
    [SerializeField] private SOAudioEvent weaponHeatResetSfx;
    
    [Header("References")]
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming playerAiming;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;
    
    

    private bool _attackInputHeld;
    private bool _attack2InputHeld;
    private SOWeapon _previousSpecialWeapon;
    private SOWeapon _currentSpecialWeapon;
    private WeaponInfo _baseWeaponInfo;
    private WeaponInfo _currentSpecialWeaponInfo;
    private Coroutine _overHeatCooldownRoutine;
    private bool _isOverHeated;
    private float _lastFireTimer;
    private float _currentHeat;
    private float _baseWeaponFireRateCooldown;
    private float _specialWeaponFireRateCooldown;
    private float _specialWeaponTime;
    private float _specialWeaponAmmo;
    private bool AllowShooting => player.IsAlive() && (!player.LevelManager || !player.LevelManager.CurrentStage ||
                                                       player.LevelManager.CurrentStage.AllowPlayerShooting);
    
    
    public bool IsOverHeated => _isOverHeated;
    public SOWeapon BaseWeapon => baseWeapon;
    public SOWeapon CurrentSpecialWeapon => _currentSpecialWeapon;
    public float CurrentHeat => _currentHeat;
    public float MaxWeaponHeat => maxHeat;
    public float SpecialWeaponTime => _specialWeaponTime;
    public float SpecialWeaponAmmo => _specialWeaponAmmo;

    public event Action<SOWeapon> OnWeaponUsed;
    public event Action<SOWeapon,SOWeapon,WeaponInfo> OnSpecialWeaponSwitched;
    public event Action<SOWeapon,float> OnBaseWeaponCooldownUpdated;
    public event Action<SOWeapon,float> OnSpecialWeaponCooldownUpdated;
    public event Action<float> OnWeaponHeatUpdated;
    public event Action OnWeaponOverheated;
    public event Action OnWeaponHeatReset;

    
    private void OnValidate() { this.ValidateRefs(); }

    private void Awake()
    {
        if (baseWeapon)
        {
            _baseWeaponInfo = weapons[baseWeapon];
            _baseWeaponInfo.weaponGfx?.gameObject.SetActive(true);
            _baseWeaponFireRateCooldown = 0;
        }
    }

    private void OnEnable()
    {
        playerInput.OnAttackEvent += OnAttack;
        playerInput.OnAttack2Event += OnAttack2;
        playerMovement.OnDodge += OnDodge;
    }
    
    private void OnDisable()
    {
        playerInput.OnAttackEvent -= OnAttack;
        playerInput.OnAttack2Event -= OnAttack2;
        playerMovement.OnDodge -= OnDodge;
    }


    
    
    private void Update()
    {
        UpdateFireRateCooldown();
        UpdateHeatRegeneration();
        UpdateWeaponTime();
        CheckAttackInputs();
    }



    #region Weapon Usage ----------------------------------------------------------------------------------------------------

    private void FireActiveWeapon()
    {
        // If there is a special weapon selected, use it
        if (_currentSpecialWeapon)
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
        if (!baseWeapon || !(_baseWeaponFireRateCooldown <= 0)) return;
        
        
        
        if (baseWeapon.WeaponLimitationType == WeaponLimitationType.HeatBased)
        {
            if (_isOverHeated)
            { 
                return;
            }
            
            _currentHeat += baseWeapon.HeatPerShot;
            _lastFireTimer = timeBeforeRegen;
            if (_currentHeat >= maxHeat)
            {
                SetOverheating();
            }
            
            OnWeaponHeatUpdated?.Invoke(_currentHeat);
        }
            
            
        UseWeapon(baseWeapon, _baseWeaponInfo);
        _baseWeaponFireRateCooldown = baseWeapon.FireRate;
        OnBaseWeaponCooldownUpdated?.Invoke(baseWeapon,_baseWeaponFireRateCooldown);
    }

    private void FireSpecialWeapon()
    {
        if (!_currentSpecialWeapon || !(_specialWeaponFireRateCooldown <= 0)) return;
        
        
        switch (_currentSpecialWeapon.WeaponLimitationType)
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

                if (_isOverHeated)
                {
                    if (!specialWeaponsArePermanent)
                    {
                        DisableSpecialWeapon();
                    }
                    return;
                }
                else
                {
                    _currentHeat += _currentSpecialWeapon.HeatPerShot;
                    _lastFireTimer = timeBeforeRegen;
                    if (_currentHeat >= maxHeat)
                    {
                        SetOverheating();
                    }
                    
                    OnWeaponHeatUpdated?.Invoke(_currentHeat);
                }

            }
                break;
        }
            
        UseWeapon(_currentSpecialWeapon, _currentSpecialWeaponInfo);
        _specialWeaponFireRateCooldown = _currentSpecialWeapon.FireRate;
        OnSpecialWeaponCooldownUpdated?.Invoke(CurrentSpecialWeapon,_specialWeaponFireRateCooldown);
    }
    
    
    private void UseWeapon(SOWeapon weapon, WeaponInfo weaponInfo)
    {
        if (!weapon || weaponInfo == null) return;
        
        
        weapon.Fire(player, weaponInfo.projectileSpawnPoints);
        OnWeaponUsed?.Invoke(weapon);
        
    }

    #endregion Weapon Usage ----------------------------------------------------------------------------------------------------
    

    #region Weapon Limiters ----------------------------------------------------------------------------------------------------

    private void UpdateFireRateCooldown()
    {
        if (_baseWeaponFireRateCooldown > 0)
        {
            _baseWeaponFireRateCooldown -= Time.deltaTime;
            OnBaseWeaponCooldownUpdated?.Invoke(baseWeapon,_baseWeaponFireRateCooldown);
            
        }

        if (_specialWeaponFireRateCooldown > 0)
        {
            _specialWeaponFireRateCooldown -= Time.deltaTime;
            OnSpecialWeaponCooldownUpdated?.Invoke(_currentSpecialWeapon,_specialWeaponFireRateCooldown);
        }
    }

    
    private void UpdateHeatRegeneration()
    {
        // Heat regeneration
        if (!_isOverHeated && _currentHeat > 0)
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
        if (_currentSpecialWeapon && _currentSpecialWeapon.WeaponLimitationType == WeaponLimitationType.TimeBased)
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
        _overHeatCooldownRoutine = StartCoroutine(OverHeatCooldownRoutine());
        _currentHeat = maxHeat;
        _lastFireTimer = timeBeforeRegen;
        _isOverHeated = true;
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
        
        _isOverHeated = false;
        _currentHeat = 0;
        _lastFireTimer = 0;
        weaponHeatResetSfx?.Play(audioSource);
        OnWeaponHeatUpdated?.Invoke(_currentHeat);
        OnWeaponHeatReset?.Invoke();
    }
    
    private IEnumerator OverHeatCooldownRoutine()
    {
        float regenTime = overHeatCooldown * 0.7f;
        float cooldownTime = overHeatCooldown * 0.3f;
        float regenRate = maxHeat / regenTime;

        
        while (cooldownTime > 0)
        {
            cooldownTime -= Time.deltaTime;
            yield return null;
        }
        
        while (regenTime > 0 || _currentHeat > 0)
        {
            regenTime -= Time.deltaTime;
            _currentHeat -= regenRate * Time.deltaTime;
            OnWeaponHeatUpdated?.Invoke(_currentHeat);
            yield return null;
        }
        
        ResetHeat();
    }

    private void OnDodge()
    {
        if (_isOverHeated || !dodgeReleasesHeat || _currentHeat == 0) return;
        
        _currentHeat -= heatReleased;
        
        if (_currentHeat < 0)
        {
            _currentHeat = 0;
        }
        OnWeaponHeatUpdated?.Invoke(_currentHeat);
        weaponHeatResetSfx?.Play(audioSource);
    }
    


    #endregion Weapon Limiters ----------------------------------------------------------------------------------------------------
    
    
    #region Special Weapon Management --------------------------------------------------------------------------------------


    public void SelectSpecialWeapon(SOWeapon weapon)
    {
        SetSpecialWeapon(weapon);
    }
    
    
    
    private void SetSpecialWeapon(SOWeapon newWeapon)
    {
        if (!newWeapon) return;
        
        // Find the Weapon in the dictionary
        if (weapons.TryGetValue(newWeapon, out var weaponInfo))
        {
            // Disable the previous special Weapon if it is active
            if (_currentSpecialWeapon)
            {
                _currentSpecialWeaponInfo?.weaponGfx?.gameObject.SetActive(false);
                _previousSpecialWeapon = _currentSpecialWeapon;
            }

            // Set the new Weapon
            _currentSpecialWeapon = newWeapon;
            _currentSpecialWeaponInfo = weaponInfo;
            _specialWeaponFireRateCooldown = 0;
            if (newWeapon.WeaponLimitationType == WeaponLimitationType.AmmoBased) { _specialWeaponAmmo = newWeapon.AmmoLimit;}
            else if (newWeapon.WeaponLimitationType == WeaponLimitationType.TimeBased) { _specialWeaponTime = newWeapon.TimeLimit;}
            weaponInfo.weaponGfx?.gameObject.SetActive(true);
            weaponSwitchSfx?.Play(audioSource);
            if (switchingWeaponsResetsHeat)
            {
                ResetHeat();
            }
            
            OnSpecialWeaponCooldownUpdated?.Invoke(newWeapon,_specialWeaponFireRateCooldown);
            OnSpecialWeaponSwitched?.Invoke(_previousSpecialWeapon,newWeapon, weaponInfo);
        }
    }


    
    
    [Button]
    private void SelectRandomSpecialWeapon()
    {
        if (!Application.isPlaying || weapons.Count <= 0) return;
        
        // Create a list with all weapons
        List<SOWeapon> specialWeaponsList = new List<SOWeapon>();
        foreach (var weapon in weapons)
        {
            specialWeaponsList.Add(weapon.Key);
        }
        
        // Select a random special Weapon from the list,Skip the first (base) Weapon
        SOWeapon randomWeapon = specialWeaponsList[UnityEngine.Random.Range(1, specialWeaponsList.Count)];
        
        SetSpecialWeapon(randomWeapon);
    }
    
    
    [Button]
    private void DisableSpecialWeapon()
    {
        if (!Application.isPlaying || !_currentSpecialWeapon) return;
        
        _currentSpecialWeaponInfo?.weaponGfx?.gameObject.SetActive(false);
        _previousSpecialWeapon = _currentSpecialWeapon;
        _currentSpecialWeapon = null;
    }

    
    
    


    #endregion Special Weapon Management --------------------------------------------------------------------------------------


    #region Input Handling --------------------------------------------------------------------------------------

    
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!AllowShooting)
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
        
        if (!AllowShooting)
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
        
        if (_attack2InputHeld && allowBaseWeaponWithSpecialWeapon && _currentSpecialWeapon)
        {
            FireBaseWeapon();
        }
        
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var secondWeapon = weapons.Keys.Skip(1).FirstOrDefault();
            if (secondWeapon)
            {
                SetSpecialWeapon(secondWeapon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            var secondWeapon = weapons.Keys.Skip(2).FirstOrDefault();
            if (secondWeapon)
            {
                SetSpecialWeapon(secondWeapon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            var secondWeapon = weapons.Keys.Skip(3).FirstOrDefault();
            if (secondWeapon)
            {
                SetSpecialWeapon(secondWeapon);
            }

        }
    }

    #endregion Input Handling --------------------------------------------------------------------------------------

    
}
