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
    
    [Header("WeaponData System Settings")]
    [Tooltip("If true, the player can use the base weaponData even with a special weaponData, using a different button.")]
    [SerializeField] private bool allowBaseWeaponWithSpecialWeapon = true;
    [SerializeField, CreateEditableAsset] private SOWeaponData baseWeaponData;
    [SerializeField] private SerializedDictionary<SOWeaponData, WeaponInfo> weapons = new SerializedDictionary<SOWeaponData, WeaponInfo>();
    
    [Header("References")]
    [SerializeField, Self] private RailPlayer player;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    
    private bool _attackInputHeld;
    private bool _attack2InputHeld;
    private SOWeaponData _previousSpecialWeaponData;
    private SOWeaponData _currentSpecialWeaponData;
    private WeaponInfo _baseWeaponInfo;
    private WeaponInfo _currentSpecialWeaponInfo;
    private float _baseWeaponFireRateCooldown;
    private float _specialWeaponFireRateCooldown;
    private float _specialWeaponTime;
    private float _specialWeaponAmmo;


    public SOWeaponData BaseWeaponData => baseWeaponData;
    public SOWeaponData CurrentSpecialWeaponData => _currentSpecialWeaponData;
    public float SpecialWeaponTime => _specialWeaponTime;
    public float SpecialWeaponAmmo => _specialWeaponAmmo;

    private void OnValidate() { this.ValidateRefs(); }

    private void Awake()
    {
        if (baseWeaponData)
        {
            _baseWeaponInfo = weapons[baseWeaponData];
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



    #region WeaponData Usage ----------------------------------------------------------------------------------------------------

    private void FireActiveWeapon()
    {
        // If there is a special weaponData selected, use it
        if (_currentSpecialWeaponData)
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
        if (baseWeaponData && _baseWeaponFireRateCooldown <= 0)
        {
            UseWeapon(baseWeaponData, _baseWeaponInfo);
            _baseWeaponFireRateCooldown = baseWeaponData.FireRate;
        }
    }

    private void FireSpecialWeapon()
    {
        if (_currentSpecialWeaponData && _specialWeaponFireRateCooldown <= 0)
        {
            
            // Check if the special weaponData is ammo-based, and there is ammo left
            if (_currentSpecialWeaponData.WeaponDurationType == WeaponDurationType.AmmoBased)
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
            
            
            UseWeapon(_currentSpecialWeaponData, _currentSpecialWeaponInfo);
            _specialWeaponFireRateCooldown = _currentSpecialWeaponData.FireRate;
        }
    }
    
    
    private void UseWeapon(SOWeaponData weaponData, WeaponInfo weaponInfo)
    {
        if (!weaponData || weaponInfo == null) return;

        // Shoot a projectile from each projectile spawn point
        foreach (var spawnPoint in weaponInfo.projectileSpawnPoints)
        {
            weaponData.CreateProjectile(spawnPoint.transform.position, player);
        }
    }

    #endregion WeaponData Usage ----------------------------------------------------------------------------------------------------

    

    #region Special WeaponData Management --------------------------------------------------------------------------------------

    private void SetSpecialWeapon(SOWeaponData weaponData)
    {
        if (!weaponData) return;
        
        // Find the weaponData in the dictionary
        if (weapons.TryGetValue(weaponData, out var weaponInfo))
        {
            // Disable the previous special weaponData if it is active
            if (_currentSpecialWeaponData)
            {
                _currentSpecialWeaponInfo?.weaponGfx?.gameObject.SetActive(false);
                _previousSpecialWeaponData = _currentSpecialWeaponData;
            }

            // Set the new weaponData
            _currentSpecialWeaponData = weaponData;
            _currentSpecialWeaponInfo = weaponInfo;
            _specialWeaponFireRateCooldown = 0;
            if (weaponData.WeaponDurationType == WeaponDurationType.AmmoBased) { _specialWeaponAmmo = weaponData.AmmoLimit;}
            else if (weaponData.WeaponDurationType == WeaponDurationType.TimeBased) { _specialWeaponTime = weaponData.TimeLimit;}
            weaponInfo.weaponGfx?.gameObject.SetActive(true);
        }
    }


    [Button]
    private void DisableSpecialWeapon()
    {
        if (!Application.isPlaying || !_currentSpecialWeaponData) return;
        
        _currentSpecialWeaponInfo?.weaponGfx?.gameObject.SetActive(false);
        _previousSpecialWeaponData = _currentSpecialWeaponData;
        _currentSpecialWeaponData = null;
    }

    
    
    public void SelectSpecialWeapon(SOWeaponData weaponData)
    {
        SetSpecialWeapon(weaponData);
    }
    
    
    [Button] // Select a random special weaponData from the list of available weapons only used for testing purposes
    private void SelectRandomSpecialWeapon()
    {
        if (!Application.isPlaying || weapons.Count <= 0) return;
        
        // Create a list with all weapons
        List<SOWeaponData> specialWeaponsList = new List<SOWeaponData>();
        foreach (var weapon in weapons)
        {
            specialWeaponsList.Add(weapon.Key);
        }
        
        // Select a random special weaponData from the list,Skip the first (base) weaponData
        SOWeaponData randomWeaponData = specialWeaponsList[UnityEngine.Random.Range(1, specialWeaponsList.Count)];
        
        SetSpecialWeapon(randomWeaponData);
    }

    #endregion Special WeaponData Management --------------------------------------------------------------------------------------



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
        
        if (_currentSpecialWeaponData && _currentSpecialWeaponData.WeaponDurationType == WeaponDurationType.TimeBased && _specialWeaponTime > 0)
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
        
        if (_attack2InputHeld && allowBaseWeaponWithSpecialWeapon && _currentSpecialWeaponData)
        {
            FireBaseWeapon();
        }
    }

    #endregion Input Handling --------------------------------------------------------------------------------------

    
}
