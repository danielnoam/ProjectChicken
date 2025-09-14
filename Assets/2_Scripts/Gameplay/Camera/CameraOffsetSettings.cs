using System;
using UnityEngine;

[Serializable]
public class CameraDepthSettings
{
    [Tooltip("How much the camera moves forward/backward based on overall activity level")]
    public float range = 1f;
    
    [Tooltip("Minimum activity level before depth effect kicks in")]
    [Range(0f, 0.99f)]
    public float threshold = 0.2f;
    
    [Tooltip("Reverse depth movement direction")]
    public bool invert;

    public void Validate()
    {
        threshold = Mathf.Clamp(threshold, 0f, 0.99f);
    }
}

[Serializable]
public class CameraSettings
{
    [Header("Position Influence")]
    [Tooltip("Enable camera position influence")]
    public bool enablePosition = true;
    
    [Tooltip("Maximum offset distance the camera can move (X=horizontal, Y=vertical)")]
    public Vector2 positionRange = new Vector2(10f, 5f);
    
    [Tooltip("Dead zone for normalized position values. Camera won't move until position exceeds these values.")]
    [Range(0f, 0.99f)]
    public Vector2 positionThreshold = new Vector2(0.2f, 0.2f);
    
    [Tooltip("How quickly the camera moves to the target position. Higher values = faster response.")]
    [Range(0.1f, 10f)]
    public float positionSmoothness = 2f;
    
    [Tooltip("Reverse horizontal camera movement direction")]
    public bool invertPositionX;
    
    [Tooltip("Reverse vertical camera movement direction")]
    public bool invertPositionY;
    
    [Header("Rotation Influence")]
    [Tooltip("Enable camera rotation influence")]
    public bool enableRotation = true;
    
    [Tooltip("Maximum rotation angles the camera can rotate (X=pitch up/down, Y=yaw left/right) in degrees")]
    public Vector2 rotationRange = new Vector2(5f, 10f);
    
    [Tooltip("Dead zone for normalized position values. Camera won't rotate until position exceeds these values.")]
    [Range(0f, 0.99f)]
    public Vector2 rotationThreshold = new Vector2(0.2f, 0.2f);
    
    [Tooltip("How quickly the camera rotates to the target angle. Higher values = faster response.")]
    [Range(0.1f, 10f)]
    public float rotationSmoothness = 2f;
    
    [Tooltip("Reverse horizontal camera rotation direction")]
    public bool invertRotationX;
    
    [Tooltip("Reverse vertical camera rotation direction")]
    public bool invertRotationY;
    
    [Header("Depth Effect")]
    [Tooltip("Depth camera movement based on overall activity level")]
    public CameraDepthSettings depthSettings = new CameraDepthSettings();

    public void Validate()
    {
        positionThreshold.x = Mathf.Clamp(positionThreshold.x, 0f, 0.99f);
        positionThreshold.y = Mathf.Clamp(positionThreshold.y, 0f, 0.99f);
        rotationThreshold.x = Mathf.Clamp(rotationThreshold.x, 0f, 0.99f);
        rotationThreshold.y = Mathf.Clamp(rotationThreshold.y, 0f, 0.99f);
        positionSmoothness = Mathf.Max(0.1f, positionSmoothness);
        rotationSmoothness = Mathf.Max(0.1f, rotationSmoothness);
        depthSettings.Validate();
    }
}