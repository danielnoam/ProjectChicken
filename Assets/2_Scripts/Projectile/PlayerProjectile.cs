using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectile : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField, Self] private AudioSource audioSource;
    [SerializeField, Self] private Rigidbody rigidBody;
    
    
    public SOWeapon Weapon { get; private set; }
    public RailPlayer Owner { get; private set;  }
    public ChickenController Target { get; private set;  }
    public Vector3 StartDirection { get; private set; }
    public float Damage { get; private set; }
    public float Lifetime { get; private set; }
    public bool IsInitialized { get; private set; }

    public List<ProjectileBehaviorBase> ProjectileSpecificBehaviors { get; private set; }
    
    public AudioSource AudioSource => audioSource;
    public Rigidbody Rigidbody => rigidBody;


    private void OnValidate() { this.ValidateRefs(); }
    

    private void Update()
    {
        if (!IsInitialized) return;
        
        CheckLiftTime();
    }


    private void FixedUpdate()
    {
        if (!IsInitialized) return;
        
        ApplyMovementBehaviors(this, Owner);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsInitialized) return;
        
        if (other.TryGetComponent(out ChickenController collision))
        {
            // Apply custom behaviors on collision
            ApplyCollisionBehaviors(this, Owner, collision);
        
            // Play impact effect
            Weapon?.PlayImpactEffect(transform.position, Quaternion.identity);
        
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
        ApplyDestroyBehaviors(this, Owner);
        
        // Destroy the projectile object
        Destroy(gameObject);
    }

    


    #region SetUp -------------------------------------------------------------------------

    public void SetUpProjectile(SOWeapon weapon, RailPlayer player, ChickenController target)
    {
        if (IsInitialized) return;
    
        // Set up the projectile
        Weapon = weapon;
        Owner = player;
    
        // Create unique behavior instances for this projectile
        ProjectileSpecificBehaviors = CreateUniqueBehaviorInstances(weapon.ProjectileBehaviors);
        
        Lifetime = weapon.ProjectileLifetime;
        Damage = weapon.Damage;
        StartDirection = player.GetAimDirection();
        rigidBody.rotation = Quaternion.LookRotation(StartDirection);
        Target = target;
        IsInitialized = true;
    
        // Apply custom behaviors on spawn
        ApplySpawnBehaviors(this, player);
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

    private void ApplySpawnBehaviors(PlayerProjectile projectile, RailPlayer owner )
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorSpawn(projectile, owner);
        }
    }
    
    
    private void ApplyMovementBehaviors(PlayerProjectile projectile, RailPlayer owner)
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorMovement(projectile, owner);
        }
    }
    
    private void ApplyCollisionBehaviors(PlayerProjectile projectile, RailPlayer owner, ChickenController collision)
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorCollision(projectile, owner, collision);
        }
    }
    
    private void ApplyDestroyBehaviors(PlayerProjectile projectile, RailPlayer owner)
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorDestroy(projectile, owner);
        }
    }
    
    private void ApplyDrawGizmoBehaviors(PlayerProjectile projectile, RailPlayer owner)
    {
        foreach (ProjectileBehaviorBase behavior in ProjectileSpecificBehaviors)
        {
            behavior.OnBehaviorDrawGizmos(projectile, owner);
        }
    }

    #endregion Projectile Behaviors Calls ---------------------------------------------------------------
    
    
    
    #region Editor -------------------------------------------------------------------------
#if UNITY_EDITOR


    private void OnDrawGizmos()
    {
        
        if (Application.isPlaying && IsInitialized)
        {
            ApplyDrawGizmoBehaviors(this, Owner);
        }

    }


#endif
    #endregion Editor -------------------------------------------------------------------------
    
}
