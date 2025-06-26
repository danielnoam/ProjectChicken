using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using VInspector;

public class RailPlayerAiming : MonoBehaviour
{
    [Header("Aim Settings")]
    [SerializeField, Min(0.1f), Tooltip("Base speed multiplier for reticle movement")] private float baseSensitivity = 1f;
    [SerializeField, Range(0f, 1f), Tooltip("Reduces sensitivity near boundaries to prevent wall sliding (1 = no slowdown, 0 = full slowdown)")] private float edgeSlowdown = 0.3f;
    [SerializeField, Tooltip("Use screen-relative input for consistent feel across different resolutions")] private bool useScreenSpaceInput;
    [SerializeField, ShowIf("useScreenSpaceInput"), Tooltip("Screen pixel equivalent for mouse movement normalization")] private Vector2 screenSensitivity = new Vector2(800f, 600f);[EndIf]
    
    [Header("Reticles")]
    [SerializeField, Tooltip("How fast the reticle smoothly moves to its target position")] private float reticleFollowSpeed = 25f;
    [SerializeField] private float reticleGrowSpeed = 5f;
    [SerializeField] private float reticleSizeMultiplier = 2f;
    [SerializeField, Range(0f, 1f)] private float smallReticleRange = 0.8f;
    
    [Header("Auto Center")]
    [SerializeField] private bool autoCenter = true;
    [EnableIf("autoCenter")]
    [SerializeField, Min(0)] private float autoCenterDelay = 5f;
    [SerializeField, Min(0)] private float autoCenterSpeed = 1f;
    [EndIf]
    
    [Header("References")]
    [SerializeField] private Transform reticleWorldPosition;
    [SerializeField] private Transform reticlesHolder;
    [SerializeField] private Transform activeReticle;
    [SerializeField] private Transform smallReticle;
    [SerializeField] private Transform targetReticle;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem playerWeapon;


    private bool _allowAiming = true;
    private float _noInputTimer;
    private Vector2 _processedLookInput;
    private Vector2 _normalizedReticlePosition;
    private Vector3 _aimDirection;
    private Quaternion _splineRotation = Quaternion.identity;
    private ChickenController _currentAimLockTarget;
    private bool _isAimLocked;
    private float _aimLockCooldownTimer;
    private float CrosshairBoundaryX => player.LevelManager ? player.LevelManager.EnemyBoundary.x : 25f;
    private float CrosshairBoundaryY => player.LevelManager ? player.LevelManager.EnemyBoundary.y : 15f;

    public Transform ReticleWorldPosition => reticleWorldPosition;
    public Vector2 NormalizedReticlePosition => _normalizedReticlePosition;
    
    private void OnValidate() { this.ValidateRefs(); }

    private void Start()
    {
        if (activeReticle)
        {
            UpdateAimPosition();
        }
    }

