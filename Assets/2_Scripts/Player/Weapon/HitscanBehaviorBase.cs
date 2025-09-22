
using UnityEngine;


[System.Serializable]

public abstract class HitscanBehaviorBase
{
    public abstract void OnStart(WeaponInstance weaponInstance, RailPlayer owner, ChickenStateController target = null);
    public abstract void OnHit(WeaponInstance weaponInstance,RailPlayer owner, ChickenStateController collision);
    public abstract void OnEnd(WeaponInstance weaponInstance, RailPlayer owner, ChickenStateController target = null);
}

