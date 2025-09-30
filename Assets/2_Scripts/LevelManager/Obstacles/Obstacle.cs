using System;
using DNExtensions;
using UnityEngine;
using UnityEngine.Splines;
using PrimeTween;
using Unity.Cinemachine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public enum ObstacleMovementType
{
    Forward,
    Spline
}

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(ControllerVibrationSource))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class Obstacle : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float baseSpeed = 75f;
    [SerializeField, MinMaxRange(1f, 100f)] private RangedFloat moveSpeedRange = new(50f, 100f);
    [SerializeField, MinMaxRange(1, 15)] private RangedInt healthRange = new(1, 5);

    
    [Header("Gfx")]
    [SerializeField, MinMaxRange(1f, 5f)] private RangedFloat spawnAnimationDuration = new RangedFloat(2f,5f);
    [SerializeField, MinMaxRange(1f, 360f)] private RangedFloat rotationSpeedRange = new(30f, 100f);
    [SerializeField] private SOAudioEvent obstacleFlyBySfx;
    [SerializeField] private SOAudioEvent obstacleDestroyedSfx;
    [SerializeField] private ParticleSystem obstacleDestroyedParticleEffect;
    [SerializeField] private CameraShakeSettings cameraShakeSettings;
    [SerializeField] private ControllerVibrationEffectSettings vibrationEffectSettings;
    
    [Header("References")]
    [SerializeField] private ControllerVibrationSource vibrationSource;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private AudioSource audioSource;
    
    
    private bool _initialized;
    private bool _shakeEffectPlayed;
    private int _health;
    private float _moveSpeed;
    private float _rotationSpeed;
    private ObstacleMovementType _movementType;
    private Vector3 _moveDirection;
    private Vector3 _rotationDirection;
    private SplineContainer _spline;
    private float _splineProgress;
    
    public event Action<Obstacle> OnObstacleDestroyed;
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (!_initialized) return;
        
        if (other.TryGetComponent<PlayerProjectile>(out var projectile))
        {
            TakeDamage(1);
        }
        
        if (other.TryGetComponent<ChickenStateController>(out var chicken))
        {
            TakeDamage(1);
            chicken.TakeDamage(100);
        }
        
        if (other.TryGetComponent(out GameObjectCenterer gameObjectCenterer))
        {
            TakeDamage(_health);
        }
        
        if (other.TryGetComponent<Obstacle>(out var obstacle))
        {
            TakeDamage(_health);
        }
    }
    
    public void Initialize(ObstacleMovementType movementType, SplineContainer spline = null, Vector3 moveDirection = default)
    {
        if (_initialized) return;
        
        _health = healthRange.RandomValue;
        _moveSpeed = baseSpeed + moveSpeedRange.RandomValue;
        _movementType = movementType;
        transform.eulerAngles = Random.onUnitSphere;
        _rotationDirection = Random.onUnitSphere;
        _rotationSpeed = rotationSpeedRange.RandomValue;
        
        switch (_movementType)
        {
            case ObstacleMovementType.Spline:
                _spline = spline;
                _splineProgress = 0f;
                break;
            case ObstacleMovementType.Forward:
                _moveDirection = moveDirection;
                break;
        }
        
        _initialized = true;
        
        Tween.Scale(transform, startValue: Vector3.zero, endValue: Vector3.one, duration: spawnAnimationDuration.RandomValue, ease: Ease.InOutSine);
    }

    
    private void DestroyObstacle()
    {
        if (_spline) Destroy(_spline.gameObject);
        
        if (obstacleDestroyedParticleEffect) Instantiate(obstacleDestroyedParticleEffect, transform.position, Quaternion.identity);
        
        obstacleDestroyedSfx?.PlayAtPoint(transform.position);
        
        OnObstacleDestroyed?.Invoke(this);
        
        Destroy(gameObject);
    }
    
    private void Update()
    {
        if (!_initialized) return;
        
        MoveObstacle();

        if (LevelManager.Instance && !_shakeEffectPlayed)
        {
            if (Vector3.Distance(transform.position, LevelManager.Instance.PlayerPosition) < 50f)
            {
                _shakeEffectPlayed = true;
                obstacleFlyBySfx?.Play(audioSource);
                cameraShakeSettings.GenerateImpulse(impulseSource);
                vibrationSource.Vibrate(vibrationEffectSettings);
            }
        }

    }
    
    private void MoveObstacle()
    {
        switch (_movementType)
        {
            case ObstacleMovementType.Forward:
                MoveForward();
                break;
            
            case ObstacleMovementType.Spline:
                MoveAlongSpline();
                break;
        }
        

        transform.Rotate(_rotationDirection, _rotationSpeed * Time.deltaTime);
    }
    
    private void MoveForward()
    {
        transform.position += _moveDirection * (_moveSpeed * Time.deltaTime);
    }
    
    private void MoveAlongSpline()
    {
        if (!_spline) return;
        
        float splineLength = _spline.Spline.GetLength();
        float progressIncrement = (_moveSpeed * Time.deltaTime) / splineLength;
        
        _splineProgress += progressIncrement;
        
        if (_splineProgress >= 1f)
        {
            DestroyObstacle();
            return;
        }
        
        _spline.Evaluate(_splineProgress, out var position, out var tangent, out var up);
        
        transform.position = position;
    }
    

    public void TakeDamage(int damage)
    {
        if (!_initialized) return;
        
        _health -= damage;
        
        if (_health <= 0)
        {
            DestroyObstacle();
        }
    }
    
    public void TakeFullDamage()
    {
        if (!_initialized) return;

        DestroyObstacle();
    }
    

}