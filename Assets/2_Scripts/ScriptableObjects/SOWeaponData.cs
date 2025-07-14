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
    
    [Header("Weapon Upgrades")]
    [SerializeField] private List<SOWeaponUpgrade> weaponUpgrades = new List<SOWeaponUpgrade>();
    
    
    [ShowIf("weaponType", WeaponType.Projectile)]
    [Header("Projectile Settings")]
    [SerializeField] private PlayerProjectile playerProjectilePrefab;
    [SerializeField, Min(0)] private float projectileLifetime = 5f;
    [SerializeReference] private List<ProjectileBehaviorBase> projectileBehaviors = new List<ProjectileBehaviorBase>();
    [EndIf]
    
    [ShowIf("weaponType", WeaponType.Hitscan)]
    [Header("Hitscan Settings")]
    [SerializeReference] private List<HitscanBehaviorBase> hitscanBehaviors = new List<HitscanBehaviorBase>();
    [EndIf]
    
    
    [Header("Fire Effect")]
    [SerializeField] private SOAudioEvent fireSound;
    [SerializeField] private ParticleSystem fireEffectPrefab;
    [SerializeField] private bool shakeCameraOnFire;
    [ShowIf("shakeCameraOnFire")]
    [SerializeField] private CameraShakeSettings fireShakeSettings;
    [EndIf]
    
    [Header("Impact Effect")]
    [SerializeField] private SOAudioEvent impactSound;
    [SerializeField] private ParticleSystem impactEffectPrefab;
    [SerializeField] private bool shakeCameraOnImpact;
    [ShowIf("shakeCameraOnImpact")]
    [SerializeField] private CameraShakeSettings impactShakeSettings;
    [EndIf]
    



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
    public  List<ProjectileBehaviorBase> ProjectileBehaviors => projectileBehaviors;
    public PlayerProjectile PlayerProjectilePrefab => playerProjectilePrefab;
    public List<HitscanBehaviorBase> HitscanBehaviors => hitscanBehaviors;
    
    public SOAudioEvent FireSound => fireSound;
    public ParticleSystem FireEffectPrefab => fireEffectPrefab;
    public bool ShakeCameraOnFire => shakeCameraOnFire;
    public CameraShakeSettings FireShakeSettings => fireShakeSettings;
    public SOAudioEvent ImpactSound => impactSound;
    public ParticleSystem ImpactEffectPrefab => impactEffectPrefab;
    public bool ShakeCameraOnImpact => shakeCameraOnImpact;
    public CameraShakeSettings ImpactShakeSettings => impactShakeSettings;


    private void OnValidate()
    {
        foreach (var upgrade in weaponUpgrades.ToList())
        {
            if (!upgrade)
            {
                RemoveUpgrade(upgrade);
            }
            
            
            if (upgrade.BaseWeapon !=  this)
            {
                upgrade.SetBaseWeapon(this);
            }
        }
    }


    #region Upgrade management ----------------------------------------------------------------------------------

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
    [ContextMenu("Create Weapon Upgrade"),Button]
    private void CreateWeaponUpgrade()
    {
        CreateWeaponUpgradeAsset(this);
    }
    
    private static void CreateWeaponUpgradeAsset(SOWeaponData baseWeapon)
    {
        // Create the upgrade asset
        SOWeaponUpgrade upgrade = CreateInstance<SOWeaponUpgrade>();
        upgrade.SetBaseWeapon(baseWeapon);
        
        // Get the path of the base weapon
        string weaponPath = AssetDatabase.GetAssetPath(baseWeapon);
        string weaponDirectory = System.IO.Path.GetDirectoryName(weaponPath);
        string weaponFileName = System.IO.Path.GetFileNameWithoutExtension(weaponPath);
        int weaponUpgradeIndex = baseWeapon.WeaponUpgrades.Count + 1;
        
        // Create upgrades folder if it doesn't exist
        if (weaponDirectory != null)
        {
            string upgradesFolder = System.IO.Path.Combine(weaponDirectory);
            if (!System.IO.Directory.Exists(upgradesFolder))
            {
                System.IO.Directory.CreateDirectory(upgradesFolder);
                AssetDatabase.Refresh();
            }
        
            // Generate unique filename
            string upgradeName = $"{baseWeapon.weaponName}Upgrade{weaponUpgradeIndex}";
            string upgradePath = System.IO.Path.Combine(upgradesFolder, $"{upgradeName}.asset");
        
            // Ensure unique filename
            int counter = 1;
            while (System.IO.File.Exists(upgradePath))
            {
                upgradePath = System.IO.Path.Combine(upgradesFolder, $"{upgradeName}_{counter}.asset");
                counter++;
            }
        
            // Create the asset
            AssetDatabase.CreateAsset(upgrade, upgradePath);
        }
        
        
        // Save and refresh
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Add to the weapon's upgrade list
        baseWeapon.AddUpgrade(upgrade);
        
        // Focus on the new asset
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = upgrade;
    }
    #endif

    #endregion Upgrade management ----------------------------------------------------------------------------------


    #region Instance creation ---------------------------------------------------------------------------
    
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
        shakeCameraOnFire = source.ShakeCameraOnFire;
        fireShakeSettings = source.FireShakeSettings;
        impactSound = source.ImpactSound;
        impactEffectPrefab = source.ImpactEffectPrefab;
        shakeCameraOnImpact = source.ShakeCameraOnImpact;
        impactShakeSettings = source.ImpactShakeSettings;

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
    }

    #endregion Instance creation ---------------------------------------------------------------------------

}
