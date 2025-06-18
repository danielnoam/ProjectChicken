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
    [SerializeField] private WeaponLimitationType weaponLimitationType = WeaponLimitationType.None;
    [SerializeField, Min(0), ShowIf("weaponLimitationType", WeaponLimitationType.HeatBased)] private float heatPerShot = 1f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponLimitationType", WeaponLimitationType.TimeBased)] private float timeLimit = 10f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponLimitationType", WeaponLimitationType.AmmoBased)] private float ammoLimit = 3f;[EndIf]
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
    [SerializeField, Min(0)] private float pushForce = 5f;
    [SerializeField, Min(0)] private float stunTime;
    [SerializeField] private LayerMask hitLayers = -1;
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
    public WeaponLimitationType WeaponLimitationType => weaponLimitationType;
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
        
        
        if (maxTargets == 1) // If we only want to target one enemy
        {
            SpawnProjectile(owner, position, owner.GetTarget(targetCheckRadius));
            
        } 
        else // If we want to target multiple enemies
        {

            // Create a list of enemies
            ChickenController[] enemies = Array.Empty<ChickenController>();
            
            // Get the right number of targets
            if (maxTargets <= 0)
            {
                enemies = owner.GetAllTargets(999, targetCheckRadius);
            } 
            else if (maxTargets > 1)
            {
                enemies = owner.GetAllTargets(maxTargets, targetCheckRadius);
            }

            
            // Attack all enemies in the list
            if (enemies.Length > 0)
            {
                foreach (ChickenController target in enemies)
                {
                    if (target)
                    {
                        SpawnProjectile(owner, position, target);
                    }
                }
            }
            else // If no enemies were found, just spawn a projectile without a target
            {
                SpawnProjectile(owner, position, null);
            }

        }
        
    }
    
    
    private void SpawnProjectile(RailPlayer owner, Vector3 spawnPosition, ChickenController target)
    {
        // Instantiate the base projectile
        PlayerProjectile projectile = Instantiate(playerProjectilePrefab, spawnPosition, Quaternion.identity);
        
        
        // Initialize the projectile
        projectile.SetUpProjectile(this, owner, target);
        
    }
    

    

    #endregion Projectile  ---------------------------------------------------------------------------------

    

    #region Hitscan ----------------------------------------------------------------------------------

    private void Hitscan(RailPlayer owner, Vector3 startPosition)
    {
        
        // Play spawn effect
        PlayFireEffect(startPosition, Quaternion.identity);

        // Get enemy target
        if (maxTargets == 1)
        {
            ChickenController enemy = owner.GetTarget(targetCheckRadius);
            
            if (enemy)
            {
                // Apply damage
                enemy.TakeDamage(damage);
            
                enemy.ApplyConcussion(stunTime);

                enemy.ApplyForce(startPosition, pushForce);
            
                // Play impact effect
                PlayImpactEffect(enemy.transform.position, Quaternion.identity);
            }
        } 
        else
        {

            // Create a list of enemies
            ChickenController[] enemies = Array.Empty<ChickenController>();
            
            // Get the right number of targets
            if (maxTargets <= 0)
            {
                enemies = owner.GetAllTargets(999, targetCheckRadius);
            } 
            else if (maxTargets > 1)
            {
                enemies = owner.GetAllTargets(maxTargets, targetCheckRadius);
            }

            
            // Attack all enemies in the list
            foreach (ChickenController target in enemies)
            {
                if (target)
                {
                    // Apply damage
                    target.TakeDamage(damage);
                
                    target.ApplyConcussion(stunTime);

                    target.ApplyForce(startPosition, pushForce);
                
                    // Play impact effect
                    PlayImpactEffect(target.transform.position, Quaternion.identity);
                }
            }
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
    }

    #endregion Effects ---------------------------------------------------------------------------------
    




    

}
