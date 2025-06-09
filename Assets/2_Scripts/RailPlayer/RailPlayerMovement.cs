using System;
using KBCore.Refs;
using Unity.Mathematics;
using UnityEngine;
using PrimeTween;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using VInspector;

[RequireComponent(typeof(Rigidbody))]
public class RailPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float maxMoveSpeed = 15f;
    [SerializeField, Min(0.1f)] private float acceleration = 10f;
    [SerializeField, Min(0.1f)] private float deceleration = 5f;
    [SerializeField, Min(0)] private float pathFollowSpeed = 5f;
    
    [Header("Rotation Settings")]
    [SerializeField, Min(0)] private float rotationSpeed = 22f;
    [SerializeField, Min(0)] private float movementTiltAmount = 30f;
    [SerializeField, Min(0)] private float maxPitchAngle = 30f;
    [SerializeField, Min(0)] private float maxYawAngle = 45f;
    [SerializeField, Min(0)] private float pathRotationSpeed = 5f;
    
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
    [SerializeField, Self] private RailPlayer player;
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField, Self] private Rigidbody playerRigidbody;
    [SerializeField] private Transform shipModel;


    
    private float _horizontalInput;
    private float _verticalInput;
    
    private Quaternion _splineRotation = Quaternion.identity;
    private Quaternion _aimRotation = Quaternion.identity;
    private Vector3 _currentMoveVelocity;
    private Vector3 _targetMoveVelocity;
    private Vector3 _splineFollowVelocity = Vector3.zero;
    
    

    
    




    private bool _isDodging;
    private float _dodgeCooldownTimer;
    private float _dodgeTimeCounter;
    private float _currentBarrelRoll;
    private Vector3 _dodgeDirection;
    private Tween _dodgeTween;

    private float MovementBoundaryX => LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.x : 10f;
    private float MovementBoundaryY => LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.y : 6f;
    public bool IsDodging => _isDodging;
    
    

    private void OnValidate() { this.ValidateRefs(); }
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
        HandleShipRotation();
    }

    private void FixedUpdate()
    {
        HandleRotation();
        HandleMovement();
    }


    #region Movement & Rotation --------------------------------------------------------------------------------------

    private void HandleMovement()
    {
        // Handle the spline following
        if (LevelManager.Instance && player.AlignToSplineDirection)
        {
            Vector3 splineFollowDirection = LevelManager.Instance.PlayerPosition - transform.position;
            float distanceToSpline = splineFollowDirection.magnitude;
            
            
            float lerpFactor = Mathf.Clamp01(distanceToSpline / 10f);
            _splineFollowVelocity = Vector3.Lerp(Vector3.zero, splineFollowDirection.normalized * pathFollowSpeed, lerpFactor * Time.fixedDeltaTime);
        }
        else
        {
            _splineFollowVelocity = Vector3.zero;
        }
        
        
        
        // Handle input movement or dodging
        if (!_isDodging)
        {
            // Get the input direction based on horizontal and vertical input
            Vector3 inputDirection = new Vector3(_horizontalInput, _verticalInput, 0);
        
            // Transform input to world space relative to spline rotation
            Vector3 moveDirection = _splineRotation * inputDirection;
            
            // Set target acceleration based on input
            float targetAcceleration = inputDirection != Vector3.zero ? acceleration : deceleration;
            
            // Calculate target move velocity based on input
            _targetMoveVelocity = moveDirection.normalized * maxMoveSpeed;
            
            // Check if we are within movement boundaries
            if (LevelManager.Instance && LevelManager.Instance.SplineContainer)
            {
                Vector3 playerSplinePosition = LevelManager.Instance.PlayerPosition;
                Vector3 localPosition = Quaternion.Inverse(_splineRotation) * (playerSplinePosition - transform.position);
                
                // Check if we are outside the movement boundaries
                if (Mathf.Abs(localPosition.x) > MovementBoundaryX || Mathf.Abs(localPosition.y) > MovementBoundaryY)
                {
                    targetAcceleration = acceleration * 2f;
                    _targetMoveVelocity = Vector3.zero;
                }
            }
            
            // Smoothly interpolate current move velocity towards target
            _currentMoveVelocity = Vector3.Lerp(_currentMoveVelocity, _targetMoveVelocity, targetAcceleration * Time.fixedDeltaTime);
        }
        else
        {
            // Calculate dodge movement in world space
            Vector3 worldDodgeMovement = _splineRotation * _dodgeDirection * (dodgeMoveSpeed * Time.fixedDeltaTime);
            
            // Apply dodge movement to input
            _currentMoveVelocity += worldDodgeMovement;
        }

        
        playerRigidbody.linearVelocity = _splineFollowVelocity + _currentMoveVelocity;
        Debug.Log($"Current Move Velocity: {_currentMoveVelocity}, Spline Follow Velocity: {_splineFollowVelocity}");
    }

    private void HandleRotation()
    {
        // Get spline rotation
        if (LevelManager.Instance && player.AlignToSplineDirection)
        {
            Vector3 splineDirection = LevelManager.Instance.GetDirectionOnSpline(LevelManager.Instance.PlayerPosition);
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineDirection, Vector3.up);
        
            // Smoothly rotate towards the spline direction
            _splineRotation = Quaternion.Slerp(_splineRotation, targetSplineRotation, pathRotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            _splineRotation = Quaternion.identity;
        }
        
        
        // Get aim rotation from player aiming (only pitch and yaw, no roll)
        // Not being used for now, looks better when just rotating the ship model
        if (playerAiming)
        {
            Vector3 aimDirection = playerAiming.GetAimDirection();
            
            // Convert aim direction to local space relative to spline
            Vector3 localAimDirection = Quaternion.Inverse(_splineRotation) * aimDirection;
            
            // Calculate pitch and yaw angles only
            float yawAngle = Mathf.Atan2(localAimDirection.x, localAimDirection.z) * Mathf.Rad2Deg;
            float pitchAngle = -Mathf.Asin(Mathf.Clamp(localAimDirection.y, -1f, 1f)) * Mathf.Rad2Deg;
            
            // Clamp the angles
            yawAngle = Mathf.Clamp(yawAngle, -maxYawAngle, maxYawAngle);
            pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
            
            // Create target aim rotation with only pitch and yaw (no roll)
            Quaternion targetAimRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            
            // Smoothly rotate towards the target aim rotation
            _aimRotation = Quaternion.Slerp(_aimRotation, targetAimRotation, rotationSpeed * Time.fixedDeltaTime);
            
        }
        else
        {
            _aimRotation = Quaternion.identity;
        }

        // Apply the combined rotation to the rigidbody
        playerRigidbody.rotation = _splineRotation;
    }
        
        

    
        
    private void HandleShipRotation()
    {
        if (!shipModel || !playerAiming) return;
        
        Vector3 aimDirection = playerAiming.GetAimDirection();
        
        // Convert the aim direction to local space relative to spline rotation
        Vector3 localAimDirection = Quaternion.Inverse(_splineRotation) * aimDirection;
        
        // Clamp the local aim direction to prevent excessive angles
        float yawAngle = Mathf.Atan2(localAimDirection.x, localAimDirection.z) * Mathf.Rad2Deg;
        float pitchAngle = -Mathf.Asin(localAimDirection.y) * Mathf.Rad2Deg;
        yawAngle = Mathf.Clamp(yawAngle, -maxYawAngle, maxYawAngle);
        pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
        
        // Get current Z rotation 
        float currentZRotation = shipModel.localEulerAngles.z;
        
        // Handle banking only when not dodging
        if (!_dodgeTween.isAlive)
        {
            float bankAngle = -_horizontalInput * movementTiltAmount * 0.5f;
            currentZRotation = bankAngle;
        }
        
        // Apply rotation but preserve the Z-axis if tween is running
        Quaternion targetRotation = Quaternion.Euler(pitchAngle, yawAngle, currentZRotation);
        
        // Only interpolate X and Y axes when tween is active
        if (_dodgeTween.isAlive)
        {
            // Preserve the exact Z rotation from the tween, only lerp X and Y
            Vector3 currentEuler = shipModel.localEulerAngles;
            Vector3 targetEuler = targetRotation.eulerAngles;
            
            // Interpolate only pitch and yaw
            float lerpedPitch = Mathf.LerpAngle(currentEuler.x, targetEuler.x, rotationSpeed * Time.deltaTime);
            float lerpedYaw = Mathf.LerpAngle(currentEuler.y, targetEuler.y, rotationSpeed * Time.deltaTime);
            
            shipModel.localRotation = Quaternion.Euler(lerpedPitch, lerpedYaw, currentEuler.z);
        }
        else
        {
            // Normal rotation when no tween is active
            shipModel.localRotation = Quaternion.Slerp(shipModel.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    


    #endregion Movement & Rotation --------------------------------------------------------------------------------------
    

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
            }
        }
        
        
        // Check cooldown
        if (!_isDodging && _dodgeCooldownTimer > 0f)
        {
            _dodgeCooldownTimer -= Time.deltaTime;
            if (_dodgeCooldownTimer < 0f) _dodgeCooldownTimer = 0f;
        }
    }
    
    
    private void PlayDodgeRollAnimation()
    {
        if (_dodgeTween.isAlive) _dodgeTween.Stop();
    
        // Calculate target roll
        float startRoll = shipModel.localEulerAngles.z;
        float targetRoll = startRoll + (-_dodgeDirection.x * dodgeRollAmount);
    
        // Only tween the roll angle
        _dodgeTween = Tween.Custom(
            onValueChange: rollAngle => { Vector3 currentEuler = shipModel.localEulerAngles; shipModel.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, rollAngle); },
            startValue: startRoll,
            endValue: targetRoll,
            settings:dodgeTweenSettings
        );
    }

    #endregion Dodge --------------------------------------------------------------------------------------
    

    #region Input Handling --------------------------------------------------------------------------------------

    private void OnMove(InputAction.CallbackContext context)
    {
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
        if (!enableDodging) return;
        

        if (_dodgeCooldownTimer <= 0f && !_isDodging)
        {
            _dodgeDirection = Vector3.left;
            _dodgeTimeCounter = 0f;
            _isDodging = true;
                
            PlayDodgeRollAnimation();
        }
    }
    
    private void OnDodgeRight(InputAction.CallbackContext context)
    {
        if (!enableDodging) return;
        
        if (_dodgeCooldownTimer <= 0f && !_isDodging)
        {
            _dodgeDirection = Vector3.right;
            _dodgeTimeCounter = 0f;
            _isDodging = true;
                
            PlayDodgeRollAnimation();
        }
    }
    
    private void OnDodgeFreeform(InputAction.CallbackContext context)
    {
        if (!enableDodging) return;

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
        if (LevelManager.Instance && LevelManager.Instance.SplineContainer)
        {
            Vector3 playerSplinePosition = LevelManager.Instance.PlayerPosition;
            
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
                worldCorners[i] = playerSplinePosition + (_splineRotation * localCorners[i]);
            }

            Gizmos.color = Color.blue;
            // Draw boundary rectangle
            for (int i = 0; i < 4; i++)
            {
                int nextIndex = (i + 1) % 4;
                Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
            }
            
            UnityEditor.Handles.Label(playerSplinePosition + (_splineRotation * Vector3.up * (MovementBoundaryY + 0.5f)), "Player Boundaries");
            
        }
    }

    #endregion Editor -------------------------------------------------------------------------------------
    #endif
    
    
    
    
}