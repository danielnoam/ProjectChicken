using System;
using DNExtensions.Utilities;
using UnityEngine;
using DNExtensions.Systems.ControllerRumble;


public class BehaviorVibrateAndShakeOnHit : HitscanBehaviorBase
{
    [SerializeField] private bool vibrateControllerOnHit = true;
    [SerializeField] private ControllerRumbleEffectSettings vibrationSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnHit = true;
    [SerializeField] private CameraShakeSettings shakeSettings;

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner,ITargetable target = null)
    {
        
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ITargetable target = null)
    {
        if (vibrateControllerOnHit) weaponInstance.ControllerRumbleSource.Rumble(vibrationSettings);
        if (shakeCameraOnHit) shakeSettings.GenerateImpulse(weaponInstance.CinemachineImpulseSource);
    }

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner,ITargetable target = null)
    {

    }
    
}