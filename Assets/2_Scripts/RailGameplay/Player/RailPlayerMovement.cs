
using System;
using KBCore.Refs;
using UnityEngine;
using PrimeTween;
using UnityEngine.InputSystem;
using VInspector;

[RequireComponent(typeof(Rigidbody))]
public class RailPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float maxMoveSpeed = 35f;
    [SerializeField, Min(0.1f)] private float acceleration = 5f;
    [SerializeField, Min(0.1f)] private float deceleration = 3f;
    [SerializeField, Min(0)] private float pathFollowSpeed = 1000f;
    
    [Header("Rotation Settings")]
    [SerializeField, Min(0)] private float rollSpeed = 5f;
    [SerializeField, Min(0)] private float maxRollAmount = 30f;
    [SerializeField, Min(0)] private float pitchYawSpeed = 22f;
    [SerializeField, Min(0)] private float maxPitchAngle = 30f;
    [SerializeField, Min(0)] private float maxYawAngle = 45f;

    
    [Header("Dodge Settings")]
    [SerializeField] private bool enableDodging = true;
    [EnableIf("enableDodging")]
    [SerializeField, Min(0)] private float dodgeMoveSpeed = 20f;
    [SerializeField, Min(0)] private float dodgeTime = 0.2f;
    [SerializeField, Min(0)] private float dodgeCooldown = 1.5f;
    [SerializeField, Min(0)] private float dodgeRollAmount = 720f;
    [SerializeField, Min(0)] private TweenSettings dodgeTweenSettings = new TweenSettings(1.2f, Ease.Custom);
    [EndIf]
    
    [Header("References")] 
    [SerializeField] private SOAudioEvent dodgeSfx;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming playerAiming;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private Rigidbody playerRigidbody;
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;
    [SerializeField] private Transform shipModel;


    private float _horizontalInput;
    private float _verticalInput;
    private Quaternion _velocityRotation = Quaternion.identity;
    private Quaternion _aimRotation = Quaternion.identity;
    private Vector3 _targetOffsetFromSpline = Vector3.zero;
    private Vector3 _currentOffsetFromSpline = Vector3.zero;
    private bool _isDodging;
    private float _dodgeCooldownTimer;
    private float _dodgeTimeCounter;
    private float _currentDodgeRoll;
    private Vector3 _dodgeDirection;
    private Tween _dodgeTween;

    private float MovementBoundaryX => player.LevelManager ? player.LevelManager.PlayerBoundary.x : 10f;
    private float MovementBoundaryY => player.LevelManager ? player.LevelManager.PlayerBoundary.y : 6f;
    private bool AllowMovement => player.IsAlive() && (!player.LevelManager || !player.LevelManager.CurrentStage ||
                                                       player.LevelManager.CurrentStage.AllowPlayerMovement);

    public float MaxDodgeCooldown => dodgeCooldown;
    public bool IsDodging => _isDodging;
    public event Action OnDodge;
    public event Action<float> OnDodgeCooldownUpdated;

    private void OnValidate() { this.ValidateRefs(); }


    private void Start()
    {
        OnDodgeCooldownUpdated?.Invoke(_dodgeCooldownTimer);
    }

    private void OnEnable()
    {
        playerInput.OnMoveEvent += OnMove;
        playerInput.OnDodgeLeftEvent += OnDodgeLeft;
        playerInput.OnDodgeRightEvent += OnDodgeRight;
        playerInput.OnDodgeFreeformEvent += OnDodgeFreeform;
        
    }
    
    private void OnDisable()
    {
        playerInput.OnMoveEvent -= OnMove;
        playerInput.OnDodgeLeftEvent -= OnDodgeLeft;
        playerInput.OnDodgeRightEvent -= OnDodgeRight;
        playerInput.OnDodgeFreeformEvent -= OnDodgeFreeform;
    }

    private void Update()
    {
        UpdateDodgeState();
        HandleShipModelMovementAndRotation();
    }

    private void FixedUpdate()
    {
        HandleMovementAndRotation();
    }
    
    

    #region Movement --------------------------------------------------------------------------------------
    

    private void HandleMovementAndRotation()
    {
        Vector3 playerSplinePosition = player.LevelManager.PlayerPosition;
        
        // Calculate current offset from spline in local space
        Vector3 worldOffset = transform.position - playerSplinePosition;
        _currentOffsetFromSpline = Quaternion.Inverse(player.PlayerSplineRotation) * worldOffset;
        
        // Handle input movement or dodging
        if (!_isDodging)
        {
            // Get the input direction based on horizontal and vertical input
            Vector3 inputDirection = new Vector3(_horizontalInput, _verticalInput, 0);
            
            // Update target offset based on input
            if (inputDirection != Vector3.zero)
            {
                // Add to the target offset based on input
                _targetOffsetFromSpline += inputDirection * (maxMoveSpeed * Time.fixedDeltaTime);
                
                // Clamp the target offset to boundaries
                _targetOffsetFromSpline.x = Mathf.Clamp(_targetOffsetFromSpline.x, -MovementBoundaryX, MovementBoundaryX);
                _targetOffsetFromSpline.y = Mathf.Clamp(_targetOffsetFromSpline.y, -MovementBoundaryY, MovementBoundaryY);
                _targetOffsetFromSpline.z = 0; // Keep Z offset at 0
            }
            
            // Smoothly interpolate current offset towards target offset
            float lerpSpeed = inputDirection != Vector3.zero ? acceleration : deceleration;
            _currentOffsetFromSpline = Vector3.Lerp(_currentOffsetFromSpline, _targetOffsetFromSpline, lerpSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // During dodge, add dodge movement to the offset
            Vector3 dodgeMovement = _dodgeDirection * (dodgeMoveSpeed * Time.fixedDeltaTime);
            _targetOffsetFromSpline += dodgeMovement;
            _currentOffsetFromSpline += dodgeMovement;
            
            // Clamp to boundaries after dodge
            _targetOffsetFromSpline.x = Mathf.Clamp(_targetOffsetFromSpline.x, -MovementBoundaryX, MovementBoundaryX);
            _targetOffsetFromSpline.y = Mathf.Clamp(_targetOffsetFromSpline.y, -MovementBoundaryY, MovementBoundaryY);
            _currentOffsetFromSpline.x = Mathf.Clamp(_currentOffsetFromSpline.x, -MovementBoundaryX, MovementBoundaryX);
            _currentOffsetFromSpline.y = Mathf.Clamp(_currentOffsetFromSpline.y, -MovementBoundaryY, MovementBoundaryY);
        }
        
        // Calculate the desired world position (spline position and offset)
        Vector3 desiredWorldPosition = playerSplinePosition + (player.PlayerSplineRotation * _currentOffsetFromSpline);
        
        // Calculate velocity to reach the desired position
        Vector3 positionDifference = desiredWorldPosition - transform.position;
        
        // Use a higher follow speed when we're far from the desired position
        float distanceToDesired = positionDifference.magnitude;
        float effectiveFollowSpeed = pathFollowSpeed * (1f + distanceToDesired);
        
        // Set the rigidbody velocity
        playerRigidbody.linearVelocity = positionDifference.normalized * Mathf.Min(effectiveFollowSpeed, distanceToDesired / Time.fixedDeltaTime);
        playerRigidbody.rotation = player.PlayerSplineRotation;
    }
    
    
    
    private void HandleShipModelMovementAndRotation()
    {
        if (!shipModel) return;
    
        // Movement rotation based on movement (only roll)
        float inputRoll = -_horizontalInput * maxRollAmount;
        Quaternion targetVelocityRotation = Quaternion.Euler(0f, 0f, inputRoll);
    
        if (_horizontalInput != 0f)
        {
            _velocityRotation = Quaternion.Slerp(_velocityRotation, targetVelocityRotation, rollSpeed * Time.deltaTime);
        }
        else
        {
            _velocityRotation = Quaternion.Slerp(_velocityRotation, targetVelocityRotation, rollSpeed / 2 * Time.deltaTime);
        }

        // Aim rotation from aiming (only pitch and yaw)
        if (playerAiming)
        {
            Vector3 aimDirection = playerAiming.GetAimDirection();
            Vector3 localAimDirection = Quaternion.Inverse(player.PlayerSplineRotation) * aimDirection;
        
            float yawAngle = Mathf.Atan2(localAimDirection.x, localAimDirection.z) * Mathf.Rad2Deg;
            float pitchAngle = -Mathf.Asin(Mathf.Clamp(localAimDirection.y, -1f, 1f)) * Mathf.Rad2Deg;
        
            yawAngle = Mathf.Clamp(yawAngle, -maxYawAngle, maxYawAngle);
            pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
        
            Quaternion targetAimRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            _aimRotation = Quaternion.Slerp(_aimRotation, targetAimRotation, pitchYawSpeed * Time.deltaTime);
        }
        else
        {
            _aimRotation = Quaternion.identity;
        }

        // Combine all rotations: aim + input roll + dodge roll
        Vector3 finalEuler = _aimRotation.eulerAngles;
        finalEuler.z = _velocityRotation.eulerAngles.z + _currentDodgeRoll;
    
        shipModel.localRotation = Quaternion.Euler(finalEuler);
    }
    
    
    #endregion Movement  --------------------------------------------------------------------------------------

    
    

    #region Dodge --------------------------------------------------------------------------------------

    private void UpdateDodgeState()
    {
        if (!enableDodging) return;
    
        // Check if we are currently dodging
        if (_isDodging && _dodgeTimeCounter <= dodgeTime)
        {
            _dodgeTimeCounter += Time.deltaTime;
        
            // Reset dodge if we exceed the dodge time
            if (_dodgeTimeCounter >= dodgeTime)
            {
                _isDodging = false;
                _dodgeCooldownTimer = dodgeCooldown;
                _currentDodgeRoll = 0f;
            }
        }
    
        // Check cooldown
        if (!_isDodging && _dodgeCooldownTimer > 0f)
        {
            _dodgeCooldownTimer -= Time.deltaTime;
            if (_dodgeCooldownTimer < 0f) _dodgeCooldownTimer = 0f;
            OnDodgeCooldownUpdated?.Invoke(_dodgeCooldownTimer);
        }
    }
    
    private void PlayDodgeRollAnimation()
    {
        if (_dodgeTween.isAlive) _dodgeTween.Stop();

        // Calculate target roll
        float startRoll = 0;
        float targetRoll = startRoll + (-_dodgeDirection.x * dodgeRollAmount);
        
        // Play dodge sound effect
        dodgeSfx?.Play(audioSource);
        
        
        // Tween just the dodge roll component
        _dodgeTween = Tween.Custom(
            onValueChange: rollAngle => _currentDodgeRoll = rollAngle,
            startValue: startRoll,
            endValue: targetRoll,
            settings: dodgeTweenSettings
        );
    }

    #endregion Dodge --------------------------------------------------------------------------------------
    
    
    
    #region Input Handling --------------------------------------------------------------------------------------

    private void OnMove(InputAction.CallbackContext context)
    {
        if (!AllowMovement)
        {
            _horizontalInput = 0f;
            _verticalInput = 0f;
            return;
        }
        
        if (context.started || context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            _horizontalInput = input.x;
            _verticalInput = input.y;
        } 
        else if (context.canceled)
        {
            _horizontalInput = 0f;
            _verticalInput = 0f;
        }
    }
    
    private void OnDodgeLeft(InputAction.CallbackContext context)
    {
        if (!enableDodging || !AllowMovement) return;
        
        if (_dodgeCooldownTimer <= 0f && !_isDodging)
        {
            OnDodge?.Invoke();
            _dodgeDirection = Vector3.left;
            _dodgeTimeCounter = 0f;
            _isDodging = true;
                
            PlayDodgeRollAnimation();
        }
    }
    
    private void OnDodgeRight(InputAction.CallbackContext context)
    {
        if (!enableDodging || !AllowMovement) return;
        
        if (_dodgeCooldownTimer <= 0f && !_isDodging)
        {
            OnDodge?.Invoke();
            _dodgeDirection = Vector3.right;
            _dodgeTimeCounter = 0f;
            _isDodging = true;
                
            PlayDodgeRollAnimation();
        }
    }
    
    private void OnDodgeFreeform(InputAction.CallbackContext context)
    {
        if (!enableDodging || !AllowMovement) return;
        

        if (_horizontalInput < 0)
        {
            OnDodgeLeft(context);
        } 
        else if (_horizontalInput > 0)
        {
            OnDodgeRight(context);
        }
    }

    #endregion Input Handling --------------------------------------------------------------------------------------

    
    
    #if UNITY_EDITOR
    #region Editor -------------------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        // Draw boundaries from the actual player position on spline (with pathOffset)
        if (player.LevelManager)
        {
            Vector3 playerSplinePosition = player.LevelManager.PlayerPosition;
            
            // Create boundary corners in local spline space, then transform to world space
            Vector3[] localCorners = new Vector3[]
            {
                new Vector3(-MovementBoundaryX, -MovementBoundaryY, 0), // Bottom-left
                new Vector3(MovementBoundaryX, -MovementBoundaryY, 0),  // Bottom-right
                new Vector3(MovementBoundaryX, MovementBoundaryY, 0),   // Top-right
                new Vector3(-MovementBoundaryX, MovementBoundaryY, 0)   // Top-left
            };
            
            // Transform corners to world space using spline rotation
            Vector3[] worldCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                worldCorners[i] = playerSplinePosition + (player.PlayerSplineRotation * localCorners[i]);
            }

            Gizmos.color = Color.blue;
            // Draw boundary rectangle
            for (int i = 0; i < 4; i++)
            {
                int nextIndex = (i + 1) % 4;
                Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
            }
            
            UnityEditor.Handles.Label(playerSplinePosition + (player.PlayerSplineRotation * Vector3.up * (MovementBoundaryY + 0.5f)), "Player Boundaries");
        }
    }

    #endregion Editor -------------------------------------------------------------------------------------
    #endif
}