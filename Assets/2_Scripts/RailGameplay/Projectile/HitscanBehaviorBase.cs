
using UnityEngine;


[System.Serializable]

public abstract class HitscanBehaviorBase
{
    public abstract void OnBehaviorStart(SOWeapon weapon, RailPlayer owner, ChickenController target = null);
    public abstract void OnBehaviorHit(SOWeapon weapon,RailPlayer owner, ChickenController collision);
    public abstract void OnBehaviorEnd(SOWeapon weapon, RailPlayer owner, ChickenController target = null);
    public abstract void OnBehaviorDrawGizmos(SOWeapon weapon, RailPlayer owner, ChickenController target = null);
}

