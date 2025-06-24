using System.Collections.Generic;
using KBCore.Refs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

public class RailPlayerAiming : MonoBehaviour
{
    [Header("Aiming Settings")]
    [SerializeField] private float aimSpeed = 15f;
    [SerializeField, Min(0)] private float lookOffsetStrength = 35f;
    [SerializeField, Min(0)] private float lookOffsetSmoothing = 1.4f;

    [Header("Auto Center Settings")]
    [SerializeField] private bool autoCenter = true;
    [EnableIf("autoCenter")]
    [SerializeField, Min(0)] private float autoCenterDelay = 5f;
    [SerializeField, Min(0)] private float autoCenterSpeed = 1f;
    [EndIf]
    
    [Header("References")]
    [SerializeField] private Transform crosshair;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;


    private Vector2 _movementInput;
    private Vector2 _lookInput;
    private Vector2 _smoothedLookInput;
    private Vector2 _smoothedMovementInput;
    private Vector3 _lookOffset;
    private Vector3 _movementOffset;
    private Vector3 _aimDirection;
    private Vector3 _crosshairWorldPosition;
    private Quaternion _splineRotation = Quaternion.identity;
    private float _noInputTimer;
    private float CrosshairBoundaryX => LevelManager.Instance ? LevelManager.Instance.EnemyBoundary.x : 25f;
    private float CrosshairBoundaryY => LevelManager.Instance ? LevelManager.Instance.EnemyBoundary.y :  15f;
    private bool AllowAiming => !LevelManager.Instance || !LevelManager.Instance.CurrentStage || LevelManager.Instance.CurrentStage.AllowPlayerAim;
    
    
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
        playerInput.OnProcessedLookEvent += OnProcessedLook;
    }
    
    private void OnDisable()
    {
        playerInput.OnProcessedLookEvent -= OnProcessedLook;
    }

    private void Update()
    {
        HandleSplineRotation();
        HandleLookOffset();
        HandleAutoCenter();
        UpdateAimPosition();
    }
    
    
    


    #region Aiming --------------------------------------------------------------------------------------------------------

        private void HandleSplineRotation()
    {
        if (!player.AlignToSplineDirection || !LevelManager.Instance)
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
        
    
    private void HandleLookOffset()
    {
        _lookOffset = Vector3.Lerp(_lookOffset, Vector3.zero, lookOffsetSmoothing * Time.deltaTime);

        bool hasLookInput = _lookInput.magnitude > 0.01f;

        if (hasLookInput)
        {
            Vector3 targetOffset = new Vector3(
                _lookInput.normalized.x * lookOffsetStrength,
                _lookInput.normalized.y * lookOffsetStrength,
                0
            );
            
            _lookOffset = Vector3.Lerp(_lookOffset, targetOffset, lookOffsetSmoothing * Time.deltaTime);
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
       
       // Lerp aim direction for smoothness
        _aimDirection = Vector3.Lerp(_aimDirection, (_crosshairWorldPosition - transform.position).normalized, aimSpeed * Time.deltaTime);


       // Update crosshair position and rotation
       if (crosshair)
       {
           // Lerp the crosshair rotation for smoothness
            crosshair.position = Vector3.Lerp(crosshair.position, _crosshairWorldPosition, aimSpeed * Time.deltaTime);
           
           // Rotate crosshair to match boundary rotation
           crosshair.rotation = player.AlignToSplineDirection ? _splineRotation : Quaternion.identity;
       }
    }
    
    
    private void HandleAutoCenter()
    {
        if (!autoCenter) return;
    
        // Check if we have any input (look or movement)
        bool hasLookInput = _lookInput.magnitude > 0.01f;
        bool hasMovementInput = _movementInput.magnitude > 0.01f;
        bool hasAnyInput = hasLookInput || hasMovementInput;
    
        if (hasAnyInput)
        {
            // Reset the timer when we have any input
            _noInputTimer = 0f;
        }
        else
        {
            // Increment timer when no input is detected
            _noInputTimer += Time.deltaTime;
        
            // Start centering after the delay has passed
            if (_noInputTimer >= autoCenterDelay)
            {
                _lookOffset = Vector3.Lerp(_lookOffset, Vector3.zero, autoCenterSpeed * Time.deltaTime);
            }
        }
    }

    #endregion Aiming --------------------------------------------------------------------------------------------------------
    
    
    
    #region Input --------------------------------------------------------------------------------------------------------

    private void OnProcessedLook(Vector2 processedLookInput)
    {
        if (!AllowAiming) return;
        
        _lookInput = processedLookInput;
    }
    

    #endregion Input --------------------------------------------------------------------------------------------------------



    #region Helper -------------------------------------------------------------------------
    
    public ChickenController GetEnemyTarget(float radius)
    {
        // Create a dictionary to store distances to each ChickenEnemy
        Dictionary<ChickenController, float> enemyDistances = new Dictionary<ChickenController, float>();
        
        // Create a sphere cast to detect all colliders
        Collider[] hitColliders = Physics.OverlapSphere(_crosshairWorldPosition, radius);
        
        // Check each collider for ChickenEnemy
        foreach (Collider hitCollider in hitColliders)
        {
            // Try to get ChickenEnemy component
            if (hitCollider.TryGetComponent(out ChickenController enemy))
            {
                // Calculate distance from crosshair to chicken
                float distance = Vector3.Distance(_crosshairWorldPosition, enemy.transform.position);
                
                // Store the distance in the dictionary
                enemyDistances[enemy] = distance;
            }
        }
        
        // return the closest ChickenEnemy
        if (enemyDistances.Count > 0)
        {
            // Find the ChickenEnemy with the minimum distance
            ChickenController closestEnemy = null;
            float minDistance = float.MaxValue;
            
            foreach (var kvp in enemyDistances)
            {
                if (kvp.Value < minDistance)
                {
                    minDistance = kvp.Value;
                    closestEnemy = kvp.Key;
                }
            }
            
            return closestEnemy; // Return the closest enemy found
        }
        
        
        return null; 
    }
    
    public ChickenController[] GetEnemyTargets(int maxTargets, float radius)
    {
        // Create a list to store detected ChickenEnemies and order them by distance
        Dictionary<ChickenController, float> enemyDistances = new Dictionary<ChickenController, float>();
        
        // Create a sphere cast to detect all colliders
        Collider[] hitColliders = Physics.OverlapSphere(_crosshairWorldPosition, radius);
        // Check each collider for ChickenEnemy
        foreach (Collider hitCollider in hitColliders)
        {
            // Try to get ChickenEnemy component
            if (hitCollider.TryGetComponent(out ChickenController enemy))
            {
                // Calculate distance from crosshair to chicken
                float distance = Vector3.Distance(_crosshairWorldPosition, enemy.transform.position);
                
                // Store the distance in the dictionary
                enemyDistances[enemy] = distance;
            }
        }
        
        // Sort the ChickenEnemies by distance
        List<ChickenController> sortedEnemies = new List<ChickenController>(enemyDistances.Keys);
        sortedEnemies.Sort((a, b) => enemyDistances[a].CompareTo(enemyDistances[b]));
        
        
        // Return the closest ChickenEnemies up to maxTargets
        int targetCount = Mathf.Min(maxTargets, sortedEnemies.Count);
        ChickenController[] targets = new ChickenController[targetCount];
        for (int i = 0; i < targetCount; i++)
        {
            targets[i] = sortedEnemies[i];
        }
        
        return targets;
        
    }
    
    public Vector3 GetAimDirection()
    {
        return _aimDirection;
    }
    
    public Vector3 GetAimDirectionFromBarrelPosition(Vector3 position, float convergenceMultiplier = 0f)
    {
        if (convergenceMultiplier == 0f)
        {
            // Parallel shooting - both barrels shoot in the same direction
            Vector3 parallelDirection = (_crosshairWorldPosition - transform.position).normalized;
            return parallelDirection;
        }
        else
        {
            // Converging shooting - aim directly at the convergence point
            Vector3 baseCrosshairDirection = (_crosshairWorldPosition - transform.position).normalized;
            float crosshairDistance = Vector3.Distance(transform.position, _crosshairWorldPosition);
            Vector3 convergencePoint = transform.position + (baseCrosshairDirection * (crosshairDistance * convergenceMultiplier));
        
            return (convergencePoint - position).normalized;
        }
    }
    
    
    private Vector3 GetSplineDirection()
    {
        return !LevelManager.Instance ? Vector3.forward : LevelManager.Instance.GetDirectionOnSpline(LevelManager.Instance.CurrentPositionOnPath.position);
    }
    
    private Vector3 GetCrosshairSplinePosition()
    {
        if (!LevelManager.Instance) return transform.position;
        
        return LevelManager.Instance.EnemyPosition;
    }
    


    #endregion Helper -------------------------------------------------------------------------

    
    
    #region Editor -------------------------------------------------------------------------
    #if UNITY_EDITOR

    
    private void OnDrawGizmos()
    {
        // Draw boundaries from spline position 
        if (LevelManager.Instance)
        {
            Gizmos.color = Color.blue;
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
    
    #endif 
    #endregion Editor -------------------------------------------------------------------------
}