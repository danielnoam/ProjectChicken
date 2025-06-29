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
    [SerializeField, Min(0), Tooltip("0 = Means infinite targets")] private int maxTargets = 1;
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
    public float ConvergenceMultiplier => convergenceMultiplier;
    public float TimeLimit => timeLimit;
    public float AmmoLimit => ammoLimit;
    public float HeatPerShot => heatPerShot;
    public float ProjectileLifetime => projectileLifetime;
    public  List<ProjectileBehaviorBase> ProjectileBehaviors => projectileBehaviors;
    

    
    
    public void Fire(RailPlayer owner, Transform[] barrelPositions)
    {
        switch (weaponType)
        {
            case WeaponType.Projectile:

                foreach (var barrelPosition in barrelPositions)
                {
                    CreateProjectile(owner, barrelPosition.position);
                }

                break;
            case WeaponType.Hitscan:
                
                foreach (var barrelPosition in barrelPositions)
                {
                    Hitscan(owner, barrelPosition.position);
                }

                break;
        }
    }
    
    


    #region Projectile  ---------------------------------------------------------------------------------

    

    private void CreateProjectile(RailPlayer owner, Vector3 position)
    {
        if (!playerProjectilePrefab) return;
        
        if (maxTargets == 1)
        {
            SpawnProjectile(owner, position, owner.GetTarget(targetCheckRadius));
            
        } 
        else
        {
            ChickenController[] enemies = maxTargets switch
            {
                0 => owner.GetAllTargets(999, targetCheckRadius),
                > 1 => owner.GetAllTargets(maxTargets, targetCheckRadius),
                _ => Array.Empty<ChickenController>()
            };

            if (enemies.Length > 0)
            {
                foreach (ChickenController enemy in enemies)
                {
                    if (enemy)
                    {
                        SpawnProjectile(owner, position, enemy);
                    }
                }
            }
            else
            {
                SpawnProjectile(owner, position, null);
            }
        }
        
    }
    
    
    private void SpawnProjectile(RailPlayer owner, Vector3 spawnPosition, ChickenController target)
    {
        PlayerProjectile projectile = Instantiate(playerProjectilePrefab, spawnPosition, Quaternion.identity);
        projectile.SetUpProjectile(this, owner, target);
    }
    

    

    #endregion Projectile  ---------------------------------------------------------------------------------

    

    #region Hitscan ----------------------------------------------------------------------------------

    private void Hitscan(RailPlayer owner, Vector3 startPosition)
    {
        PlayFireEffect(startPosition, Quaternion.identity);
        
        foreach (var behavior in hitscanBehaviors)
        {
            behavior.OnStart(this, owner);
        }

        
        if (maxTargets == 1)
        {
            ChickenController enemy = owner.GetTarget(targetCheckRadius);
            
            if (enemy)
            {
                foreach (var behavior in hitscanBehaviors)
                {
                    behavior.OnHit(this, owner, enemy);
                }
                
                enemy.TakeDamage(damage);
                PlayImpactEffect(enemy.transform.position, Quaternion.identity);
            }
        } 
        else
        {
            ChickenController[] enemies = maxTargets switch
            {
                0 => owner.GetAllTargets(999, targetCheckRadius),
                > 1 => owner.GetAllTargets(maxTargets, targetCheckRadius),
                _ => Array.Empty<ChickenController>()
            };
            foreach (ChickenController enemy in enemies)
            {
                if (!enemy) continue;
                foreach (var behavior in hitscanBehaviors)
                {
                    behavior.OnHit(this, owner, enemy);
                }
                enemy.TakeDamage(damage);
                PlayImpactEffect(enemy.transform.position, Quaternion.identity);
            }
        }
        
        foreach (var behavior in hitscanBehaviors)
        {
            behavior.OnEnd(this, owner);
        }
    }

    #endregion Hitscan ----------------------------------------------------------------------------------


    
    #region Effects ---------------------------------------------------------------------------------

    public void PlayImpactEffect(Vector3 position, Quaternion rotation)
    {
        if (impactEffectPrefab)
        {
            Instantiate(impactEffectPrefab, position, rotation);
        }
        
        if (impactSound)
        {
            impactSound.PlayAtPoint(position);
        }

        if (shakeCameraOnImpact)
        {
            CameraManager.Instance?.ShakeCamera(impactShakeSettings.impulseShape, impactShakeSettings.intensity, impactShakeSettings.duration);
        }

    }
    
    
    public void PlayFireEffect(Vector3 position, Quaternion rotation, AudioSource audioSource = null)
    {
        if (fireEffectPrefab)
        {
            Instantiate(fireEffectPrefab, position, rotation);
        }
        
        if (fireSound)
        {
            if (audioSource)
            {
                fireSound.Play(audioSource);
            }
            else
            {
                fireSound.PlayAtPoint(position);
            }
        }
        
        if (shakeCameraOnFire)
        {
            CameraManager.Instance?.ShakeCamera(fireShakeSettings.impulseShape, fireShakeSettings.intensity, fireShakeSettings.duration);
        }
    }
    


    #endregion Effects ---------------------------------------------------------------------------------
    




    

}
