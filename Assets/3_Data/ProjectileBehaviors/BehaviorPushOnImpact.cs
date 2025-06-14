using System;
using UnityEngine;


public class BehaviorPushOnImpact : ProjectileBehaviorBase
{
    [SerializeField] private float pushForce = 5f;


    public override void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owne )
    {

    }

    public override void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner )
    {

    }

    public override void OnBehaviorCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision)
    {
        collision?.ApplyForce(projectile.StartDirection, pushForce);
    }

    public override void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner )
    {

    }

    public override void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner )
    {

    }
}