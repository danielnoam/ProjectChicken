using System;
using System.Collections.Generic;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    [SerializeField, Self(Flag.EditableAnywhere)] private PlayerInput playerInput;
    
    // Sprite assets has to be in /Resources/Sprite Assets/ folder
    [Header("Controls Sprite Assets")]
    [SerializeField] private TMP_SpriteAsset keyboardMouseSpriteAsset;
    [SerializeField] private TMP_SpriteAsset gamepadSpriteAsset;
    
    [Header("Cursor Settings")] 
    [SerializeField] private bool hideCursor = true;

    private readonly ControlSchemeSettings _keyboardMouseScheme = new ControlSchemeSettings();
    private readonly ControlSchemeSettings _gamepadScheme = new ControlSchemeSettings();
    private ControlSchemeSettings _currentControlScheme = new ControlSchemeSettings();
    private bool _isCurrentDeviceGamepad;
    
    public ControlSchemeSettings CurrentControlScheme => _currentControlScheme;
    public ControlSchemeSettings KeyboardMouseScheme => _keyboardMouseScheme;
    public ControlSchemeSettings GamepadScheme => _gamepadScheme;
    public bool IsCurrentDeviceGamepad => _isCurrentDeviceGamepad;
    public PlayerInput PlayerInput => playerInput;
    
    // Events for device changes
    public event Action<PlayerInput> OnDeviceRegainedEvent;
    public event Action<PlayerInput> OnDeviceLostEvent; 
    public event Action<PlayerInput> OnControlsChangedEvent;

    private void OnValidate()
    {
        this.ValidateRefs(); 
    }

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (!playerInput) return;
        
        SetCursorVisibility(!hideCursor);
        UpdateControlSchemeSettings();
        _currentControlScheme.SetControlSchemeSettings(_keyboardMouseScheme);
    }

    private void OnEnable()
    {
        if (!playerInput) return;
        
        playerInput.onDeviceRegained += OnDeviceRegained;
        playerInput.onDeviceLost += OnDeviceLost;
        playerInput.onControlsChanged += OnControlsChanged;
        
        if (SaveManager.Instance)
        {
            SaveManager.Instance.OnSettingsDataChanged += UpdateControlSchemeSettings;
        }
    }

    private void OnDisable()
    {
        if (!playerInput) return;
        
        playerInput.onDeviceRegained -= OnDeviceRegained;
        playerInput.onDeviceLost -= OnDeviceLost;
        playerInput.onControlsChanged -= OnControlsChanged;
        
        if (SaveManager.Instance)
        {
            SaveManager.Instance.OnSettingsDataChanged -= UpdateControlSchemeSettings;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDeviceRegained(PlayerInput input)
    {
        SetActiveControlScheme(input);
        OnDeviceRegainedEvent?.Invoke(input);
    }

    private void OnDeviceLost(PlayerInput input)
    {
        SetActiveControlScheme(input);
        OnDeviceLostEvent?.Invoke(input);
    }

    private void OnControlsChanged(PlayerInput input)
    {
        SetActiveControlScheme(input);
        OnControlsChangedEvent?.Invoke(input);
    }
    
    private void SetActiveControlScheme(PlayerInput input)
    {
        if (input.currentControlScheme == "Gamepad")
        {
            _isCurrentDeviceGamepad = true;
            _currentControlScheme = _gamepadScheme;
        }
        else
        {
            _isCurrentDeviceGamepad = false;
            _currentControlScheme = _keyboardMouseScheme;
        }
    }
    
    private void UpdateControlSchemeSettings()
    {
        _keyboardMouseScheme.SetControlSchemeSettings(SaveManager.GetKeyboardControlScheme());
        _gamepadScheme.SetControlSchemeSettings(SaveManager.GetGamepadControlScheme());
        _currentControlScheme = _isCurrentDeviceGamepad ? _gamepadScheme : _keyboardMouseScheme;
    }
    
    public void SetCursorVisibility(bool state)
    {
        if (state)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }
    }
    
    [Button]
    public void ToggleCursorVisibility()
    {
        if (Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public static string ReplaceActionBindingsWithSprites(string text)
    {
        if (!Instance) return text;
        
        TMP_SpriteAsset spriteAsset = Instance._isCurrentDeviceGamepad 
            ? Instance.gamepadSpriteAsset 
            : Instance.keyboardMouseSpriteAsset;
            
        return InputManagerBindingFormatter.ReplaceActionBindings(text, true, Instance.playerInput, spriteAsset);
    }
    
    public static string ReplaceActionBindingsWithText(string text)
    {
        return InputManagerBindingFormatter.ReplaceActionBindings(text, false, Instance.playerInput);
    }
}