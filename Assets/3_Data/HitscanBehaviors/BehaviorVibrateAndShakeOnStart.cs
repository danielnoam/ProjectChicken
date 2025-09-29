using System;
using DNExtensions;
using UnityEngine;


public class BehaviorVibrateAndShakeOnStart : HitscanBehaviorBase
{
    [SerializeField] private bool vibrateControllerOnFire = true;
    [SerializeField] private ControllerVibrationEffectSettings vibrationSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnFire = true;
    [SerializeField] private CameraShakeSettings shakeSettings;

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner,ChickenStateController target = null)
    {
        if (vibrateControllerOnFire) weaponInstance.ControllerVibrationSource.Vibrate(vibrationSettings);
        if (shakeCameraOnFire) shakeSettings.GenerateImpulse(weaponInstance.CinemachineImpulseSource);
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ChickenStateController target)
    {

    }

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner,ChickenStateController target = null)
    {

    }
    
}