using System;
using DNExtensions;
using UnityEngine;


public class BehaviorVibrateAndShakeOnHit : HitscanBehaviorBase
{
    [SerializeField] private bool vibrateControllerOnHit = true;
    [SerializeField] private ControllerVibrationEffectSettings vibrationSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnHit = true;
    [SerializeField] private CameraShakeSettings shakeSettings;

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner,ITargetable target = null)
    {
        
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ITargetable target = null)
    {
        if (vibrateControllerOnHit) weaponInstance.ControllerVibrationSource.Vibrate(vibrationSettings);
        if (shakeCameraOnHit) shakeSettings.GenerateImpulse(weaponInstance.CinemachineImpulseSource);
    }

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner,ITargetable target = null)
    {

    }
    
}