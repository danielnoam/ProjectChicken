using System;
using UnityEngine;


public class BehaviorStunOnHit : HitscanBehaviorBase
{
    [SerializeField, Range(0,100)] private float stunChance = 50f;
    [SerializeField, Min(0f)] private float stunDuration = 2f;

    
    
    public override void OnStart(SOWeaponData weaponData, RailPlayer owner,ChickenController target = null)
    {
        
    }

    public override void OnHit(SOWeaponData weaponData, RailPlayer owner, ChickenController target)
    {
        if (UnityEngine.Random.Range(0f, 100f) > stunChance) return;
        target?.ApplyConcussion(stunDuration);
    }

    public override void OnEnd(SOWeaponData weaponData, RailPlayer owner,ChickenController target = null)
    {

    }

    public override void OnDrawGizmos(SOWeaponData weaponData, RailPlayer owner,ChickenController target = null)
    {

    }
}