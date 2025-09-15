using System;
using System.Collections.Generic;
using AYellowpaper;
using DNExtensions;
using UnityEngine;
using VInspector;
using UnityEditor;



public class SOWeaponUpgrade : SOUpgradeBase
{
    [SerializeField, VInspector.ReadOnly] private SOWeaponData baseWeapon;
    
    [Header("Override Weapon Settings")]
    [SerializeField] private bool overrideWeaponGfx;
    [SerializeField] private bool overrideWeaponBarrels;
    [SerializeField] private bool overrideFireRate;
    [ShowIf("overrideFireRate")]
    [SerializeField, Min(0)] private float fireRate = 0.15f;
    [EndIf]
    
    [SerializeField] private bool overrideMaxTargets;
    [ShowIf("overrideMaxTargets")]
    [SerializeField, Min(0), Tooltip("0 = Infinite targets")] private int maxTargets = 1;
    [EndIf]
    
    [SerializeField] private bool overrideTargetCheckRadius;
    [ShowIf("overrideTargetCheckRadius")]
    [SerializeField, Min(0.1f)] private float targetCheckRadius = 4f;
    [EndIf]
    
    [SerializeField] private bool overrideBarrelAimOffsets;
    [ShowIf("overrideBarrelAimOffsets")]
    [SerializeField, Tooltip("Offset applied to aim direction for each barrel")] 
    private Vector3[] barrelAimOffsets = { Vector3.zero };
    [EndIf]
    
    [SerializeField] private bool overrideWeaponLimitation;
    [ShowIf("ShowHeatPerShot")]
    [SerializeField, Min(0)] private float heatPerShot = 1f;
    [EndIf]
    
    [ShowIf("ShowTimeLimit")]
    [SerializeField, Min(0)] private float timeLimit = 10f;
    [EndIf]
    
    [ShowIf("ShowAmmoLimit")]
    [SerializeField, Min(0)] private float ammoLimit = 3f;
    [EndIf]
    
    [SerializeField] private bool overrideSpreadSettings;
    [ShowIf("overrideSpreadSettings")]
    [SerializeField, MinMaxRange(0f, 5f)] private RangedFloat spreadRange = new(0, 2f);
    [SerializeField, Min(0)] private float spreadRate = 1f;
    [SerializeField, Min(0)] private float spreadDecayRate = 2f;
    [EndIf]
    
    [ShowIf("WeaponType", WeaponType.Projectile), SerializeField] private bool overrideProjectileBehaviors; [EndIf]
    [ShowIf("ShowProjectileBehaviors"),SerializeReference] private List<ProjectileBehaviorBase> projectileBehaviors = new List<ProjectileBehaviorBase>(); [EndIf]
    
