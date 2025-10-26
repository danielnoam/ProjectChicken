using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

[Serializable]
public class SettingsData
{
    [Header("Input")] 
    public ControlSchemeSettings keyboardMouseScheme;
    public ControlSchemeSettings gamepadScheme;
    
    [Header("Volume")]
    public VolumeSettings volumeSettings;
    
    [Header("Display")]
    public DisplaySettings displaySettings;
    
    
    public SettingsData (ControlSchemeSettings keyboardMouseScheme, ControlSchemeSettings gamepadScheme, VolumeSettings volumeSettings, DisplaySettings displaySettings)
    {
        this.keyboardMouseScheme = keyboardMouseScheme;
        this.gamepadScheme = gamepadScheme;
        this.volumeSettings = volumeSettings;
        this.displaySettings = displaySettings;
    }
    
}



