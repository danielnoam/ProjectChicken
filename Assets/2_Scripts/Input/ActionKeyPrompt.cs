using System;
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
    [SerializeField] private InputActionReference[] inputActionReferences = Array.Empty<InputActionReference>();
    
    [Header("Reference")]
    [SerializeField] private TextMeshProUGUI prompt;
    [SerializeField] private InputManager inputManager;

    private void Awake()
    {
        if (!inputManager) inputManager = FindFirstObjectByType<InputManager>();
    }

    private void Start()
    {
        UpdateDisplay();
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

    private void OnInputChanged(PlayerInput input) => UpdateDisplay();

    [Button("Update Display")]
    public void UpdateDisplay()
    {
        if (!prompt || inputActionReferences == null || inputActionReferences.Length == 0) return;
        
        InputAction[] actions = new InputAction[inputActionReferences.Length];
        for (int i = 0; i < inputActionReferences.Length; i++)
        {
            actions[i] = inputActionReferences[i]?.action;
        }
        
        prompt.text = InputManager.GetActionBindings(actions, separator, useSprites);
    }

}