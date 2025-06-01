using UnityEngine;
using UnityEngine.InputSystem;

public class RailPlayerInput : MonoBehaviour
{
    [Header("Input Settings")] 
    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private float inputSmoothing = 0.1f;
    [SerializeField] private KeyCode useBaseWeaponKey = KeyCode.Space;
    [SerializeField] private KeyCode useSpecialWeaponKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode dodgeLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode dodgeRightKey = KeyCode.E;

    private Vector3 _lastMousePosition;

    public Vector2 MovementInput { get; private set; }

    public Vector2 RawMovementInput { get; private set; }

    public Vector2 AimInput { get; private set; }

    public Vector2 RawAimInput { get; private set; }

    public Vector3 MousePosition { get; private set; }

    public Vector2 MouseDelta { get; private set; }
    public bool UseBaseWeaponInput { get; private set; }
    public bool UseSpecialWeaponInput { get; private set; }
    public bool DodgeLeftInput { get; private set; }
    public bool DodgeRightInput { get; private set; }
    private void Start()
    {
        MousePosition = Input.mousePosition;
        _lastMousePosition = MousePosition;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleAimInput();
        HandleWeaponInput();
    }




    #region Input Handling --------------------------------------------------------------------------------------

    private void HandleMovementInput()
    {
        Vector2 input = Vector2.zero;

        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        RawMovementInput = Vector2.ClampMagnitude(input, 1f);
        MovementInput = Vector2.Lerp(MovementInput, RawMovementInput, inputSmoothing > 0 ? Time.deltaTime / inputSmoothing : 1f);
        DodgeLeftInput = Input.GetKeyDown(dodgeLeftKey);
        DodgeRightInput = Input.GetKeyDown(dodgeRightKey);
    }

    private void HandleAimInput()
    {
        _lastMousePosition = MousePosition;
        MousePosition = Input.mousePosition;
        MouseDelta = (MousePosition - _lastMousePosition) * mouseSensitivity;
        RawAimInput = MouseDelta;
        AimInput = Vector2.Lerp(AimInput, RawAimInput, inputSmoothing > 0 ? Time.deltaTime / inputSmoothing : 1f);
    }
    
    private void HandleWeaponInput()
    {
        UseBaseWeaponInput = Input.GetKeyDown(useBaseWeaponKey) || Input.GetKey(useBaseWeaponKey) || Input.GetMouseButton(0) || Input.GetMouseButtonDown(0);
        UseSpecialWeaponInput = Input.GetKeyDown(useSpecialWeaponKey) || Input.GetKey(useSpecialWeaponKey) || Input.GetMouseButton(1) || Input.GetMouseButtonDown(1);
    }

    #endregion Input Handling --------------------------------------------------------------------------------------
    
    




}