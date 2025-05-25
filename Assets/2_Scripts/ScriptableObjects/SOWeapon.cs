using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Scriptable Objects/New Weapon")]
public class SOWeapon : ScriptableObject
{
    [Header("Weapon Settings")]
    [SerializeField, Min(0)] private float damage = 10f;
    [SerializeField, Min(0)] private float fireRate = 1f;
    
    [Header("Projectile Settings")]
    [SerializeField, Min(0)] private float projectileSpeed = 100f;
    [SerializeField, Min(0)] private float projectilePushForce;
    [SerializeField, Min(0)] private float projectileLifetime = 5f;
    [SerializeField] private GameObject projectilePrefab;
}
