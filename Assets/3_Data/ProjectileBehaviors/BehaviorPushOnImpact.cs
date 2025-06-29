using System;
using UnityEngine;


public class BehaviorPushOnImpact : ProjectileBehaviorBase
{
    [SerializeField] private float pushForce = 5f;


    public override void OnSpawn(PlayerProjectile projectile, RailPlayer owne )
    {

    }

    public override void OnMovement(PlayerProjectile projectile, RailPlayer owner )
    {

    }

    public override void OnCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision)
    {
        collision?.ApplyForce(projectile.StartDirection, pushForce);
    }

    public override void OnDestroy(PlayerProjectile projectile, RailPlayer owner )
    {

    }

    public override void OnDrawGizmos(PlayerProjectile projectile, RailPlayer owner )
    {

    }
}