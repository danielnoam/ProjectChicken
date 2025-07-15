using System;
using DNExtensions;
using UnityEngine;


public class BehaviorRumbleAndShakeOnImpact : ProjectileBehaviorBase
{
    
    [SerializeField] private bool rumbleControllerOnImpact = true;
    [SerializeField] private ControllerRumbleEffectSettings rumbleSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnImpact = true;
    [SerializeField] private CameraShakeSettings shakeSettings;




    public override void OnSpawn(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnMovement(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision)
    {
        if (rumbleControllerOnImpact) projectile.WeaponInstance.ControllerRumbleSource.Rumble(rumbleSettings);

        if (shakeCameraOnImpact)
        {
            projectile.WeaponInstance.CinemachineImpulseSource.ImpulseDefinition.ImpulseShape = shakeSettings.impulseShape;
            projectile.WeaponInstance.CinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = shakeSettings.intensity;
            projectile.WeaponInstance.CinemachineImpulseSource.DefaultVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
            projectile.WeaponInstance.CinemachineImpulseSource.GenerateImpulseWithForce(shakeSettings.duration);
        }
    }

    public override void OnDestroy(PlayerProjectile projectile, RailPlayer owner)
    {

    }
    
}