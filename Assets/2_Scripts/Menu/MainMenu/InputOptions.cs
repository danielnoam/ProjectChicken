
using DNExtensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputOptions : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField, MinMaxRange(0.1f,10f)] private RangedFloat sensitivityRange = new RangedFloat(0.1f, 2f);
    
    
    [Header("References")]
    [SerializeField] private Slider keyboardAimSensitivity;
    [SerializeField] private TextMeshProUGUI keyboardAimSensitivityText;
    [SerializeField] private Toggle keyboardInvertY;
    [SerializeField] private Toggle keyboardInvertX;
    [SerializeField] private Slider gamepadAimSensitivity;
    [SerializeField] private TextMeshProUGUI gamepadAimSensitivityText;
    [SerializeField] private Toggle gamepadInvertY;
    [SerializeField] private Toggle gamepadInvertX;

    private ControlSchemeSettings _keyboardControlSchemeSettings;
    private ControlSchemeSettings _gamepadControlSchemeSettings;


    private void OnValidate()
    {
        if (keyboardAimSensitivity)
        {
            keyboardAimSensitivity.minValue = sensitivityRange.minValue;
            keyboardAimSensitivity.maxValue = sensitivityRange.maxValue;
        }
        
        if (gamepadAimSensitivity)
        {
            gamepadAimSensitivity.minValue = sensitivityRange.minValue;
            gamepadAimSensitivity.maxValue = sensitivityRange.maxValue;
        }

        
    }

    private void Awake()
    {
        _keyboardControlSchemeSettings = SaveManager.GetKeyboardControlScheme();
        _gamepadControlSchemeSettings = SaveManager.GetGamepadControlScheme();



        if (keyboardAimSensitivity)
        {
            keyboardAimSensitivity.value = _keyboardControlSchemeSettings.aimSensitivity;
            keyboardAimSensitivityText.text = _keyboardControlSchemeSettings.aimSensitivity.ToString("0.0");
            keyboardAimSensitivity.onValueChanged.AddListener(SetKeyboardSchemeAimSensitivity);
        }
        
        if (keyboardInvertY)
        {
            keyboardInvertY.isOn = _keyboardControlSchemeSettings.invertY;
            keyboardInvertY.onValueChanged.AddListener(SetKeyboardSchemeInvertY);

        }
        
        if (keyboardInvertX)
        {
            keyboardInvertX.isOn = _keyboardControlSchemeSettings.invertX;
            keyboardInvertX.onValueChanged.AddListener(SetKeyboardSchemeInvertX);
        }
        
        if (gamepadAimSensitivity)
        {
            gamepadAimSensitivity.value = _gamepadControlSchemeSettings.aimSensitivity;
            gamepadAimSensitivityText.text = _gamepadControlSchemeSettings.aimSensitivity.ToString("0.0");
            gamepadAimSensitivity.onValueChanged.AddListener(SetGamepadSchemeAimSensitivity);
        }
        
        if (gamepadInvertY)
        {
            gamepadInvertY.isOn = _gamepadControlSchemeSettings.invertY;
            gamepadInvertY.onValueChanged.AddListener(SetGamepadSchemeInvertY);
        }
        
        if (gamepadInvertX)
        {
            gamepadInvertX.isOn = _gamepadControlSchemeSettings.invertX;
            gamepadInvertX.onValueChanged.AddListener(SetGamepadSchemeInvertX);
        }
    }
    
    private void SetKeyboardSchemeAimSensitivity(float value)
    {
        _keyboardControlSchemeSettings.aimSensitivity = value;
        keyboardAimSensitivityText.text = value.ToString("0.0");
        SaveManager.UpdateKeyboardControlScheme(_keyboardControlSchemeSettings);
    }
    
    private void SetKeyboardSchemeInvertY(bool value)
    {
        _keyboardControlSchemeSettings.invertY = value;
        SaveManager.UpdateKeyboardControlScheme(_keyboardControlSchemeSettings);
    }
    
    private void SetKeyboardSchemeInvertX(bool value)
    {
        _keyboardControlSchemeSettings.invertX = value;
        SaveManager.UpdateKeyboardControlScheme(_keyboardControlSchemeSettings);
    }
    
    private void SetGamepadSchemeAimSensitivity(float value)
    {
        _gamepadControlSchemeSettings.aimSensitivity = value;
        gamepadAimSensitivityText.text = value.ToString("0.0");
        SaveManager.UpdateGamepadControlScheme(_gamepadControlSchemeSettings);
    }
    
    private void SetGamepadSchemeInvertY(bool value)
    {
        _gamepadControlSchemeSettings.invertY = value;
        SaveManager.UpdateGamepadControlScheme(_gamepadControlSchemeSettings);
    }
    
    private void SetGamepadSchemeInvertX(bool value)
    {
        _gamepadControlSchemeSettings.invertX = value;
        SaveManager.UpdateGamepadControlScheme(_gamepadControlSchemeSettings);
    }
    
}
