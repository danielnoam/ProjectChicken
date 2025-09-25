



using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CinematicInput : InputReaderBase
{
    private InputActionMap _cinematicActionMap;
    private InputAction _skipAction;

    
    public event Action<InputAction.CallbackContext> OnSkipActionEvent;
    
    protected override void Awake()
    {
        base.Awake();
        
        _cinematicActionMap = PlayerInput.actions.FindActionMap("Cinematic");
        
        
        if (_cinematicActionMap == null)
        {
            Debug.LogError("Cinematic Map not found. Please check the action maps in the Player Input component.");
            return;
        }
        

        _skipAction = _cinematicActionMap.FindAction("Skip");
        
        if (_skipAction == null)
        {
            Debug.LogError("Skip Action not found. Please check the action maps in the Player Input component.");
            return;
        }
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        _cinematicActionMap.Enable();

        SubscribeToAction(_skipAction, OnSkipAction);
        
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        _cinematicActionMap.Disable();
        
        UnsubscribeFromAction(_skipAction, OnSkipAction);

    }

    private void OnSkipAction(InputAction.CallbackContext callbackContext)
    {
        OnSkipActionEvent?.Invoke(callbackContext);
    }
}