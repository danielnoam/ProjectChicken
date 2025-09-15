using System;
using DNExtensions;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, MinMaxRange(25, 200f)] private RangedFloat healthRange = new(50, 150);
    [SerializeField, MinMaxRange(1f, 100f)] private RangedFloat moveSpeedRange = new(1f, 5f);
    [SerializeField, MinMaxRange(5f, 50f)] private RangedFloat damageRange = new(10f, 30f);
    
    [Header("References")]
    [SerializeField] private Rigidbody rigidBody;

    private bool _initialized;
    private float _health;
    private float _damage;
    private float _moveSpeed;
    private Vector3 _moveDirection;
    
    public event Action<Obstacle> OnObstacleDestroyed;
    
    private void DestroyObstacle()
    {
        OnObstacleDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
    
    private void Update()
    {
        if (_initialized)
        {
            rigidBody.MovePosition(transform.position + _moveDirection * (_moveSpeed * Time.deltaTime));
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized) return;
        
        if (other.gameObject.TryGetComponent<RailPlayer>(out var player))
        {
            player?.Health.TakeDamage(_damage);
            DestroyObstacle();
        }
        
        if (other.gameObject.TryGetComponent<PlayerProjectile>(out var projectile))
        {
            DestroyObstacle();
        }
    }
    
    public void Initialize(Vector3 moveDirection, float speed)
    {
        if (_initialized) return;
        
        _health = healthRange.RandomValue;
        _damage = damageRange.RandomValue;
        _moveSpeed = moveSpeedRange.RandomValue + speed;
        _moveDirection = moveDirection;
        _initialized = true;
    }

    public void TakeDamage(float damage)
    {
        if (!_initialized) return;
        
        _health -= damage;
        
        if (_health <= 0)
        {
            DestroyObstacle();
        }
    }
}