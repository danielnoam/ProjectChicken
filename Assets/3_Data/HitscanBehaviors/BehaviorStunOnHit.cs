using System;
using UnityEngine;


public class BehaviorStunOnHit : HitscanBehaviorBase
{
    [SerializeField, Range(0,100)] private float stunChance = 50f;
    [SerializeField, Min(0f)] private float stunDuration = 2f;

    
    
    public override void OnStart(SOWeapon weapon, RailPlayer owner,ChickenController target = null)
    {
        
    }

    public override void OnHit(SOWeapon weapon, RailPlayer owner, ChickenController target)
    {
        // Check if the stun should be applied based on chance
        if (UnityEngine.Random.Range(0f, 100f) > stunChance) return;
        target.ApplyConcussion(stunDuration);
    }

    public override void OnEnd(SOWeapon weapon, RailPlayer owner,ChickenController target = null)
    {

    }

    public override void OnDrawGizmos(SOWeapon weapon, RailPlayer owner,ChickenController target = null)
    {

    }
}