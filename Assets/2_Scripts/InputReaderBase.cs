using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;


[RequireComponent(typeof(PlayerInput))]
public class InputReaderBase : MonoBehaviour
{
    [Header("Cursor Settings")] 
    [SerializeField] private bool hideCursor = true;
    
    [SerializeField, Self, HideInInspector] protected PlayerInput playerInput;
    
    
    private void OnValidate() { this.ValidateRefs(); }

    protected virtual void Awake()
    {
        SetCursorVisibility(hideCursor);
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
    
    
    
    private void SetCursorVisibility(bool state)
    {
        if (state)
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
    
    [Button]
    private void ToggleCursorVisibility()
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
    


}