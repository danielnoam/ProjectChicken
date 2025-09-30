using DNExtensions;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;


public class Obstacle : BaseObstacle
{
    [Header("Settings")]
    [SerializeField, MinMaxRange(1, 15)] private RangedInt healthRange = new(1, 5);
    [SerializeField] private SOAudioEvent obstacleDestroyedSfx;
    [SerializeField] private ParticleSystem obstacleDestroyedParticleEffect;
    
    private int _health;
    
    
    public override void Initialize(SplineContainer spline)
    {
        if (initialized) return;
        
        _health = healthRange.RandomValue;
        transform.eulerAngles = Random.onUnitSphere;
        
        base.Initialize(spline);
    }
    

    
    protected override void OnCollisionWithPlayer(RailPlayer player)
    {
        TakeDamage(1);
        player.Health.TakeDamage(50f);
        Vector3 moveDirection = (player.transform.position - transform.position).normalized;
        player.Movement.Push(moveDirection, 2f);
    }
    
    protected override void OnCollisionWithChicken(ChickenStateController chicken)
    {
        TakeDamage(1);
        chicken.TakeDamage(100);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!initialized) return;
        
        if (other.TryGetComponent<PlayerProjectile>(out var projectile))
        {
            TakeDamage(1);
        }
        
        if (other.TryGetComponent<ChickenStateController>(out var chicken))
        {
            TakeDamage(1);
            OnCollisionWithChicken(chicken);
        }
        
        if (other.TryGetComponent(out PassthroughObstacle passthroughObstacle))
        {
            TakeDamage(_health);
        }
    }
    
    
    protected override void DestroyObstacle()
    {
        if (obstacleDestroyedParticleEffect) Instantiate(obstacleDestroyedParticleEffect, transform.position, Quaternion.identity);
        
        obstacleDestroyedSfx?.PlayAtPoint(transform.position);
        
        base.DestroyObstacle();
    }
    


    public void TakeDamage(int damage)
    {
        if (!initialized) return;
        
        _health -= damage;
        
        if (_health <= 0)
        {
            DestroyObstacle();
        }
    }
    
    public void TakeFullDamage()
    {
        if (!initialized) return;

        DestroyObstacle();
    }
}