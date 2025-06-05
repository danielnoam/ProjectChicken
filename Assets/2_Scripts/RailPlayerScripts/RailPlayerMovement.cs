using System;
using KBCore.Refs;
using Unity.Mathematics;
using UnityEngine;
using PrimeTween;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using VInspector;


public class RailPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float moveSpeed = 15f;
    
    [Header("Rotation Settings")]
    [SerializeField, Min(0)] private float rotationSpeed = 22f;
    [SerializeField, Min(0)] private float movementTiltAmount = 30f;
    [SerializeField, Min(0)] private float maxPitchAngle = 30f;
    [SerializeField, Min(0)] private float maxYawAngle = 45f;
    
    [Header("Dodge Settings")]
    [SerializeField] private bool enableDodging = true;
    [EnableIf("enableDodging")]
    [SerializeField, Min(0)] private float dodgeMoveSpeed = 20f;
    [SerializeField, Min(0)] private float dodgeTime = 0.2f;
    [SerializeField, Min(0)] private float dodgeCooldown = 1.5f;
    [SerializeField, Min(0)] private float dodgeRollAmount = 720f;
    [SerializeField] private TweenSettings dodgeTweenSettings = new TweenSettings(1.2f, Ease.Custom);
    [EndIf]
    
    [Header("References")] 
    [SerializeField, Self] private RailPlayer player;
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField] private Transform shipModel;


    
    private float _horizontalInput;
    private float _verticalInput;
    private Vector3 _targetMovePosition;
    private Vector3 _targetPathPosition;
    private Quaternion _splineRotation = Quaternion.identity;
    
    private bool _isDodging;
    private float _dodgeCooldownTimer;
    private float _dodgeTimeCounter;
    private float _currentBarrelRoll;
    private Vector3 _dodgeDirection;
    private Tween _dodgeTween;

    private float MovementBoundaryX => LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.x : 10f;
    private float MovementBoundaryY => LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.y : 6f;

    private void OnValidate() { this.ValidateRefs(); }
    private void OnEnable()
    {
        playerInput.OnMoveEvent += OnMove;
        playerInput.OnDodgeLeftEvent += OnDodgeLeft;
        playerInput.OnDodgeRightEvent += OnDodgeRight;
    }
    
    private void OnDisable()
    {
        playerInput.OnMoveEvent -= OnMove;
        playerInput.OnDodgeLeftEvent -= OnDodgeLeft;
        playerInput.OnDodgeRightEvent -= OnDodgeRight;
    }

    private void Update()
    {
        HandleMovement();
        HandleDodgeMovement();
        HandleSplineRotation();
        HandleShipRotation();
        UpdateDodgeCooldown();
        
        // Apply final movement
        transform.position = _targetPathPosition + _targetMovePosition;
    }



    #region Movement & Rotation --------------------------------------------------------------------------------------

        private void HandleMovement()
    {
        if (!_isDodging)
        {
            // Create input movement in a local spline space
            Vector3 localInputMovement = new Vector3(_horizontalInput, _verticalInput, 0) * (moveSpeed * Time.deltaTime);
        
            // Transform input to world space relative to spline rotation
            Vector3 worldInputMovement = _splineRotation * localInputMovement;
            _targetMovePosition += worldInputMovement;
    
            // Transform current offset to spline local space for boundary clamping
            Vector3 localOffset = Quaternion.Inverse(_splineRotation) * _targetMovePosition;
        
            // Clamp in the local spline space
            localOffset.x = Mathf.Clamp(localOffset.x, -MovementBoundaryX, MovementBoundaryX);
            localOffset.y = Mathf.Clamp(localOffset.y, -MovementBoundaryY, MovementBoundaryY);
        
            // Transform back to world space    
            _targetMovePosition = _splineRotation * localOffset;
        }

        // Handle the path following
        if (LevelManager.Instance)
        {
            _targetPathPosition = Vector3.Lerp(_targetPathPosition, LevelManager.Instance.PlayerPosition, player.PathFollowSpeed * Time.deltaTime);
        } 
        else 
        {
            _targetPathPosition = Vector3.zero;
        }
        
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

    private void HandleDodgeMovement()
    {
        if (!enableDodging) return;
        
        
        // Apply dodge movement as long as we are dodging and within the dodge time
        if (_isDodging && _dodgeTimeCounter <= dodgeTime)
        {
            // Calculate dodge movement in world space
            Vector3 worldDodgeMovement = _splineRotation * _dodgeDirection * (dodgeMoveSpeed * Time.deltaTime);
            
            // Apply dodge movement to _targetMovePosition
            _targetMovePosition += worldDodgeMovement;

            // Transform to local spline space and clamp to boundaries
            Vector3 localOffset = Quaternion.Inverse(_splineRotation) * _targetMovePosition;
            localOffset.x = Mathf.Clamp(localOffset.x, -MovementBoundaryX, MovementBoundaryX);
            localOffset.y = Mathf.Clamp(localOffset.y, -MovementBoundaryY, MovementBoundaryY);

            // Transform back to world space
            _targetMovePosition = _splineRotation * localOffset;
            
            _dodgeTimeCounter += Time.deltaTime;
            
            // Reset dodge if we exceed the dodge time
            if (_dodgeTimeCounter >= dodgeTime)
            {
                _isDodging = false;
                _dodgeCooldownTimer = dodgeCooldown;
            }
        }
    }
    
    private void UpdateDodgeCooldown()
    {
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
    
    
    #region Spline --------------------------------------------------------------------------------------

    private void HandleSplineRotation()
    {
        if (!player.AlignToSplineDirection || !LevelManager.Instance || !LevelManager.Instance.SplineContainer)
        {
            _splineRotation = Quaternion.identity;
            return;
        }

        // Get the spline direction - you'll need to implement this based on your spline system
        Vector3 splineForward = GetPlayerDirectionOnSpline();
        
        if (splineForward != Vector3.zero)
        {
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineForward, Vector3.up);
            _splineRotation = Quaternion.Slerp(_splineRotation, targetSplineRotation, player.SplineRotationSpeed * Time.deltaTime);
            
            // Apply spline rotation to the entire player transform
            transform.rotation = _splineRotation;
        }
    }
    
    private Vector3 GetPlayerDirectionOnSpline()
    {
        return !LevelManager.Instance ? Vector3.forward : LevelManager.Instance.GetEnemyDirectionOnSpline(LevelManager.Instance.PlayerPosition);
    }
    
    

    #endregion Spline --------------------------------------------------------------------------------------
 
    
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
        
        
        if (context.performed)
        {
            
            if (_dodgeCooldownTimer <= 0f && !_isDodging)
            {
                _dodgeDirection = Vector3.left;
                _dodgeTimeCounter = 0f;
                _isDodging = true;
                
                PlayDodgeRollAnimation();
            }
        }
    }
    
    private void OnDodgeRight(InputAction.CallbackContext context)
    {
        if (context.performed)
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