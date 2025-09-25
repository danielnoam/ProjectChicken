using System;
using System.Linq;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

public class ActionKeyPrompt : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool useSprites = true;
    [SerializeField] private string separator = " | ";
    [SerializeField] private string prefix = "";
    [SerializeField] private string suffix = "";
    [SerializeField] private InputActionReference[] inputActionReferences = Array.Empty<InputActionReference>();
    
    [Header("Button Filtering")]
    [SerializeField] private bool enableButtonFiltering;
    [SerializeField] private string[] excludedButtonNames = Array.Empty<string>();
    [SerializeField] private string[] forceShowButtonNames = Array.Empty<string>();
    
    [Header("Pressed Effects")]
    [SerializeField] private bool enablePressedEffects = true;
    [SerializeField] private bool enableWhenGamePaused;
    [SerializeField] private Color pressedColor = Color.gray;
    [SerializeField] private Vector3 pressedScale = Vector3.one * 0.9f;
    
    [Header("Reference")]
    [SerializeField] private TextMeshProUGUI prompt;
    [SerializeField] private InputManager inputManager;

    private Color _originalColor;
    private Vector3 _originalScale;
    private bool _isPressed;

    private void Awake()
    {
        if (!inputManager) inputManager = FindFirstObjectByType<InputManager>();
    }

    private void Start()
    {
        if (prompt)
        {
            _originalColor = prompt.color;
            _originalScale = prompt.transform.localScale;
        }
        UpdateDisplay();
    }

    private void OnEnable()
    {
        if (inputManager)
        {
            inputManager.OnControlsChangedEvent += OnInputChanged;
        }

        if (enablePressedEffects)
        {
            foreach (var actionReference in inputActionReferences)
            {
                if (actionReference?.action != null)
                {
                    actionReference.action.started += OnActionStarted;
                    actionReference.action.canceled += OnActionCanceled;
                }
            }
        }
    }

    private void OnDisable()
    {
        if (inputManager)
        {
            inputManager.OnControlsChangedEvent -= OnInputChanged;
        }

        if (enablePressedEffects)
        {
            foreach (var actionReference in inputActionReferences)
            {
                if (actionReference?.action != null)
                {
                    actionReference.action.started -= OnActionStarted;
                    actionReference.action.canceled -= OnActionCanceled;
                }
            }
        }
    }
    
    private void OnInputChanged(PlayerInput input) => UpdateDisplay();

    private void OnActionStarted(InputAction.CallbackContext context)
    {
        if (LevelManager.Instance && LevelManager.Instance.IsGamePaused && !enableWhenGamePaused) return;
        
        SetFontColor(pressedColor);
        SetScale(pressedScale);
        _isPressed = true;
    }

    private void OnActionCanceled(InputAction.CallbackContext context)
    {
        if (LevelManager.Instance && LevelManager.Instance.IsGamePaused && !enableWhenGamePaused) return;
        
        ResetFontColor();
        ResetScale();
        _isPressed = false;
    }

    [Button("Update Display")]
    public void UpdateDisplay()
    {
        if (!prompt || inputActionReferences == null || inputActionReferences.Length == 0) return;
        
        InputAction[] actions = new InputAction[inputActionReferences.Length];
        for (int i = 0; i < inputActionReferences.Length; i++)
        {
            actions[i] = inputActionReferences[i]?.action;
        }
        
        string actionBinding;
        if (enableButtonFiltering && (excludedButtonNames.Length > 0 || forceShowButtonNames.Length > 0))
        {
            actionBinding = GetFilteredActionBindings(actions, separator, useSprites);
        }
        else
        {
            actionBinding = InputManager.GetActionBindings(actions, separator, useSprites);
        }
        
        prompt.text = $"{prefix}{actionBinding}{suffix}";

        if (enablePressedEffects)
        {
            if (_isPressed)
            {
                SetFontColor(pressedColor);
                SetScale(pressedScale);
            }
            else
            {
                ResetFontColor();
                ResetScale();
            }
        }
    }
    
    private string GetFilteredActionBindings(InputAction[] actions, string separator, bool asSprites)
    {
        if (actions == null || actions.Length == 0) return "";
        
        var filteredBindings = new System.Collections.Generic.List<string>();
        
        foreach (var action in actions)
        {
            if (action == null) continue;
            
            string binding = GetFilteredActionBinding(action, asSprites);
            if (!string.IsNullOrEmpty(binding))
            {
                filteredBindings.Add(binding);
            }
        }
        
        // Add forced buttons that don't exist in the input actions
        if (forceShowButtonNames != null && forceShowButtonNames.Length > 0)
        {
            string currentScheme = InputManager.Instance?.PlayerInput?.currentControlScheme ?? "";
            
            foreach (var forcedButton in forceShowButtonNames)
            {
                if (!string.IsNullOrEmpty(forcedButton))
                {
                    // Check if this forced button matches the current input scheme
                    if (!DoesButtonMatchCurrentScheme(forcedButton, currentScheme))
                    {
                        continue; // Skip this button if it doesn't match current scheme
                    }
                    
                    // Check if this forced button actually exists in any of the actions
                    bool existsInActions = DoesButtonExistInActions(actions, forcedButton);
                    
                    if (!existsInActions)
                    {
                        // Add the forced button as a custom display
                        string customButtonText = GetCustomButtonText(forcedButton, asSprites);
                        if (!string.IsNullOrEmpty(customButtonText))
                        {
                            filteredBindings.Add(customButtonText);
                        }
                    }
                }
            }
        }
        
        return string.Join(separator, filteredBindings);
    }

    private bool DoesButtonMatchCurrentScheme(string buttonName, string currentScheme)
    {
        if (string.IsNullOrEmpty(currentScheme)) return true; // Show all if no scheme
        
        // Convert button name to lowercase for easier comparison
        string lowerButtonName = buttonName.ToLower();
        string lowerScheme = currentScheme.ToLower();
        
        // Check for gamepad-specific buttons
        if (lowerScheme.Contains("gamepad") || lowerScheme.Contains("controller"))
        {
            return lowerButtonName.Contains("gamepad") || 
                   lowerButtonName.Contains("controller") || 
                   lowerButtonName.Contains("stick") ||
                   lowerButtonName.Contains("trigger") ||
                   lowerButtonName.Contains("bumper") ||
                   lowerButtonName.Contains("dpad") ||
                   lowerButtonName.StartsWith("button") ||
                   (!lowerButtonName.Contains("keyboard") && !lowerButtonName.Contains("mouse"));
        }
        
        // Check for keyboard/mouse-specific buttons
        if (lowerScheme.Contains("keyboard") || lowerScheme.Contains("mouse"))
        {
            return lowerButtonName.Contains("keyboard") || 
                   lowerButtonName.Contains("mouse") ||
                   lowerButtonName.Contains("key") ||
                   (!lowerButtonName.Contains("gamepad") && 
                    !lowerButtonName.Contains("controller") && 
                    !lowerButtonName.Contains("stick") &&
                    !lowerButtonName.Contains("trigger"));
        }
        
        return true; // Default to show if scheme doesn't match known patterns
    }
    private bool DoesButtonExistInActions(InputAction[] actions, string buttonName)
    {
        foreach (var action in actions)
        {
            if (action == null) continue;
            
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!binding.isComposite && !binding.isPartOfComposite)
                {
                    string existingButtonName = InputManagerBindingFormatter.RenameInput(binding.effectivePath);
                    // Only exact matches for existence check
                    if (existingButtonName.Equals(buttonName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else if (binding.isPartOfComposite)
                {
                    string existingButtonName = InputManagerBindingFormatter.RenameInput(binding.effectivePath);
                    // Only exact matches for existence check  
                    if (existingButtonName.Equals(buttonName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    
    private string GetCustomButtonText(string buttonName, bool asSprites)
    {
        if (asSprites && InputManager.Instance?.CurrentSpriteAsset)
        {
            // Try to create a sprite tag directly
            return $"<sprite=\"{InputManager.Instance.CurrentSpriteAsset.name}\" name=\"{buttonName}\">";
        }
        else
        {
            // Return the button name as readable text, clean it up a bit
            return buttonName.Replace("_", " ").Replace("Gamepad ", "").Replace("Keyboard ", "");
        }
    }
    
    private string GetFilteredActionBinding(InputAction action, bool asSprites)
    {
        if (!InputManager.Instance?.PlayerInput || action == null) return action?.name ?? "Unknown";
        
        string currentScheme = InputManager.Instance.PlayerInput.currentControlScheme;
        var filteredBindings = new System.Collections.Generic.List<string>();
        
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            
            if (binding.isComposite)
            {
                // Process composite binding parts (e.g., WASD for movement)
                var compositeParts = new System.Collections.Generic.List<string>();
                
                for (int j = i + 1; j < action.bindings.Count && action.bindings[j].isPartOfComposite; j++)
                {
                    var partBinding = action.bindings[j];
                    bool partMatchesScheme = string.IsNullOrEmpty(partBinding.groups) || partBinding.groups.Contains(currentScheme);
                    
                    if (partMatchesScheme && ShouldShowButton(partBinding))
                    {
                        string bindingText = asSprites ? InputManagerBindingFormatter.GetSpriteTag(partBinding, InputManager.Instance.CurrentSpriteAsset): InputManagerBindingFormatter.ConvertPathToReadableText(partBinding.effectivePath);
                        compositeParts.Add(bindingText);
                    }
                }
                
                if (compositeParts.Count > 0)
                {
                    filteredBindings.AddRange(compositeParts);
                }
                
                // Skip past all part bindings
                while (i + 1 < action.bindings.Count && action.bindings[i + 1].isPartOfComposite)
                {
                    i++;
                }
            }
            else if (!binding.isPartOfComposite)
            {
                // Process single binding
                bool matchesScheme = string.IsNullOrEmpty(binding.groups) || binding.groups.Contains(currentScheme);
                
                if (matchesScheme && ShouldShowButton(binding))
                {
                    string bindingText = asSprites ? InputManagerBindingFormatter.GetSpriteTag(binding, InputManager.Instance.CurrentSpriteAsset) : InputManagerBindingFormatter.ConvertPathToReadableText(binding.effectivePath);
                    filteredBindings.Add(bindingText);
                }
            }
        }
        
        return filteredBindings.Count > 0 ? string.Join(separator, filteredBindings) : "";
    }
    
    private bool ShouldShowButton(InputBinding binding)
    {
        string buttonName = InputManagerBindingFormatter.RenameInput(binding.effectivePath);
        
        if (forceShowButtonNames != null && forceShowButtonNames.Length > 0)
        {
            if (excludedButtonNames is { Length: > 0 })
            {
                bool shouldExclude = excludedButtonNames.Any(excludedName => 
                    buttonName.Equals(excludedName, StringComparison.OrdinalIgnoreCase) ||
                    buttonName.StartsWith(excludedName + "/", StringComparison.OrdinalIgnoreCase));
                    
                if (shouldExclude)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        if (excludedButtonNames is { Length: > 0 })
        {
            return !excludedButtonNames.Any(excludedName => 
                buttonName.Equals(excludedName, StringComparison.OrdinalIgnoreCase) ||
                buttonName.StartsWith(excludedName + "/", StringComparison.OrdinalIgnoreCase));
        }
        
        return true;
    }

    
    private void SetFontColor(Color color)
    {
        if (prompt)
        {
            prompt.color = color;
        }
    }

    private void ResetFontColor()
    {
        if (prompt)
        {
            prompt.color = _originalColor;
        }
    }

    private void SetScale(Vector3 scale)
    {
        if (prompt)
        {
            prompt.transform.localScale = scale;
        }
    }

    private void ResetScale()
    {
        if (prompt)
        {
            prompt.transform.localScale = _originalScale;
        }
    }
    
    
        
    [Button("Debug Button Names")]
    private void DebugButtonNames()
    {
        if (inputActionReferences == null || inputActionReferences.Length == 0) return;
        
        foreach (var actionRef in inputActionReferences)
        {
            if (actionRef?.action == null) continue;
            
            Debug.Log($"Action: {actionRef.action.name}");
            
            for (int i = 0; i < actionRef.action.bindings.Count; i++)
            {
                var binding = actionRef.action.bindings[i];
                string buttonName = InputManagerBindingFormatter.RenameInput(binding.effectivePath);
                Debug.Log($"  Binding {i}: Path='{binding.effectivePath}' -> ButtonName='{buttonName}'");
            }
        }
        
        if (forceShowButtonNames != null)
        {
            for (int i = 0; i < forceShowButtonNames.Length; i++)
            {
                Debug.Log($"  [{i}]: '{forceShowButtonNames[i]}'");
            }
        }
    }
}