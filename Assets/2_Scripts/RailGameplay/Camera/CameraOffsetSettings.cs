using System;
using UnityEngine;


[Serializable]
public class CameraOffsetSettings
{
    public Vector3 range = new Vector3(10f, 5f, 1f);
    public Vector3 threshold = new Vector3(0.2f, 0.2f, 0.2f);
    public float smoothness = 2f;
    public bool invertX;
    public bool invertY;


    public void Validate()
    {
        threshold.x = Mathf.Clamp(threshold.x, 0f, 0.99f);
        threshold.y = Mathf.Clamp(threshold.y, 0f, 0.99f);
        threshold.z = Mathf.Clamp(threshold.z, 0f, 0.99f);
        threshold.x = Mathf.Clamp(threshold.x, 0f, 0.99f);
        threshold.y = Mathf.Clamp(threshold.y, 0f, 0.99f);
    }
}