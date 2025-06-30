
using UnityEngine;


[System.Serializable]

public abstract class HitscanBehaviorBase
{
    public abstract void OnStart(SOWeapon weapon, RailPlayer owner, ChickenController target = null);
    public abstract void OnHit(SOWeapon weapon,RailPlayer owner, ChickenController collision);
    public abstract void OnEnd(SOWeapon weapon, RailPlayer owner, ChickenController target = null);
    public abstract void OnDrawGizmos(SOWeapon weapon, RailPlayer owner, ChickenController target = null);
}

