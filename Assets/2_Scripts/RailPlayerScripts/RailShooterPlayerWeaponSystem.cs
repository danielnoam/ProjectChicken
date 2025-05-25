using System;
using KBCore.Refs;
using TMPEffects.SerializedCollections;
using UnityEngine;

[Serializable]
public class WeaponInfo
{
    public Transform weaponGfx;
    public bool hasWeapon = true;
}

public class RailShooterPlayerWeaponSystem : MonoBehaviour
{
    
    [Header("Weapon System Settings")]
    [SerializeField] private SerializedDictionary<SOWeapon, WeaponInfo> weapons = new SerializedDictionary<SOWeapon, WeaponInfo>();
    
    [Header("References")]
    [SerializeField, Self] private RailShooterPlayerInput playerInput;
    [SerializeField, Self] private RailShooterPlayerMovement playerMovement;
    [SerializeField, Self] private RailShooterPlayerAiming playerAiming;
    
    
    private SOWeapon previousWeapon;
    private SOWeapon currentWeapon;


    private void Awake()
    {

    }
    
    
    
    private void SelectWeapon(SOWeapon selectedWeapon)
    {
        if (!selectedWeapon) return;
        
        if (weapons.TryGetValue(selectedWeapon, out var weaponInfo))
        {
            if (currentWeapon)
            {
                previousWeapon = currentWeapon;
                if (weapons.TryGetValue(previousWeapon, out var previousWeaponInfo))
                {
                    previousWeaponInfo.weaponGfx.gameObject.SetActive(false);
                }
            }

            currentWeapon = selectedWeapon;
            if (weaponInfo.weaponGfx)
            {
                weaponInfo.weaponGfx.gameObject.SetActive(true);
            }
        }
    }
}
