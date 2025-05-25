using System;
using KBCore.Refs;
using UnityEngine;

[RequireComponent(typeof(RailShooterPlayerInput))]
[RequireComponent(typeof(RailShooterPlayerAiming))]
public class RailShooterPlayer : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float moveSpeed = 5f;
    [SerializeField, Min(0)] private float boundaryX = 8f;
    [SerializeField, Min(0)] private float boundaryY = 4f;
    
    [Header("Movement Tilting")]
    [SerializeField, Min(0)] private float tiltAmount = 30f;
    [SerializeField, Min(0)] private float tiltSpeed = 5f;
    
    [Header("Aiming Rotation")]
    [SerializeField, Min(0)] private float aimRotationSpeed = 5f;
    [SerializeField, Min(0)] private float maxPitchAngle = 30f;
    [SerializeField, Min(0)] private float maxYawAngle = 45f;
    
    [Header("References")] 
    [SerializeField, Self] private RailShooterPlayerAiming aimingScript;
    [SerializeField, Self] private RailShooterPlayerInput inputScript;
    [SerializeField] private Transform shipModel;
    
    private Vector3 targetMovePosition;
    private float horizontalInput => inputScript.MovementInput.x;
    private float verticalInput => inputScript.MovementInput.y;

    private void Awake()
    {
        if (!shipModel)
        {
            Debug.LogError("Ship model not set in RailShooterPlayer");
            return;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }
    
    
    private void HandleMovement()
    {
        targetMovePosition = new Vector3(horizontalInput, verticalInput, 0) * (moveSpeed * Time.deltaTime);
        
        transform.Translate(targetMovePosition, Space.World);
        
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -boundaryX, boundaryX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -boundaryY, boundaryY);
        transform.position = clampedPosition;
    }
    
    
    private void HandleRotation()
    {
        Vector3 aimDirection = aimingScript.GetAimDirection();
        
        float yawAngle = Mathf.Atan2(aimDirection.x, aimDirection.z) * Mathf.Rad2Deg;
        float pitchAngle = -Mathf.Asin(aimDirection.y) * Mathf.Rad2Deg;
        
        yawAngle = Mathf.Clamp(yawAngle, -maxYawAngle, maxYawAngle);
        pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
        
        float bankAngle = -horizontalInput * tiltAmount * 0.5f;
        
        Quaternion targetRotation = Quaternion.Euler(pitchAngle, yawAngle, bankAngle);
        
        shipModel.localRotation = Quaternion.Slerp(shipModel.localRotation, targetRotation, aimRotationSpeed * Time.deltaTime);
    }

    
    
    

    #region Editor -------------------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        // Draw boundaries
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(-boundaryX, -boundaryY, 0), new Vector3(-boundaryX, boundaryY, 0));
        Gizmos.DrawLine(new Vector3(boundaryX, -boundaryY, 0), new Vector3(boundaryX, boundaryY, 0));
        Gizmos.DrawLine(new Vector3(-boundaryX, -boundaryY, 0), new Vector3(boundaryX, -boundaryY, 0));
        Gizmos.DrawLine(new Vector3(-boundaryX, boundaryY, 0), new Vector3(boundaryX, boundaryY, 0));
        UnityEditor.Handles.Label(new Vector3(0, boundaryY + 0.5f, 0), "Player Boundaries");
    }

    #endregion Editor -------------------------------------------------------------------------------------
    
    
    
}