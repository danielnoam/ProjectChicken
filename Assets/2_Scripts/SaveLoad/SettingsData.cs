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
    
    
    
    public SettingsData (ControlSchemeSettings keyboardMouseScheme, ControlSchemeSettings gamepadScheme)
    {
        this.keyboardMouseScheme = keyboardMouseScheme;
        this.gamepadScheme = gamepadScheme;
    }
    
}



