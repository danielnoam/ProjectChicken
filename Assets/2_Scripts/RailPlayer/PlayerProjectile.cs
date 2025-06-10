using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectile : MonoBehaviour
{
    
    public Rigidbody Rigidbody { get; private set; }
    public SOWeaponData WeaponDataData { get; private set; }
    public RailPlayer Owner { get; private set;  }
    public ChickenEnemy Target { get; private set;  }
    public Vector3 StartDirection { get; private set; }
    public float Damage { get; private set; }
    public float Lifetime { get; private set; }
    public bool IsInitialized { get; private set; }

    public List<ProjectileBehaviorBase> ProjectileSpecificBehaviors { get; private set; }

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        if (!IsInitialized) return;
        
        CheckLiftTime();
    }


    private void FixedUpdate()
    {
        if (!IsInitialized) return;
        
        ApplyMovementBehaviors(this, Owner, Target);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsInitialized) return;
        
        if (other.TryGetComponent(out ChickenEnemy collision))
        {
            // Apply custom behaviors on collision
            ApplyCollisionBehaviors(this, Owner, Target, collision);
        
            // Play impact effect
            WeaponDataData?.PlayImpactEffect(transform.position, Quaternion.identity);
        
            // Apply damage to the enemy object
            collision.TakeDamage(Damage);
        
        
            // Destroy the projectile on impact
            DestroyProjectile();
        }


    }
    
    private void CheckLiftTime()
    {
        Lifetime -= Time.deltaTime;
        if (Lifetime <= 0f)
        {
            DestroyProjectile();
        }
    }
    
    
    private void DestroyProjectile()
    {
        // Apply custom behaviors on destroy
        ApplyDestroyBehaviors(this, Owner, Target);
        
        // Destroy the projectile object
        Destroy(gameObject);
    }

    


    #region SetUp -------------------------------------------------------------------------

    public void SetUpProjectile(SOWeaponData weaponDataData, RailPlayer player)
    {
        if (IsInitialized) return;
    
        // Set up the projectile
        WeaponDataData = weaponDataData;
        Owner = player;
    
        // Create unique behavior instances for this projectile
        ProjectileSpecificBehaviors = CreateUniqueBehaviorInstances(weaponDataData.ProjectileBehaviors);
    
        Lifetime = weaponDataData.ProjectileLifetime;
        Damage = weaponDataData.Damage;
        StartDirection = player.GetAimDirection();
        Target = player.GetTarget();
        IsInitialized = true;
    
        // Apply custom behaviors on spawn
        ApplySpawnBehaviors(this, player, Target);
    }
    
    private List<ProjectileBehaviorBase> CreateUniqueBehaviorInstances(List<ProjectileBehaviorBase> originalBehaviors)
    {
        List<ProjectileBehaviorBase> uniqueBehaviors = new List<ProjectileBehaviorBase>();
    
        foreach (ProjectileBehaviorBase originalBehavior in originalBehaviors)
        {
            // Create a copy of the behavior for this specific projectile
            ProjectileBehaviorBase behaviorCopy = CreateBehaviorCopy(originalBehavior);
            uniqueBehaviors.Add(behaviorCopy);
        }
    
        return uniqueBehaviors;
    }

    private ProjectileBehaviorBase CreateBehaviorCopy(ProjectileBehaviorBase original)
    {
        // Use reflection to create a new instance of the behavior type
        System.Type behaviorType = original.GetType();
        ProjectileBehaviorBase copy = (ProjectileBehaviorBase)System.Activator.CreateInstance(behaviorType);
    
        // Copy serialized fields from original to copy using reflection
        System.Reflection.FieldInfo[] fields = behaviorType.GetFields(
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance
        );
    
        foreach (var field in fields)
        {
            // Skip fields that shouldn't be copied
            if (field.IsNotSerialized || field.IsStatic || field.IsLiteral) continue;
        
            // Copy the field value
            field.SetValue(copy, field.GetValue(original));
        }
    
        return copy;
    }

    #endregion

    
    
    #region Projectile Behaviors Calls ---------------------------------------------------------------

    private void ApplySpawnBehaviors(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorSpawn(projectile, owner, target);
        }
    }
    
    
    private void ApplyMovementBehaviors(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorMovement(projectile, owner, target);
        }
    }
    
    private void ApplyCollisionBehaviors(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target, ChickenEnemy collision)
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorCollision(projectile, owner, target, collision);
        }
    }
    
    private void ApplyDestroyBehaviors(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorDestroy(projectile, owner , target);
        }
    }
    
    private void ApplyDrawGizmoBehaviors(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorDrawGizmos(projectile, owner , target);
        }
    }

    #endregion Projectile Behaviors Calls ---------------------------------------------------------------
    
    
    
    #region Editor -------------------------------------------------------------------------
#if UNITY_EDITOR


    private void OnDrawGizmos()
    {
        ApplyDrawGizmoBehaviors(this, Owner, Target);
    }


#endif
    #endregion Editor -------------------------------------------------------------------------
    
}
