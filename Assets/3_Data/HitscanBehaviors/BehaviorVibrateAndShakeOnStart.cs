using System;
using DNExtensions.Utilities;
using UnityEngine;
using DNExtensions.Systems.ControllerRumble;


public class BehaviorVibrateAndShakeOnStart : HitscanBehaviorBase
{
    [SerializeField] private bool vibrateControllerOnFire = true;
    [SerializeField] private ControllerRumbleEffectSettings vibrationSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnFire = true;
    [SerializeField] private CameraShakeSettings shakeSettings;

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner,ITargetable target = null)
    {
        if (vibrateControllerOnFire) weaponInstance.ControllerRumbleSource.Rumble(vibrationSettings);
        if (shakeCameraOnFire) shakeSettings.GenerateImpulse(weaponInstance.CinemachineImpulseSource);
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ITargetable target = null)
    {

    }

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner,ITargetable target = null)
    {

    }
    
}