using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectile : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private SOWeapon _weaponData;
    private float _speed;
    private float _pushForce;
    private float _lifetime;
    private float _damage;
    private Vector3 _direction;
    private bool _isInitialized;

    
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
        
        ProjectileHit(other);

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
    
    
    public void SetUpProjectile(SOWeapon weaponData, Vector3 direction)
    {
        if (_isInitialized) return;
        
        _weaponData = weaponData;
        _speed = weaponData.ProjectileSpeed;
        _pushForce = weaponData.ProjectilePushForce;
        _lifetime = weaponData.ProjectileLifetime;
        _direction = direction;
        _damage = weaponData.Damage;
        _isInitialized = true;
    }

    #endregion Base -------------------------------------------------------------------------
    

    #region Custom Behivors -----------------------------------------------------------------------------

    protected virtual void MoveProjectile()
    {
        _rigidbody?.MovePosition(_rigidbody.position + _direction * (_speed * Time.fixedDeltaTime));
    }

    
    protected virtual void DestroyProjectile()
    {
        _weaponData?.SpawnImpactEffect(transform.position, Quaternion.identity);
        Destroy(gameObject);
    }


    protected virtual void ProjectileHit(Collider other)
    {
        // Apply a force to the hit object
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(_direction * _pushForce, ForceMode.Impulse);
        }

        // Destroy the projectile on impact
        DestroyProjectile();
    }

    #endregion Custom Behivors -----------------------------------------------------------------------------


    


    
}
