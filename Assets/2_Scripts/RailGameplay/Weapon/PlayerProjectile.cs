using System;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;




[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectile : MonoBehaviour, IPooledObject
{
    
    [Header("References")]
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;
    [SerializeField, Self, HideInInspector] private Rigidbody rigidBody;
    
    private RailPlayer _owner;
    private float _lifetime;
    private bool _isInitialized;
    private List<ProjectileBehaviorBase> _projectileBehaviors;
    private Vector3 _aimOffsetFromSpline;
    
    public Rigidbody Rigidbody => rigidBody;
    public SOWeaponData WeaponData { get; private set; }
    public WeaponInstance WeaponInstance { get; private set; }
    public ChickenController Target { get; private set;  }
    public Vector3 CurrentTargetPosition { get; private set; }
    public Vector3 StartDirection { get; private set; }
    public float StartTime { get; private set; }



    private void OnValidate()
    {
        this.ValidateRefs();
    }
    

    private void Update()
    {
        if (!_isInitialized) return;
        
        CheckLiftTime();
        UpdateTargetPosition();
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
            WeaponInstance.PlayImpactEffect(transform.position, Quaternion.identity);
            foreach (var behavior in _projectileBehaviors)
            {
                behavior.OnCollision(this, _owner, collision);
            }
            ReturnObjectToPool();
        }
    }
    
    private void CheckLiftTime()
    {
        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            ReturnObjectToPool();
        }
    }
    

    private void UpdateTargetPosition()
    {
        if (!_owner) return;
        
        Vector3 currentEnemySplinePosition = _owner.LevelManager.EnemyPosition;
        
        if (_owner.AlignToSplineDirection)
        {
            CurrentTargetPosition = currentEnemySplinePosition + (_owner.SplineRotation * _aimOffsetFromSpline);
        }
        else
        {
            CurrentTargetPosition = currentEnemySplinePosition + _aimOffsetFromSpline;
        }
    }


    #region Pool Object -------------------------------------------------------------------------

    private void ReturnObjectToPool()
    {
        if (!_isInitialized) return;
        
        foreach (var behavior in _projectileBehaviors)
        {
            behavior.OnDestroy(this, _owner);
        }
        UnInitializeProjectile();
        ObjectPooler.ReturnObjectToPool(gameObject);
    }
    
    public void OnPoolGet()
    {

        
    }

    public void OnPoolReturn()
    {

    }

    public void OnPoolRecycle()
    {
        if (!_isInitialized) return;
        UnInitializeProjectile();
    }
    
    
    
    

    #endregion Pool Object -------------------------------------------------------------------------


    

    #region SetUp -------------------------------------------------------------------------

    public void SetUpProjectile(SOWeaponData weaponData, RailPlayer owner, WeaponInstance weaponInstance, ChickenController target)
    {
        if (_isInitialized) return;
        
        StartTime = Time.time;
        WeaponData = weaponData;
        WeaponInstance = weaponInstance;
        Target = target;
        _owner = owner;
        _lifetime = weaponData.ProjectileLifetime;
        _projectileBehaviors = CreateUniqueBehaviorInstances(weaponData.ProjectileBehaviors);
        
        
        Vector3 enemySplinePosition = owner.LevelManager.EnemyPosition;
        Vector3 currentAimPosition = owner.PlayerAiming.AimWorldPosition.position;
        if (owner.AlignToSplineDirection)
        {
            _aimOffsetFromSpline = Quaternion.Inverse(owner.SplineRotation) * (currentAimPosition - enemySplinePosition);
        }
        else
        {
            _aimOffsetFromSpline = currentAimPosition - enemySplinePosition;
        }
        UpdateTargetPosition();
        
        
        StartDirection = (owner.PlayerAiming.AimWorldPosition.position - transform.position).normalized;
        rigidBody.rotation = Quaternion.LookRotation(StartDirection);
        
        
        weaponInstance.PlayFireEffect(transform.position, Quaternion.identity, audioSource);
        foreach (var behavior in _projectileBehaviors)
        {
            behavior.OnSpawn(this, _owner);
        }
        
        _isInitialized = true;
    }
    
    private void UnInitializeProjectile()
    {
        _isInitialized = false;
        
        _owner = null;
        _lifetime = 0f;
        WeaponData = null;
        WeaponInstance = null;
        Target = null;
        _projectileBehaviors = null;
        _aimOffsetFromSpline = Vector3.zero;
        CurrentTargetPosition = Vector3.zero;
        StartDirection = Vector3.zero;
        StartTime = 0f;
    }
    
    public void SetUpProjectileWithCustomBehaviors(SOWeaponData weaponData, RailPlayer owner, WeaponInstance weaponInstance, ChickenController target, List<ProjectileBehaviorBase> projectileBehaviors)
    {
        if (_isInitialized) return;

        StartTime = Time.time;
        WeaponData = weaponData;
        WeaponInstance = weaponInstance;
        Target = target;
        _owner = owner;
        _lifetime = weaponData.ProjectileLifetime;
        _projectileBehaviors = CreateUniqueBehaviorInstances(projectileBehaviors);
        
        
        Vector3 enemySplinePosition = owner.LevelManager.EnemyPosition;
        Vector3 currentAimPosition = owner.PlayerAiming.AimWorldPosition.position;
        if (owner.AlignToSplineDirection)
        {
            _aimOffsetFromSpline = Quaternion.Inverse(owner.SplineRotation) * (currentAimPosition - enemySplinePosition);
        }
        else
        {
            _aimOffsetFromSpline = currentAimPosition - enemySplinePosition;
        }
        UpdateTargetPosition();
        
        
        StartDirection = (owner.PlayerAiming.AimWorldPosition.position - transform.position).normalized;
        rigidBody.rotation = Quaternion.LookRotation(StartDirection);
        
        
        weaponInstance.PlayFireEffect(transform.position, Quaternion.identity, audioSource);
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
    
    
    
    
    [Obsolete]
    private void DestroyProjectile()
    {
        foreach (var behavior in _projectileBehaviors)
        {
            behavior.OnDestroy(this, _owner);
        }
        
        _isInitialized = false;
        Destroy(gameObject);
    }

    
}
