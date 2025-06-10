using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectile : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private SOWeaponData _weaponDataData;
    private float _speed;
    private float _pushForce;
    private float _lifetime;
    private float _damage;
    private Vector3 _direction;
    private bool _isInitialized;

    
    public Rigidbody Rigidbody => _rigidbody;
    public SOWeaponData WeaponDataData => _weaponDataData;
    public float Speed => _speed;
    public float PushForce => _pushForce;
    public Vector3 Direction => _direction;
    public float Damage => _damage;
    
    
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        if (!_isInitialized) return;
        
        CheckLiftTime();
    }


    private void FixedUpdate()
    {
        if (!_isInitialized) return;
        
        MoveProjectile();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized) return;


        
        if (TryGetComponent(out ChickenEnemy enemy))
        {
            
            ProjectileHit(enemy);
        }


    }
    
    

        
    #region Base -------------------------------------------------------------------------

    private void CheckLiftTime()
    {
        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            DestroyProjectile();
        }
    }
    
    
    public void SetUpProjectile(SOWeaponData weaponDataData, Vector3 direction)
    {
        if (_isInitialized) return;
        
        _weaponDataData = weaponDataData;
        _speed = weaponDataData.ProjectileSpeed;
        _pushForce = weaponDataData.ProjectilePushForce;
        _lifetime = weaponDataData.ProjectileLifetime;
        _direction = direction;
        _damage = weaponDataData.Damage;
        _isInitialized = true;
        
        _weaponDataData?.OnProjectileSpawn(this);
    }

    #endregion Base -------------------------------------------------------------------------
    

    #region Custom Behivors -----------------------------------------------------------------------------

    protected virtual void MoveProjectile()
    {
        _weaponDataData?.OnProjectileMovement(this);
    }

    
    protected virtual void DestroyProjectile()
    {
        _weaponDataData?.OnProjectileDestroy(this);
        
        Destroy(gameObject);
    }


    protected virtual void ProjectileHit(ChickenEnemy enemy)
    {
        
        _weaponDataData?.OnProjectileCollision(this, enemy);
        
        // Apply a force to the hit object
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(_direction * _pushForce, ForceMode.Impulse);
        }
        
        // Apply damage to the enemy object
        enemy.TakeDamage(_damage);
        

        // Play impact effect
        _weaponDataData?.PlayImpactEffect(transform.position, Quaternion.identity);
        
        
        // Destroy the projectile on impact
        DestroyProjectile();
    }

    #endregion Custom Behivors -----------------------------------------------------------------------------



#if UNITY_EDITOR
    #region Editor -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        _weaponDataData?.OnProjectileDrawGizmos(this);
    }

    #endregion Editor -------------------------------------------------------------------------
#endif

    
}
