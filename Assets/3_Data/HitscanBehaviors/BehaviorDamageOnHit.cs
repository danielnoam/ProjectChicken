using System;
using UnityEngine;


public class BehaviorDamageOnHit : HitscanBehaviorBase
{
    [SerializeField] private float damage = 10f;

    
    
    public override void OnStart(SOWeaponData weaponData, RailPlayer owner,ChickenController target = null)
    {
        
    }

    public override void OnHit(SOWeaponData weaponData, RailPlayer owner, ChickenController target)
    {
        target.TakeDamage(damage);
    }

    public override void OnEnd(SOWeaponData weaponData, RailPlayer owner,ChickenController target = null)
    {

    }

    public override void OnDrawGizmos(SOWeaponData weaponData, RailPlayer owner,ChickenController target = null)
    {

    }
}