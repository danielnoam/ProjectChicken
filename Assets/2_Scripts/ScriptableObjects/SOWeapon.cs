using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;


[CreateAssetMenu(fileName = "New Weapon", menuName = "Scriptable Objects/New Weapon")]
public class SOWeapon : ScriptableObject
{
    [Header("Weapon Settings")]
    [SerializeField] private string weaponName = "New Weapon";
    [SerializeField] private string weaponDescription = "A Weapon";
    [SerializeField] private Sprite weaponWeaponIcon;
    [SerializeField] private WeaponType weaponType = WeaponType.Projectile;
    [SerializeField] private WeaponLimitation weaponLimitation = global::WeaponLimitation.None;
    [SerializeField, Min(0), ShowIf("weaponLimitation", global::WeaponLimitation.HeatBased)] private float heatPerShot = 1f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponLimitation", global::WeaponLimitation.TimeBased)] private float timeLimit = 10f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponLimitation", global::WeaponLimitation.AmmoBased)] private float ammoLimit = 3f;[EndIf]
    [SerializeField, Min(0)] private float damage = 10f;
    [SerializeField, Min(0)] private float fireRate = 1f;
    [SerializeField, Min(0), Tooltip("0 = Infinite targets")] private int maxTargets = 1;
    [SerializeField, Min(0.1f)] private float targetCheckRadius = 3f;

    
    [ShowIf("weaponType", WeaponType.Projectile)]
    [Header("Projectile Settings")]
    [SerializeField] private PlayerProjectile playerProjectilePrefab;
    [SerializeField, Min(0)] private float projectileLifetime = 5f;
    [SerializeField, Tooltip("Controls where projectiles converge: 0 = parallel, 1 = at crosshair, 0.5 = halfway to crosshair")] private float convergenceMultiplier = 1f;
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
    public float Damage => damage;
    public float FireRate => fireRate;
    public int MaxTargets => maxTargets;
    public float TargetCheckRadius => targetCheckRadius;
    
    
    public float ConvergenceMultiplier => convergenceMultiplier;
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
    

    

}
