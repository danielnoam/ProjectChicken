using System;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using VInspector;

public class ReplaceTextWithBinding : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private bool autoSetOnStart = true;
    [SerializeField] private bool useSpriteMode = true;
    
    [Header("Reference")]
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private InputManager inputManager;
    
    private string _originalText;

    private void Awake()
    {

        if (!inputManager) inputManager = FindFirstObjectByType<InputManager>();
        
        if (textComponent)
        {
            _originalText = textComponent.text;
        }
        
    }
    
    private void OnEnable()
    {
        if (inputManager)
        {
            inputManager.OnControlsChangedEvent += OnInputChanged;
        }
    }

    private void OnDisable()
    {
        if (inputManager)
        {
            inputManager.OnControlsChangedEvent -= OnInputChanged;
        }

    }

    private void OnInputChanged(PlayerInput input) => UpdateBindingText();
    

    private void Start()
    {
        if (autoSetOnStart)
        {
            UpdateBindingText();
        }
    }

    [Button]
    public void UpdateBindingText()
    {
        if (!textComponent) return;

        string processedText = useSpriteMode 
            ? InputManager.ReplaceActionTokenInText(_originalText)
            : InputManager.ReplaceActionTokenInText(_originalText, false);
            
        textComponent.text = processedText;
    }
}