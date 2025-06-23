using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerInput))]
public class InputReaderBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Self] protected PlayerInput playerInput;
    
    
    private void OnValidate() { this.ValidateRefs(); }
    
    
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