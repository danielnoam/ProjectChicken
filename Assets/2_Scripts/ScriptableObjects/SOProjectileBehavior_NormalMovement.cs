using System;
using UnityEngine;


[CreateAssetMenu(fileName = "Movement_Normal", menuName = "Scriptable Objects/Projectile Behavior/Movement_Normal")]
public class SoSoProjectileBehaviorBaseNormalMovement : SOProjectileBehaviorBase
{
    
    public override void OnSpawn(PlayerProjectile projectile)
    {

    }

    public override void OnMovement(PlayerProjectile projectile)
    {
        projectile.Rigidbody?.MovePosition(projectile.Rigidbody.position + projectile.Direction * (projectile.Speed * Time.fixedDeltaTime));
    }

    public override void OnCollision(PlayerProjectile projectile, ChickenEnemy enemy)
    {

    }

    public override void OnDestroy()
    {

    }

    public override void OnDrawGizmos(PlayerProjectile projectile)
    {
        
    }
}
