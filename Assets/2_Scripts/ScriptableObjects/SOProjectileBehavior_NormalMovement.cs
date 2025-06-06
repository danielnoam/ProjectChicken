using System;
using UnityEngine;


[CreateAssetMenu(fileName = "Movement_Normal", menuName = "Scriptable Objects/Projectile Behavior/Movement_Normal")]
public class SoSoProjectileBehaviorBaseNormalMovement : SOProjectileBehaviorBase
{
    
    public override void OnBehaviorSpawn(PlayerProjectile projectile)
    {

    }

    public override void OnBehaviorMovement(PlayerProjectile projectile)
    {
        projectile.Rigidbody?.MovePosition(projectile.Rigidbody.position + projectile.Direction * (projectile.Speed * Time.fixedDeltaTime));
    }

    public override void OnBehaviorCollision(PlayerProjectile projectile, ChickenEnemy enemy)
    {

    }

    public override void OnBehaviorDestroy(PlayerProjectile projectile)
    {

    }

    public override void OnBehaviorDrawGizmos(PlayerProjectile projectile)
    {
        
    }
}
