using System;
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
    public Transform[] projectileSpawnPoints;
}

public class RailPlayerWeaponSystem : MonoBehaviour
{
    
    [Header("Weapon System Settings")]
    [SerializeField] private SOWeapon baseWeapon;
    [SerializeField] private SerializedDictionary<SOWeapon, WeaponInfo> weapons = new SerializedDictionary<SOWeapon, WeaponInfo>();
    
    [Header("References")]
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    
    private bool _attackInputHeld;
    private SOWeapon _previousSpecialWeapon;
    private SOWeapon _currentSpecialWeapon;
    private WeaponInfo _baseWeaponInfo;
    private WeaponInfo _currentSpecialWeaponInfo;
    private float _baseWeaponFireRateCooldown;
    private float _specialWeaponFireRateCooldown;
    private float _specialWeaponTime;
    private float _specialWeaponAmmo;


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
    }
    
    private void OnDisable()
    {
        playerInput.OnAttackEvent -= OnAttack;
    }


    
    
    private void Update()
    {
        
        UpdateWeaponTimers();

        if (_attackInputHeld)
        {
            FireActiveWeapon();
        }


        if (Input.GetKeyDown(KeyCode.F1))
        {
            SelectRandomSpecialWeapon();
        }
    }




    private void FireActiveWeapon()
    {
        // If there is a special weapon selected, use it
        if (_currentSpecialWeapon && _specialWeaponFireRateCooldown <= 0)
        {
            
            // Check if the special weapon is ammo-based, and there is ammo left
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
            
            return;
        }

        // If not, use the base weapon
         if (!_currentSpecialWeapon && baseWeapon && _baseWeaponFireRateCooldown <= 0)
        {
            UseWeapon(baseWeapon, _baseWeaponInfo);
            _baseWeaponFireRateCooldown = baseWeapon.FireRate;
        }
    }

    private void UseWeapon(SOWeapon weapon, WeaponInfo weaponInfo)
    {
        if (!weapon || weaponInfo == null) return;

        // Shoot a projectile from each projectile spawn point
        foreach (var spawnPoint in weaponInfo.projectileSpawnPoints)
        {
            weapon.CreateProjectile(spawnPoint.transform.position, playerAiming.GetAimDirection());
        }
        
    }
    
    
    private void SetSpecialWeapon(SOWeapon weapon)
    {
        if (!weapon) return;
        
        // Find the weapon in the dictionary
        if (weapons.TryGetValue(weapon, out var weaponInfo))
        {
            // Disable the previous special weapon if it is active
            if (_currentSpecialWeapon)
            {
                _currentSpecialWeaponInfo?.weaponGfx?.gameObject.SetActive(false);
                _previousSpecialWeapon = _currentSpecialWeapon;
            }

            // Set the new weapon
            _currentSpecialWeapon = weapon;
            _currentSpecialWeaponInfo = weaponInfo;
            _specialWeaponFireRateCooldown = 0;
            if (weapon.WeaponDurationType == WeaponDurationType.AmmoBased) { _specialWeaponAmmo = weapon.AmmoLimit;}
            else if (weapon.WeaponDurationType == WeaponDurationType.TimeBased) { _specialWeaponTime = weapon.TimeLimit;}
            weaponInfo.weaponGfx?.gameObject.SetActive(true);
        }
    }


    [Button]
    private void DisableSpecialWeapon()
    {
        if (!Application.isPlaying || !_currentSpecialWeapon) return;
        
        _currentSpecialWeaponInfo?.weaponGfx?.gameObject.SetActive(false);
        _previousSpecialWeapon = _currentSpecialWeapon;
        _currentSpecialWeapon = null;
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
        
        // Select a random special weapon from the list,Skip the first (base) weapon
        SOWeapon randomWeapon = specialWeaponsList[UnityEngine.Random.Range(1, specialWeaponsList.Count)];
        
        SetSpecialWeapon(randomWeapon);
    }
    
    

    #region Weapon Timers ----------------------------------------------------------------------------------------------------

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

    #endregion Weapon Timers ----------------------------------------------------------------------------------------------------
    

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

    #endregion Input Handling --------------------------------------------------------------------------------------

    
}
