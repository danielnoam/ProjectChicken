using System;
using Unity.Cinemachine;
using UnityEngine;


[Serializable]
public class CameraShakeSettings
{
    public CinemachineImpulseDefinition.ImpulseTypes impulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
    public CinemachineImpulseDefinition.ImpulseShapes impulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
   [Min(0.1f)] public float intensity = 1f;
   [Min(0.1f)] public float duration = 0.3f;
   
   public void GenerateImpulse(CinemachineImpulseSource impulseSource)
   {
       if (!impulseSource) return;
       
       impulseSource.ImpulseDefinition.ImpulseType = impulseType;
       impulseSource.ImpulseDefinition.ImpulseShape = impulseShape;
       impulseSource.ImpulseDefinition.ImpulseDuration = duration;
       impulseSource.DefaultVelocity = new Vector3(
           UnityEngine.Random.Range(-1f, 1f),
           UnityEngine.Random.Range(-1f, 1f),
           UnityEngine.Random.Range(-1f, 1f)
       );
       impulseSource.GenerateImpulseWithForce(intensity);
   }
}