    private void OnEnable()
    {
        playerInput.OnProcessedLookEvent += OnProcessedLook;
        playerWeapon.OnSpecialWeaponSwitched += OnSpecialWeaponSwitched;

        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged += OnStageChanged;
        }

    }
    
    private void OnDisable()
    {
        playerInput.OnProcessedLookEvent -= OnProcessedLook;
        playerWeapon.OnSpecialWeaponSwitched -= OnSpecialWeaponSwitched;

        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged -= OnStageChanged;
        }
    }
    

    private void Update()
    {
        HandleSplineRotation();
        HandleAimLock();
        ProcessAimingInput();
        HandleAutoCenter();
        UpdateAimPosition();
        UpdateReticlesPosition();
    }
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        _allowAiming = stage.AllowPlayerAim;
        ToggleReticle(stage.AllowPlayerAim);
    }
    
    private void OnSpecialWeaponSwitched(SOWeapon oldWeapon, SOWeapon newWeapon, WeaponInfo newWeaponInfo)
    {
        if (!newWeapon || newWeaponInfo == null) return;
        
        ChangeActiveReticle(newWeaponInfo.weaponReticle);
    }
    

    #region Input Processing --------------------------------------------------------------------------------------------------------

    private void OnProcessedLook(Vector2 processedLookInput)
    {
        if (!_allowAiming || !player.IsAlive()) return;
        _processedLookInput = processedLookInput;
    }

    private Vector2 _lastSmoothedInput;
    
    private void ProcessAimingInput()
    {
        // Skip normal input processing if aim lock is active and controlling the reticle
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
        
        
        // Apply input smoothing for gamepads to reduce jitter
        if (playerInput.IsCurrentDeviceGamepad)
        {
            float smoothingStrength = 0.15f; // Adjust between 0.1-0.3 (higher = more smooth but less responsive)
            inputDelta = Vector2.Lerp(_lastSmoothedInput, inputDelta, 1f - smoothingStrength);
            _lastSmoothedInput = inputDelta;
        }

        // Don't use screen space input for controllers
        if (useScreenSpaceInput)
        {
            // Convert to screen-relative movement for consistent feel across resolutions
            inputDelta = new Vector2(
                inputDelta.x * screenSensitivity.x / Screen.width,
                inputDelta.y * screenSensitivity.y / Screen.height
            );
        }
        
        float rawInputMagnitude = Mathf.Clamp01(inputDelta.magnitude);
        
        // Apply dead zone with proper radial dead zone for controllers
        float deadZone = playerInput.CurrentControlScheme.deadZone;
        if (inputDelta.magnitude < deadZone)
        {
            inputDelta = Vector2.zero;
        }
        else
        {
            // Scale input to remove dead zone properly
            float scaledMagnitude = (inputDelta.magnitude - deadZone) / (1f - deadZone);
            inputDelta = inputDelta.normalized * scaledMagnitude;
        }
        
        // Apply magnitude to sensitivity curve for 1:1 movement feel
        if (inputDelta.magnitude > 0)
        {
            float originalMagnitude = inputDelta.magnitude;
            float curvedSensitivity = playerInput.CurrentControlScheme.magnitudeToSensitivityCurve.Evaluate(rawInputMagnitude);
            inputDelta = inputDelta.normalized * (originalMagnitude * curvedSensitivity * baseSensitivity * playerInput.CurrentControlScheme.aimSensitivity);
        }
        
        // Apply edge slowdown to prevent wall sliding
        Vector2 edgeDistance = new Vector2(
            1f - Mathf.Abs(_normalizedReticlePosition.x),
            1f - Mathf.Abs(_normalizedReticlePosition.y)
        );

        Vector2 edgeMultiplier = new Vector2(
            Mathf.Lerp(edgeSlowdown, 1f, edgeDistance.x),
            Mathf.Lerp(edgeSlowdown, 1f, edgeDistance.y)
        );

        inputDelta.x *= edgeMultiplier.x;
        inputDelta.y *= edgeMultiplier.y;

        // Update normalized position
        _normalizedReticlePosition += inputDelta * Time.deltaTime;
        _normalizedReticlePosition.x = Mathf.Clamp(_normalizedReticlePosition.x, -1f, 1f);
        _normalizedReticlePosition.y = Mathf.Clamp(_normalizedReticlePosition.y, -1f, 1f);

        // Store the final processed input delta
        _processedLookInput = inputDelta;
    }

    #endregion Input Processing --------------------------------------------------------------------------------------------------------

    
    #region Aim Lock --------------------------------------------------------------------------------------------------------

    private void HandleAimLock()
    {
        if (!playerInput.CurrentControlScheme.aimLock || !_allowAiming || !player.IsAlive())
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
        float distanceToTarget = Vector3.Distance(reticleWorldPosition.position, _currentAimLockTarget.transform.position);
        if (distanceToTarget > playerInput.CurrentControlScheme.aimLockRadius * 1.2f) // Add some hysteresis to prevent flickering
        {
            BreakAimLock();
            return;
        }
        
        // Move reticle towards target
        Vector3 targetWorldPosition = _currentAimLockTarget.transform.position;
        
        // Convert target world position to normalized reticle position
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
        _normalizedReticlePosition = Vector2.Lerp(
            _normalizedReticlePosition,
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
        if (!player.AlignToSplineDirection || !player.LevelManager)
        {
            _splineRotation = Quaternion.identity;
            return;
        }

        // Get the spline direction at the reticle position
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
            _normalizedReticlePosition.x * CrosshairBoundaryX,
            _normalizedReticlePosition.y * CrosshairBoundaryY,
            0
        );

        // Apply spline rotation if enabled
        if (player.AlignToSplineDirection)
        {
            localOffset = _splineRotation * localOffset;
        }

        // Calculate final world position
        reticleWorldPosition.position = boundaryCenter + localOffset;
       
        // Update aim direction with smoothing
        _aimDirection = Vector3.Lerp(_aimDirection, (reticleWorldPosition.position - transform.position).normalized, reticleFollowSpeed * Time.deltaTime);
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
                _normalizedReticlePosition = Vector2.Lerp(
                    _normalizedReticlePosition, 
                    Vector2.zero, 
                    autoCenterSpeed * Time.deltaTime
                );
            }
        }
    }

    #endregion Aiming --------------------------------------------------------------------------------------------------------

    #region Reticle -----------------------------------------------------------------------------------------------

    private void UpdateReticlesPosition()
    {
        if (!_allowAiming) return;

        if (activeReticle)
        {
            activeReticle.position = Vector3.Lerp(activeReticle.position, reticleWorldPosition.position, reticleFollowSpeed * Time.deltaTime);
            activeReticle.rotation = player.AlignToSplineDirection ? _splineRotation : Quaternion.identity;
            
            Vector3 activeReticleSize;

            if (_noInputTimer <= 0)
            {
                activeReticleSize = Vector3.one * 1.5f;
            }
            else
            {
                activeReticleSize = _currentAimLockTarget ? (Vector3.one / reticleSizeMultiplier) : Vector3.one; 
            }

            activeReticle.localScale = Vector3.Lerp(activeReticle.localScale, activeReticleSize, reticleGrowSpeed * Time.deltaTime);
        }
        
        if (smallReticle)
        {
            Vector3 smallReticlePosition = Vector3.Lerp(transform.position, reticleWorldPosition.position, smallReticleRange);
            smallReticle.position = Vector3.Lerp(smallReticle.position, smallReticlePosition, reticleFollowSpeed * Time.deltaTime);
            smallReticle.rotation = player.AlignToSplineDirection ? _splineRotation : Quaternion.identity;
        }

        if (targetReticle)
        {
            targetReticle.position = Vector3.Lerp(targetReticle.position, reticleWorldPosition.position, reticleFollowSpeed * Time.deltaTime);
            targetReticle.rotation = player.AlignToSplineDirection ? _splineRotation : Quaternion.identity;
            Vector3 targetReticleSize = _currentAimLockTarget ? (Vector3.one * reticleSizeMultiplier) : Vector3.one; 
            targetReticle.localScale = Vector3.Lerp(targetReticle.localScale, targetReticleSize, reticleGrowSpeed * Time.deltaTime);
        }
    }
    
    private void ChangeActiveReticle(Transform newReticle)
    {
        if (newReticle == activeReticle || !newReticle) return;
        
        if (activeReticle)
        {
            activeReticle.gameObject.SetActive(false);
        }
        
        activeReticle = newReticle;


        if (activeReticle)
        {
            activeReticle.gameObject.SetActive(true);
        }
    }
    
    private void ToggleReticle(bool state)
    {
        if (!reticlesHolder) return;
        
        reticlesHolder.gameObject.SetActive(state);
    }

    #endregion Reticle -----------------------------------------------------------------------------------------------
    
    
    #region Helper Methods -------------------------------------------------------------------------
    
    public ChickenController GetEnemyTarget(float radius)
    {
        
        Dictionary<ChickenController, float> enemyDistances = new Dictionary<ChickenController, float>();
        Collider[] hitColliders = Physics.OverlapSphere(reticleWorldPosition.position, radius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ChickenController enemy))
            {
                float distance = Vector3.Distance(reticleWorldPosition.position, enemy.transform.position);
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
        Collider[] hitColliders = Physics.OverlapSphere(reticleWorldPosition.position, radius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ChickenController enemy))
            {
                float distance = Vector3.Distance(reticleWorldPosition.position, enemy.transform.position);
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
            Vector3 parallelDirection = (reticleWorldPosition.position - transform.position).normalized;
            return parallelDirection;
        }
        else
        {
            Vector3 baseCrosshairDirection = (reticleWorldPosition.position - transform.position).normalized;
            float crosshairDistance = Vector3.Distance(transform.position, reticleWorldPosition.position);
            Vector3 convergencePoint = transform.position + (baseCrosshairDirection * (crosshairDistance * convergenceMultiplier));
        
            return (convergencePoint - position).normalized;
        }
    }
    
    private Vector3 GetSplineDirection()
    {
        return !player.LevelManager ? Vector3.forward : player.LevelManager.GetDirectionOnSpline(player.LevelManager.CurrentPositionOnPath.position);
    }
    
    private Vector3 GetCrosshairSplinePosition()
    {
        if (!player.LevelManager) return transform.position;
        return player.LevelManager.EnemyPosition;
    }
    
    public bool IsAimLocked => _isAimLocked;
    public ChickenController CurrentAimLockTarget => _currentAimLockTarget;
    public float AimLockCooldownRemaining => _aimLockCooldownTimer;

    #endregion Helper Methods -------------------------------------------------------------------------

    
    #region Editor -------------------------------------------------------------------------
    #if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        // Draw boundaries from spline position 
        if (player.LevelManager)
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
                    string debugText = $"Normalized Position: ({_normalizedReticlePosition.x:F2}, {_normalizedReticlePosition.y:F2})";
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
                Gizmos.DrawWireSphere(reticleWorldPosition.position, playerInput.CurrentControlScheme.aimLockRadius);
            }
        }
    }
    
    #endif 
    #endregion Editor -------------------------------------------------------------------------
}