using System;
using KBCore.Refs;
using UnityEngine;


public class RailShooterPlayerAiming : MonoBehaviour
{
    [Header("Aiming Settings")]
    [SerializeField, Min(0)] private float crosshairBoundaryX = 21f;
    [SerializeField, Min(0)] private float crosshairBoundaryY = 12f;
    [SerializeField] private Vector3 crosshairOffset = Vector3.zero;
    
    [Header("Aim Offset")]
    [SerializeField] private bool useAimOffset = true;
    [SerializeField, Min(0)] private float maxOffsetStrength = 5f;
    [SerializeField, Min(0)] private float aimOffsetSmoothing = 2f;
    
    [Header("Movement Offset")]
    [SerializeField] private bool useMovementOffset = true;
    [SerializeField, Min(0)] private float movementOffsetStrength = 11f;
    [SerializeField, Min(0)] private float movementOffsetSmoothing = 5f;
    
    
    [Header("References")]
    [SerializeField, Self] private RailShooterPlayerInput playerInput;
    [SerializeField, Self] private RailShooterPlayerMovement playerMovement;
    [SerializeField] private Transform crosshair; 
    
    
    private Vector3 aimDirection;
    private Vector3 crosshairWorldPosition;
    private Vector3 mouseOffset;
    private Vector3 movementOffset;


    private void OnValidate()
    {
        if (crosshair)
        {
            crosshair.position = transform.position + crosshairOffset;
        }
    }

    private void Start()
    {
        if (crosshair)
        {
            crosshairWorldPosition = transform.position + Vector3.forward * 10f;
            UpdateAimPosition();
        }
    }
    
    private void Update()
    {
        HandleMovementOffset();
        HandleAimingOffset();
        UpdateAimPosition();
    }
    
    
    private void UpdateAimPosition()
    {
        
        aimDirection = (crosshairWorldPosition - transform.position).normalized;
        crosshairWorldPosition = transform.position + mouseOffset + movementOffset + crosshairOffset;
        
        Vector3 clampedPosition = crosshairWorldPosition;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -crosshairBoundaryX, crosshairBoundaryX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -crosshairBoundaryY, crosshairBoundaryY);
        crosshairWorldPosition = clampedPosition;
        
        if (!crosshair) return;
        
        crosshair.position = crosshairWorldPosition;
    }
    
    

    private void HandleMovementOffset()
    {
        if (!useMovementOffset)
        {
            movementOffset = Vector3.zero;
            return;
        }
        
        Vector2 movementInput = playerInput.MovementInput;
        
        Vector3 targetOffset = new Vector3(
            movementInput.x * movementOffsetStrength,
            movementInput.y * movementOffsetStrength,
            0
        );
        
        movementOffset = Vector3.Lerp(movementOffset, targetOffset, movementOffsetSmoothing * Time.deltaTime);
    }
    
    private void HandleAimingOffset()
    {

        if (!useAimOffset)
        {
            mouseOffset = Vector3.zero;
            return;
        }
        
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        Vector3 currentMousePosition = playerInput.MousePosition;
        
        Vector3 mouseFromCenter = currentMousePosition - screenCenter;
        
        Vector2 normalizedMouse = new Vector2(
            mouseFromCenter.x / (Screen.width * 0.5f),
            mouseFromCenter.y / (Screen.height * 0.5f)
        );
        
        normalizedMouse.x = Mathf.Clamp(normalizedMouse.x, -1f, 1f);
        normalizedMouse.y = Mathf.Clamp(normalizedMouse.y, -1f, 1f);
        
        Vector3 targetOffset = new Vector3(
            normalizedMouse.x * maxOffsetStrength,
            normalizedMouse.y * maxOffsetStrength,
            0
        );
        
        mouseOffset = Vector3.Lerp(mouseOffset, targetOffset, aimOffsetSmoothing * Time.deltaTime);
    }
    

    
    
    #region Public -------------------------------------------------------------------------

    public Vector3 GetAimDirection()
    {
        return aimDirection;
    }
    
    public Vector3 GetAimPosition()
    {
        return crosshairWorldPosition;
    }
    
    
    public bool IsAimingAtTarget(Transform target, float tolerance = 1f)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float dot = Vector3.Dot(aimDirection, directionToTarget);
        return dot > (1f - tolerance);
    }
    
    

    #endregion Public -------------------------------------------------------------------------
    
    

    #region Editor -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        
        // Aim direction ray
        Gizmos.DrawRay(transform.position, aimDirection * 10f);
        
        
        // Draw boundaries
        if (crosshair)
        {
            Gizmos.DrawLine(new Vector3(-crosshairBoundaryX, -crosshairBoundaryY, crosshair.position.z), new Vector3(-crosshairBoundaryX, crosshairBoundaryY, crosshair.position.z));
            Gizmos.DrawLine(new Vector3(crosshairBoundaryX, -crosshairBoundaryY, crosshair.position.z), new Vector3(crosshairBoundaryX, crosshairBoundaryY, crosshair.position.z));
            Gizmos.DrawLine(new Vector3(-crosshairBoundaryX, -crosshairBoundaryY, crosshair.position.z), new Vector3(crosshairBoundaryX, -crosshairBoundaryY, crosshair.position.z));
            Gizmos.DrawLine(new Vector3(-crosshairBoundaryX, crosshairBoundaryY, crosshair.position.z), new Vector3(crosshairBoundaryX, crosshairBoundaryY, crosshair.position.z));
            UnityEditor.Handles.Label(new Vector3(0, crosshairBoundaryY + 0.5f, crosshair.position.z), "Crosshair Boundaries");
        }

    }

    #endregion Editor -------------------------------------------------------------------------
    

}