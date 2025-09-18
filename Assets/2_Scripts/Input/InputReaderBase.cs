using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReaderBase : MonoBehaviour
{
    [SerializeField, Scene(Flag.EditableAnywhere)] protected InputManager inputManager;
    
    public ControlSchemeSettings CurrentControlScheme => inputManager?.CurrentControlScheme;
    public bool IsCurrentDeviceGamepad => inputManager?.IsCurrentDeviceGamepad ?? false;
    protected PlayerInput PlayerInput => inputManager?.PlayerInput;


    protected virtual void OnValidate()
    {
        if (!inputManager) inputManager = FindFirstObjectByType<InputManager>();
        this.ValidateRefs();
    }

    protected virtual void Awake()
    {
        if (!PlayerInput)
        {
            Debug.LogError("PlayerInput not found in InputManager. Please check InputManager setup.");
            return;
        }
    }

    protected virtual void OnEnable()
    {
        
    }

    protected virtual void OnDisable()
    {
        
    }

    protected void SubscribeToAction(InputAction action, Action<InputAction.CallbackContext> callback)
    {
        if (action == null) return;
        
        action.performed += callback;
        action.started += callback;
        action.canceled += callback;
    }
    
    protected void UnsubscribeFromAction(InputAction action, Action<InputAction.CallbackContext> callback)
    {
        if (action == null) return;
        
        action.performed -= callback;
        action.started -= callback;
        action.canceled -= callback;
    }
}