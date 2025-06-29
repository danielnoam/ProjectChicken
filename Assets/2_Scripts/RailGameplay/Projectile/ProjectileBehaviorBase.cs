
using UnityEngine;


[System.Serializable]

public abstract class ProjectileBehaviorBase
{
    public abstract void OnSpawn(PlayerProjectile projectile, RailPlayer owner);
    public abstract void OnMovement(PlayerProjectile projectile, RailPlayer owner);
    public abstract void OnCollision(PlayerProjectile projectile,RailPlayer owner, ChickenController collision);
    public abstract void OnDestroy(PlayerProjectile projectile, RailPlayer owner);
    public abstract void OnDrawGizmos(PlayerProjectile projectile, RailPlayer owner);
}

