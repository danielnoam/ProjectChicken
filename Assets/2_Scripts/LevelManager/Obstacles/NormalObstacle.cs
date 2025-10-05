using System;
using DNExtensions;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;


public class NormalObstacle : BaseObstacle
{
    [Header("Settings")]
    [SerializeField, MinMaxRange(1, 1000)] private RangedFloat healthRange = new(50f, 150f);
    [SerializeField] private SOAudioEvent obstacleDestroyedSfx;
    [SerializeField] private ParticleSystem obstacleDestroyedParticleEffect;
    [SerializeField] private ParticleSystem obstacleImpactParticleEffect;
    
    [Header("Collision With Player")]
    [SerializeField] private CameraShakeSettings hitPlayerCameraShakeSettings;
    [SerializeField] private ControllerVibrationEffectSettings hitPlayerVibrationEffectSettings;
    
    private float _health;
    
    
    public event Action<NormalObstacle> OnObstacleBroke; 
    
    public override void Initialize(SplineContainer spline)
    {
        if (initialized) return;
        
        _health = healthRange.RandomValue;
        transform.eulerAngles = Random.onUnitSphere;
        
        base.Initialize(spline);
    }

    

    protected override void OnCollisionWithPlayer(Collider other, RailPlayer player)
    {
        Vector3 contactPoint = other.ClosestPoint(transform.position);
        PlayImpactEffect(contactPoint);
        TakeDamage(_health);
        player.Health.TakeDamage(50f);
        Vector3 moveDirection = (player.transform.position - transform.position).normalized;
        player.Movement.Push(moveDirection, 2f);
        hitPlayerCameraShakeSettings.GenerateImpulse(impulseSource);
        vibrationSource.Vibrate(hitPlayerVibrationEffectSettings);
    }
    
    protected override void OnCollisionWithChicken(Collider other, ChickenStateController chicken)
    {
        Vector3 contactPoint = other.ClosestPoint(transform.position);
        PlayImpactEffect(contactPoint);
        TakeDamage(_health/2);
        chicken.TakeDamage(100);
    }

    protected override void OnCollisionWithPassthroughObstacle(Collider other, PassthroughObstacle passthroughObstacle)
    {
        Vector3 contactPoint = other.ClosestPoint(transform.position);
        PlayImpactEffect(contactPoint);
        TakeDamage(_health);
    }

    protected override void OnCollisionWithObstacle(Collider other, NormalObstacle normalObstacle)
    {
        Vector3 contactPoint = other.ClosestPoint(transform.position);
        PlayImpactEffect(contactPoint);
        TakeDamage(_health);
    }


    public override void TakeDamage(float damage)
    {
        if (!initialized || _health <= 0) return;
        
        _health -= damage;
        
        if (_health <= 0)
        {
            OnObstacleBroke?.Invoke(this);
            _health = 0f;
            DestroyObstacle();
        }
    }

    public override void ApplyStun(float duration)
    {

    }

    public override void ApplyForce(Vector3 direction, float force)
    {

    }
    
    protected override void DestroyObstacle()
    {
        if (obstacleDestroyedParticleEffect) Instantiate(obstacleDestroyedParticleEffect, transform.position, Quaternion.identity);
        
        obstacleDestroyedSfx?.PlayAtPoint(transform.position);
        
        base.DestroyObstacle();
    }
    
    private void PlayImpactEffect(Vector3 position)
    {
        if (obstacleImpactParticleEffect) Instantiate(obstacleImpactParticleEffect, position, Quaternion.identity);
    }

}