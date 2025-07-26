using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;
using VInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SOWeaponUpgrade : ScriptableObject, IStoreItem
{
    [SerializeField, ReadOnly] private SOWeaponData baseWeapon;
    
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
    
    [ShowIf("WeaponType", WeaponType.Projectile), SerializeField] private bool overrideProjectileBehaviors; [EndIf]
    [ShowIf("ShowProjectileBehaviors"),SerializeReference] private List<ProjectileBehaviorBase> projectileBehaviors = new List<ProjectileBehaviorBase>(); [EndIf]
    
    [ShowIf("WeaponType", WeaponType.Hitscan),SerializeField] private bool overrideHitscanBehaviors;[EndIf]
    [ShowIf("ShowHitscanBehaviors"), SerializeReference] private List<HitscanBehaviorBase> hitscanBehaviors = new List<HitscanBehaviorBase>(); [EndIf]

    
    [Header("Store Interface")]
    [SerializeField] private string itemName = "New Store Item";
    [SerializeField] private string itemDescription = "An Item";
    [SerializeField, Min(0)] private int itemCost = 10;
    [SerializeField] private GameObject itemGfx;
    [SerializeField] private List<InterfaceReference<IStoreItem>> neededItemsToUnlock = new  List<InterfaceReference<IStoreItem>>();
    [SerializeField, ReadOnly] private int itemID;
    
    
    
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
    public float HeatPerShot => overrideWeaponLimitation && baseWeapon.WeaponLimitation == WeaponLimitation.HeatBased ? heatPerShot : (baseWeapon ? baseWeapon.HeatPerShot : 1f);
    public float TimeLimit => overrideWeaponLimitation && baseWeapon.WeaponLimitation == WeaponLimitation.TimeBased ? timeLimit : (baseWeapon ? baseWeapon.TimeLimit : 10f);
    public float AmmoLimit => overrideWeaponLimitation && baseWeapon.WeaponLimitation == WeaponLimitation.AmmoBased ? ammoLimit : (baseWeapon ? baseWeapon.AmmoLimit : 3f);
    public List<ProjectileBehaviorBase> ProjectileBehaviors => overrideProjectileBehaviors ? projectileBehaviors : (baseWeapon ? baseWeapon.ProjectileBehaviors : new List<ProjectileBehaviorBase>());
    public List<HitscanBehaviorBase> HitscanBehaviors => overrideHitscanBehaviors ? hitscanBehaviors : (baseWeapon ? baseWeapon.HitscanBehaviors : new List<HitscanBehaviorBase>());
    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public int ItemCost => itemCost;
    public GameObject ItemGfx => itemGfx;
    public List<InterfaceReference<IStoreItem>> NeededItemsToUnlockToUnlock => neededItemsToUnlock;
    
    
    
    public int ItemID { get => itemID; set => itemID = value; }
    
    
    private void OnEnable()
    {
        IStoreItem.EnsureUniqueID(this);
    }
    
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
        if (!overrideWeaponLimitation) 
        {
            heatPerShot = baseWeapon.HeatPerShot;
            timeLimit = baseWeapon.TimeLimit;
            ammoLimit = baseWeapon.AmmoLimit;
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
        overrideWeaponLimitation = previousUpgrade.overrideWeaponLimitation;
        overrideProjectileBehaviors = previousUpgrade.overrideProjectileBehaviors;
        overrideHitscanBehaviors = previousUpgrade.overrideHitscanBehaviors;
        overrideWeaponGfx = previousUpgrade.overrideWeaponGfx;
        overrideWeaponBarrels = previousUpgrade.overrideWeaponBarrels;
        
        if (!overrideFireRate) fireRate = previousUpgrade.FireRate;
        if (!overrideMaxTargets) maxTargets = previousUpgrade.MaxTargets;
        if (!overrideTargetCheckRadius) targetCheckRadius = previousUpgrade.TargetCheckRadius;
        if (!overrideWeaponLimitation)
        {
            heatPerShot = previousUpgrade.HeatPerShot;
            timeLimit = previousUpgrade.TimeLimit;
            ammoLimit = previousUpgrade.AmmoLimit;
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
}