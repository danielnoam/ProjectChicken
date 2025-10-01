using DNExtensions;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;


public class Obstacle : BaseObstacle
{
    [Header("Settings")]
    [SerializeField, MinMaxRange(1, 1000)] private RangedFloat healthRange = new(50f, 150f);
    [SerializeField] private SOAudioEvent obstacleDestroyedSfx;
    [SerializeField] private ParticleSystem obstacleDestroyedParticleEffect;
    
    private float _health;
    
    
    public override void Initialize(SplineContainer spline)
    {
        if (initialized) return;
        
        _health = healthRange.RandomValue;
        transform.eulerAngles = Random.onUnitSphere;
        
        base.Initialize(spline);
    }
    

    
    protected override void OnCollisionWithPlayer(RailPlayer player)
    {
        TakeDamage(_health);
        player.Health.TakeDamage(50f);
        Vector3 moveDirection = (player.transform.position - transform.position).normalized;
        player.Movement.Push(moveDirection, 2f);
    }
    
    protected override void OnCollisionWithChicken(ChickenStateController chicken)
    {
        TakeDamage(_health/2);
        chicken.TakeDamage(100);
    }

    protected override void OnCollisionWithPassthroughObstacle(PassthroughObstacle passthroughObstacle)
    {
        TakeDamage(_health);

    }

    protected override void OnCollisionWithObstacle(Obstacle obstacle)
    {
        TakeDamage(_health);
    }


    public override void TakeDamage(float damage)
    {
        if (!initialized || _health <= 0) return;
        
        _health -= damage;
        
        if (_health <= 0)
        {
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

}