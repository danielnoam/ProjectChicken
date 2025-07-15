
using UnityEngine;


[System.Serializable]

public abstract class HitscanBehaviorBase
{
    public abstract void OnStart(WeaponInstance weaponInstance, RailPlayer owner, ChickenController target = null);
    public abstract void OnHit(WeaponInstance weaponInstance,RailPlayer owner, ChickenController collision);
    public abstract void OnEnd(WeaponInstance weaponInstance, RailPlayer owner, ChickenController target = null);
}