    [ShowIf("WeaponType", WeaponType.Hitscan),SerializeField] private bool overrideHitscanBehaviors;[EndIf]
    [ShowIf("ShowHitscanBehaviors"), SerializeReference] private List<HitscanBehaviorBase> hitscanBehaviors = new List<HitscanBehaviorBase>(); [EndIf]
    
    
    
    
    private WeaponType WeaponType => baseWeapon ? baseWeapon.WeaponType : WeaponType.Projectile;
    private bool ShowProjectileBehaviors => overrideProjectileBehaviors && WeaponType == WeaponType.Projectile;
    private bool ShowHitscanBehaviors => overrideHitscanBehaviors && WeaponType == WeaponType.Hitscan;
    private bool ShowHeatPerShot => overrideWeaponLimitation && baseWeapon.WeaponLimitation == WeaponLimitation.HeatBased;
    private bool ShowTimeLimit => overrideWeaponLimitation && baseWeapon.WeaponLimitation == WeaponLimitation.TimeBased;
    private bool ShowAmmoLimit => overrideWeaponLimitation && baseWeapon.WeaponLimitation == WeaponLimitation.AmmoBased;
    
    
    public SOWeaponData BaseWeapon => baseWeapon;
    public bool OverrideWeaponGfx => overrideWeaponGfx;
    public bool OverrideWeaponBarrels => overrideWeaponBarrels;
    public float FireRate => overrideFireRate ? fireRate : (baseWeapon ? baseWeapon.FireRate : 1f);
    public int MaxTargets => overrideMaxTargets ? maxTargets : (baseWeapon ? baseWeapon.MaxTargets : 1);
    public float TargetCheckRadius => overrideTargetCheckRadius ? targetCheckRadius : (baseWeapon ? baseWeapon.TargetCheckRadius : 3f);
    public Vector3[] BarrelAimOffsets => overrideBarrelAimOffsets ? barrelAimOffsets : (baseWeapon ? baseWeapon.BarrelAimOffsets : new Vector3[] { Vector3.zero });
    public float HeatPerShot => overrideWeaponLimitation && baseWeapon.WeaponLimitation == WeaponLimitation.HeatBased ? heatPerShot : (baseWeapon ? baseWeapon.HeatPerShot : 1f);
    public float TimeLimit => overrideWeaponLimitation && baseWeapon.WeaponLimitation == WeaponLimitation.TimeBased ? timeLimit : (baseWeapon ? baseWeapon.TimeLimit : 10f);
    public float AmmoLimit => overrideWeaponLimitation && baseWeapon.WeaponLimitation == WeaponLimitation.AmmoBased ? ammoLimit : (baseWeapon ? baseWeapon.AmmoLimit : 3f);
    public List<ProjectileBehaviorBase> ProjectileBehaviors => overrideProjectileBehaviors ? projectileBehaviors : (baseWeapon ? baseWeapon.ProjectileBehaviors : new List<ProjectileBehaviorBase>());
    public List<HitscanBehaviorBase> HitscanBehaviors => overrideHitscanBehaviors ? hitscanBehaviors : (baseWeapon ? baseWeapon.HitscanBehaviors : new List<HitscanBehaviorBase>());
    public RangedFloat SpreadRange => overrideSpreadSettings ? spreadRange : (baseWeapon ? baseWeapon.SpreadRange : new RangedFloat(0, 2f));
    public float SpreadRate => overrideSpreadSettings ? spreadRate : (baseWeapon ? baseWeapon.SpreadRate : 1f);
    public float SpreadDecayRate => overrideSpreadSettings ? spreadDecayRate : (baseWeapon ? baseWeapon.SpreadDecayRate : 2f);
    
    #if UNITY_EDITOR
    private void OnDestroy()
    {
        if (baseWeapon)
        {
            baseWeapon.RemoveUpgrade(this);
        }
    }
    #endif
    
    
    
    public void SetBaseWeapon(SOWeaponData weapon)
    {
        baseWeapon = weapon;
        
        // Initialize with base weapon values
        if (weapon)
        {
            // Set default upgrade name
            if (itemName == "New Store Item")
            {
                int weaponUpgradeIndex = baseWeapon.WeaponUpgrades.Count + 1;
                itemName = $"{weapon.WeaponName} Upgrade {weaponUpgradeIndex}";
            }
        }

        CopyDataFromBaseWeapon();

        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
    }
    
    

    [Button]
    private void CopyDataFromBaseWeapon()
    {
        if (!baseWeapon)
        {
            Debug.Log("There is no base weapon");
            return;
        }
        
        if (!overrideFireRate) fireRate = baseWeapon.FireRate;
        if (!overrideMaxTargets) maxTargets = baseWeapon.MaxTargets;
        if (!overrideTargetCheckRadius) targetCheckRadius = baseWeapon.TargetCheckRadius;
        if (!overrideBarrelAimOffsets && baseWeapon.BarrelAimOffsets != null) 
        {
            barrelAimOffsets = new Vector3[baseWeapon.BarrelAimOffsets.Length];
            Array.Copy(baseWeapon.BarrelAimOffsets, barrelAimOffsets, baseWeapon.BarrelAimOffsets.Length);
        }
        if (!overrideWeaponLimitation) 
        {
            heatPerShot = baseWeapon.HeatPerShot;
            timeLimit = baseWeapon.TimeLimit;
            ammoLimit = baseWeapon.AmmoLimit;
        }
        
        if (!overrideSpreadSettings)
        {
            spreadRange = baseWeapon.SpreadRange;
            spreadRate = baseWeapon.SpreadRate;
            spreadDecayRate = baseWeapon.SpreadDecayRate;
        }
        
        switch (baseWeapon.WeaponType)
        {
            case WeaponType.Projectile:
                if (!overrideProjectileBehaviors) projectileBehaviors = new List<ProjectileBehaviorBase>(baseWeapon.ProjectileBehaviors);
                break;
            case WeaponType.Hitscan:
                 if (!overrideHitscanBehaviors)hitscanBehaviors = new List<HitscanBehaviorBase>(baseWeapon.HitscanBehaviors);
                break;
        }
    }

