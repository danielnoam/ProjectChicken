using System;
using UnityEngine;



public class BehaviorNormalMovement : ProjectileBehaviorBase
{
    [Header("Normal Movement Settings")]
    [SerializeField, Min(0)] private float moveSpeed = 100f;
    
    public override void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {

    }

    public override void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {
        projectile.Rigidbody?.MovePosition(projectile.Rigidbody.position + projectile.StartDirection * (moveSpeed * Time.fixedDeltaTime));
    }

    public override void OnBehaviorCollision(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target, ChickenEnemy collision)
    {

    }

    public override void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {

    }

    public override void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {

    }
}
