using System;
using UnityEngine;


public class BehaviorDamageOnHit : HitscanBehaviorBase
{
    [SerializeField] private float damage = 10f;

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner,ChickenStateController target = null)
    {
        
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ChickenStateController target)
    {
        target?.TakeDamage(damage);
    }

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner,ChickenStateController target = null)
    {

    }
}