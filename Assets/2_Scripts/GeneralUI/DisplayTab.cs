using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DisplayTab : MonoBehaviour
{
    [Header("Display References")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown fullScreenModeDropdown;
    [SerializeField] private TMP_Dropdown vSyncDropdown;

    private DisplaySettings _currentDisplaySettings;
    private Resolution[] _availableResolutions;

    private void Start()
    {
        // _availableResolutions = Screen.resolutions;
        // _currentDisplaySettings = SaveManager.GetDisplaySettings();
        //
        // SetupResolutionDropdown();
        // SetupFullScreenModeDropdown();
        // SetupVSyncDropdown();
        //
        // ApplyDisplaySettings(_currentDisplaySettings);
        //
        // if (resolutionDropdown)
        //     resolutionDropdown.onValueChanged.AddListener(SetResolution);
        //
        // if (fullScreenModeDropdown)
        //     fullScreenModeDropdown.onValueChanged.AddListener(SetFullScreenMode);
        //
        // if (vSyncDropdown)
        //     vSyncDropdown.onValueChanged.AddListener(SetVSync);
    }

    private void SetupResolutionDropdown()
    {
        if (!resolutionDropdown) return;
        
        resolutionDropdown.ClearOptions();
        
        var options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;
        
        for (int i = 0; i < _availableResolutions.Length; i++)
        {
            string option = $"{_availableResolutions[i].width} x {_availableResolutions[i].height} @ {_availableResolutions[i].refreshRateRatio.value:0}Hz";
            options.Add(option);
            
            if (_availableResolutions[i].width == Screen.currentResolution.width &&
                _availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        
        resolutionDropdown.value = _currentDisplaySettings.resolutionIndex >= 0 && 
                                   _currentDisplaySettings.resolutionIndex < _availableResolutions.Length
            ? _currentDisplaySettings.resolutionIndex
            : currentResolutionIndex;
        
        resolutionDropdown.RefreshShownValue();
    }

    private void SetupFullScreenModeDropdown()
    {
        if (!fullScreenModeDropdown) return;
        
        fullScreenModeDropdown.ClearOptions();
        
        var options = new System.Collections.Generic.List<string>
        {
            "Exclusive Fullscreen",
            "Fullscreen Window",
            "Maximized Window",
            "Windowed"
        };
        
        fullScreenModeDropdown.AddOptions(options);
        fullScreenModeDropdown.value = (int)_currentDisplaySettings.fullScreenMode;
        fullScreenModeDropdown.RefreshShownValue();
    }

    private void SetupVSyncDropdown()
    {
        if (!vSyncDropdown) return;
        
        vSyncDropdown.ClearOptions();
        
        var options = new System.Collections.Generic.List<string>
        {
            "Off",
            "Every Frame",
            "Every Second Frame"
        };
        
        vSyncDropdown.AddOptions(options);
        vSyncDropdown.value = (int)_currentDisplaySettings.vSyncType;
        vSyncDropdown.RefreshShownValue();
    }

    private void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= _availableResolutions.Length)
            return;
        
        _currentDisplaySettings.resolutionIndex = resolutionIndex;
        ApplyDisplaySettings(_currentDisplaySettings);
        SaveManager.UpdateDisplaySettings(_currentDisplaySettings);
    }

    private void SetFullScreenMode(int modeIndex)
    {
        _currentDisplaySettings.fullScreenMode = (FullScreenMode)modeIndex;
        ApplyDisplaySettings(_currentDisplaySettings);
        SaveManager.UpdateDisplaySettings(_currentDisplaySettings);
    }

    private void SetVSync(int vSyncIndex)
    {
        _currentDisplaySettings.vSyncType = (VSyncType)vSyncIndex;
        ApplyDisplaySettings(_currentDisplaySettings);
        SaveManager.UpdateDisplaySettings(_currentDisplaySettings);
    }

    private void ApplyDisplaySettings(DisplaySettings settings)
    {
        if (settings.resolutionIndex >= 0 && settings.resolutionIndex < _availableResolutions.Length)
        {
            Resolution res = _availableResolutions[settings.resolutionIndex];
            Screen.SetResolution(res.width, res.height, settings.fullScreenMode, res.refreshRateRatio);
        }
        else
        {
            Screen.fullScreenMode = settings.fullScreenMode;
        }
        
        QualitySettings.vSyncCount = settings.vSyncType switch
        {
            VSyncType.Off => 0,
            VSyncType.EveryFrame => 1,
            VSyncType.EverySecondFrame => 2,
            _ => 0
        };
    }
}