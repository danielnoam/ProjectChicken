using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

[RequireComponent(typeof(PlayerInput))]
public class RailPlayerInput : MonoBehaviour
{
    [Header("Input Settings")] 
    [SerializeField, Self] private PlayerInput playerInput;
    [SerializeField] private bool autoHideCursor = true;
    

    private InputActionMap _playerActionMap;
    private InputActionMap _uiActionMap;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _attackAction;
    private InputAction _dodgeLeftAction;
    private InputAction _dodgeRightAction;
    
    public event Action<InputAction.CallbackContext> OnMoveEvent;
    public event Action<InputAction.CallbackContext> OnLookEvent;
    public event Action<InputAction.CallbackContext> OnAttackEvent;
    public event Action<InputAction.CallbackContext> OnDodgeLeftEvent;
    public event Action<InputAction.CallbackContext> OnDodgeRightEvent;
    public Vector3 PointerPosition => Input.mousePosition;


    
    
    
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
        _dodgeLeftAction = _playerActionMap.FindAction("DodgeLeft");
        _dodgeRightAction = _playerActionMap.FindAction("DodgeRight");
        
        if (_moveAction == null || _lookAction == null || _attackAction == null || _dodgeLeftAction == null || _dodgeRightAction == null)
        {
            Debug.LogError("One or more actions are not found in the Player Action Map. Please check the action names.");
        }
        
        if (autoHideCursor)
        {
            ToggleCursorVisibility();
        }
        
    }

    private void OnEnable()
    {
        _moveAction.performed += OnMove;
        _moveAction.started += OnMove;
        _moveAction.canceled += OnMove;
        _lookAction.started += OnLook;
        _lookAction.performed += OnLook;
        _lookAction.canceled += OnLook;
        _attackAction.started += OnAttack;
        _attackAction.performed += OnAttack;
        _attackAction.canceled += OnAttack;
        _dodgeLeftAction.started += OnDodgeLeft;
        _dodgeLeftAction.performed += OnDodgeLeft;
        _dodgeLeftAction.canceled += OnDodgeLeft;
        _dodgeRightAction.started += OnDodgeRight;
        _dodgeRightAction.performed += OnDodgeRight;
        _dodgeRightAction.canceled += OnDodgeRight;
    }
    
    private void OnDisable()
    {
        _moveAction.performed -= OnMove;
        _moveAction.started -= OnMove;
        _moveAction.canceled -= OnMove;
        _lookAction.started -= OnLook;
        _lookAction.performed -= OnLook;
        _lookAction.canceled -= OnLook;
        _attackAction.started -= OnAttack;
        _attackAction.performed -= OnAttack;
        _attackAction.canceled -= OnAttack;
        _dodgeLeftAction.started -= OnDodgeLeft;
        _dodgeLeftAction.performed -= OnDodgeLeft;
        _dodgeLeftAction.canceled -= OnDodgeLeft;
        _dodgeRightAction.started -= OnDodgeRight;
        _dodgeRightAction.performed -= OnDodgeRight;
        _dodgeRightAction.canceled -= OnDodgeRight;
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


    #region Input Handling --------------------------------------------------------------------------------------
    
    
    public void OnMove(InputAction.CallbackContext context)
    {
        OnMoveEvent?.Invoke(context);
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        OnLookEvent?.Invoke(context);
    }
    
    public void OnAttack(InputAction.CallbackContext context)
    {
        OnAttackEvent?.Invoke(context);
    }
    
    public void OnDodgeLeft(InputAction.CallbackContext context)
    {
        OnDodgeLeftEvent?.Invoke(context);
    }
    
    public void OnDodgeRight(InputAction.CallbackContext context)
    {
        OnDodgeRightEvent?.Invoke(context);
    }
    

    #endregion Input Handling --------------------------------------------------------------------------------------
    
    




}