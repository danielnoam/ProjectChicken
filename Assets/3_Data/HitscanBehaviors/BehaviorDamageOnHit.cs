using System;
using UnityEngine;


public class BehaviorDamageOnHit : HitscanBehaviorBase
{
    [SerializeField] private float damage = 10f;

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner, ITargetable target = null)
    {
        
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ITargetable target = null)
    {
        Debug.Log("Test");
        if (target is IDamageable damageable)
        {
            damageable.TakeDamage(damage);

        }

    }

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner,ITargetable target = null)
    {

    }
}