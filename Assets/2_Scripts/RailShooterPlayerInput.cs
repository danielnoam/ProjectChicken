using UnityEngine;

public class RailShooterPlayerInput : MonoBehaviour
{
    [Header("Input Settings")] 
    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private float inputSmoothing = 0.1f;




    private Vector3 mousePosition;
    private Vector3 lastMousePosition;
    private Vector2 mouseDelta;

    private Vector2 rawMovementInput;
    private Vector2 smoothedMovementInput;
    private Vector2 rawAimInput;
    private Vector2 smoothedAimInput;
    
    public Vector2 MovementInput => smoothedMovementInput;
    public Vector2 RawMovementInput => rawMovementInput;
    public Vector2 AimInput => smoothedAimInput;
    public Vector2 RawAimInput => rawAimInput;
    public Vector3 MousePosition => mousePosition;
    public Vector2 MouseDelta => mouseDelta;

    private void Start()
    {
        mousePosition = Input.mousePosition;
        lastMousePosition = mousePosition;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleAimInput();
    }




    #region Input Handling --------------------------------------------------------------------------------------

    private void HandleMovementInput()
    {
        Vector2 input = Vector2.zero;

        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        rawMovementInput = Vector2.ClampMagnitude(input, 1f);
        smoothedMovementInput = Vector2.Lerp(smoothedMovementInput, rawMovementInput, inputSmoothing > 0 ? Time.deltaTime / inputSmoothing : 1f);
    }

    private void HandleAimInput()
    {

        lastMousePosition = mousePosition;
        mousePosition = Input.mousePosition;
        mouseDelta = (mousePosition - lastMousePosition) * mouseSensitivity;
        rawAimInput = mouseDelta;
        smoothedAimInput = Vector2.Lerp(smoothedAimInput, rawAimInput, inputSmoothing > 0 ? Time.deltaTime / inputSmoothing : 1f);
    }

    #endregion Input Handling --------------------------------------------------------------------------------------
    
    
    
    #region Public Methods -------------------------------------------------------------------------

    public Vector3 GetMouseWorldPosition(Camera camera, float distance = 10f)
    {
        Vector3 mousePos = mousePosition;
        mousePos.z = distance;
        return camera.ScreenToWorldPoint(mousePos);
    }
    
    
    #endregion Public Methods -------------------------------------------------------------------------




}