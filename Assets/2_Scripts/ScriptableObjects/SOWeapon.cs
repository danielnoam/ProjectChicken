using System.Collections.Generic;
using UnityEngine;
using VInspector;



[CreateAssetMenu(fileName = "New Weapon", menuName = "Scriptable Objects/New Weapon")]
public class SOWeapon : ScriptableObject
{
    [Header("Weapon Settings")]
    [SerializeField] private string weaponName = "New Weapon";
    [SerializeField] private string weaponDescription = "A Weapon";
    [SerializeField] private WeaponDurationType weaponDurationType = WeaponDurationType.Permanent;
    [SerializeField, Min(0)] private float damage = 10f;
    [SerializeField, Min(0)] private float fireRate = 1f;
    [SerializeField, Min(0), ShowIf("weaponDurationType", WeaponDurationType.TimeBased)] private float timeLimit = 10f;[EndIf]
    [SerializeField, Min(0), ShowIf("weaponDurationType", WeaponDurationType.AmmoBased)] private float ammoLimit = 3f;[EndIf]
    
    [Header("Projectile Settings")]
    [SerializeField, Min(0)] private float projectileSpeed = 100f;
    [SerializeField, Min(0)] private float projectilePushForce;
    [SerializeField, Min(0)] private float projectileLifetime = 5f;
    [SerializeField] private PlayerProjectile playerProjectilePrefab;
    
    
    [Header("Impact Effect")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private GameObject impactEffectPrefab;

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
        
        return projectile;
    }
    
    
    public void SpawnImpactEffect(Vector3 position, Quaternion rotation)
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
}
