using System;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using VInspector;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Scriptable Objects/New Weapon")]
public class SOWeaponData : ScriptableObject
{
    [Header("Weapon Settings")]
    [SerializeField] private string weaponName = "New Weapon";
    [SerializeField] private string weaponDescription = "A Weapon";
    [SerializeField] private Sprite weaponWeaponIcon;
    [SerializeField] private WeaponType weaponType = WeaponType.Projectile;
    [SerializeField] private WeaponLimitation weaponLimitation = WeaponLimitation.None;
    [SerializeField, Min(0), ShowIf("weaponLimitation", WeaponLimitation.HeatBased)] private float heatPerShot = 1f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponLimitation", WeaponLimitation.TimeBased)] private float timeLimit = 10f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponLimitation", WeaponLimitation.AmmoBased)] private float ammoLimit = 3f;[EndIf]
    [SerializeField, Min(0)] private float fireRate = 1f;
    [SerializeField, Min(0), Tooltip("0 = Infinite targets")] private int maxTargets = 1;
    [SerializeField, Min(0.1f)] private float targetCheckRadius = 3f;
    [SerializeField] private List<SOWeaponUpgrade> weaponUpgrades = new List<SOWeaponUpgrade>();
    
    
    [Header("Spread Settings")]
    [SerializeField, MinMaxRange(0f, 5f)] private RangedFloat spreadRange = new(0, 2f);
    [SerializeField, Min(0)] private float spreadRate = 1f;
    [SerializeField, Min(0)] private float spreadDecayRate = 2f;
    
    
    [ShowIf("weaponType", WeaponType.Projectile)]
    [Header("Projectile Settings")]
    [SerializeField] private PlayerProjectile playerProjectilePrefab;
    [SerializeField, Min(0)] private float projectileLifetime = 5f;
    [SerializeField, Tooltip("Offset applied to aim direction for each barrel. Should match the number of barrels on the weapon.")] private Vector3[] barrelAimOffsets = { Vector3.zero };
    [SerializeReference] private List<ProjectileBehaviorBase> projectileBehaviors = new List<ProjectileBehaviorBase>();
    [EndIf]
    
    [ShowIf("weaponType", WeaponType.Hitscan)]
    [Header("Hitscan Settings")]
    [SerializeReference] private List<HitscanBehaviorBase> hitscanBehaviors = new List<HitscanBehaviorBase>();
    [EndIf]
    
    [Header("Fire Effect")]
    [SerializeField] private SOAudioEvent fireSound;
    [SerializeField] private ParticleSystem fireEffectPrefab;
    
    [Header("Impact Effect")]
    [SerializeField] private SOAudioEvent impactSound;
    [SerializeField] private ParticleSystem impactEffectPrefab;


    
    
    public string WeaponName => weaponName;
    public string WeaponDescription => weaponDescription;
    public Sprite WeaponIcon => weaponWeaponIcon;
    public WeaponLimitation WeaponLimitation => weaponLimitation;
    public WeaponType WeaponType => weaponType;
    public float FireRate => fireRate;
    public int MaxTargets => maxTargets;
    public float TargetCheckRadius => targetCheckRadius;
    public List<SOWeaponUpgrade> WeaponUpgrades => weaponUpgrades;
    public float TimeLimit => timeLimit;
    public float AmmoLimit => ammoLimit;
    public float HeatPerShot => heatPerShot;
    public float ProjectileLifetime => projectileLifetime;
    public List<ProjectileBehaviorBase> ProjectileBehaviors => projectileBehaviors;
    public PlayerProjectile PlayerProjectilePrefab => playerProjectilePrefab;
    public List<HitscanBehaviorBase> HitscanBehaviors => hitscanBehaviors;
    public SOAudioEvent FireSound => fireSound;
    public ParticleSystem FireEffectPrefab => fireEffectPrefab;
    public SOAudioEvent ImpactSound => impactSound;
    public ParticleSystem ImpactEffectPrefab => impactEffectPrefab;
    public Vector3[] BarrelAimOffsets => barrelAimOffsets;
    public RangedFloat SpreadRange => spreadRange;
    public float SpreadRate => spreadRate;
    public float SpreadDecayRate => spreadDecayRate;

    
    
    private void OnValidate()
    {
        foreach (var upgrade in weaponUpgrades.ToList())
        {
            if (!upgrade)
            {
                RemoveUpgrade(upgrade);
            }
            
            if (upgrade.BaseWeapon != this)
            {
                upgrade.SetBaseWeapon(this);
            }
        }
        
        if (barrelAimOffsets == null || barrelAimOffsets.Length == 0)
        {
            barrelAimOffsets = new[] { Vector3.zero };
        }
    }

    #region Upgrade management
    private void AddUpgrade(SOWeaponUpgrade upgrade)
    {
        if (!weaponUpgrades.Contains(upgrade))
        {
            weaponUpgrades.Add(upgrade);
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }
    }
    
    public void RemoveUpgrade(SOWeaponUpgrade upgrade)
    {
        if (weaponUpgrades.Contains(upgrade))
        {
            weaponUpgrades.Remove(upgrade);
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("Create Weapon Upgrade"), Button]
    private void CreateWeaponUpgrade()
    {
        CreateWeaponUpgradeAsset(this);
    }
    
    private static void CreateWeaponUpgradeAsset(SOWeaponData baseWeapon)
    {
        SOWeaponUpgrade upgrade = CreateInstance<SOWeaponUpgrade>();
        upgrade.SetBaseWeapon(baseWeapon);
        
        string weaponPath = AssetDatabase.GetAssetPath(baseWeapon);
        string weaponDirectory = System.IO.Path.GetDirectoryName(weaponPath);
        int weaponUpgradeIndex = baseWeapon.WeaponUpgrades.Count + 1;
        
        if (weaponDirectory != null)
        {
            string upgradesFolder = System.IO.Path.Combine(weaponDirectory);
            if (!System.IO.Directory.Exists(upgradesFolder))
            {
                System.IO.Directory.CreateDirectory(upgradesFolder);
                AssetDatabase.Refresh();
            }
        
            string upgradeName = $"{baseWeapon.weaponName}Upgrade{weaponUpgradeIndex}";
            string upgradePath = System.IO.Path.Combine(upgradesFolder, $"{upgradeName}.asset");
        
            int counter = 1;
            while (System.IO.File.Exists(upgradePath))
            {
                upgradePath = System.IO.Path.Combine(upgradesFolder, $"{upgradeName}_{counter}.asset");
                counter++;
            }
        
            AssetDatabase.CreateAsset(upgrade, upgradePath);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        baseWeapon.AddUpgrade(upgrade);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = upgrade;
    }
    #endif
    #endregion

    
    #region Instance creation
    public void CopyBaseWeaponData(SOWeaponData source)
    {
        weaponName = source.WeaponName;
        weaponDescription = source.WeaponDescription;
        weaponWeaponIcon = source.WeaponIcon;
        weaponType = source.WeaponType;
        weaponLimitation = source.WeaponLimitation;
        heatPerShot = source.HeatPerShot;
        timeLimit = source.TimeLimit;
        ammoLimit = source.AmmoLimit;
        fireRate = source.FireRate;
        maxTargets = source.MaxTargets;
        targetCheckRadius = source.TargetCheckRadius;
        weaponUpgrades = source.WeaponUpgrades;
        
        spreadRange = source.SpreadRange;
        spreadRate = source.SpreadRate;
        spreadDecayRate = source.SpreadDecayRate;
        
        barrelAimOffsets = new Vector3[source.barrelAimOffsets.Length];
        Array.Copy(source.barrelAimOffsets, barrelAimOffsets, source.barrelAimOffsets.Length);

        if (weaponType == WeaponType.Projectile)
        {
            playerProjectilePrefab = source.PlayerProjectilePrefab;
            projectileBehaviors = source.ProjectileBehaviors;
            projectileLifetime = source.ProjectileLifetime;
        }
        else if (weaponType == WeaponType.Hitscan)
        {
            hitscanBehaviors = source.HitscanBehaviors;
        }

        fireSound = source.FireSound;
        fireEffectPrefab = source.FireEffectPrefab;
        impactSound = source.ImpactSound;
        impactEffectPrefab = source.ImpactEffectPrefab;
    }

    public void ApplyUpgradeData(SOWeaponUpgrade upgrade)
    {
        fireRate = upgrade.FireRate;
        maxTargets = upgrade.MaxTargets;
        targetCheckRadius = upgrade.TargetCheckRadius;
        heatPerShot = upgrade.HeatPerShot;
        timeLimit = upgrade.TimeLimit;
        ammoLimit = upgrade.AmmoLimit;
        projectileBehaviors = upgrade.ProjectileBehaviors;
        hitscanBehaviors = upgrade.HitscanBehaviors;
        
        spreadRange = upgrade.SpreadRange;
        spreadRate = upgrade.SpreadRate;
        spreadDecayRate = upgrade.SpreadDecayRate;
        
        if (upgrade.BarrelAimOffsets is { Length: > 0 })
        {
            barrelAimOffsets = new Vector3[upgrade.BarrelAimOffsets.Length];
            Array.Copy(upgrade.BarrelAimOffsets, barrelAimOffsets, upgrade.BarrelAimOffsets.Length);
        }
    }
    #endregion
}