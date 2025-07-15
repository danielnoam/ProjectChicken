using System;
using DNExtensions;
using UnityEngine;


public class BehaviorRumbleAndShakeOnHit : HitscanBehaviorBase
{
    [SerializeField] private bool rumbleControllerOnHit = true;
    [SerializeField] private ControllerRumbleEffectSettings rumbleSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnHit = true;
    [SerializeField] private CameraShakeSettings shakeSettings;

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner,ChickenController target = null)
    {
        
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ChickenController target)
    {
        if (rumbleControllerOnHit) weaponInstance.ControllerRumbleSource.Rumble(rumbleSettings);

        if (shakeCameraOnHit)
        {
            weaponInstance.CinemachineImpulseSource.ImpulseDefinition.ImpulseShape = shakeSettings.impulseShape;
            weaponInstance.CinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = shakeSettings.intensity;
            weaponInstance.CinemachineImpulseSource.DefaultVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
            weaponInstance.CinemachineImpulseSource.GenerateImpulseWithForce(shakeSettings.duration);
        }
    }

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner,ChickenController target = null)
    {

    }
    
}