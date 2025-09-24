using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;

public class RailPlayerInput : InputReaderBase
{
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    
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
    private bool _isPaused;

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

        _playerActionMap = PlayerInput.actions.FindActionMap("Player");
        
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
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        
        SubscribeToAction(_moveAction, OnMove);
        SubscribeToAction(_lookAction, OnLook);
        SubscribeToAction(_attackAction, OnAttack);
        SubscribeToAction(_attack2Action, OnAttack2);
        SubscribeToAction(_dodgeLeftAction, OnDodgeLeft);
        SubscribeToAction(_dodgeRightAction, OnDodgeRight);
        SubscribeToAction(_dodgeFreeformAction, OnDodgeFreeform);


        if (player.LevelManager) player.LevelManager.OnPause += OnPause;
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        
        UnsubscribeFromAction(_moveAction, OnMove);
        UnsubscribeFromAction(_lookAction, OnLook);
        UnsubscribeFromAction(_attackAction, OnAttack);
        UnsubscribeFromAction(_attack2Action, OnAttack2);
        UnsubscribeFromAction(_dodgeLeftAction, OnDodgeLeft);
        UnsubscribeFromAction(_dodgeRightAction, OnDodgeRight);
        UnsubscribeFromAction(_dodgeFreeformAction, OnDodgeFreeform);
        
        
        if (player.LevelManager) player.LevelManager.OnPause -= OnPause;
        
    }

    private void OnPause(bool paused)
    {
        _isPaused = paused;
    }


    #region Input Events --------------------------------------------------------------------------------------
    
    private void OnMove(InputAction.CallbackContext context)
    {
        if (_isPaused) return;
        
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
    
    private void OnLook(InputAction.CallbackContext context)
    {
        if (_isPaused) return;
        
        
        Vector2 lookDelta = context.ReadValue<Vector2>();
        
        Vector2 processedLookDelta = new Vector2(
            CurrentControlScheme.invertX ? -lookDelta.x : lookDelta.x,
            CurrentControlScheme.invertY ? -lookDelta.y : lookDelta.y
        );
        
        OnLookEvent?.Invoke(context);                    
        OnProcessedLookEvent?.Invoke(processedLookDelta); 
    }
    
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (_isPaused) return;
        
        OnAttackEvent?.Invoke(context);
    }
    
    private void OnAttack2(InputAction.CallbackContext context)
    {
        if (_isPaused) return;
        
        OnAttack2Event?.Invoke(context);
    }
    
    private void OnDodgeLeft(InputAction.CallbackContext context)
    {
        if (_isPaused) return;
        
        OnDodgeLeftEvent?.Invoke(context);
    }
    
    private void OnDodgeRight(InputAction.CallbackContext context)
    {
        if (_isPaused) return;
        
        OnDodgeRightEvent?.Invoke(context);
    }
    
    private void OnDodgeFreeform(InputAction.CallbackContext context)
    {
        if (_isPaused) return;
        
        if (!CurrentControlScheme.allowFreeformDodge) return;
        OnDodgeFreeformEvent?.Invoke(context);
    }
    

    #endregion Input Events --------------------------------------------------------------------------------------
}