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
    private Vector3 _direction;
    private bool _isInitialized;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!_isInitialized) return;


        // Move the projectile
        _rigidbody.MovePosition(_rigidbody.position + _direction * (_speed * Time.deltaTime));


        // Destroy the projectile after its lifetime ends
        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized) return;

        // Check if the projectile hit an object with a Rigidbody
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb)
        {
            // Apply a force to the hit object
            rb.AddForce(_direction * _pushForce, ForceMode.Impulse);
            _weaponData.SpawnImpactEffect(transform.position, Quaternion.identity);
        }

        // Destroy the projectile on impact
        Destroy(gameObject);
    }

    public void SetUp(SOWeapon weaponData, Vector3 direction)
    {
        if (_isInitialized) return;
        
        // Set up the projectile
        _weaponData = weaponData;
        _speed = weaponData.ProjectileSpeed;
        _pushForce = weaponData.ProjectilePushForce;
        _lifetime = weaponData.ProjectileLifetime;
        _direction = direction;
        _isInitialized = true;
    }
    
}
