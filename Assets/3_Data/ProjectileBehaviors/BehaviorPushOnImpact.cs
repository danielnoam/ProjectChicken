using System;
using UnityEngine;


public class BehaviorPushOnImpact : ProjectileBehaviorBase
{
    [Header("Push Settings")]
    [SerializeField] private float pushForce = 5f;


    public override void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {

    }

    public override void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {

    }

    public override void OnBehaviorCollision(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target, ChickenEnemy collision)
    {
        if (collision.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(projectile.StartDirection * pushForce, ForceMode.Impulse);
        }
    }

    public override void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {

    }

    public override void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {

    }
}