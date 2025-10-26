using System;
using DNExtensions;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Configuration settings for camera shake impulses using Cinemachine.
/// </summary>
[Serializable]
public class CameraShakeSettings
{
    public CinemachineImpulseDefinition.ImpulseTypes impulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
    public CinemachineImpulseDefinition.ImpulseShapes impulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
    [Min(0.01f)] public float intensity = 1f;
    [Min(0.01f)] public float duration = 0.3f;
    public RangedFloat xVelocityRange = new RangedFloat(-1f, 1f);
    public RangedFloat yVelocityRange = new RangedFloat(-1f, 1f);
    public RangedFloat zVelocityRange = new RangedFloat(-1f, 1f);
    
    /// <summary>
    /// Generates a camera shake impulse using the configured settings.
    /// </summary>
    /// <param name="impulseSource">The Cinemachine impulse source component.</param>
    public void GenerateImpulse(CinemachineImpulseSource impulseSource)
    {
        if (!impulseSource) return;
        
        impulseSource.ImpulseDefinition.ImpulseType = impulseType;
        impulseSource.ImpulseDefinition.ImpulseShape = impulseShape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = new Vector3(
            UnityEngine.Random.Range(xVelocityRange.minValue, xVelocityRange.maxValue),
            UnityEngine.Random.Range(yVelocityRange.minValue, yVelocityRange.maxValue),
            UnityEngine.Random.Range(zVelocityRange.minValue, zVelocityRange.maxValue)
        );
        impulseSource.GenerateImpulseWithForce(intensity);
    }
    
    /// <summary>
    /// Generates a camera shake impulse with a custom impulse type.
    /// </summary>
    /// <param name="impulseSource">The Cinemachine impulse source component.</param>
    /// <param name="type">The type of impulse to generate (overrides configured impulseType).</param>
    public void GenerateImpulse(CinemachineImpulseSource impulseSource, CinemachineImpulseDefinition.ImpulseTypes type)
    {
        if (!impulseSource) return;
        
        impulseSource.ImpulseDefinition.ImpulseType = type;
        impulseSource.ImpulseDefinition.ImpulseShape = impulseShape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = new Vector3(
            UnityEngine.Random.Range(xVelocityRange.minValue, xVelocityRange.maxValue),
            UnityEngine.Random.Range(yVelocityRange.minValue, yVelocityRange.maxValue),
            UnityEngine.Random.Range(zVelocityRange.minValue, zVelocityRange.maxValue)
        );
        impulseSource.GenerateImpulseWithForce(intensity);
    }
    
    /// <summary>
    /// Generates a camera shake impulse with a custom impulse shape.
    /// </summary>
    /// <param name="impulseSource">The Cinemachine impulse source component.</param>
    /// <param name="shape">The shape of impulse to generate (overrides configured impulseShape).</param>
    public void GenerateImpulse(CinemachineImpulseSource impulseSource, CinemachineImpulseDefinition.ImpulseShapes shape)
    {
        if (!impulseSource) return;

        impulseSource.ImpulseDefinition.ImpulseType = impulseType;
        impulseSource.ImpulseDefinition.ImpulseShape = shape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = new Vector3(
            UnityEngine.Random.Range(xVelocityRange.minValue, xVelocityRange.maxValue),
            UnityEngine.Random.Range(yVelocityRange.minValue, yVelocityRange.maxValue),
            UnityEngine.Random.Range(zVelocityRange.minValue, zVelocityRange.maxValue)
        );
        impulseSource.GenerateImpulseWithForce(intensity);
    }
    
