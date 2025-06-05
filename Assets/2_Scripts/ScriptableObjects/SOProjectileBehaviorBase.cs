
using UnityEngine;



public abstract class SOProjectileBehaviorBase : ScriptableObject
{
    public abstract void OnSpawn(PlayerProjectile projectile);
    public abstract void OnMovement(PlayerProjectile projectile);
    public abstract void OnCollision(PlayerProjectile projectile, ChickenEnemy enemy);
    public abstract void OnDestroy();
    
    public abstract void OnDrawGizmos(PlayerProjectile projectile);
}

