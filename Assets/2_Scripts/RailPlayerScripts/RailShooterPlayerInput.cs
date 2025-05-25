using UnityEngine;

public class RailShooterPlayerInput : MonoBehaviour
{
    [Header("Input Settings")] 
    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private float inputSmoothing = 0.1f;
    [SerializeField] private KeyCode useWeaponKey = KeyCode.Space;
    [SerializeField] private KeyCode nextWeaponKey = KeyCode.E;
    [SerializeField] private KeyCode previousWeaponKey = KeyCode.Q;

    private Vector3 lastMousePosition;

    public Vector2 MovementInput { get; private set; }

    public Vector2 RawMovementInput { get; private set; }

    public Vector2 AimInput { get; private set; }

    public Vector2 RawAimInput { get; private set; }

    public Vector3 MousePosition { get; private set; }

    public Vector2 MouseDelta { get; private set; }
    public bool UseWeaponInput { get; private set; }
    public bool NextWeaponInput { get; private set; }
    public bool PreviousWeaponInput { get; private set; }
    private void Start()
    {
        MousePosition = Input.mousePosition;
        lastMousePosition = MousePosition;
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
    }

    private void HandleAimInput()
    {

        lastMousePosition = MousePosition;
        MousePosition = Input.mousePosition;
        MouseDelta = (MousePosition - lastMousePosition) * mouseSensitivity;
        RawAimInput = MouseDelta;
        AimInput = Vector2.Lerp(AimInput, RawAimInput, inputSmoothing > 0 ? Time.deltaTime / inputSmoothing : 1f);
    }
    
    private void HandleWeaponInput()
    {
        UseWeaponInput = Input.GetKeyDown(useWeaponKey);
        NextWeaponInput = Input.GetKeyDown(nextWeaponKey);
        PreviousWeaponInput = Input.GetKeyDown(previousWeaponKey);
    }

    #endregion Input Handling --------------------------------------------------------------------------------------
    
    




}