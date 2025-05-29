using System;
using KBCore.Refs;
using Unity.Mathematics;
using UnityEngine;
using PrimeTween;


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
    [SerializeField, Min(0)] private float dodgeMoveSpeed = 20f;
    [SerializeField, Min(0)] private float dodgeTime = 0.2f;
    [SerializeField, Min(0)] private float dodgeCooldown = 1.5f;
    [SerializeField, Min(0)] private float dodgeRollAmount = 720f;
    [SerializeField] private TweenSettings dodgeTweenSettings = new TweenSettings(1.2f, Ease.Custom);
    
    [Header("References")] 
    [SerializeField, Self] private RailPlayer player;
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField] private Transform shipModel;



    // Movement
    private Vector3 _targetMovePosition;
    private Vector3 _targetPathPosition;
    private Quaternion _splineRotation = Quaternion.identity;
    
    // Dodging
    private bool _isDodging;
    private float _dodgeCooldownTimer;
    private float _dodgeTimeCounter;
    private float _currentBarrelRoll;
    private Vector3 _dodgeDirection;
    private Tween _dodgeTween;
    
    
    // Input properties
    private float horizontalInput => playerInput.MovementInput.x;
    private float verticalInput => playerInput.MovementInput.y;
    
    // Boundary settings
    private readonly float _boundaryX = LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.x : 10f;
    private readonly float _boundaryY = LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.y : 6f;
    private readonly float _pathOffset =  LevelManager.Instance ? LevelManager.Instance.PlayerOffset : -8f;

    
    private void Update()
    {
        HandleMovement();
        HandleDodging();
        HandleSplineRotation();
        HandleShipRotation();
        
        // Apply final movement
        transform.position = _targetPathPosition + _targetMovePosition;
    }
    
    
    private void HandleMovement()
    {
        if (!_isDodging)
        {
            // Create input movement in a local spline space
            Vector3 localInputMovement = new Vector3(horizontalInput, verticalInput, 0) * (moveSpeed * Time.deltaTime);
        
            // Transform input to world space relative to spline rotation
            Vector3 worldInputMovement = _splineRotation * localInputMovement;
            _targetMovePosition += worldInputMovement;
    
            // Transform current offset to spline local space for boundary clamping
            Vector3 localOffset = Quaternion.Inverse(_splineRotation) * _targetMovePosition;
        
            // Clamp in the local spline space
            localOffset.x = Mathf.Clamp(localOffset.x, -_boundaryX, _boundaryX);
            localOffset.y = Mathf.Clamp(localOffset.y, -_boundaryY, _boundaryY);
        
            // Transform back to world space    
            _targetMovePosition = _splineRotation * localOffset;
        }

        // Handle the path following
        if (LevelManager.Instance && LevelManager.Instance.LevelPath && LevelManager.Instance.CurrentPositionOnPath)
        {
            // Calculate target position on spline with offset from CurrentPositionOnPath
            float offsetT = _pathOffset / LevelManager.Instance.LevelPath.CalculateLength();
            float targetSplineT = LevelManager.Instance.GetCurrentSplineT() + offsetT;
            targetSplineT = Mathf.Clamp01(targetSplineT); // Keep within spline bounds
    
            Vector3 targetPosition = LevelManager.Instance.LevelPath.EvaluatePosition(targetSplineT);
            _targetPathPosition = Vector3.Lerp(_targetPathPosition, targetPosition, player.PathFollowSpeed * Time.deltaTime);
        } 
        else 
        {
            _targetPathPosition = Vector3.zero;
        }
        
    }
    
    private void HandleDodging()
    {
        if (!enableDodging) return;
        
        // Check for dodge input
        if (_dodgeCooldownTimer <= 0f && !_isDodging)
        {
            if (playerInput.DodgeLeftInput || playerInput.DodgeRightInput)
            {
                _dodgeDirection = playerInput.DodgeLeftInput ? Vector3.left : Vector3.right;
                _dodgeTimeCounter = 0f;
                _isDodging = true;
                
                PlayDodgeRollAnimation();
            }
        }
        
        // Apply dodge movement as long as we are dodging and within the dodge time
        if (_isDodging && _dodgeTimeCounter <= dodgeTime)
        {
            // Calculate dodge movement in world space
            Vector3 worldDodgeMovement = _splineRotation * _dodgeDirection * (dodgeMoveSpeed * Time.deltaTime);
            
            // Apply dodge movement to _targetMovePosition
            _targetMovePosition += worldDodgeMovement;

            // Transform to local spline space and clamp to boundaries
            Vector3 localOffset = Quaternion.Inverse(_splineRotation) * _targetMovePosition;
            localOffset.x = Mathf.Clamp(localOffset.x, -_boundaryX, _boundaryX);
            localOffset.y = Mathf.Clamp(localOffset.y, -_boundaryY, _boundaryY);

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
        
        // Update cooldown timer (only when not dodging)
        if (!_isDodging && _dodgeCooldownTimer > 0f)
        {
            _dodgeCooldownTimer -= Time.deltaTime;
            if (_dodgeCooldownTimer < 0f) _dodgeCooldownTimer = 0f;
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
            float bankAngle = -horizontalInput * movementTiltAmount * 0.5f;
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


    #region Spline --------------------------------------------------------------------------------------

    private void HandleSplineRotation()
    {
        if (!player.AlignToSplineDirection || !LevelManager.Instance || !LevelManager.Instance.LevelPath)
        {
            _splineRotation = Quaternion.identity;
            return;
        }

        // Get the spline direction - you'll need to implement this based on your spline system
        Vector3 splineForward = GetSplineDirection();
        
        if (splineForward != Vector3.zero)
        {
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineForward, Vector3.up);
            _splineRotation = Quaternion.Slerp(_splineRotation, targetSplineRotation, player.SplineRotationSpeed * Time.deltaTime);
            
            // Apply spline rotation to the entire player transform
            transform.rotation = _splineRotation;
        }
    }
    
    private Vector3 GetSplineDirection()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.LevelPath) 
            return Vector3.forward;
        
        float offsetT = _pathOffset / LevelManager.Instance.LevelPath.CalculateLength();
        float playerSplineT = LevelManager.Instance.GetCurrentSplineT() + offsetT;
        playerSplineT = Mathf.Clamp01(playerSplineT);
    
        // Use math.normalize for float3, then convert to Vector3
        var tangent = LevelManager.Instance.LevelPath.EvaluateTangent(playerSplineT);
        return math.normalize(tangent);
    }

    public Vector3 GetPlayerSplinePosition()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.LevelPath)
            return transform.position;
            
        float offsetT = _pathOffset / LevelManager.Instance.LevelPath.CalculateLength();
        float playerSplineT = LevelManager.Instance.GetCurrentSplineT() + offsetT;
        playerSplineT = Mathf.Clamp01(playerSplineT);
        
        return LevelManager.Instance.LevelPath.EvaluatePosition(playerSplineT);
    }
    
    

    #endregion Spline --------------------------------------------------------------------------------------
 
    
    
    #if UNITY_EDITOR
    #region Editor -------------------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        // Draw boundaries from the actual player position on spline (with pathOffset)
        if (LevelManager.Instance && LevelManager.Instance.LevelPath)
        {
            Vector3 playerSplinePosition = GetPlayerSplinePosition();
            
            // Create boundary corners in local spline space, then transform to world space
            Vector3[] localCorners = new Vector3[]
            {
                new Vector3(-_boundaryX, -_boundaryY, 0), // Bottom-left
                new Vector3(_boundaryX, -_boundaryY, 0),  // Bottom-right
                new Vector3(_boundaryX, _boundaryY, 0),   // Top-right
                new Vector3(-_boundaryX, _boundaryY, 0)   // Top-left
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
            
            UnityEditor.Handles.Label(playerSplinePosition + (_splineRotation * Vector3.up * (_boundaryY + 0.5f)), "Player Boundaries");
            
            // Draw spline direction
            Gizmos.color = Color.red;
            Vector3 splineDir = GetSplineDirection();
            Gizmos.DrawRay(transform.position, splineDir * 5f);
            UnityEditor.Handles.Label(transform.position + splineDir * 5.5f, "Spline Direction");
        }
    }

    #endregion Editor -------------------------------------------------------------------------------------
    #endif
    
    
    
    
}