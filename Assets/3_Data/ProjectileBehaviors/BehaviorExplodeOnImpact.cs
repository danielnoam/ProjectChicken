using System;
using UnityEngine;


public class BehaviorExplodeOnImpact : ProjectileBehaviorBase
{
    [SerializeField] private float minRadius = 2f;
    [SerializeField] private float maxRadius = 6f;
    [SerializeField] private float maxDamage = 25f;
    [SerializeField] private float maxForce = 25f;
    [SerializeField, Range(0,100)] private float stunChance = 25f;
    [SerializeField] private float maxStunTime = 2f;

    
    

    public override void OnSpawn(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnMovement(PlayerProjectile projectile, RailPlayer owner )
    {

    }

    public override void OnCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision)
    {
        // Get the collision point position
        Vector3 explosionCenter = collision.transform.position;
        
        // Create a sphere cast to detect all colliders within the explosion radius
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, maxRadius);
        
        // Check each collider for ChickenEnemy
        foreach (Collider hitCollider in hitColliders)
        {
            // Try to get ChickenEnemy component
            ChickenController chickenEnemy = hitCollider.GetComponent<ChickenController>();
            if (chickenEnemy)
            {
                // Calculate distance from explosion center to chicken
                float distance = Vector3.Distance(explosionCenter, hitCollider.transform.position);
                float distanceMultiplier = CalculateDistanceMultiplier(distance);
                
                // Apply damage based on distance
                float finalDamage = maxDamage * distanceMultiplier;
                chickenEnemy.TakeDamage(finalDamage);
                
                
                Vector3 forceDirection = (hitCollider.transform.position - explosionCenter).normalized;
                float finalForce = maxForce * distanceMultiplier;
                chickenEnemy.ApplyForce(forceDirection, finalForce);
                
                // Apply stun effect based on distance and chance
                if (UnityEngine.Random.Range(0f, 100f) <= stunChance)
                {
                    float finalStunTime = maxStunTime * distanceMultiplier;
                    chickenEnemy.ApplyConcussion(finalStunTime);
                    chickenEnemy.ApplyTorque(forceDirection, finalForce * 3);
                }

            }
        }
    }

    public override void OnDestroy(PlayerProjectile projectile, RailPlayer owner )
    {

    }

    public override void OnDrawGizmos(PlayerProjectile projectile, RailPlayer owner)
    {
        // Draw the explosion radius spheres
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(projectile.transform.position, maxRadius);
        Gizmos.DrawWireSphere(projectile.transform.position, minRadius);
    }
    
    
    
    private float CalculateDistanceMultiplier(float distance)
    {
        // If within minimum radius, apply full damage/force
        if (distance <= minRadius)
        {
            return 1f;
        }
        
        // If beyond maximum radius, no damage/force
        if (distance >= maxRadius)
        {
            return 0f;
        }
        
        // Linear interpolation between min and max radius
        // Closer to center = higher multiplier, farther = lower multiplier
        float normalizedDistance = (distance - minRadius) / (maxRadius - minRadius);
        return 1f - normalizedDistance; // Inverted so closer = higher value
    }
}