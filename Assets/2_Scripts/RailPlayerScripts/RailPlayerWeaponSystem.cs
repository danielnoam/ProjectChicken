using System;
using KBCore.Refs;
using TMPEffects.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private float _baseWeaponCooldown;
    private float _specialWeaponCooldown;

    private void OnValidate() { this.ValidateRefs(); }

    private void Awake()
    {
        if (baseWeapon)
        {
            _baseWeaponInfo = weapons[baseWeapon];
            _baseWeaponInfo.weaponGfx?.gameObject.SetActive(true);
            _baseWeaponCooldown = 0;
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
        
        UpdateCooldowns();

        if (_attackInputHeld)
        {
            TryFireWeapon();
        }
    }
    


    private void TryFireWeapon()
    {
        if (_currentSpecialWeapon && _specialWeaponCooldown <= 0)
        {
            UseWeapon(_currentSpecialWeapon, _currentSpecialWeaponInfo);
            _specialWeaponCooldown = _currentSpecialWeapon.FireRate;
            return;
        }

            
        if (baseWeapon && _baseWeaponCooldown <= 0)
        {
            UseWeapon(baseWeapon, _baseWeaponInfo);
            _baseWeaponCooldown = baseWeapon.FireRate;
        }
    }

    private void UseWeapon(SOWeapon weapon, WeaponInfo weaponInfo)
    {
        if (!weapon || weaponInfo == null) return;

        
        foreach (var spawnPoint in weaponInfo.projectileSpawnPoints)
        {
            PlayerProjectile playerProjectile = Instantiate(weapon.PlayerProjectilePrefab, spawnPoint.position, Quaternion.identity);
            playerProjectile.SetUp(weapon, playerAiming.GetAimDirection());
        }
        
    }
    
    
    private void SetSpecialWeapon(SOWeapon weapon)
    {
        if (!weapon) return;
        
        if (weapons.TryGetValue(weapon, out var weaponInfo))
        {
            if (_currentSpecialWeapon)
            {
                _currentSpecialWeaponInfo?.weaponGfx?.gameObject.SetActive(false);
                _previousSpecialWeapon = _currentSpecialWeapon;
            }

            _currentSpecialWeapon = weapon;
            _currentSpecialWeaponInfo = weaponInfo;
            _specialWeaponCooldown = 0;
            weaponInfo.weaponGfx?.gameObject.SetActive(true);
        }
    }

    private void UpdateCooldowns()
    {
        if (_baseWeaponCooldown > 0)
        {
            _baseWeaponCooldown -= Time.deltaTime;
        }

        if (_specialWeaponCooldown > 0)
        {
            _specialWeaponCooldown -= Time.deltaTime;
        }
        
    }


    #region Input Handling --------------------------------------------------------------------------------------

    
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _attackInputHeld = true;
            TryFireWeapon();
        }
        else if (context.canceled)
        {
            _attackInputHeld = false;
        }

    }

    #endregion Input Handling --------------------------------------------------------------------------------------

    
}
