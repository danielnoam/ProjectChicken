using System;
using UnityEngine;
using UnityEngine.UI;

public class InputOptions : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider keyboardAimSensitivity;
    [SerializeField] private Toggle keyboardInvertY;
    [SerializeField] private Toggle keyboardInvertX;
    [SerializeField] private Slider gamepadAimSensitivity;
    [SerializeField] private Toggle gamepadInvertY;
    [SerializeField] private Toggle gamepadInvertX;

    private ControlSchemeSettings _keyboardControlSchemeSettings;
    private ControlSchemeSettings _gamepadControlSchemeSettings;


    private void Awake()
    {
        _keyboardControlSchemeSettings = SaveManager.GetKeyboardControlScheme();
        _gamepadControlSchemeSettings = SaveManager.GetGamepadControlScheme();



        if (keyboardAimSensitivity)
        {
            keyboardAimSensitivity.value = _keyboardControlSchemeSettings.aimSensitivity;
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
