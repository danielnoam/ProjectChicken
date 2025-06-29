using System;
using Unity.Cinemachine;
using UnityEngine;


[Serializable]
public class CameraShakeSettings
{
    public CinemachineImpulseDefinition.ImpulseShapes impulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
   [Min(0.1f)] public float intensity = 1f;
   [Min(0.1f)] public float duration = 0.3f;
}