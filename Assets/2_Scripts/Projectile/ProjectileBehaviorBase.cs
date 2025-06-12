
using UnityEngine;


[System.Serializable]

public abstract class ProjectileBehaviorBase
{
    public abstract void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner, ChickenController target);
    public abstract void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner, ChickenController target);
    public abstract void OnBehaviorCollision(PlayerProjectile projectile,RailPlayer owner, ChickenController target, ChickenController collision);
    public abstract void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner, ChickenController target);
    public abstract void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner, ChickenController target);
}

