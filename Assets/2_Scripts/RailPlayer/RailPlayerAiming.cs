using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using VInspector;

public class RailPlayerAiming : MonoBehaviour
{
    [Header("Aim Settings")]
    [SerializeField, Tooltip("Controls how input magnitude maps to sensitivity")] private AnimationCurve magnitudeToSensitivityCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField, Min(0.1f), Tooltip("Base speed multiplier for crosshair movement")] private float baseSensitivity = 1f;
    [SerializeField, Range(0f, 1f), Tooltip("Reduces sensitivity near boundaries to prevent wall sliding (1 = no slowdown, 0 = full slowdown)")] private float edgeSlowdown = 0.3f;
    [SerializeField, Tooltip("Use screen-relative input for consistent feel across different resolutions")] private bool useScreenSpaceInput;
    [SerializeField, ShowIf("useScreenSpaceInput"), Tooltip("Screen pixel equivalent for mouse movement normalization")] private Vector2 screenSensitivity = new Vector2(800f, 600f);[EndIf]
    
    [Header("Crosshairs")]
    [SerializeField, Tooltip("How fast the crosshair smoothly moves to its target position")] private float crosshairFollowSpeed = 25f;
    [SerializeField] private float crosshairsGrowSpeed = 5f;
    [SerializeField, Range(0f, 1f)] private float smallCrosshairRange = 0.8f;
    
    [Header("Auto Center")]
    [SerializeField] private bool autoCenter = true;
    [EnableIf("autoCenter")]
    [SerializeField, Min(0)] private float autoCenterDelay = 5f;
    [SerializeField, Min(0)] private float autoCenterSpeed = 1f;
    [EndIf]
    
    [Header("References")]
    [SerializeField] private Transform crosshair;
    [SerializeField] private Transform smallCrosshair;
    [SerializeField] private Transform targetCrosshair;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;


    private float _noInputTimer;
    private Vector2 _processedLookInput;
    private Vector2 _normalizedCrosshairPosition;
    private Vector3 _aimDirection;
    private Vector3 _crosshairWorldPosition;
    private Quaternion _splineRotation = Quaternion.identity;
    private ChickenController _currentAimLockTarget;
    private bool _isAimLocked;
    private float _aimLockCooldownTimer;
    private float CrosshairBoundaryX => LevelManager.Instance ? LevelManager.Instance.EnemyBoundary.x : 25f;
    private float CrosshairBoundaryY => LevelManager.Instance ? LevelManager.Instance.EnemyBoundary.y : 15f;
    private bool AllowAiming => player.IsAlive() && (!LevelManager.Instance || !LevelManager.Instance.CurrentStage ||
                                                     LevelManager.Instance.CurrentStage.AllowPlayerAim);
    
    
    
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
        HandleAimLock();
        ProcessAimingInput();
        HandleAutoCenter();
        UpdateAimPosition();
    }
    

    #region Input Processing --------------------------------------------------------------------------------------------------------

    private void OnProcessedLook(Vector2 processedLookInput)
    {
        if (!AllowAiming) return;
        _processedLookInput = processedLookInput;
    }

    private void ProcessAimingInput()
    {
        // Skip normal input processing if aim lock is active and controlling the crosshair
        if (_isAimLocked && _processedLookInput.magnitude <= playerInput.CurrentControlScheme.lockAimStrength)
        {
            _processedLookInput = Vector2.zero;
            return;
        }
        
        // If we have strong enough input while aim locked, break the aim lock
        if (_isAimLocked && _processedLookInput.magnitude > playerInput.CurrentControlScheme.lockAimStrength)
        {
            BreakAimLock();
        }

        Vector2 inputDelta = _processedLookInput;
    
        if (useScreenSpaceInput)
        {
            // Convert to screen-relative movement for consistent feel across resolutions
            inputDelta = new Vector2(
                inputDelta.x * screenSensitivity.x / Screen.width,
                inputDelta.y * screenSensitivity.y / Screen.height
            );
        }
        
        float rawInputMagnitude = Mathf.Clamp01(inputDelta.magnitude);
        
        // Apply dead zone
        if (inputDelta.magnitude < playerInput.CurrentControlScheme.deadZone)
        {
            inputDelta = Vector2.zero;
        }
        else
        {
            // Normalize past dead zone
            float normalizedMagnitude = (inputDelta.magnitude - playerInput.CurrentControlScheme.deadZone) / (1f - playerInput.CurrentControlScheme.deadZone);
            inputDelta = inputDelta.normalized * normalizedMagnitude;
        }
        
    
        // Apply magnitude to sensitivity curve for 1:1 movement feel
        if (inputDelta.magnitude > 0)
        {
            float originalMagnitude = inputDelta.magnitude;
            float curvedSensitivity = magnitudeToSensitivityCurve.Evaluate(rawInputMagnitude );
            inputDelta = inputDelta.normalized * (originalMagnitude * curvedSensitivity * baseSensitivity * playerInput.CurrentControlScheme.aimSensitivity);
        }
    
        
        // Apply edge slowdown to prevent wall sliding
        Vector2 edgeDistance = new Vector2(
            1f - Mathf.Abs(_normalizedCrosshairPosition.x),
            1f - Mathf.Abs(_normalizedCrosshairPosition.y)
        );
    
        Vector2 edgeMultiplier = new Vector2(
            Mathf.Lerp(edgeSlowdown, 1f, edgeDistance.x),
            Mathf.Lerp(edgeSlowdown, 1f, edgeDistance.y)
        );
    
        inputDelta.x *= edgeMultiplier.x;
        inputDelta.y *= edgeMultiplier.y;
    
        // Update normalized position
        _normalizedCrosshairPosition += inputDelta * Time.deltaTime;
        _normalizedCrosshairPosition.x = Mathf.Clamp(_normalizedCrosshairPosition.x, -1f, 1f);
        _normalizedCrosshairPosition.y = Mathf.Clamp(_normalizedCrosshairPosition.y, -1f, 1f);
    
        // Store the final processed input delta for debugging
        _processedLookInput = inputDelta;
    }

    #endregion Input Processing --------------------------------------------------------------------------------------------------------

    
    #region Aim Lock --------------------------------------------------------------------------------------------------------

    private void HandleAimLock()
    {
        if (!playerInput.CurrentControlScheme.aimLock || !AllowAiming)
        {
            if (_isAimLocked)
            {
                BreakAimLock();
            }
            return;
        }
        
        // Update cooldown timer
        if (_aimLockCooldownTimer > 0)
        {
            _aimLockCooldownTimer -= Time.deltaTime;
        }
        
        // Check if we should start aim lock
        if (!_isAimLocked && _aimLockCooldownTimer <= 0 && _noInputTimer > 0.1f) // Small delay to avoid instant lock
        {
            TryStartAimLock();
        }
        
        // Handle active aim lock
        if (_isAimLocked)
        {
            HandleActiveAimLock();
        }
    }
    
    private void TryStartAimLock()
    {
        ChickenController target = GetEnemyTarget(playerInput.CurrentControlScheme.aimLockRadius);
        if (target)
        {
            _currentAimLockTarget = target;
            _isAimLocked = true;
        }
    }
    
    private void HandleActiveAimLock()
    {
        // Check if target is still valid
        if (!_currentAimLockTarget || !_currentAimLockTarget.gameObject.activeInHierarchy)
        {
            BreakAimLock();
            return;
        }
        
        // Check if target is still within range
        float distanceToTarget = Vector3.Distance(_crosshairWorldPosition, _currentAimLockTarget.transform.position);
        if (distanceToTarget > playerInput.CurrentControlScheme.aimLockRadius * 1.2f) // Add some hysteresis to prevent flickering
        {
            BreakAimLock();
            return;
        }
        
        // Move crosshair towards target
        Vector3 targetWorldPosition = _currentAimLockTarget.transform.position;
        
        // Convert target world position to normalized crosshair position
        Vector3 boundaryCenter = GetCrosshairSplinePosition();
        Vector3 localTargetOffset = targetWorldPosition - boundaryCenter;
        
        // Apply inverse spline rotation if enabled
        if (player.AlignToSplineDirection)
        {
            localTargetOffset = Quaternion.Inverse(_splineRotation) * localTargetOffset;
        }
        
        Vector2 targetNormalizedPosition = new Vector2(
            Mathf.Clamp(localTargetOffset.x / CrosshairBoundaryX, -1f, 1f),
            Mathf.Clamp(localTargetOffset.y / CrosshairBoundaryY, -1f, 1f)
        );
        
        // Lerp towards target position
        _normalizedCrosshairPosition = Vector2.Lerp(
            _normalizedCrosshairPosition,
            targetNormalizedPosition,
            playerInput.CurrentControlScheme.lockAimSpeed * Time.deltaTime
        );
    }
    
    private void BreakAimLock()
    {
        _isAimLocked = false;
        _currentAimLockTarget = null;
        _aimLockCooldownTimer = playerInput.CurrentControlScheme.lockAimCooldown;
    }

    #endregion Aim Lock --------------------------------------------------------------------------------------------------------

    
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

    private void UpdateAimPosition()
    {
        Vector3 boundaryCenter = GetCrosshairSplinePosition();

        // Convert normalized position (-1 to 1) to world position within boundaries
        Vector3 localOffset = new Vector3(
            _normalizedCrosshairPosition.x * CrosshairBoundaryX,
            _normalizedCrosshairPosition.y * CrosshairBoundaryY,
            0
        );

        // Apply spline rotation if enabled
        if (player.AlignToSplineDirection)
        {
            localOffset = _splineRotation * localOffset;
        }

        // Calculate final world position
        _crosshairWorldPosition = boundaryCenter + localOffset;
       
        // Update aim direction with smoothing
        _aimDirection = Vector3.Lerp(_aimDirection, (_crosshairWorldPosition - transform.position).normalized, crosshairFollowSpeed * Time.deltaTime);

        // Update crosshair visual
        if (crosshair)
        {
            crosshair.position = Vector3.Lerp(crosshair.position, _crosshairWorldPosition, crosshairFollowSpeed * Time.deltaTime);
            crosshair.rotation = player.AlignToSplineDirection ? _splineRotation : Quaternion.identity;
            
            if (smallCrosshair)
            {
                // Place the second crosshair between transform and the first crosshair
                Vector3 secondCrosshairPosition = Vector3.Lerp(transform.position, _crosshairWorldPosition, smallCrosshairRange);
                smallCrosshair.position = Vector3.Lerp(smallCrosshair.position, secondCrosshairPosition, crosshairFollowSpeed * Time.deltaTime);
                smallCrosshair.rotation = player.AlignToSplineDirection ? _splineRotation : Quaternion.identity;
            }

            if (targetCrosshair)
            {
                // Place the third crosshair between transform and the first crosshair
                Vector3 targetCrosshairSize = _currentAimLockTarget ? (Vector3.one * 2) : Vector3.one; 
                targetCrosshair.localScale = Vector3.Lerp(targetCrosshair.localScale, targetCrosshairSize, crosshairsGrowSpeed * Time.deltaTime);
                targetCrosshair.position = crosshair.position;
                targetCrosshair.rotation = player.AlignToSplineDirection ? _splineRotation : Quaternion.identity;

                Vector3 crosshairSize;

                if (_noInputTimer <= 0)
                {
                    crosshairSize = Vector3.one * 1.5f;
                }
                else
                {
                    crosshairSize = _currentAimLockTarget ? (Vector3.one / 2) : Vector3.one; 
                }

                crosshair.localScale = Vector3.Lerp(crosshair.localScale, crosshairSize, crosshairsGrowSpeed * Time.deltaTime);
            }
        }
    }
    
    private void HandleAutoCenter()
    {
        // Don't auto-center if aim lock is active
        if (!autoCenter || _isAimLocked) return;
    
        bool hasInput = _processedLookInput.magnitude > 0.01f;
    
        if (hasInput)
        {
            _noInputTimer = 0f;
        }
        else
        {
            _noInputTimer += Time.deltaTime;
        
            if (_noInputTimer >= autoCenterDelay)
            {
                // Return to center in normalized space for smooth, consistent centering
                _normalizedCrosshairPosition = Vector2.Lerp(
                    _normalizedCrosshairPosition, 
                    Vector2.zero, 
                    autoCenterSpeed * Time.deltaTime
                );
            }
        }
    }

    #endregion Aiming --------------------------------------------------------------------------------------------------------

    
    #region Helper Methods -------------------------------------------------------------------------
    
    public ChickenController GetEnemyTarget(float radius)
    {
        Dictionary<ChickenController, float> enemyDistances = new Dictionary<ChickenController, float>();
        Collider[] hitColliders = Physics.OverlapSphere(_crosshairWorldPosition, radius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ChickenController enemy))
            {
                float distance = Vector3.Distance(_crosshairWorldPosition, enemy.transform.position);
                enemyDistances[enemy] = distance;
            }
        }
        
        if (enemyDistances.Count > 0)
        {
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
            
            return closestEnemy;
        }
        
        return null; 
    }
    
    public ChickenController[] GetEnemyTargets(int maxTargets, float radius)
    {
        Dictionary<ChickenController, float> enemyDistances = new Dictionary<ChickenController, float>();
        Collider[] hitColliders = Physics.OverlapSphere(_crosshairWorldPosition, radius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ChickenController enemy))
            {
                float distance = Vector3.Distance(_crosshairWorldPosition, enemy.transform.position);
                enemyDistances[enemy] = distance;
            }
        }
        
        List<ChickenController> sortedEnemies = new List<ChickenController>(enemyDistances.Keys);
        sortedEnemies.Sort((a, b) => enemyDistances[a].CompareTo(enemyDistances[b]));
        
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
            Vector3 parallelDirection = (_crosshairWorldPosition - transform.position).normalized;
            return parallelDirection;
        }
        else
        {
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
    
    // Public methods to check aim lock state
    public bool IsAimLocked => _isAimLocked;
    public ChickenController CurrentAimLockTarget => _currentAimLockTarget;
    public float AimLockCooldownRemaining => _aimLockCooldownTimer;

    #endregion Helper Methods -------------------------------------------------------------------------

    
    #region Editor -------------------------------------------------------------------------
    #if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        // Draw boundaries from spline position 
        if (LevelManager.Instance)
        {
            Gizmos.color = Color.blue;
            Vector3 crosshairSplinePosition = GetCrosshairSplinePosition();
            
            if (player && player.AlignToSplineDirection)
            {
                // Draw rotated boundaries based on spline rotation
                Vector3[] localCorners = new Vector3[]
                {
                    new Vector3(-CrosshairBoundaryX, -CrosshairBoundaryY, 0),
                    new Vector3(CrosshairBoundaryX, -CrosshairBoundaryY, 0),
                    new Vector3(CrosshairBoundaryX, CrosshairBoundaryY, 0),
                    new Vector3(-CrosshairBoundaryX, CrosshairBoundaryY, 0)
                };
                
                Vector3[] worldCorners = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    worldCorners[i] = crosshairSplinePosition + (_splineRotation * localCorners[i]);
                }
                
                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;
                    Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
                }
                
                
                if (Application.isPlaying)
                {
                    string debugText = $"Normalized Position: ({_normalizedCrosshairPosition.x:F2}, {_normalizedCrosshairPosition.y:F2})";
                    if (_isAimLocked && _currentAimLockTarget)
                    {
                        debugText += $"\nAim Locked: {_currentAimLockTarget.name}";
                    }
                    else if (_aimLockCooldownTimer > 0)
                    {
                        debugText += $"\nCooldown: {_aimLockCooldownTimer:F1}s";
                    }

                    debugText += $"\nCrosshair Boundaries";
                
                    UnityEditor.Handles.Label(crosshairSplinePosition + (_splineRotation * Vector3.up * (CrosshairBoundaryY + 0.5f)), debugText);
                }
            }
            else
            {
                // Draw simple rectangular boundaries
                Gizmos.DrawWireCube(crosshairSplinePosition, new Vector3(CrosshairBoundaryX * 2, CrosshairBoundaryY * 2, 0));
            }
            
            
            // Draw aim lock radius
            if (playerInput.CurrentControlScheme.aimLock)
            {
                Gizmos.color = _isAimLocked ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(_crosshairWorldPosition, playerInput.CurrentControlScheme.aimLockRadius);
            }
        }
    }
    
    #endif 
    #endregion Editor -------------------------------------------------------------------------
}