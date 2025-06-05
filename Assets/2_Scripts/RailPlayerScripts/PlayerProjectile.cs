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

    private void FixedUpdate()
    {
        if (!_isInitialized) return;


        // Move the projectile
        _rigidbody.MovePosition(_rigidbody.position + _direction * (_speed * Time.fixedDeltaTime));


        // Destroy the projectile after its lifetime ends
        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            DestroyProjectile();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized) return;
        
        
        // Apply a force to the hit object
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(_direction * _pushForce, ForceMode.Impulse);
        }

        // Destroy the projectile on impact
        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        _weaponData.SpawnImpactEffect(transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void SetUpProjectile(SOWeapon weaponData, Vector3 direction)
    {
        if (_isInitialized) return;
        
        // Set up the projectile
        _weaponData = weaponData;
        _speed = weaponData.ProjectileSpeed;
        _pushForce = weaponData.ProjectilePushForce;
        _lifetime = weaponData.ProjectileLifetime;
        _direction = direction;
        _damage = weaponData.Damage;
        _isInitialized = true;
    }
    
}
