
using UnityEngine;


[System.Serializable]

public abstract class HitscanBehaviorBase
{
    public abstract void OnStart(SOWeaponData weaponData, RailPlayer owner, ChickenController target = null);
    public abstract void OnHit(SOWeaponData weaponData,RailPlayer owner, ChickenController collision);
    public abstract void OnEnd(SOWeaponData weaponData, RailPlayer owner, ChickenController target = null);
    public abstract void OnDrawGizmos(SOWeaponData weaponData, RailPlayer owner, ChickenController target = null);
}

