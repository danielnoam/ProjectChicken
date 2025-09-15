using System;
using DNExtensions;
using UnityEngine;
using PrimeTween;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ControllerVibrationSource))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class Obstacle : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, MinMaxRange(1, 15)] private RangedInt healthRange = new(1, 5);
    [SerializeField, MinMaxRange(1f, 100f)] private RangedFloat moveSpeedRange = new(1f, 5f);
    [SerializeField, MinMaxRange(5f, 50f)] private RangedFloat damageRange = new(10f, 30f);
    [SerializeField, Tooltip("Time before the obstacle destroys itself (0 = unlimited time)"), Min(0)] private float lifetime = 10f;
    
    [Header("Animation")]
    [SerializeField] private float spawnAnimationDuration = 2f;
    
    [Header("References")]
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private ControllerVibrationSource vibrationSource;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    

    private bool _initialized;
    private float _health;
    private float _damage;
    private float _moveSpeed;
    private Vector3 _moveDirection;
    private float _currentLifetime;
    
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
            CheckLifetime();
            rigidBody.MovePosition(transform.position + _moveDirection * (_moveSpeed * Time.deltaTime));
        }
    }
    
    private void CheckLifetime()
    {
        if (lifetime <= 0f) return;
        
        _currentLifetime -= Time.deltaTime;
        
        if (_currentLifetime <= 0f)
        {
            DestroyObstacle();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized) return;
        
        
        if (other.TryGetComponent<PlayerProjectile>(out var projectile))
        {
            TakeDamage(1);
        }
        
        if (other.TryGetComponent<RailPlayer>(out var player))
        {
            TakeDamage(1);
            player.Health.TakeDamage(_damage);
        }
        
        
        if (other.TryGetComponent<ChickenStateController>(out var chicken))
        {
            TakeDamage(1);
            chicken.TakeDamage(100);
        }
    }
    

    private void TakeDamage(int damage)
    {
        if (!_initialized) return;
        
        _health -= damage;
        
        if (_health <= 0)
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
        _currentLifetime = lifetime;
        _initialized = true;
        
        Tween.Scale(transform, startValue: Vector3.zero, endValue: Vector3.one, duration: spawnAnimationDuration, ease: Ease.InOutSine);
    }
}