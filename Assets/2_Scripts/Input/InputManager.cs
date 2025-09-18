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
    
    
    public event Action<PlayerInput> OnDeviceRegainedEvent;
    public event Action<PlayerInput> OnDeviceLostEvent; 
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


    public static string ReplaceTextWithBinding(string text, InputBinding action)
    {
        if (!Instance) return text;
        
        TMP_SpriteAsset spriteAssetToUse = Instance._isCurrentDeviceGamepad ? Instance.gamepadSpriteAsset : Instance.keyboardMouseSpriteAsset;
        string stringButtonName = action.ToString();
        stringButtonName = RenameInput(stringButtonName);
        
        text = text.Replace("BUTTONPROMPT", $"<sprite=\"{spriteAssetToUse.name}\" name=\"{stringButtonName}\">");


        return text;
    }
    
    public static string ReplaceAllActionBindings(string text)
    {
        if (!Instance) return text;
        
        var actionMappings = new Dictionary<string, string>
        {
            {"ACTION_MOVE", "Move"},
            {"ACTION_LOOK", "Look"},
            {"ACTION_FIRE", "Fire"},
        };
    
        foreach (var mapping in actionMappings)
        {
            if (!text.Contains(mapping.Key)) continue;
            var action = Instance.playerInput.actions[mapping.Value];
            
            if (action is not { bindings: { Count: > 0 } }) continue;
            var binding = action.bindings[0]; 
            string spriteTag = GetSpriteTag(binding);
            text = text.Replace(mapping.Key, spriteTag);
        }
    
        return text;
    }
    
    private static string GetSpriteTag(InputBinding binding)
    {
        TMP_SpriteAsset spriteAssetToUse = Instance._isCurrentDeviceGamepad ? Instance.gamepadSpriteAsset : Instance.keyboardMouseSpriteAsset;
        string stringButtonName = binding.ToString();
        stringButtonName = RenameInput(stringButtonName);
    
        return $"<sprite=\"{spriteAssetToUse.name}\" name=\"{stringButtonName}\">";
    }

    private static string RenameInput(string buttonName)
    {
        buttonName = buttonName.Replace("<Keyboard>/", "Keyboard_");
        buttonName = buttonName.Replace("<Mouse>/", "Mouse_");
        buttonName = buttonName.Replace("<Gamepad>/", "Gamepad_");

        return buttonName;
    }

    [SerializeField] protected TextMeshProUGUI textTest;
    [Button]
    private void UpdateTextTest()
    {

        textTest.text = ReplaceAllActionBindings(textTest.text);
    }
}