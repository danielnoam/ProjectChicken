using System;
using KBCore.Refs;
using Unity.Mathematics;
using UnityEngine;


public class RailPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float moveSpeed = 15f;
    
    [Header("Rotation Settings")]
    [SerializeField, Min(0)] private float rotationSpeed = 22f;
    [SerializeField, Min(0)] private float movementTiltAmount = 30f;
    [SerializeField, Min(0)] private float maxPitchAngle = 30f;
    [SerializeField, Min(0)] private float maxYawAngle = 45f;
    
    [Header("References")] 
    [SerializeField, Self] private RailPlayer player;
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField] private Transform shipModel;
    
    private Vector3 _targetMovePosition;
    private Vector3 _targetPathPosition;
    private Quaternion _splineRotation = Quaternion.identity;
    
    private float horizontalInput => playerInput.MovementInput.x;
    private float verticalInput => playerInput.MovementInput.y;
    private readonly float boundaryX = LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.x : 10f;
    private readonly float boundaryY = LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.y : 6f;
    private readonly float pathOffset =  LevelManager.Instance ? LevelManager.Instance.PlayerOffset : -8f;

    
    private void Update()
    {
        HandleMovement();
        HandleSplineRotation();
        HandleShipRotation();
    }
    
    
    private void HandleMovement()
    {
        // Create input movement in a local spline space
        Vector3 localInputMovement = new Vector3(horizontalInput, verticalInput, 0) * (moveSpeed * Time.deltaTime);
        
        // Transform input to world space relative to spline rotation
        Vector3 worldInputMovement = _splineRotation * localInputMovement;
        _targetMovePosition += worldInputMovement;
    
        // Transform current offset to spline local space for boundary clamping
        Vector3 localOffset = Quaternion.Inverse(_splineRotation) * _targetMovePosition;
        
        // Clamp in the local spline space
        localOffset.x = Mathf.Clamp(localOffset.x, -boundaryX, boundaryX);
        localOffset.y = Mathf.Clamp(localOffset.y, -boundaryY, boundaryY);
        
        // Transform back to world space
        _targetMovePosition = _splineRotation * localOffset;

        // Handle the path following
        if (LevelManager.Instance && LevelManager.Instance.LevelPath && LevelManager.Instance.CurrentPositionOnPath)
        {
            // Calculate target position on spline with offset from CurrentPositionOnPath
            float offsetT = pathOffset / LevelManager.Instance.LevelPath.CalculateLength();
            float targetSplineT = LevelManager.Instance.GetCurrentSplineT() + offsetT;
            targetSplineT = Mathf.Clamp01(targetSplineT); // Keep within spline bounds
    
            Vector3 targetPosition = LevelManager.Instance.LevelPath.EvaluatePosition(targetSplineT);
            _targetPathPosition = Vector3.Lerp(_targetPathPosition, targetPosition, player.PathFollowSpeed * Time.deltaTime);
        } 
        else 
        {
            _targetPathPosition = Vector3.zero;
        }

        transform.position = _targetPathPosition + _targetMovePosition;
    }
    
    
    
    
    private void HandleShipRotation()
    {
        Vector3 aimDirection = playerAiming.GetAimDirection();
        
        // Convert the aim direction to local space relative to spline rotation
        Vector3 localAimDirection = Quaternion.Inverse(_splineRotation) * aimDirection;
        
        float yawAngle = Mathf.Atan2(localAimDirection.x, localAimDirection.z) * Mathf.Rad2Deg;
        float pitchAngle = -Mathf.Asin(localAimDirection.y) * Mathf.Rad2Deg;
        
        yawAngle = Mathf.Clamp(yawAngle, -maxYawAngle, maxYawAngle);
        pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
        
        float bankAngle = -horizontalInput * movementTiltAmount * 0.5f;
        
        Quaternion targetRotation = Quaternion.Euler(pitchAngle, yawAngle, bankAngle);
        
        shipModel.localRotation = Quaternion.Slerp(shipModel.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
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
        
        float offsetT = pathOffset / LevelManager.Instance.LevelPath.CalculateLength();
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
            
        float offsetT = pathOffset / LevelManager.Instance.LevelPath.CalculateLength();
        float playerSplineT = LevelManager.Instance.GetCurrentSplineT() + offsetT;
        playerSplineT = Mathf.Clamp01(playerSplineT);
        
        return LevelManager.Instance.LevelPath.EvaluatePosition(playerSplineT);
    }
    
    

    #endregion Spline --------------------------------------------------------------------------------------
 
    
    
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
                new Vector3(-boundaryX, -boundaryY, 0), // Bottom-left
                new Vector3(boundaryX, -boundaryY, 0),  // Bottom-right
                new Vector3(boundaryX, boundaryY, 0),   // Top-right
                new Vector3(-boundaryX, boundaryY, 0)   // Top-left
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
            
            UnityEditor.Handles.Label(playerSplinePosition + (_splineRotation * Vector3.up * (boundaryY + 0.5f)), "Player Boundaries");
            
            // Draw spline direction
            Gizmos.color = Color.red;
            Vector3 splineDir = GetSplineDirection();
            Gizmos.DrawRay(transform.position, splineDir * 5f);
            UnityEditor.Handles.Label(transform.position + splineDir * 5.5f, "Spline Direction");
        }
    }

    #endregion Editor -------------------------------------------------------------------------------------
    
    
    
    
}