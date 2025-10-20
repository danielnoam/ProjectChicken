using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeTab : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Min(-90f)] private float minDB = -80f;
    [SerializeField] private float maxDB; // 0 dB is standard max
    
    [Header("References")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolume;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Slider sfxVolume;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private Slider musicVolume;
    [SerializeField] private TextMeshProUGUI musicVolumeText;

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
            masterVolumeText.text = $"{_currentVolumeSettings.masterVolume * 100f:0}%";
            masterVolume.onValueChanged.AddListener(SetMasterVolume);
        }
        
        if (sfxVolume)
        {
            sfxVolume.value = _currentVolumeSettings.sfxVolume;
            sfxVolumeText.text = $"{_currentVolumeSettings.sfxVolume * 100f:0}%";
            sfxVolume.onValueChanged.AddListener(SetSfxVolume);
        }
        
        if (musicVolume)
        {
            musicVolume.value = _currentVolumeSettings.musicVolume;
            musicVolumeText.text = $"{_currentVolumeSettings.musicVolume * 100f:0}%";
            musicVolume.onValueChanged.AddListener(SetMusicVolume);
        }
    }
    
    private void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", ConvertToDecibelRange(volume));
        _currentVolumeSettings.masterVolume = volume;
        masterVolumeText.text = $"{volume * 100f:0}%";
        SaveManager.UpdateVolumeSettings(_currentVolumeSettings);
    }
    
    private void SetSfxVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", ConvertToDecibelRange(volume));
        _currentVolumeSettings.sfxVolume = volume;
        sfxVolumeText.text = $"{volume * 100f:0}%";
        SaveManager.UpdateVolumeSettings(_currentVolumeSettings);
    }
    
    private void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", ConvertToDecibelRange(volume));
        _currentVolumeSettings.musicVolume = volume;
        musicVolumeText.text = $"{volume * 100f:0}%";
        SaveManager.UpdateVolumeSettings(_currentVolumeSettings);
    }
    
    // Convert linear slider (0-1) to logarithmic decibel scale
    private float ConvertToDecibelRange(float normalizedValue)
    {
        // Handle the zero case (mute)
        if (normalizedValue <= 0.0001f)
            return minDB;
        
        // Logarithmic conversion: 20 * log10(value)
        // This maps 0.0001-1.0 to minDB-maxDB in a logarithmic way
        return maxDB + 20f * Mathf.Log10(normalizedValue);
    }
}