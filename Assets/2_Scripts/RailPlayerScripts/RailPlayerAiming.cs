using System;
using KBCore.Refs;
using Unity.Mathematics;
using UnityEngine;
using VInspector;

public class RailPlayerAiming : MonoBehaviour
{
    [Header("Aiming Settings")]
    [SerializeField] private bool hideCursor = true;
    
    [Header("Mouse Offset")]
    [SerializeField] private bool useMouseOffset = true;
    [SerializeField, Min(0)] private float mouseOffsetStrength = 25f;
    [SerializeField, Min(0)] private float mouseOffsetSmoothing = 3f;
    
    [Header("Movement Offset")]
    [SerializeField] private bool useMovementOffset = true;
    [SerializeField, Min(0)] private float movementOffsetStrength = 11f;
    [SerializeField, Min(0)] private float movementOffsetSmoothing = 5f;
    
    [Header("References")]
    [SerializeField, Self] private RailPlayer player;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField, Self] private RailPlayerMovement playerMovement;
    [SerializeField] private Transform crosshair; 
    
    
    private Vector3 _aimDirection;
    private Vector3 _crosshairWorldPosition;
    private Vector3 _mouseOffset;
    private Vector3 _movementOffset;
    private Quaternion _splineRotation = Quaternion.identity;
    private readonly float _crosshairBoundaryX = LevelManager.Instance ? LevelManager.Instance.EnemyBoundary.x : 25f;
    private readonly float _crosshairBoundaryY = LevelManager.Instance ? LevelManager.Instance.EnemyBoundary.y :  15f;
    private readonly float _pathOffset = LevelManager.Instance ? LevelManager.Instance.EnemyOffset : 10f;

    private void Awake()
    {
        if (hideCursor)
        {
            ToggleCursorVisibility();
        }
    }

    private void Start()
    {
        if (crosshair)
        {
            _crosshairWorldPosition = transform.position + Vector3.forward * 10f;
            UpdateAimPosition();
        }
    }
    
    private void Update()
    {
        HandleSplineRotation();
        HandleMovementOffset();
        HandleMouseOffset();
        UpdateAimPosition();
    }
    
    private void HandleSplineRotation()
    {
        if (!player.AlignToSplineDirection || !LevelManager.Instance || !LevelManager.Instance.LevelPath)
        {
            _splineRotation = Quaternion.identity;
            return;
        }

        // Get the spline direction at the crosshair position
        Vector3 splineForward = GetCrosshairSplineDirection();
        
        if (splineForward != Vector3.zero)
        {
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineForward, Vector3.up);
            _splineRotation = Quaternion.Slerp(_splineRotation, targetSplineRotation, player.SplineRotationSpeed * Time.deltaTime);
        }
    }
    
    private Vector3 GetCrosshairSplineDirection()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.LevelPath) 
            return Vector3.forward;
        
        float crosshairSplineT = GetCrosshairSplineT();
        var tangent = LevelManager.Instance.LevelPath.EvaluateTangent(crosshairSplineT);
        return math.normalize(tangent);
    }
    
    private float GetCrosshairSplineT()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.LevelPath)
            return 0f;
            
        float offsetT = _pathOffset / LevelManager.Instance.LevelPath.CalculateLength();
        float crosshairSplineT = LevelManager.Instance.GetCurrentSplineT() + offsetT;
        return Mathf.Clamp01(crosshairSplineT);
    }
    
    private Vector3 GetCrosshairSplinePosition()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.LevelPath)
            return transform.position;
            
        float crosshairSplineT = GetCrosshairSplineT();
        return LevelManager.Instance.LevelPath.EvaluatePosition(crosshairSplineT);
    }
    
    private void UpdateAimPosition()
    {
       Vector3 boundaryCenter = GetCrosshairSplinePosition();

       // Calculate offsets in the local spline space
       Vector3 localMouseOffset = _mouseOffset;
       Vector3 localMovementOffset = _movementOffset;
       
       if (player.AlignToSplineDirection)
       {
           // Transform offsets to world space using spline rotation
           localMouseOffset = _splineRotation * _mouseOffset;
           localMovementOffset = _splineRotation * _movementOffset;
       }

       // Calculate target position in world space
       _crosshairWorldPosition = boundaryCenter + localMouseOffset + localMovementOffset;

       // Apply boundary clamping
       if (player.AlignToSplineDirection)
       {
           // Transform to local spline space for clamping
           Vector3 localOffset = Quaternion.Inverse(_splineRotation) * (_crosshairWorldPosition - boundaryCenter);
           
           // Clamp in the local spline space
           localOffset.x = Mathf.Clamp(localOffset.x, -_crosshairBoundaryX, _crosshairBoundaryX);
           localOffset.y = Mathf.Clamp(localOffset.y, -_crosshairBoundaryY, _crosshairBoundaryY);
           
           // Transform back to world space
           _crosshairWorldPosition = boundaryCenter + (_splineRotation * localOffset);
       }
       else
       {
           // Traditional world-space clamping
           _crosshairWorldPosition.x = Mathf.Clamp(_crosshairWorldPosition.x, boundaryCenter.x - _crosshairBoundaryX, boundaryCenter.x + _crosshairBoundaryX);
           _crosshairWorldPosition.y = Mathf.Clamp(_crosshairWorldPosition.y, boundaryCenter.y - _crosshairBoundaryY, boundaryCenter.y + _crosshairBoundaryY);
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
        
        Vector2 movementInput = playerInput.MovementInput;
        
        Vector3 targetOffset = new Vector3(
            movementInput.x * movementOffsetStrength,
            movementInput.y * movementOffsetStrength,
            0
        );
        
        _movementOffset = Vector3.Lerp(_movementOffset, targetOffset, movementOffsetSmoothing * Time.deltaTime);
    }
    
    private void HandleMouseOffset()
    {
        if (!useMouseOffset)
        {
            _mouseOffset = Vector3.zero;
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
            normalizedMouse.x * mouseOffsetStrength,
            normalizedMouse.y * mouseOffsetStrength,
            0
        );
        
        _mouseOffset = Vector3.Lerp(_mouseOffset, targetOffset, mouseOffsetSmoothing * Time.deltaTime);
    }
    
    [Button]
    private void ToggleCursorVisibility()
    {
        if (Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    #region Public -------------------------------------------------------------------------

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

    #endregion Public -------------------------------------------------------------------------
    

    #if UNITY_EDITOR
    #region Editor -------------------------------------------------------------------------
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
    
        // Aim direction ray
        Gizmos.DrawLine(transform.position, _crosshairWorldPosition);
    
        // Draw boundaries from the crosshair spline position 
        if (crosshair && LevelManager.Instance && LevelManager.Instance.LevelPath)
        {
            Vector3 crosshairSplinePosition = GetCrosshairSplinePosition();
            
            if (player.AlignToSplineDirection)
            {
                // Draw rotated boundaries based on spline rotation
                Vector3[] localCorners = new Vector3[]
                {
                    new Vector3(-_crosshairBoundaryX, -_crosshairBoundaryY, 0), // Bottom-left
                    new Vector3(_crosshairBoundaryX, -_crosshairBoundaryY, 0),  // Bottom-right
                    new Vector3(_crosshairBoundaryX, _crosshairBoundaryY, 0),   // Top-right
                    new Vector3(-_crosshairBoundaryX, _crosshairBoundaryY, 0)   // Top-left
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
                
                UnityEditor.Handles.Label(crosshairSplinePosition + (_splineRotation * Vector3.up * (_crosshairBoundaryY + 0.5f)), "Crosshair Boundaries (Spline Aligned)");
            }
            else
            {
                // Draw traditional axis-aligned boundaries
                Vector3 bottomLeft = crosshairSplinePosition + new Vector3(-_crosshairBoundaryX, -_crosshairBoundaryY, 0);
                Vector3 bottomRight = crosshairSplinePosition + new Vector3(_crosshairBoundaryX, -_crosshairBoundaryY, 0);
                Vector3 topLeft = crosshairSplinePosition + new Vector3(-_crosshairBoundaryX, _crosshairBoundaryY, 0);
                Vector3 topRight = crosshairSplinePosition + new Vector3(_crosshairBoundaryX, _crosshairBoundaryY, 0);
            
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(bottomLeft, bottomRight);  // Bottom edge
                Gizmos.DrawLine(bottomRight, topRight);    // Right edge  
                Gizmos.DrawLine(topRight, topLeft);        // Top edge
                Gizmos.DrawLine(topLeft, bottomLeft);      // Left edge
                
                UnityEditor.Handles.Label(crosshairSplinePosition + new Vector3(0, _crosshairBoundaryY + 0.5f, 0), "Crosshair Boundaries (World Aligned)");
            }
        }
    }

    #endregion Editor -------------------------------------------------------------------------
    #endif // UNITY_EDITOR
}