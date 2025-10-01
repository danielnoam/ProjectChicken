using System;
using DNExtensions;
using UnityEngine;


public class BehaviorVibrateAndShakeOnImpact : ProjectileBehaviorBase
{
    
    [SerializeField] private bool vibrateControllerOnImpact = true;
    [SerializeField] private ControllerVibrationEffectSettings vibrationSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnImpact = true;
    [SerializeField] private CameraShakeSettings shakeSettings;




    public override void OnSpawn(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnMovement(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnCollision(PlayerProjectile projectile, RailPlayer owner, IDamageable damageable)
    {
        if (vibrateControllerOnImpact) projectile.WeaponInstance.ControllerVibrationSource.Vibrate(vibrationSettings);
        if (shakeCameraOnImpact) shakeSettings.GenerateImpulse(projectile.WeaponInstance.CinemachineImpulseSource);
    }

    public override void OnDestroy(PlayerProjectile projectile, RailPlayer owner)
    {

    }
    
}