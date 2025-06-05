
using UnityEngine;



public abstract class SOProjectileBehaviorBase : ScriptableObject
{
    public abstract void OnBehaviorSpawn(PlayerProjectile projectile);
    public abstract void OnBehaviorMovement(PlayerProjectile projectile);
    public abstract void OnBehaviorCollision(PlayerProjectile projectile, ChickenEnemy enemy);
    public abstract void OnBehaviorDestroy(PlayerProjectile projectile);
    
    public abstract void OnBehaviorDrawGizmos(PlayerProjectile projectile);
}

