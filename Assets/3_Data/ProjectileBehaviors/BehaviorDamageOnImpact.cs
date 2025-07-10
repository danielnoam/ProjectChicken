using System;
using UnityEngine;


public class BehaviorDamageOnImpact : ProjectileBehaviorBase
{
    [SerializeField] private float damage = 10f;


    public override void OnSpawn(PlayerProjectile projectile, RailPlayer owne )
    {

    }

    public override void OnMovement(PlayerProjectile projectile, RailPlayer owner )
    {

    }

    public override void OnCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision)
    {
        collision.TakeDamage(damage);
    }

    public override void OnDestroy(PlayerProjectile projectile, RailPlayer owner )
    {

    }
    
}