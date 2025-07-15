using System;
using UnityEngine;


public class BehaviorDamageOnHit : HitscanBehaviorBase
{
    [SerializeField] private float damage = 10f;

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner,ChickenController target = null)
    {
        
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ChickenController target)
    {
        target?.TakeDamage(damage);
    }

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner,ChickenController target = null)
    {

    }
}