using System.Collections.Generic;
using UnityEngine;
using VInspector;



[CreateAssetMenu(fileName = "New WeaponData", menuName = "Scriptable Objects/New WeaponData")]
public class SOWeaponData : ScriptableObject
{
    [Header("Weapon Settings")]
    [SerializeField] private string weaponName = "New WeaponData";
    [SerializeField] private string weaponDescription = "A WeaponData";
    [SerializeField] private WeaponDurationType weaponDurationType = WeaponDurationType.Permanent;
    [SerializeField, Min(0), ShowIf("weaponDurationType", WeaponDurationType.TimeBased)] private float timeLimit = 10f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponDurationType", WeaponDurationType.AmmoBased)] private float ammoLimit = 3f;[EndIf]
    [SerializeField, Min(0)] private float damage = 10f;
    [SerializeField, Min(0)] private float fireRate = 1f;
    [SerializeField, Min(0)] private float projectileLifetime = 5f;
    [SerializeField] private PlayerProjectile playerProjectilePrefab;
    [SerializeReference] private List<ProjectileBehaviorBase> projectileBehaviors = new List<ProjectileBehaviorBase>();
    
    [Header("Projectile Spawn Effect")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private ParticleSystem spawnEffectPrefab;
    
    [Header("Projectile Impact Effect")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private ParticleSystem impactEffectPrefab;

    public string WeaponName => weaponName;
    public string WeaponDescription => weaponDescription;
    public WeaponDurationType WeaponDurationType => weaponDurationType;
    public float Damage => damage;
    public float FireRate => fireRate;
    public float TimeLimit => timeLimit;
    public float AmmoLimit => ammoLimit;
    public float ProjectileLifetime => projectileLifetime;
    public  List<ProjectileBehaviorBase> ProjectileBehaviors => projectileBehaviors;
    

    
    public PlayerProjectile CreateProjectile(Vector3 position, RailPlayer owner)
    {
        if (!playerProjectilePrefab) return null;
        
        // Instantiate the base projectile
        PlayerProjectile projectile = Instantiate(playerProjectilePrefab, position, Quaternion.identity);
        
        // Initialize the projectile
        projectile.SetUpProjectile(this, owner);
        
        // Spawn effect
        PlaySpawnEffect(projectile.transform.position, Quaternion.identity);
        
        return projectile;
    }
    
    


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


    

}
