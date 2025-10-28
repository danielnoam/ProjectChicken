using DNExtensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InputType
{
    KeyboardMouse,
    Gamepad
}

public class InputTab : MonoBehaviour
{
    [Header("Input Type")]
    [SerializeField] private InputType inputType = InputType.KeyboardMouse;
    
    [Header("Settings")]
    [SerializeField, MinMaxRange(0.1f,10f)] private RangedFloat sensitivityRange = new RangedFloat(0.1f, 10f);
    
    [Header("References")]
    [SerializeField] private Slider aimSensitivity;
    [SerializeField] private TextMeshProUGUI aimSensitivityText;
    [SerializeField] private Toggle invertY;
    [SerializeField] private Toggle invertX;
    [SerializeField] private Toggle doubleTapDodge;

    private ControlSchemeSettings _controlSchemeSettings;

    private void OnValidate()
    {
        if (aimSensitivity)
        {
            aimSensitivity.minValue = sensitivityRange.minValue;
            aimSensitivity.maxValue = sensitivityRange.maxValue;
        }
    }

    private void Awake()
    {
        _controlSchemeSettings = inputType == InputType.KeyboardMouse 
            ? SaveManager.GetKeyboardControlScheme() 
            : SaveManager.GetGamepadControlScheme();

        InitializeUI();
        SetupEventListeners();
    }

    private void InitializeUI()
    {
        if (aimSensitivity)
        {
            aimSensitivity.value = _controlSchemeSettings.aimSensitivity;
            UpdateSensitivityText(_controlSchemeSettings.aimSensitivity);
        }
        
        if (invertY)
        {
            invertY.isOn = _controlSchemeSettings.invertY;
        }
        
        if (invertX)
        {
            invertX.isOn = _controlSchemeSettings.invertX;
        }

        if (doubleTapDodge)
        {
            
            doubleTapDodge.isOn = _controlSchemeSettings.doubleTapToDodge;
        }
        
    }

    private void SetupEventListeners()
    {
        if (aimSensitivity)
        {
            aimSensitivity.onValueChanged.AddListener(SetAimSensitivity);
        }
        
        if (invertY)
        {
            invertY.onValueChanged.AddListener(SetInvertY);
        }
        
        if (invertX)
        {
            invertX.onValueChanged.AddListener(SetInvertX);
        }
        
        
        if (doubleTapDodge)
        {
            doubleTapDodge.onValueChanged.AddListener(SetDoubleTapDodge);
        }
        
    }
    
    private void SetAimSensitivity(float value)
    {
        _controlSchemeSettings.aimSensitivity = value;
        UpdateSensitivityText(value);
        UpdateControlScheme();
    }
    
    private void SetInvertY(bool value)
    {
        _controlSchemeSettings.invertY = value;
        UpdateControlScheme();
    }
    
    private void SetInvertX(bool value)
    {
        _controlSchemeSettings.invertX = value;
        UpdateControlScheme();
    }
    
    
    private void SetDoubleTapDodge(bool value)
    {
        _controlSchemeSettings.doubleTapToDodge = value;
        UpdateControlScheme();
    }

    private void UpdateSensitivityText(float value)
    {
        if (aimSensitivityText)
        {
            aimSensitivityText.text = value.ToString("0.0");
        }
    }

    private void UpdateControlScheme()
    {
        if (inputType == InputType.KeyboardMouse)
        {
            SaveManager.UpdateKeyboardControlScheme(_controlSchemeSettings);
        }
        else
        {
            SaveManager.UpdateGamepadControlScheme(_controlSchemeSettings);
        }
    }
}