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
    
    [Header("Aim Lock")]
    public bool aimLock;
    public float aimLockRadius;
    [Min(0)] public float lockAimSpeed;
    [Min(0)] public float lockAimStrength;
    [Min(0)] public float lockAimCooldown;
    
    [Header("Dodge")]
    public bool allowFreeformDodge;
    public bool doubleTapToDodge;
    [Min(0.1f), ShowIf("doubleTapToDodge")] public float doubleTapTime;

    public ControlSchemeSettings(bool invertY, bool invertX, float aimSensitivity, float deadZone, 
        bool aimLock, float aimLockRadius, float lockAimSpeed, float lockAimStrength, float lockAimCooldown,
        bool allowFreeformDodge, bool doubleTapToDodge, float doubleTapTime)
    {
        this.invertY = invertY;
        this.invertX = invertX;
        this.aimSensitivity = aimSensitivity;
        this.deadZone = deadZone;
        this.aimLock = aimLock;
        this.aimLockRadius = aimLockRadius;
        this.lockAimSpeed = lockAimSpeed;
        this.lockAimStrength = lockAimStrength;
        this.lockAimCooldown = lockAimCooldown;
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
        this.aimLock = settings.aimLock;
        this.aimLockRadius = settings.aimLockRadius;
        this.lockAimSpeed = settings.lockAimSpeed;
        this.lockAimStrength = settings.lockAimStrength;
        this.lockAimCooldown = settings.lockAimCooldown;
        this.allowFreeformDodge = settings.allowFreeformDodge;
        this.doubleTapToDodge = settings.doubleTapToDodge;
        this.doubleTapTime = settings.doubleTapTime;
    }
}