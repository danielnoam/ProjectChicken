
using UnityEngine;


[System.Serializable]

public abstract class ProjectileBehaviorBase
{
    public abstract void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner);
    public abstract void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner);
    public abstract void OnBehaviorCollision(PlayerProjectile projectile,RailPlayer owner, ChickenController collision);
    public abstract void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner);
    public abstract void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner);
}

