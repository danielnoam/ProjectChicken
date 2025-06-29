using System;
using System.Collections.Generic;
using System.Linq;
using KBCore.Refs;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectile : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;
    [SerializeField, Self, HideInInspector] private Rigidbody rigidBody;
    private RailPlayer _owner;
    private float _lifetime;
    private bool _isInitialized;
    private List<ProjectileBehaviorBase> _projectileBehaviors;
    
    
    public SOWeapon Weapon { get; private set; }
    public ChickenController Target { get; private set;  }
    public Vector3 StartDirection { get; private set; }
    public Rigidbody Rigidbody => rigidBody;


    private void OnValidate()
    {
        this.ValidateRefs();
    }
    

    private void Update()
    {
        if (!_isInitialized) return;
        
        CheckLiftTime();
    }


    private void FixedUpdate()
    {
        if (!_isInitialized) return;
        
        foreach (var behavior in _projectileBehaviors)
        {
            behavior.OnMovement(this, _owner);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized) return;
        
        if (other.TryGetComponent(out ChickenController collision))
        {
            Weapon.PlayImpactEffect(transform.position, Quaternion.identity);
            collision.TakeDamage(Weapon.Damage);
            foreach (var behavior in _projectileBehaviors)
            {
                behavior.OnCollision(this, _owner, collision);
            }
            DestroyProjectile();
        }
    }
    
    private void CheckLiftTime()
    {
        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            DestroyProjectile();
        }
    }
    
    
    private void DestroyProjectile()
    {
        foreach (var behavior in _projectileBehaviors)
        {
            behavior.OnDestroy(this, _owner);
        }
        _isInitialized = false;
        Destroy(gameObject);
    }

    


    #region SetUp -------------------------------------------------------------------------

    public void SetUpProjectile(SOWeapon weapon, RailPlayer player, ChickenController target)
    {
        if (_isInitialized) return;
        
        Weapon = weapon;
        _owner = player;
        _projectileBehaviors = CreateUniqueBehaviorInstances(weapon.ProjectileBehaviors);
        _lifetime = weapon.ProjectileLifetime;
        StartDirection = player.GetAimDirectionFromBarrelPosition(transform.position, weapon.ConvergenceMultiplier);
        rigidBody.rotation = Quaternion.LookRotation(StartDirection);
        Target = target;
        weapon.PlayFireEffect(transform.position, Quaternion.identity, audioSource);
        foreach (var behavior in _projectileBehaviors)
        {
            behavior.OnSpawn(this, _owner);
        }
        
        _isInitialized = true;
    }
    
    public void SetUpMiniProjectile(List<ProjectileBehaviorBase> projectileBehaviors, SOWeapon weapon, RailPlayer player, ChickenController target)
    {
        if (_isInitialized) return;

        Weapon = weapon;
        _owner = player;
        _projectileBehaviors = CreateUniqueBehaviorInstances(projectileBehaviors);
        _lifetime = weapon.ProjectileLifetime;
        StartDirection = player.GetAimDirectionFromBarrelPosition(transform.position, weapon.ConvergenceMultiplier);
        rigidBody.rotation = Quaternion.LookRotation(StartDirection);
        Target = target;
        weapon.PlayFireEffect(transform.position, Quaternion.identity, audioSource);
        foreach (var behavior in _projectileBehaviors)
        {
            behavior.OnSpawn(this, _owner);
        }
        
        _isInitialized = true;
    }
    
    
    private List<ProjectileBehaviorBase> CreateUniqueBehaviorInstances(List<ProjectileBehaviorBase> originalBehaviors)
    {
        return originalBehaviors.Select(CreateBehaviorCopy).ToList();
    }

    private ProjectileBehaviorBase CreateBehaviorCopy(ProjectileBehaviorBase original)
    {
        var behaviorType = original.GetType();
        var copy = (ProjectileBehaviorBase)Activator.CreateInstance(behaviorType);
        
        var fields = behaviorType.GetFields(
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance
        );
    
        foreach (var field in fields)
        {
            if (field.IsNotSerialized || field.IsStatic || field.IsLiteral) continue;
            field.SetValue(copy, field.GetValue(original));
        }
    
        return copy;
    }

    #endregion SetUp -------------------------------------------------------------------------
    
    
    
    #region Editor -------------------------------------------------------------------------
#if UNITY_EDITOR


    private void OnDrawGizmos()
    {
        if (Application.isPlaying && _isInitialized)
        {
            ApplyDrawGizmoBehaviors(this, _owner);
        }
    }
    
    private void ApplyDrawGizmoBehaviors(PlayerProjectile projectile, RailPlayer owner)
    {
        foreach (ProjectileBehaviorBase behavior in _projectileBehaviors)
        {
            behavior.OnDrawGizmos(projectile, owner);
        }
    }


#endif
    #endregion Editor -------------------------------------------------------------------------
    
}
