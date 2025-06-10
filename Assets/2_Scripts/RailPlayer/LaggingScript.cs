using System;
using UnityEngine;

public class LaggingScript : MonoBehaviour
{
    [Range(1, 120)]
    public int targetFPS = 20;

    private void Awake()
    {
        Application.targetFrameRate = targetFPS;
        QualitySettings.vSyncCount = 0; // Disable vSync to allow frame rate limiting
    }

    private void Update()
    {
        Application.targetFrameRate = targetFPS;
        QualitySettings.vSyncCount = 0; // Disable vSync to allow frame rate limiting
    }
}