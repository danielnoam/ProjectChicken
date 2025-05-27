using System;
using KBCore.Refs;
using UnityEngine;


public class RailPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float moveSpeed = 15f;
    [SerializeField, Min(0)] private float boundaryX = 10f;
    [SerializeField, Min(0)] private float boundaryY = 6f;
    
    [Header("Rotation Settings")]
    [SerializeField, Min(0)] private float rotationSpeed = 22f;
    [SerializeField, Min(0)] private float movementTiltAmount = 30f;
    [SerializeField, Min(0)] private float maxPitchAngle = 30f;
    [SerializeField, Min(0)] private float maxYawAngle = 45f;
    
    [Header("Path Settings")]
    [SerializeField] private float pathFollowSpeed = 5f;
    [SerializeField] private Vector3 pathOffset = Vector3.zero;
    
    [Header("References")] 
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField] private Transform shipModel;
    
    private Vector3 _targetMovePosition;
    private Vector3 _targetPathPosition;
    private float horizontalInput => playerInput.MovementInput.x;
    private float verticalInput => playerInput.MovementInput.y;

    private void Awake()
    {
        if (!shipModel)
        {
            Debug.LogError("Ship model not set in RailShooterPlayerMovement");
            return;
        }
    }
    

    private void Update()
    {
        HandleMovement();
        HandleShipRotation();
    }
    
    
    private void HandleMovement()
    {
        // Accumulate player input as offset from path
        Vector3 inputMovement = new Vector3(horizontalInput, verticalInput, 0) * (moveSpeed * Time.deltaTime);
        _targetMovePosition += inputMovement;
    
        // Clamp the offset within boundaries
        _targetMovePosition.x = Mathf.Clamp(_targetMovePosition.x, -boundaryX, boundaryX);
        _targetMovePosition.y = Mathf.Clamp(_targetMovePosition.y, -boundaryY, boundaryY);

        // Handle the path following
        if (LevelManager.Instance && LevelManager.Instance.LevelPath)
        {
            Vector3 targetPosition = LevelManager.Instance.CurrentPositionOnPath.position + pathOffset;
            _targetPathPosition = Vector3.Lerp(_targetPathPosition, targetPosition, pathFollowSpeed * Time.deltaTime);
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
        
        float yawAngle = Mathf.Atan2(aimDirection.x, aimDirection.z) * Mathf.Rad2Deg;
        float pitchAngle = -Mathf.Asin(aimDirection.y) * Mathf.Rad2Deg;
        
        yawAngle = Mathf.Clamp(yawAngle, -maxYawAngle, maxYawAngle);
        pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
        
        float bankAngle = -horizontalInput * movementTiltAmount * 0.5f;
        
        Quaternion targetRotation = Quaternion.Euler(pitchAngle, yawAngle, bankAngle);
        
        shipModel.localRotation = Quaternion.Slerp(shipModel.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
    }


    
    
    

    #region Editor -------------------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        // Draw boundaries
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + new Vector3(-boundaryX, -boundaryY, 0), transform.position +  new Vector3(-boundaryX, boundaryY, 0));
        Gizmos.DrawLine(transform.position + new Vector3(boundaryX, -boundaryY, 0), transform.position + new Vector3(boundaryX, boundaryY, 0));
        Gizmos.DrawLine(transform.position + new Vector3(-boundaryX, -boundaryY, 0), transform.position + new Vector3(boundaryX, -boundaryY, 0));
        Gizmos.DrawLine(transform.position + new Vector3(-boundaryX, boundaryY, 0), transform.position + new Vector3(boundaryX, boundaryY, 0));
        UnityEditor.Handles.Label(new Vector3(0, boundaryY + 0.5f, 0), "Player Boundaries");
    }

    #endregion Editor -------------------------------------------------------------------------------------
    
    
    
}