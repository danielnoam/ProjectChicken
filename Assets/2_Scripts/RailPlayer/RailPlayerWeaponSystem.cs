using System;
using System.Collections.Generic;
using System.Linq;
using Core.Attributes;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

[Serializable]
public class WeaponInfo
{
    public Transform weaponGfx;
    public Transform[] projectileSpawnPoints;
}

public class RailPlayerWeaponSystem : MonoBehaviour
{
    
    [Header("Weapon System Settings")]
    [Tooltip("If true, the player can use the base weapon even with a special weapon, using a different button.")]
    [SerializeField] private bool allowBaseWeaponWithSpecialWeapon = true;
    [SerializeField] private SOWeapon baseWeapon;
    [SerializeField] private SerializedDictionary<SOWeapon, WeaponInfo> weapons = new SerializedDictionary<SOWeapon, WeaponInfo>();
    
    [Header("SFXs")]
    [SerializeField] private SOAudioEvent weaponSwitchSfx;
    
    [Header("References")]
    [SerializeField, Self] private RailPlayer player;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    [SerializeField, Self] private AudioSource audioSource;
    
    private bool _attackInputHeld;
    private bool _attack2InputHeld;
    private SOWeapon _previousSpecialWeapon;
    private SOWeapon _currentSpecialWeapon;
    private WeaponInfo _baseWeaponInfo;
    private WeaponInfo _currentSpecialWeaponInfo;
    private float _baseWeaponFireRateCooldown;
    private float _specialWeaponFireRateCooldown;
    private float _specialWeaponTime;
    private float _specialWeaponAmmo;


