using System;
using UnityEngine;
using VInspector;

[Serializable]
public class ControlSchemeSettings
{
    [Header("Aiming")]
    public bool invertY;
    public bool invertX;
    [Min(0.1f), Tooltip("Speed multiplier for crosshair movement")] public float aimSensitivity;
    [Range(0f, 0.3f), Tooltip("Input below this threshold is ignored to prevent drift")] public float deadZone;
    [Tooltip("Controls how input magnitude maps to sensitivity")] public AnimationCurve magnitudeToSensitivityCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Header("Aim Lock")]
    public bool aimLock;
    public float aimLockRadius;
    [Min(0)] public float aimLockSpeed;
    [Min(0)] public float aimLockStrength;
    [Min(0)] public float aimLockCooldown;
    
    [Header("Dodge")]
    public bool allowFreeformDodge;
    public bool doubleTapToDodge;
    [Min(0.1f), ShowIf("doubleTapToDodge")] public float doubleTapTime;

    public ControlSchemeSettings(bool invertY, bool invertX, float aimSensitivity, float deadZone,AnimationCurve magnitudeToSensitivityCurve,
        bool aimLock, float aimLockRadius, float aimLockSpeed, float aimLockStrength, float aimLockCooldown,
        bool allowFreeformDodge, bool doubleTapToDodge, float doubleTapTime)
    {
        this.invertY = invertY;
        this.invertX = invertX;
        this.aimSensitivity = aimSensitivity;
        this.deadZone = deadZone;
        this.magnitudeToSensitivityCurve = magnitudeToSensitivityCurve;
        this.aimLock = aimLock;
        this.aimLockRadius = aimLockRadius;
        this.aimLockSpeed = aimLockSpeed;
        this.aimLockStrength = aimLockStrength;
        this.aimLockCooldown = aimLockCooldown;
        this.allowFreeformDodge = allowFreeformDodge;
        this.doubleTapToDodge = doubleTapToDodge;
        this.doubleTapTime = doubleTapTime;
    }
    
    public ControlSchemeSettings()
    {
        
    }
    
    public void SetControlSchemeSettings(ControlSchemeSettings settings)
    {
        this.invertY = settings.invertY;
        this.invertX = settings.invertX;
        this.aimSensitivity = settings.aimSensitivity;
        this.deadZone = settings.deadZone;
        this.magnitudeToSensitivityCurve = settings.magnitudeToSensitivityCurve;
        this.aimLock = settings.aimLock;
        this.aimLockRadius = settings.aimLockRadius;
        this.aimLockSpeed = settings.aimLockSpeed;
        this.aimLockStrength = settings.aimLockStrength;
        this.aimLockCooldown = settings.aimLockCooldown;
        this.allowFreeformDodge = settings.allowFreeformDodge;
        this.doubleTapToDodge = settings.doubleTapToDodge;
        this.doubleTapTime = settings.doubleTapTime;
    }
}