using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeOptions : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Min(-90f)] private float minDB = -60f;
    [Tooltip("Using 0 instead of 20 because 20 is above the default range")]
    [SerializeField, Min(20)] private float maxDB; // using 0 instead of 20 because 20 is above the default range
    [Header("References")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolume;
    [SerializeField] private Slider sfxVolume;
    [SerializeField] private Slider musicVolume;

    private VolumeSettings _currentVolumeSettings;


    private void Start()
    {
        _currentVolumeSettings = SaveManager.GetVolumeSettings();
        audioMixer.SetFloat("MasterVolume", ConvertToDecibelRange(_currentVolumeSettings.masterVolume));
        audioMixer.SetFloat("SFXVolume", ConvertToDecibelRange(_currentVolumeSettings.sfxVolume));
        audioMixer.SetFloat("MusicVolume", ConvertToDecibelRange(_currentVolumeSettings.musicVolume));


        if (masterVolume)
        {
            masterVolume.value = _currentVolumeSettings.masterVolume;
            masterVolume.onValueChanged.AddListener(SetMasterVolume);
        }
        
        if (sfxVolume)
        {
            sfxVolume.value = _currentVolumeSettings.sfxVolume;
            sfxVolume.onValueChanged.AddListener(SetSfxVolume);
        }
        
        if (musicVolume)
        {
            musicVolume.value = _currentVolumeSettings.musicVolume;
            musicVolume.onValueChanged.AddListener(SetMusicVolume);
        }
    }
    
    
    private void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", ConvertToDecibelRange(volume));
        _currentVolumeSettings.masterVolume = volume;
        SaveManager.UpdateVolumeSettings(_currentVolumeSettings);
    }
    
    
    private void SetSfxVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", ConvertToDecibelRange(volume));
        _currentVolumeSettings.sfxVolume = volume;
        SaveManager.UpdateVolumeSettings(_currentVolumeSettings);
    }
    
    
    private void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", ConvertToDecibelRange(volume));
        _currentVolumeSettings.musicVolume = volume;
        SaveManager.UpdateVolumeSettings(_currentVolumeSettings);
    }
    
    
    private float ConvertToDecibelRange(float normalizedValue)
    {
        return minDB + (normalizedValue * (maxDB - minDB));
    }
    
    
}
