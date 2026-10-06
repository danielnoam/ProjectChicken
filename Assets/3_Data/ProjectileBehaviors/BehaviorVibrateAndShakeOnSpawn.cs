using System;
using DNExtensions.Utilities;
using UnityEngine;
using DNExtensions.Systems.ControllerRumble;


public class BehaviorVibrateAndShakeOnSpawn : ProjectileBehaviorBase
{
    [SerializeField] private bool vibrateControllerOnFire = true;
    [SerializeField] private ControllerRumbleEffectSettings vibrationSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnFire = true;
    [SerializeField] private CameraShakeSettings shakeSettings;


    public override void OnSpawn(PlayerProjectile projectile, RailPlayer owner)
    {
        if (vibrateControllerOnFire) projectile.WeaponInstance.ControllerRumbleSource.Rumble(vibrationSettings);
        if (shakeCameraOnFire) shakeSettings.GenerateImpulse(projectile.WeaponInstance.CinemachineImpulseSource);
    }

    public override void OnMovement(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnCollision(PlayerProjectile projectile, RailPlayer owner, IDamageable damageable)
    {

    }

    public override void OnDestroy(PlayerProjectile projectile, RailPlayer owner)
    {

    }
    
}