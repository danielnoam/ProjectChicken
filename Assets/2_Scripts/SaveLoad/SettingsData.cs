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
    
    
    public SettingsData (ControlSchemeSettings keyboardMouseScheme, ControlSchemeSettings gamepadScheme, VolumeSettings volumeSettings)
    {
        this.keyboardMouseScheme = keyboardMouseScheme;
        this.gamepadScheme = gamepadScheme;
        this.volumeSettings = volumeSettings;
    }
    
}



