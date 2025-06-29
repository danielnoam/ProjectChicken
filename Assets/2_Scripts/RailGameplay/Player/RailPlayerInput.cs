using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;


public class RailPlayerInput : InputReaderBase
{

    [Header("Control Settings")]
    [SerializeField] private ControlSchemeSettings keyboardMouseScheme = new ControlSchemeSettings(false, false, 0.1f, 0.3f, AnimationCurve.Linear(0, 0, 1, 1),true, 4f, 3f, 3f, 0.3f, false, true, 0.3f);
    [SerializeField] private ControlSchemeSettings gamepadScheme = new ControlSchemeSettings(false, false, 2f, 0.3f, AnimationCurve.Linear(0, 0, 1, 1),true, 4f, 3f, 0.5f, 0.5f, true, false, 0.3f);

    private InputActionMap _playerActionMap;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _attackAction;
    private InputAction _attack2Action;
    private InputAction _dodgeLeftAction;
    private InputAction _dodgeRightAction;
    private InputAction _dodgeFreeformAction;
    private float _lastMoveLeftTime;
    private float _lastMoveRightTime;



    public bool IsCurrentDeviceGamepad { get; private set; } = false;
    public ControlSchemeSettings CurrentControlScheme { get; private set; } = new ControlSchemeSettings();
    public event Action<InputAction.CallbackContext> OnMoveEvent;
    public event Action<InputAction.CallbackContext> OnLookEvent;
    public event Action<InputAction.CallbackContext> OnAttackEvent;
    public event Action<InputAction.CallbackContext> OnAttack2Event;
    public event Action<InputAction.CallbackContext> OnDodgeLeftEvent;
    public event Action<InputAction.CallbackContext> OnDodgeRightEvent;
    public event Action<InputAction.CallbackContext> OnDodgeFreeformEvent;
    public event Action<Vector2> OnProcessedLookEvent;

    

    
    
    protected override void Awake()
    {
        base.Awake();

        _playerActionMap = playerInput.actions.FindActionMap("Player");
        
        if (_playerActionMap == null)
        {
            Debug.LogError("Player Map not found. Please check the action maps in the Player Input component.");
            return;
        }
        
        _moveAction = _playerActionMap.FindAction("Move");
        _lookAction = _playerActionMap.FindAction("Look");
        _attackAction = _playerActionMap.FindAction("Attack");
        _attack2Action = _playerActionMap.FindAction("Attack2");
        _dodgeLeftAction = _playerActionMap.FindAction("DodgeLeft");
        _dodgeRightAction = _playerActionMap.FindAction("DodgeRight");
        _dodgeFreeformAction = _playerActionMap.FindAction("DodgeFreeform");
        

        CurrentControlScheme.SetControlSchemeSettings(keyboardMouseScheme);
        IsCurrentDeviceGamepad = false;
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
        playerInput.onDeviceRegained += OnDeviceRegained;
        playerInput.onDeviceLost += OnDeviceLost;
        playerInput.onControlsChanged += OnControlsChanged;
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
        playerInput.onDeviceRegained -= OnDeviceRegained;
        playerInput.onDeviceLost -= OnDeviceLost;
        playerInput.onControlsChanged -= OnControlsChanged;
    }
    
    


    #region Control Scheme --------------------------------------------------------------------------
    
    private void OnDeviceRegained(PlayerInput input)
    {
        UpdateControlScheme(input);
    }

    private void OnDeviceLost(PlayerInput input)
    {
        UpdateControlScheme(input);
    }

    private void OnControlsChanged(PlayerInput input)
    {
        UpdateControlScheme(input);
    }
    
    
    private void UpdateControlScheme(PlayerInput input)
    {
        string currentScheme = input.currentControlScheme;
        Debug.Log($"Control scheme changed to: {currentScheme}");
        
        
        switch (currentScheme)
        {
            case "Keyboard&Mouse":
                CurrentControlScheme = keyboardMouseScheme;
                IsCurrentDeviceGamepad = false;
                break;
            case "Gamepad":
                CurrentControlScheme = gamepadScheme;
                IsCurrentDeviceGamepad = true;
                break;
        }
    }
    

    #endregion Control Scheme  --------------------------------------------------------------------------
    
    

    #region Input Events --------------------------------------------------------------------------------------
    
    
    public void OnMove(InputAction.CallbackContext context)
    {
        OnMoveEvent?.Invoke(context);
        
        // Double-tap dodge logic
        if (CurrentControlScheme.doubleTapToDodge && context.started)
        {
            if (context.ReadValue<Vector2>().x < 0)   // Left movement 
            {
                if (Time.time - _lastMoveLeftTime < CurrentControlScheme.doubleTapTime)
                {
                    OnDodgeLeftEvent?.Invoke(context);
                }
                _lastMoveLeftTime = Time.time;
            }
            else if (context.ReadValue<Vector2>().x > 0)   // Right movement 
            {
                if (Time.time - _lastMoveRightTime < CurrentControlScheme.doubleTapTime)
                {
                    OnDodgeRightEvent?.Invoke(context);
                }
                _lastMoveRightTime = Time.time;
            }
        }
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 lookDelta = context.ReadValue<Vector2>();
    
        
        Vector2 processedLookDelta = new Vector2(
            CurrentControlScheme.invertX ? -lookDelta.x : lookDelta.x,
            CurrentControlScheme.invertY ? -lookDelta.y : lookDelta.y
        );
        
        OnLookEvent?.Invoke(context);                    
        OnProcessedLookEvent?.Invoke(processedLookDelta); 
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
        if (!CurrentControlScheme.allowFreeformDodge) return;
        OnDodgeFreeformEvent?.Invoke(context);
    }
    

    #endregion Input Events --------------------------------------------------------------------------------------
    
    

}