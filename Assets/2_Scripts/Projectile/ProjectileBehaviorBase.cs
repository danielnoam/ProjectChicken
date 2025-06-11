
using UnityEngine;


[System.Serializable]

public abstract class ProjectileBehaviorBase
{
    public abstract void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target);
    public abstract void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target);
    public abstract void OnBehaviorCollision(PlayerProjectile projectile,RailPlayer owner, ChickenEnemy target, ChickenEnemy collision);
    public abstract void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target);
    public abstract void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target);
}

