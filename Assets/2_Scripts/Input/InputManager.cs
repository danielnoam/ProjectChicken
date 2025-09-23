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

    
    
    
    public PlayerInput PlayerInput => playerInput;
    public ControlSchemeSettings CurrentControlScheme { get; private set; } = new ControlSchemeSettings();

    public ControlSchemeSettings KeyboardMouseScheme { get; } = new ControlSchemeSettings();

    public ControlSchemeSettings GamepadScheme { get; } = new ControlSchemeSettings();

    public bool IsCurrentDeviceGamepad { get; private set; }

    
    public event Action<PlayerInput> OnControlsChangedEvent;
    
    

    private void OnValidate()
    {
        this.ValidateRefs(); 
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (!playerInput) return;
        
        SetCursorVisibility(!hideCursor);
        UpdateControlSchemeSettings();
        CurrentControlScheme.SetControlSchemeSettings(KeyboardMouseScheme);
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
    }

    private void OnDeviceLost(PlayerInput input)
    {
        SetActiveControlScheme(input);
    }

    private void OnControlsChanged(PlayerInput input)
    {
        SetActiveControlScheme(input);
    }
    
    
    private void SetActiveControlScheme(PlayerInput input)
    {
        if (input.currentControlScheme == "Gamepad")
        {
            IsCurrentDeviceGamepad = true;
            CurrentControlScheme = GamepadScheme;
        }
        else
        {
            IsCurrentDeviceGamepad = false;
            CurrentControlScheme = KeyboardMouseScheme;
        }
        
        OnControlsChangedEvent?.Invoke(input);
    }
    
    private void UpdateControlSchemeSettings()
    {
        KeyboardMouseScheme.SetControlSchemeSettings(SaveManager.GetKeyboardControlScheme());
        GamepadScheme.SetControlSchemeSettings(SaveManager.GetGamepadControlScheme());
        CurrentControlScheme = IsCurrentDeviceGamepad ? GamepadScheme : KeyboardMouseScheme;
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

    public static string ReplaceActionTokenInText(string text, bool useSprite = true)
    {
        if (!Instance) return text;

        if (!useSprite)
        {
            return InputManagerBindingFormatter.ReplaceActionBindings(text, false, Instance.playerInput);
        }
        
        TMP_SpriteAsset spriteAsset = Instance.IsCurrentDeviceGamepad 
            ? Instance.gamepadSpriteAsset 
            : Instance.keyboardMouseSpriteAsset;
            
        return InputManagerBindingFormatter.ReplaceActionBindings(text, true, Instance.playerInput, spriteAsset);
    }
    
    
    
    
    /// <summary>
    /// Get binding for a specific InputAction
    /// </summary>
    public static string GetActionBinding(InputAction action, bool asSprite = true)
    {
        if (!Instance?.playerInput || action == null) return action?.name ?? "Unknown";
    
        TMP_SpriteAsset spriteAsset = asSprite ? Instance.IsCurrentDeviceGamepad 
            ? Instance.gamepadSpriteAsset 
            : Instance.keyboardMouseSpriteAsset : null;
    
        return InputManagerBindingFormatter.GetActionBinding(action, asSprite, Instance.playerInput, spriteAsset);
    }

    /// <summary>
    /// Get bindings for multiple InputActions
    /// </summary>
    public static string GetActionBindings(InputAction[] actions, string separator = " | ", bool asSprites = true)
    {
        if (actions == null || actions.Length == 0) return "";
    
        string[] bindings = new string[actions.Length];
        for (int i = 0; i < actions.Length; i++)
        {
            bindings[i] = GetActionBinding(actions[i], asSprites);
        }
    
        return string.Join(separator, bindings);
    }

}