    /// <summary>
    /// Generates a camera shake impulse with custom type and shape.
    /// </summary>
    /// <param name="impulseSource">The Cinemachine impulse source component.</param>
    /// <param name="type">The type of impulse to generate (overrides configured impulseType).</param>
    /// <param name="shape">The shape of impulse to generate (overrides configured impulseShape).</param>
    public void GenerateImpulse(CinemachineImpulseSource impulseSource, CinemachineImpulseDefinition.ImpulseTypes type, CinemachineImpulseDefinition.ImpulseShapes shape)
    {
        if (!impulseSource) return;

        impulseSource.ImpulseDefinition.ImpulseType = type;
        impulseSource.ImpulseDefinition.ImpulseShape = shape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = new Vector3(
            UnityEngine.Random.Range(xVelocityRange.minValue, xVelocityRange.maxValue),
            UnityEngine.Random.Range(yVelocityRange.minValue, yVelocityRange.maxValue),
            UnityEngine.Random.Range(zVelocityRange.minValue, zVelocityRange.maxValue)
        );
        impulseSource.GenerateImpulseWithForce(intensity);
    }
    
    /// <summary>
    /// Generates a camera shake impulse with custom intensity.
    /// </summary>
    /// <param name="impulseSource">The Cinemachine impulse source component.</param>
    /// <param name="customIntensity">The intensity multiplier for this impulse (overrides configured intensity).</param>
    public void GenerateImpulse(CinemachineImpulseSource impulseSource, float customIntensity)
    {
        if (!impulseSource) return;

        impulseSource.ImpulseDefinition.ImpulseType = impulseType;
        impulseSource.ImpulseDefinition.ImpulseShape = impulseShape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = new Vector3(
            UnityEngine.Random.Range(xVelocityRange.minValue, xVelocityRange.maxValue),
            UnityEngine.Random.Range(yVelocityRange.minValue, yVelocityRange.maxValue),
            UnityEngine.Random.Range(zVelocityRange.minValue, zVelocityRange.maxValue)
        );
        impulseSource.GenerateImpulseWithForce(customIntensity);
    }
    
    /// <summary>
    /// Generates a camera shake impulse with custom velocity direction.
    /// </summary>
    /// <param name="impulseSource">The Cinemachine impulse source component.</param>
    /// <param name="velocity">The velocity vector for the impulse (overrides random velocity ranges).</param>
    public void GenerateImpulse(CinemachineImpulseSource impulseSource, Vector3 velocity)
    {
        if (!impulseSource) return;

        impulseSource.ImpulseDefinition.ImpulseType = impulseType;
        impulseSource.ImpulseDefinition.ImpulseShape = impulseShape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = velocity;
        impulseSource.GenerateImpulseWithForce(intensity);
    }
    
    /// <summary>
    /// Generates a camera shake impulse with custom intensity and velocity.
    /// </summary>
    /// <param name="impulseSource">The Cinemachine impulse source component.</param>
    /// <param name="customIntensity">The intensity multiplier for this impulse (overrides configured intensity).</param>
    /// <param name="velocity">The velocity vector for the impulse (overrides random velocity ranges).</param>
    public void GenerateImpulse(CinemachineImpulseSource impulseSource, float customIntensity, Vector3 velocity)
    {
        if (!impulseSource) return;

        impulseSource.ImpulseDefinition.ImpulseType = impulseType;
        impulseSource.ImpulseDefinition.ImpulseShape = impulseShape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = velocity;
        impulseSource.GenerateImpulseWithForce(customIntensity);
    }
    
    /// <summary>
    /// Generates a camera shake impulse with custom type, intensity, and velocity.
    /// </summary>
    /// <param name="impulseSource">The Cinemachine impulse source component.</param>
    /// <param name="type">The type of impulse to generate (overrides configured impulseType).</param>
    /// <param name="customIntensity">The intensity multiplier for this impulse (overrides configured intensity).</param>
    /// <param name="velocity">The velocity vector for the impulse (overrides random velocity ranges).</param>
    public void GenerateImpulse(CinemachineImpulseSource impulseSource, CinemachineImpulseDefinition.ImpulseTypes type, float customIntensity, Vector3 velocity)
    {
        if (!impulseSource) return;

        impulseSource.ImpulseDefinition.ImpulseType = type;
        impulseSource.ImpulseDefinition.ImpulseShape = impulseShape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = velocity;
        impulseSource.GenerateImpulseWithForce(customIntensity);
    }
}