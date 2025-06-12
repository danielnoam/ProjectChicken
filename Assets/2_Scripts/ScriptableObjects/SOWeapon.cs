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
    [SerializeField] private WeaponType weaponType = WeaponType.Projectile;
    [SerializeField] private WeaponDurationType weaponDurationType = WeaponDurationType.Permanent;
    [SerializeField, Min(0), ShowIf("weaponDurationType", WeaponDurationType.TimeBased)] private float timeLimit = 10f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponDurationType", WeaponDurationType.AmmoBased)] private float ammoLimit = 3f;[EndIf]
    [SerializeField, Min(0)] private float damage = 10f;
    [SerializeField, Min(0)] private float fireRate = 1f;

    
    [ShowIf("weaponType", WeaponType.Projectile)]
    [Header("Projectile Settings")]
    [SerializeField] private PlayerProjectile playerProjectilePrefab;
    [SerializeField, Min(0)] private float projectileLifetime = 5f;
    [SerializeReference] private List<ProjectileBehaviorBase> projectileBehaviors = new List<ProjectileBehaviorBase>();
    [EndIf]
    
    [ShowIf("weaponType", WeaponType.Hitscan)]
    [Header("Hitscan Settings")]
    [SerializeField, Min(0.1f)] private float radius = 3f;
    [SerializeField, Min(0), Tooltip("0 = Means infinite targets")] private int maxTargets = 1;
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
    public WeaponDurationType WeaponDurationType => weaponDurationType;
    public WeaponType WeaponType => weaponType;
    public float Damage => damage;
    public float FireRate => fireRate;
    public float TimeLimit => timeLimit;
    public float AmmoLimit => ammoLimit;
    public float ProjectileLifetime => projectileLifetime;
    public  List<ProjectileBehaviorBase> ProjectileBehaviors => projectileBehaviors;
    

    


    #region Weapon Usage ---------------------------------------------------------------------------------

    
    public void Fire(Vector3 position, RailPlayer owner)
    {
        switch (weaponType)
        {
            case WeaponType.Projectile:
                CreateProjectile(position, owner);
                break;
            case WeaponType.Hitscan:
                Hitscan(position, owner);
                break;
        }
    }
    private PlayerProjectile CreateProjectile(Vector3 position, RailPlayer owner)
    {
        if (!playerProjectilePrefab || weaponType != WeaponType.Projectile) return null;
        
        // Instantiate the base projectile
        PlayerProjectile projectile = Instantiate(playerProjectilePrefab, position, Quaternion.identity);
        
        // Initialize the projectile
        projectile.SetUpProjectile(this, owner);
        
        // Spawn effect
        PlayFireEffect(projectile.transform.position, Quaternion.identity, projectile.AudioSource);
        
        return projectile;
    }
    
    
    private void Hitscan(Vector3 startPosition ,RailPlayer owner)
    {
        if (weaponType != WeaponType.Hitscan) return;
        
        // Play spawn effect
        PlayFireEffect(startPosition, Quaternion.identity);

        // Get enemy target
        if (maxTargets == 1)
        {
            ChickenController enemy = owner.GetTarget(radius);
            
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
                enemies = owner.GetAllTargets(999, radius);
            } 
            else if (maxTargets > 1)
            {
                enemies = owner.GetAllTargets(maxTargets, radius);
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
    

    #endregion Weapon Usage ---------------------------------------------------------------------------------




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
