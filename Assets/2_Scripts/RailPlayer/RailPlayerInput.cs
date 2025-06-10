using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

[RequireComponent(typeof(PlayerInput))]
public class RailPlayerInput : MonoBehaviour
{
    [Header("Input Settings")] 
    [SerializeField] private bool autoHideCursor = true;
    [SerializeField] private bool doubleTapToDodge = true;
    [SerializeField, ShowIf("doubleTapToDodge")] private float doubleTapTime = 0.3f;[EndIf]
    [SerializeField] private bool allowFreeformDodge = true;
    
    [Header("References")]
    [SerializeField, Self] private PlayerInput playerInput;

    
    
    private InputActionMap _playerActionMap;
    private InputActionMap _uiActionMap;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _attackAction;
    private InputAction _attack2Action;
    private InputAction _dodgeLeftAction;
    private InputAction _dodgeRightAction;
    private InputAction _dodgeFreeformAction;
    private float _lastMoveLeftTime;
    private float _lastMoveRightTime;
    
    
    public event Action<InputAction.CallbackContext> OnMoveEvent;
    public event Action<InputAction.CallbackContext> OnLookEvent;
    public event Action<InputAction.CallbackContext> OnAttackEvent;
    public event Action<InputAction.CallbackContext> OnAttack2Event;
    public event Action<InputAction.CallbackContext> OnDodgeLeftEvent;
    public event Action<InputAction.CallbackContext> OnDodgeRightEvent;
    public event Action<InputAction.CallbackContext> OnDodgeFreeformEvent;
    public Vector2 MousePosition => Mouse.current.position.ReadValue();


    
    
    
    private void OnValidate() { this.ValidateRefs(); }

    private void Awake()
    {
        
        _playerActionMap = playerInput.actions.FindActionMap("Player");
        _uiActionMap = playerInput.actions.FindActionMap("UI");
        
        if (_playerActionMap == null || _uiActionMap == null)
        {
            Debug.LogError("Player or UI Action Map not found. Please check the action maps in the Player Input component.");
            return;
        }
        
        _moveAction = _playerActionMap.FindAction("Move");
        _lookAction = _playerActionMap.FindAction("Look");
        _attackAction = _playerActionMap.FindAction("Attack");
        _attack2Action = _playerActionMap.FindAction("Attack2");
        _dodgeLeftAction = _playerActionMap.FindAction("DodgeLeft");
        _dodgeRightAction = _playerActionMap.FindAction("DodgeRight");
        _dodgeFreeformAction = _playerActionMap.FindAction("DodgeFreeform");
        
        
        if (autoHideCursor)
        {
            ToggleCursorVisibility();
        }
        
    }

    private void OnEnable()
    {
        SubscribeToAction(_moveAction, OnMove);
        SubscribeToAction(_lookAction, OnLook);
        SubscribeToAction(_attackAction, OnAttack);
        SubscribeToAction(_attack2Action, OnAttack2);
        SubscribeToAction(_dodgeLeftAction, OnDodgeLeft);
        SubscribeToAction(_dodgeRightAction, OnDodgeRight);
        SubscribeToAction(_dodgeFreeformAction, OnDodgeFreeform);
    }
    
    private void OnDisable()
    {
        UnsubscribeFromAction(_moveAction, OnMove);
        UnsubscribeFromAction(_lookAction, OnLook);
        UnsubscribeFromAction(_attackAction, OnAttack);
        UnsubscribeFromAction(_attack2Action, OnAttack2);
        UnsubscribeFromAction(_dodgeLeftAction, OnDodgeLeft);
        UnsubscribeFromAction(_dodgeRightAction, OnDodgeRight);
        UnsubscribeFromAction(_dodgeFreeformAction, OnDodgeFreeform);
    }

    


    #region Input Events --------------------------------------------------------------------------------------
    
    
    public void OnMove(InputAction.CallbackContext context)
    {
        OnMoveEvent?.Invoke(context);
        
        // Double-tap dodge logic
        if (doubleTapToDodge && context.started)
        {
            if (context.ReadValue<Vector2>().x < 0)   // Left movement 
            {
                if (Time.time - _lastMoveLeftTime < doubleTapTime)
                {
                    OnDodgeLeftEvent?.Invoke(context);
                }
                _lastMoveLeftTime = Time.time;
            }
            else if (context.ReadValue<Vector2>().x > 0)   // Right movement 
            {
                if (Time.time - _lastMoveRightTime < doubleTapTime)
                {
                    OnDodgeRightEvent?.Invoke(context);
                }
                _lastMoveRightTime = Time.time;
            }
        }
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        OnLookEvent?.Invoke(context);
    }
    
    public void OnAttack(InputAction.CallbackContext context)
    {
        OnAttackEvent?.Invoke(context);
    }
    
    public void OnAttack2(InputAction.CallbackContext context)
    {
        OnAttack2Event?.Invoke(context);
    }
    
    public void OnDodgeLeft(InputAction.CallbackContext context)
    {
        OnDodgeLeftEvent?.Invoke(context);
    }
    
    public void OnDodgeRight(InputAction.CallbackContext context)
    {
        OnDodgeRightEvent?.Invoke(context);
    }
    
    public void OnDodgeFreeform(InputAction.CallbackContext context)
    {
        if (!allowFreeformDodge) return;
        OnDodgeFreeformEvent?.Invoke(context);
    }
    

    #endregion Input Events --------------------------------------------------------------------------------------


    #region Cursor --------------------------------------------------------------------------------------

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

    #endregion Cursor --------------------------------------------------------------------------------------


    #region Helpers --------------------------------------------------------------------------------------

    private void SubscribeToAction(InputAction action, Action<InputAction.CallbackContext> callback)
    {
        if (action == null) return;
        
        action.performed += callback;
        action.started += callback;
        action.canceled += callback;
    }
    
    private void UnsubscribeFromAction(InputAction action, Action<InputAction.CallbackContext> callback)
    {
        if (action == null) return;
        
        action.performed -= callback;
        action.started -= callback;
        action.canceled -= callback;
    }

    #endregion Helpers --------------------------------------------------------------------------------------

}