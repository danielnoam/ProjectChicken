using System;
using DNExtensions;
using UnityEngine;


public class BehaviorRumbleAndShakeOnStart : HitscanBehaviorBase
{
    [SerializeField] private bool rumbleControllerOnFire = true;
    [SerializeField] private ControllerRumbleEffectSettings rumbleSettings;
    
    [Space(10)]
    
    [SerializeField] private bool shakeCameraOnFire = true;
    [SerializeField] private CameraShakeSettings shakeSettings;

    
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner,ChickenController target = null)
    {
        if (rumbleControllerOnFire) weaponInstance.ControllerRumbleSource.Rumble(rumbleSettings);
        
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