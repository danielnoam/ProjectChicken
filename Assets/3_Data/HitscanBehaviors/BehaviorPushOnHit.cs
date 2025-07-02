using System;
using UnityEngine;


public class BehaviorPushOnHit : HitscanBehaviorBase
{
    [SerializeField] private float pushForce = 5f;

    Vector3 _pushDirection = Vector3.zero;
    

    public override void OnStart(SOWeaponData weaponData, RailPlayer owner,ChickenController target = null)
    {
        if (target)
        {
            _pushDirection = owner.transform.position - target.transform.position;
        }
        else
        {
            _pushDirection = owner.transform.forward;
        }

    }

    public override void OnHit(SOWeaponData weaponData, RailPlayer owner, ChickenController target)
    {
        target?.ApplyForce(_pushDirection, pushForce);
    }

    public override void OnEnd(SOWeaponData weaponData, RailPlayer owner,ChickenController target = null)
    {

    }

    public override void OnDrawGizmos(SOWeaponData weaponData, RailPlayer owner,ChickenController target = null)
    {

    }
}