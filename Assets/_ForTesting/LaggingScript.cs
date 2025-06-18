using System;
using UnityEngine;

public class LaggingScript : MonoBehaviour
{
    [Range(1, 999)]
    public int targetFPS = 20;

    private void Awake()
    {
        Application.targetFrameRate = targetFPS;
        QualitySettings.vSyncCount = 0; // Disable vSync to allow frame rate limiting
    }

}