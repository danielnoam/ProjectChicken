using System;
using UnityEngine;
using VInspector;

[Serializable]
public class VolumeSettings
{
    public float masterVolume;
    public float sfxVolume;
    public float musicVolume;
    

    
    public VolumeSettings()
    {
        
    }
    
    public VolumeSettings(float masterVolume,float sfxVolume ,float musicVolume)
    {
        this.masterVolume = masterVolume;
        this.sfxVolume = sfxVolume;
        this.musicVolume = musicVolume;
    }
    
    
    public void SetVolumeSettings(VolumeSettings settings)
    {
        masterVolume = settings.masterVolume;
        sfxVolume = settings.sfxVolume;
        musicVolume = settings.musicVolume;

    }
}