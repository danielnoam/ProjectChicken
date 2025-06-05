using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Collision_ExplodeOnImpact", menuName = "Scriptable Objects/Projectile Behavior/Collision_ExplodeOnImpact")]
public class SOProjectileBehavior_ExplodeOnImpact : SOProjectileBehaviorBase
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionMinRadius = 1f;
    [SerializeField] private float explosionMaxRadius = 5f;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionDamage = 10f;
    
    public override void OnSpawn(PlayerProjectile projectile)
    {

    }

    public override void OnMovement(PlayerProjectile projectile)
    {

    }

    public override void OnCollision(PlayerProjectile projectile, ChickenEnemy enemy)
    {
        // Get the collision point position
        Vector3 explosionCenter = enemy.transform.position;
        
        // Create a sphere cast to detect all colliders within the explosion radius
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, explosionMaxRadius);
        
        
        Debug.Log(hitColliders);
        
        // Check each collider for ChickenEnemy
        foreach (Collider hitCollider in hitColliders)
        {
            // Try to get ChickenEnemy component
            ChickenEnemy chickenEnemy = hitCollider.GetComponent<ChickenEnemy>();
            if (chickenEnemy)
            {
                // Calculate distance from explosion center to chicken
                float distance = Vector3.Distance(explosionCenter, hitCollider.transform.position);
                float distanceMultiplier = CalculateDistanceMultiplier(distance);
                
                // Apply damage based on distance
                float finalDamage = explosionDamage * distanceMultiplier;
                chickenEnemy.TakeDamage(finalDamage);
                
                // Apply force based on distance
                Rigidbody enemyRigidbody = hitCollider.GetComponent<Rigidbody>();
                if (enemyRigidbody)
                {
                    Vector3 forceDirection = (hitCollider.transform.position - explosionCenter).normalized;
                    float finalForce = explosionForce * distanceMultiplier;
                    enemyRigidbody.AddForce(forceDirection * finalForce, ForceMode.Impulse);
                }
            }
        }
    }

    private float CalculateDistanceMultiplier(float distance)
    {
        // If within minimum radius, apply full damage/force
        if (distance <= explosionMinRadius)
        {
            return 1f;
        }
        
        // If beyond maximum radius, no damage/force
        if (distance >= explosionMaxRadius)
        {
            return 0f;
        }
        
        // Linear interpolation between min and max radius
        // Closer to center = higher multiplier, farther = lower multiplier
        float normalizedDistance = (distance - explosionMinRadius) / (explosionMaxRadius - explosionMinRadius);
        return 1f - normalizedDistance; // Inverted so closer = higher value
    }

    public override void OnDestroy()
    {

    }

    public override void OnDrawGizmos(PlayerProjectile projectile)
    {
        // Draw the explosion radius spheres
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(projectile.transform.position, explosionMaxRadius);
            
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(projectile.transform.position, explosionMinRadius);
    }
}