    [Button]
    private void CopyDataFromPreviousUpgrade()
    {
        if (baseWeapon.WeaponUpgrades.Count > 2)
        {
            Debug.Log("There are no previous upgrades");
            return;
        }
        
        int upgradeIndex = baseWeapon.WeaponUpgrades.IndexOf(this);
        if (upgradeIndex == 0)
        {
            Debug.Log("This is the first upgrade");
            return;
        }
        
        SOWeaponUpgrade previousUpgrade = baseWeapon.WeaponUpgrades[upgradeIndex - 1];
        if (!previousUpgrade)
        {
            Debug.Log("{Previous upgrade is null");
            return;
        }


        overrideFireRate = previousUpgrade.overrideFireRate;
        overrideMaxTargets = previousUpgrade.overrideMaxTargets;
        overrideTargetCheckRadius = previousUpgrade.overrideTargetCheckRadius;
        overrideBarrelAimOffsets = previousUpgrade.overrideBarrelAimOffsets;
        overrideWeaponLimitation = previousUpgrade.overrideWeaponLimitation;
        overrideProjectileBehaviors = previousUpgrade.overrideProjectileBehaviors;
        overrideHitscanBehaviors = previousUpgrade.overrideHitscanBehaviors;
        overrideWeaponGfx = previousUpgrade.overrideWeaponGfx;
        overrideWeaponBarrels = previousUpgrade.overrideWeaponBarrels;
        
        if (!overrideFireRate) fireRate = previousUpgrade.FireRate;
        if (!overrideMaxTargets) maxTargets = previousUpgrade.MaxTargets;
        if (!overrideTargetCheckRadius) targetCheckRadius = previousUpgrade.TargetCheckRadius;
        if (!overrideBarrelAimOffsets && previousUpgrade.BarrelAimOffsets != null)
        {
            barrelAimOffsets = new Vector3[previousUpgrade.BarrelAimOffsets.Length];
            Array.Copy(previousUpgrade.BarrelAimOffsets, barrelAimOffsets, previousUpgrade.BarrelAimOffsets.Length);
        }

        if (!overrideWeaponLimitation)
        {
            heatPerShot = previousUpgrade.HeatPerShot;
            timeLimit = previousUpgrade.TimeLimit;
            ammoLimit = previousUpgrade.AmmoLimit;
        }
        
        if (!overrideSpreadSettings)
        {
            spreadRange = previousUpgrade.SpreadRange;
            spreadRate = previousUpgrade.SpreadRate;
            spreadDecayRate = previousUpgrade.SpreadDecayRate;
        }
        
        switch (baseWeapon.WeaponType)
        {
            case WeaponType.Projectile:
                if (!overrideProjectileBehaviors) projectileBehaviors = new List<ProjectileBehaviorBase>(previousUpgrade.ProjectileBehaviors);
                break;
            case WeaponType.Hitscan:
                if (!overrideHitscanBehaviors) hitscanBehaviors = new List<HitscanBehaviorBase>(previousUpgrade.HitscanBehaviors);
                break;
        }
    }
    
    public override void ApplyUpgrade(RailPlayer player)
    {
        player.AddWeaponUpgrade(this, this);
    }
}