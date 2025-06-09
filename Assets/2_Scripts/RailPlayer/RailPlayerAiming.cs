using KBCore.Refs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

public class RailPlayerAiming : MonoBehaviour
{
    
    [Header("Look Offset")]
    [SerializeField] private bool useLookOffset = true;
    [EnableIf("useLookOffset")]
    [SerializeField, Min(0)] private float lookOffsetStrength = 35f;
    [SerializeField, Min(0)] private float lookOffsetSmoothing = 1.4f;
    [SerializeField] private bool autoCenter;
    [SerializeField, Min(0)] private float autoCenterDelay = 5f;
    [SerializeField, Min(0)] private float autoCenterSpeed = 1f;
    [EndIf]
    
    [Header("Movement Offset")]
    [SerializeField] private bool useMovementOffset = true;
    [EnableIf("useMovementOffset")]
    [SerializeField, Min(0)] private float movementOffsetStrength = 11f;
    [SerializeField, Min(0)] private float movementOffsetSmoothing = 5f;
    [EndIf]
    
    [Header("References")]
    [SerializeField, Self] private RailPlayer player;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField, Self] private RailPlayerMovement playerMovement;
    [SerializeField] private Transform crosshair;


    private Vector2 _movementInput;
    private Vector2 _lookInput;
    private Vector3 _aimDirection;
    private Vector3 _crosshairWorldPosition;
    private Vector3 _lookOffset;
    private Vector3 _movementOffset;
    private Quaternion _splineRotation = Quaternion.identity;
    private float _noInputTimer;
    private float CrosshairBoundaryX => LevelManager.Instance ? LevelManager.Instance.EnemyBoundary.x : 25f;
    private float CrosshairBoundaryY => LevelManager.Instance ? LevelManager.Instance.EnemyBoundary.y :  15f;
    
    private void OnValidate() { this.ValidateRefs(); }

    private void Start()
    {
        if (crosshair)
        {
            UpdateAimPosition();
        }
    }

    private void OnEnable()
    {
        playerInput.OnLookEvent += OnLook;
        playerInput.OnMoveEvent += OnMove;
    }
    
    private void OnDisable()
    {
        playerInput.OnLookEvent -= OnLook;
        playerInput.OnMoveEvent -= OnMove;
    }

    private void Update()
    {
        HandleSplineRotation();
        HandleMovementOffset();
        HandleLookOffset();
        UpdateAimPosition();
    }
    
    private void HandleSplineRotation()
    {
        if (!player.AlignToSplineDirection || !LevelManager.Instance || !LevelManager.Instance.SplineContainer)
        {
            _splineRotation = Quaternion.identity;
            return;
        }

        // Get the spline direction at the crosshair position
        Vector3 splineForward = GetSplineDirection();
        
        if (splineForward != Vector3.zero)
        {
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineForward, Vector3.up);
            _splineRotation = Quaternion.Slerp(_splineRotation, targetSplineRotation, player.SplineRotationSpeed * Time.deltaTime);
        }
    }
    
    
    
    
    
    private void UpdateAimPosition()
    {
       Vector3 boundaryCenter = GetCrosshairSplinePosition();

       // Calculate offsets in the local spline space
       Vector3 localLookOffset = _lookOffset;
       Vector3 localMovementOffset = _movementOffset;
       
       if (player.AlignToSplineDirection)
       {
           // Transform offsets to world space using spline rotation
           localLookOffset = _splineRotation * _lookOffset;
           localMovementOffset = _splineRotation * _movementOffset;
       }

       // Calculate target position in world space
       _crosshairWorldPosition = boundaryCenter + localLookOffset + localMovementOffset;

       // Apply boundary clamping
       if (player.AlignToSplineDirection)
       {
           // Transform to local spline space for clamping
           Vector3 localOffset = Quaternion.Inverse(_splineRotation) * (_crosshairWorldPosition - boundaryCenter);
           
           // Clamp in the local spline space
           localOffset.x = Mathf.Clamp(localOffset.x, -CrosshairBoundaryX, CrosshairBoundaryX);
           localOffset.y = Mathf.Clamp(localOffset.y, -CrosshairBoundaryY, CrosshairBoundaryY);
           
           // Transform back to world space
           _crosshairWorldPosition = boundaryCenter + (_splineRotation * localOffset);
       }
       else
       {
           // Traditional world-space clamping
           _crosshairWorldPosition.x = Mathf.Clamp(_crosshairWorldPosition.x, boundaryCenter.x - CrosshairBoundaryX, boundaryCenter.x + CrosshairBoundaryX);
           _crosshairWorldPosition.y = Mathf.Clamp(_crosshairWorldPosition.y, boundaryCenter.y - CrosshairBoundaryY, boundaryCenter.y + CrosshairBoundaryY);
       }
       
       // Set aim direction
       _aimDirection = (_crosshairWorldPosition - transform.position).normalized;

       // Update crosshair position and rotation
       if (crosshair)
       {
           crosshair.position = _crosshairWorldPosition;
           
           // Rotate crosshair to match boundary rotation
           crosshair.rotation = player.AlignToSplineDirection ? _splineRotation : Quaternion.identity;
       }
    }
    

    private void HandleMovementOffset()
    {
        if (!useMovementOffset)
        {
            _movementOffset = Vector3.zero;
            return;
        }
        
        Vector3 targetOffset = new Vector3(
            _movementInput.x * movementOffsetStrength,
            _movementInput.y * movementOffsetStrength,
            0
        );
        
        _movementOffset = Vector3.Lerp(_movementOffset, targetOffset, movementOffsetSmoothing * Time.deltaTime);
    }
    
    private void HandleLookOffset()
    {
        if (!useLookOffset)
        {
            _lookOffset = Vector3.zero;
            return;
        }
    
        // Check if we have any input
        bool hasInput = _lookInput.magnitude > 0.01f;
    
        if (hasInput)
        {
            // Reset the no input timer when we have input
            _noInputTimer = 0f;
        
            // Normalize the input to handle different device sensitivities consistently
            Vector2 normalizedInput = _lookInput.magnitude > 1f ? _lookInput.normalized : _lookInput;
        
            Vector3 targetOffset = new Vector3(
                normalizedInput.x * lookOffsetStrength,
                normalizedInput.y * lookOffsetStrength,
                0
            );
        
            _lookOffset = Vector3.Lerp(_lookOffset, targetOffset, lookOffsetSmoothing * Time.deltaTime);
        }
        else if (autoCenter)
        {
            _noInputTimer += Time.deltaTime;
        
            // Centering after the delay has passed
            if (_noInputTimer >= autoCenterDelay)
            {
                _lookOffset = Vector3.Lerp(_lookOffset, Vector3.zero, autoCenterSpeed * Time.deltaTime);
            }
        }
    }


    
    
    #region Input --------------------------------------------------------------------------------------------------------

    private void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        _movementInput = context.ReadValue<Vector2>();
    }

    #endregion Input --------------------------------------------------------------------------------------------------------



    #region Helper -------------------------------------------------------------------------

    public Vector3 GetAimDirection()
    {
        return _aimDirection;
    }
    
    public Vector3 GetAimPosition()
    {
        return _crosshairWorldPosition;
    }
    
    public bool IsAimingAtTarget(Transform target, float tolerance = 1f)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float dot = Vector3.Dot(_aimDirection, directionToTarget);
        return dot > (1f - tolerance);
    }
    
    
    private Vector3 GetSplineDirection()
    {
        return !LevelManager.Instance ? Vector3.forward : LevelManager.Instance.GetDirectionOnSpline(LevelManager.Instance.EnemyPosition);
    }
    
    private Vector3 GetCrosshairSplinePosition()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.SplineContainer) return transform.position;
        
        return LevelManager.Instance.EnemyPosition;
    }

    #endregion Helper -------------------------------------------------------------------------

    
    
    #if UNITY_EDITOR
    #region Editor -------------------------------------------------------------------------
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
    
        // Aim direction ray
        Gizmos.DrawLine(transform.position, _crosshairWorldPosition);
    
        // Draw boundaries from the crosshair spline position 
        if (crosshair && LevelManager.Instance && LevelManager.Instance.SplineContainer)
        {
            Vector3 crosshairSplinePosition = GetCrosshairSplinePosition();
            
            if (player.AlignToSplineDirection)
            {
                // Draw rotated boundaries based on spline rotation
                Vector3[] localCorners = new Vector3[]
                {
                    new Vector3(-CrosshairBoundaryX, -CrosshairBoundaryY, 0), // Bottom-left
                    new Vector3(CrosshairBoundaryX, -CrosshairBoundaryY, 0),  // Bottom-right
                    new Vector3(CrosshairBoundaryX, CrosshairBoundaryY, 0),   // Top-right
                    new Vector3(-CrosshairBoundaryX, CrosshairBoundaryY, 0)   // Top-left
                };
                
                // Transform corners to world space using spline rotation
                Vector3[] worldCorners = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    worldCorners[i] = crosshairSplinePosition + (_splineRotation * localCorners[i]);
                }
                
                // Draw boundary rectangle
                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;
                    Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
                }
                
                UnityEditor.Handles.Label(crosshairSplinePosition + (_splineRotation * Vector3.up * (CrosshairBoundaryY + 0.5f)), "Crosshair Boundaries");
            }
        }
    }

    #endregion Editor -------------------------------------------------------------------------
    #endif 
}