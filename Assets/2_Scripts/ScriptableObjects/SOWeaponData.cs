using System.Collections.Generic;
using UnityEngine;
using VInspector;



[CreateAssetMenu(fileName = "New WeaponData", menuName = "Scriptable Objects/New WeaponData")]
public class SOWeaponData : ScriptableObject
{
    [Header("WeaponData Settings")]
    [SerializeField] private string weaponName = "New WeaponData";
    [SerializeField] private string weaponDescription = "A WeaponData";
    [SerializeField, Min(0)] private float damage = 10f;
    [SerializeField, Min(0)] private float fireRate = 1f;
    [SerializeField] private WeaponDurationType weaponDurationType = WeaponDurationType.Permanent;
    [SerializeField, Min(0), ShowIf("weaponDurationType", WeaponDurationType.TimeBased)] private float timeLimit = 10f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponDurationType", WeaponDurationType.AmmoBased)] private float ammoLimit = 3f;[EndIf]
    
    [Header("Projectile Settings")]
    [SerializeField, Min(0)] private float projectileSpeed = 100f;
    [SerializeField, Min(0)] private float projectilePushForce;
    [SerializeField, Min(0)] private float projectileLifetime = 5f;
    [SerializeField] private PlayerProjectile playerProjectilePrefab;
    [SerializeReference] private List<ProjectileBehaviorBase> projectileBehaviors = new List<ProjectileBehaviorBase>();
    
    [Header("Projectile Spawn Effect")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private ParticleSystem spawnEffectPrefab;
    
    [Header("Impact Effect")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private ParticleSystem impactEffectPrefab;

    public string WeaponName => weaponName;
    public string WeaponDescription => weaponDescription;
    public WeaponDurationType WeaponDurationType => weaponDurationType;
    public float Damage => damage;
    public float FireRate => fireRate;
    public float TimeLimit => timeLimit;
    public float AmmoLimit => ammoLimit;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectilePushForce => projectilePushForce;
    public float ProjectileLifetime => projectileLifetime;
    
    

    
    public PlayerProjectile CreateProjectile(Vector3 position, Vector3 direction)
    {
        if (!playerProjectilePrefab) return null;
        
        // Instantiate the base projectile
        PlayerProjectile projectile = Instantiate(playerProjectilePrefab, position, Quaternion.identity);
        
        // Initialize the projectile
        projectile.SetUpProjectile(this, direction);
        
        // Spawn effect
        PlaySpawnEffect(projectile.transform.position, Quaternion.identity);
        
        return projectile;
    }
    
    



    #region Projectile Effects --------------------------------------------------------------------

    
    public void PlayImpactEffect(Vector3 position, Quaternion rotation)
    {
        if (impactEffectPrefab)
        {
            Instantiate(impactEffectPrefab, position, rotation);
        }
        
        if (impactSound)
        {
            AudioSource.PlayClipAtPoint(impactSound, position);
        }
    }

    private void PlaySpawnEffect(Vector3 position, Quaternion rotation)
    {
        if (spawnEffectPrefab)
        {
            Instantiate(spawnEffectPrefab, position, rotation);
        }
        
        if (spawnSound)
        {
            AudioSource.PlayClipAtPoint(spawnSound, position);
        }
    }
    
    

    #endregion Projectile Effects --------------------------------------------------------------------


    #region Projectile Behaviors ---------------------------------------------------------------

    public void OnProjectileSpawn(PlayerProjectile projectile)
    {
        foreach (ProjectileBehaviorBase behavior in projectileBehaviors)
        {
            behavior.OnBehaviorSpawn(projectile);
        }
    }
    
    
    public void OnProjectileMovement(PlayerProjectile projectile)
    {
        foreach (ProjectileBehaviorBase behavior in projectileBehaviors)
        {
            behavior.OnBehaviorMovement(projectile);
        }
    }
    
    public void OnProjectileCollision(PlayerProjectile projectile, ChickenEnemy enemy)
    {
        foreach (ProjectileBehaviorBase behavior in projectileBehaviors)
        {
            behavior.OnBehaviorCollision(projectile, enemy);
        }
    }
    
    public void OnProjectileDestroy(PlayerProjectile projectile)
    {
        foreach (ProjectileBehaviorBase behavior in projectileBehaviors)
        {
            behavior.OnBehaviorDestroy(projectile);
        }
    }
    
    public void OnProjectileDrawGizmos(PlayerProjectile projectile)
    {
        foreach (ProjectileBehaviorBase behavior in projectileBehaviors)
        {
            behavior.OnBehaviorDrawGizmos(projectile);
        }
    }

    #endregion Projectile Behaviors ---------------------------------------------------------------
    

}
