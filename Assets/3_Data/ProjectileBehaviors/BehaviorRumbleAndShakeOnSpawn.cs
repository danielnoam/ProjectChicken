using System;
using DNExtensions;
using UnityEngine;


public class BehaviorRumbleAndShakeOnSpawn : ProjectileBehaviorBase
{
    [SerializeField] private bool rumbleControllerOnFire = true;
    [SerializeField] private ControllerRumbleEffectSettings rumbleSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnFire = true;
    [SerializeField] private CameraShakeSettings shakeSettings;


    public override void OnSpawn(PlayerProjectile projectile, RailPlayer owner)
    {
        if (rumbleControllerOnFire) projectile.WeaponInstance.ControllerRumbleSource.Rumble(rumbleSettings);
        
        if (shakeCameraOnFire)
        {
            projectile.WeaponInstance.CinemachineImpulseSource.ImpulseDefinition.ImpulseShape = shakeSettings.impulseShape;
            projectile.WeaponInstance.CinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = shakeSettings.intensity;
            projectile.WeaponInstance.CinemachineImpulseSource.DefaultVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
            projectile.WeaponInstance.CinemachineImpulseSource.GenerateImpulseWithForce(shakeSettings.duration);
        }
    }

    public override void OnMovement(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision)
    {

    }

    public override void OnDestroy(PlayerProjectile projectile, RailPlayer owner)
    {

    }
    
}