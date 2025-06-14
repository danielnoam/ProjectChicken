using System;
using UnityEngine;


public class BehaviorStunOnImpact : ProjectileBehaviorBase
{
    [SerializeField, Range(0,100)] private float stunChance = 50f;
    [SerializeField, Min(0f)] private float stunDuration = 2f;


    public override void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnBehaviorCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision)
    {
        // Check if the stun should be applied based on chance
        if (UnityEngine.Random.Range(0f, 100f) > stunChance) return;
        collision.ApplyConcussion(stunDuration);
    }

    public override void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner)
    {

    }
}