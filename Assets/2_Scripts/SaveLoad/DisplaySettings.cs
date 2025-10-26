using System;
using UnityEngine;
using VInspector;


public enum VSyncType
{
    Off,
    EveryFrame,
    EverySecondFrame
}

[Serializable]
public class DisplaySettings
{
    public int resolutionIndex;
    public FullScreenMode fullScreenMode;
    public VSyncType vSyncType;

    public DisplaySettings()
    {

    }

    public DisplaySettings(int resolutionIndex, FullScreenMode fullScreenMode, VSyncType vSyncType)
    {
        this.resolutionIndex = resolutionIndex;
        this.fullScreenMode = fullScreenMode;
        this.vSyncType = vSyncType;

    }
    
    public DisplaySettings(FullScreenMode fullScreenMode, VSyncType vSyncType)
    {
        if (Application.isEditor)
        {
            resolutionIndex = 0;
        }
        else
        {
            Resolution[] resolutions = Screen.resolutions;
            resolutionIndex = resolutions.Length - 1;
        }

        this.fullScreenMode = fullScreenMode;
        this.vSyncType = vSyncType;

    }

    public void SetDisplaySettings(DisplaySettings settings)
    {
        resolutionIndex = settings.resolutionIndex;
        fullScreenMode = settings.fullScreenMode;
        vSyncType = settings.vSyncType;
    }
}

