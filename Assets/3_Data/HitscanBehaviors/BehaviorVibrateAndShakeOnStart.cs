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

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner,ChickenController target = null)
    {
        if (vibrateControllerOnFire) weaponInstance.ControllerVibrationSource.Vibrate(vibrationSettings);
        
        if (shakeCameraOnFire)
        {
            weaponInstance.CinemachineImpulseSource.ImpulseDefinition.ImpulseShape = shakeSettings.impulseShape;
            weaponInstance.CinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = shakeSettings.intensity;
            weaponInstance.CinemachineImpulseSource.DefaultVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
            weaponInstance.CinemachineImpulseSource.GenerateImpulseWithForce(shakeSettings.duration);
        }
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ChickenController target)
    {

    }

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner,ChickenController target = null)
    {

    }
    
}