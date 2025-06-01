using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class RailPlayerNewInput : MonoBehaviour
{
    [Header("Input Settings")] 
    [SerializeField, Min(0.01f)] private float pointerSensitivity = 1f;
    
    [Header("References")]
    [SerializeField, Self] private PlayerInput playerInput;

    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _attackAction;
    private InputAction _dodgeLeftAction;
    private InputAction _dodgeRightAction;
    
    public event Action<Vector2> OnMoveEvent;
    public event Action<Vector2> OnLookEvent;
    public event Action OnAttackEvent;
    public event Action OnDodgeLeftEvent;
    public event Action OnDodgeRightEvent;
    
    public Vector2 MovementInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public Vector3 LastPointerPosition { get; private set; }
    public Vector3 PointerPosition { get; private set; }


    
    
    
    private void OnValidate() { this.ValidateRefs(); }

    private void Awake()
    {
        PointerPosition = Input.mousePosition;
        LastPointerPosition = PointerPosition;
        
        _moveAction = playerInput.actions["Move"];
        _lookAction = playerInput.actions["Look"];
        _attackAction = playerInput.actions["Attack"];
        _dodgeLeftAction = playerInput.actions["DodgeLeft"];
        _dodgeRightAction = playerInput.actions["DodgeRight"];
    }

    private void OnEnable()
    {
        _moveAction.performed += OnMovePerformed;
        _lookAction.performed += OnLookPerformed;
        _attackAction.performed += OnAttackPerformed;
        _dodgeLeftAction.performed += OnDodgeLeftPerformed;
        _dodgeRightAction.performed += OnDodgeRightPerformed;
    }
    
    private void OnDisable()
    {
        _moveAction.performed -= OnMovePerformed;
        _lookAction.performed -= OnLookPerformed;
        _attackAction.performed -= OnAttackPerformed;
        _dodgeLeftAction.performed -= OnDodgeLeftPerformed;
        _dodgeRightAction.performed -= OnDodgeRightPerformed;
    }

    private void Update()
    {
        HandlePointerInput();
    }




    #region Input Handling --------------------------------------------------------------------------------------
    

    private void HandlePointerInput()
    {
        LastPointerPosition = PointerPosition;
        PointerPosition = Input.mousePosition;
    }
    
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        MovementInput = context.ReadValue<Vector2>();
        OnMoveEvent?.Invoke(MovementInput);
    }
    
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>() * pointerSensitivity;
        OnLookEvent?.Invoke(LookInput);
    }
    
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        OnAttackEvent?.Invoke();
    }
    
    private void OnDodgeLeftPerformed(InputAction.CallbackContext context)
    {
        OnDodgeLeftEvent?.Invoke();
    }
    
    private void OnDodgeRightPerformed(InputAction.CallbackContext context)
    {
        OnDodgeRightEvent?.Invoke();
    }
    

    #endregion Input Handling --------------------------------------------------------------------------------------
    
    




}