    public SOWeapon BaseWeapon => baseWeapon;
    public SOWeapon CurrentSpecialWeapon => _currentSpecialWeapon;
    public float SpecialWeaponTime => _specialWeaponTime;
    public float SpecialWeaponAmmo => _specialWeaponAmmo;

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
    }
    
    private void OnDisable()
    {
        playerInput.OnAttackEvent -= OnAttack;
        playerInput.OnAttack2Event -= OnAttack2;
    }


    
    
    private void Update()
    {
        
        UpdateWeaponTimers();
        CheckAttackInputsHeld();

        if (Input.GetKeyDown(KeyCode.F1))
        {
            // Select the first weapon in the dictionary (base weaponData)
            SetSpecialWeapon(weapons.Keys.FirstOrDefault());
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            // Select the second weapon in the dictionary (if available)
            var secondWeapon = weapons.Keys.Skip(1).FirstOrDefault();
            if (secondWeapon)
            {
                SetSpecialWeapon(secondWeapon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            // Select the second weapon in the dictionary (if available)
            var secondWeapon = weapons.Keys.Skip(2).FirstOrDefault();
            if (secondWeapon)
            {
                SetSpecialWeapon(secondWeapon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            // Select the second weapon in the dictionary (if available)
            var secondWeapon = weapons.Keys.Skip(3).FirstOrDefault();
            if (secondWeapon)
            {
                SetSpecialWeapon(secondWeapon);
            }
        }
        
    }



    #region Weapon Usage ----------------------------------------------------------------------------------------------------

    private void FireActiveWeapon()
    {
        // If there is a special weaponData selected, use it
        if (_currentSpecialWeapon)
        {
            FireSpecialWeapon();
        }
        else
        {
            // If not, use the base weaponData
            FireBaseWeapon();
        }


    }
    
    private void FireBaseWeapon()
    {
        if (baseWeapon && _baseWeaponFireRateCooldown <= 0)
        {
            UseWeapon(baseWeapon, _baseWeaponInfo);
            _baseWeaponFireRateCooldown = baseWeapon.FireRate;
        }
    }

    private void FireSpecialWeapon()
    {
        if (_currentSpecialWeapon && _specialWeaponFireRateCooldown <= 0)
        {
            
            // Check if the special weaponData is ammo-based, and there is ammo left
            if (_currentSpecialWeapon.WeaponDurationType == WeaponDurationType.AmmoBased)
            {
                if (_specialWeaponAmmo > 0)
                {
                    _specialWeaponAmmo -= 1;
                }
                else
                {
                    DisableSpecialWeapon();
                    return;
                }

            }
            
            
            UseWeapon(_currentSpecialWeapon, _currentSpecialWeaponInfo);
            _specialWeaponFireRateCooldown = _currentSpecialWeapon.FireRate;
        }
    }
    
    
    private void UseWeapon(SOWeapon weapon, WeaponInfo weaponInfo)
    {
        if (!weapon || weaponInfo == null) return;
        
        
        // use weapon for each "barrel" 
        foreach (var spawnPoint in weaponInfo.projectileSpawnPoints)
        {
            weapon.Fire(spawnPoint.transform.position, player);
        }
    }

    #endregion Weapon Usage ----------------------------------------------------------------------------------------------------

    

    #region Special Weapon Management --------------------------------------------------------------------------------------

    
    public void SelectSpecialWeapon(SOWeapon weapon)
    {
        SetSpecialWeapon(weapon);
    }
    
    
    
    private void SetSpecialWeapon(SOWeapon weapon)
    {
        if (!weapon) return;
        
        // Find the weaponData in the dictionary
        if (weapons.TryGetValue(weapon, out var weaponInfo))
        {
            // Disable the previous special weaponData if it is active
            if (_currentSpecialWeapon)
            {
                _currentSpecialWeaponInfo?.weaponGfx?.gameObject.SetActive(false);
                _previousSpecialWeapon = _currentSpecialWeapon;
            }

            // Set the new weaponData
            _currentSpecialWeapon = weapon;
            _currentSpecialWeaponInfo = weaponInfo;
            _specialWeaponFireRateCooldown = 0;
            if (weapon.WeaponDurationType == WeaponDurationType.AmmoBased) { _specialWeaponAmmo = weapon.AmmoLimit;}
            else if (weapon.WeaponDurationType == WeaponDurationType.TimeBased) { _specialWeaponTime = weapon.TimeLimit;}
            weaponInfo.weaponGfx?.gameObject.SetActive(true);
            
            // Play the weapon switch SFX
            weaponSwitchSfx?.Play(audioSource);
        }
    }


    
    
    [Button] // Select a random special weaponData from the list of available weapons only used for testing purposes
    private void SelectRandomSpecialWeapon()
    {
        if (!Application.isPlaying || weapons.Count <= 0) return;
        
        // Create a list with all weapons
        List<SOWeapon> specialWeaponsList = new List<SOWeapon>();
        foreach (var weapon in weapons)
        {
            specialWeaponsList.Add(weapon.Key);
        }
        
        // Select a random special weaponData from the list,Skip the first (base) weaponData
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



    #region WeaponData Timers ----------------------------------------------------------------------------------------------------

    private void UpdateWeaponTimers()
    {
        if (_baseWeaponFireRateCooldown > 0)
        {
            _baseWeaponFireRateCooldown -= Time.deltaTime;
        }

        if (_specialWeaponFireRateCooldown > 0)
        {
            _specialWeaponFireRateCooldown -= Time.deltaTime;
        }
        
        if (_currentSpecialWeapon && _currentSpecialWeapon.WeaponDurationType == WeaponDurationType.TimeBased && _specialWeaponTime > 0)
        {
            _specialWeaponTime -= Time.deltaTime;
            if (_specialWeaponTime <= 0)
            {
                DisableSpecialWeapon();
            }
        }
        
    }

    #endregion WeaponData Timers ----------------------------------------------------------------------------------------------------
    
    

    #region Input Handling --------------------------------------------------------------------------------------

    
    private void OnAttack(InputAction.CallbackContext context)
    {
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
        
        if (context.started)
        {
            _attack2InputHeld = true;
        }
        else if (context.canceled)
        {
            _attack2InputHeld = false;
        }
    }

    private void CheckAttackInputsHeld()
    {
        if (_attackInputHeld)
        {
            FireActiveWeapon();
        }
        
        if (_attack2InputHeld && allowBaseWeaponWithSpecialWeapon && _currentSpecialWeapon)
        {
            FireBaseWeapon();
        }
    }

    #endregion Input Handling --------------------------------------------------------------------------------------

    
}
