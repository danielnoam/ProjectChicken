using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

[RequireComponent(typeof(RailPlayer))]
public class RailPlayerAiming : MonoBehaviour
{
    [Header("Aim Settings")]
    [SerializeField, Min(0.1f), Tooltip("Base speed multiplier for reticle movement")] private float baseSensitivity = 1f;
    [SerializeField, Range(0f, 1f), Tooltip("Reduces sensitivity near boundaries to prevent wall sliding (1 = no slowdown, 0 = full slowdown)")] private float edgeSlowdown = 1f;
    [SerializeField, Tooltip("Use screen-relative input for consistent feel across different resolutions")] private bool useScreenSpaceInput;
    [SerializeField, ShowIf("useScreenSpaceInput"), Tooltip("Screen pixel equivalent for mouse movement normalization")] private Vector2 screenSensitivity = new Vector2(800f, 600f);[EndIf]
    
    [Header("Auto Center")]
    [SerializeField] private bool autoCenter = true;
    [EnableIf("autoCenter")]
    [SerializeField, Range(0.1f,10f)] private float autoCenterDelay = 5f;
    [SerializeField, Range(0.1f,10f)] private float autoCenterSpeed = 1f;
    [EndIf]
    
    [Header("References")]
    [SerializeField] private Transform aimWorldPosition;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem playerWeapon;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;


    [Separator]
    [SerializeField, VInspector.ReadOnly] private Vector2 processedLookInput;
    [SerializeField, VInspector.ReadOnly] private Vector2 normalizedAimPosition;
    [SerializeField, VInspector.ReadOnly] private float noInputTimer;
    [SerializeField, VInspector.ReadOnly] private float aimLockCooldownTimer;
    [SerializeField, VInspector.ReadOnly] private bool isAimLocked;

    private ITargetable _currentAimLockTarget;
    private Coroutine _aimMovementCoroutine;
    private float CrosshairBoundaryX => player.LevelManager ? player.LevelManager.EnemyBoundarySize.x : 25f;
    private float CrosshairBoundaryY => player.LevelManager ? player.LevelManager.EnemyBoundarySize.y : 15f;
    private bool IsAimMovementActive => _aimMovementCoroutine != null;


    


    public Transform AimWorldPosition => aimWorldPosition;
    public Vector2 NormalizedAimPosition => normalizedAimPosition;
    
    public event Action<bool, ITargetable> OnAimLockStateChange;
    public event Action<bool> OnAllowAimingChanged;
    
    
    public bool AllowAiming { get; private set; }
    public Vector3 AimDirection { get; private set; }

    
    
    
    private void OnValidate() { this.ValidateRefs(); }
    
    
    private void OnEnable()
    {
        player.Health.OnDeath += OnDeath;
        playerInput.OnProcessedLookEvent += OnProcessedLook;
        playerInput.OnAttackEvent += OnAttack;
        playerInput.OnAttack2Event += OnAttack2;
        
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged += OnStageChanged;

        }
    }
    

    private void OnDisable()
    {
        player.Health.OnDeath -= OnDeath;
        playerInput.OnProcessedLookEvent -= OnProcessedLook;
        playerInput.OnAttackEvent -= OnAttack;
        playerInput.OnAttack2Event -= OnAttack2;
        
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged -= OnStageChanged;
        }
    }
    


    private void Update()
    {
        ProcessAimingInput();
        UpdateAimPosition();
        HandleAimLock();
        HandleAutoCenter();
    }

    private void OnDeath()
    {
        AllowAiming = false;
        AimAtCenter();
        OnAllowAimingChanged?.Invoke(AllowAiming);
    }
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        if (isAimLocked)
        {
            BreakAimLock();
        }
    
        if (_aimMovementCoroutine != null) 
        {
            StopCoroutine(_aimMovementCoroutine);
            _aimMovementCoroutine = null; 
        }

        if (AllowAiming != stage.AllowPlayerAiming)
        {
            AllowAiming = stage.AllowPlayerAiming;

            if (!stage.AllowPlayerAiming)
            {
                AimAtCenter(1f);
            }
        }
        
        OnAllowAimingChanged?.Invoke(AllowAiming);
    }


    public void SetUp()
    {
        // Take the aim world position out of the player's transform so when the player moves it will not affect the position setting of the aim position
        // Don't change for now.
        // The aim world position is also used in the player weapon system to hold the reticles
        if (aimWorldPosition) aimWorldPosition.SetParent(null);
    }
    
        
    #region Aiming --------------------------------------------------------------------------------------------------------
    

    private void UpdateAimPosition()
    {
        Vector3 boundaryCenter = GetEnemySplinePosition();
        
        Vector3 localOffset = new Vector3(
            normalizedAimPosition.x * CrosshairBoundaryX,
            normalizedAimPosition.y * CrosshairBoundaryY,
            0
        );

        Vector3 targetPosition = boundaryCenter + localOffset;
        
        if (!float.IsNaN(targetPosition.x) && !float.IsNaN(targetPosition.y) && !float.IsNaN(targetPosition.z))
        {
            aimWorldPosition.position = targetPosition;
            AimDirection = (aimWorldPosition.position - transform.position).normalized;
        }
        else
        {
            Debug.Log($"NaN detected in target position: {targetPosition}");
            aimWorldPosition.position = boundaryCenter + transform.forward * 10f;
            AimDirection = transform.forward;
            normalizedAimPosition = Vector2.zero;
        }
    }
    
    private void HandleAutoCenter()
    {
        if (!AllowAiming || !autoCenter || isAimLocked || IsAimMovementActive) return;

        bool hasInput = processedLookInput.magnitude > 0.01f;

        if (!hasInput)
        {
            noInputTimer += Time.deltaTime;
    
            if (noInputTimer >= autoCenterDelay)
            {
                if (Vector2.Distance(normalizedAimPosition, Vector2.zero) > 0.01f)
                {
                    normalizedAimPosition = Vector2.Lerp(
                        normalizedAimPosition, 
                        Vector2.zero, 
                        autoCenterSpeed * Time.deltaTime
                    );
                }
                else
                {
                    normalizedAimPosition = Vector2.zero;
                }
                
            }
        }
    }
    
    
    private void AimAtCenter(float speed = 1f)
    {
        if (_aimMovementCoroutine != null)
        {
            StopCoroutine(_aimMovementCoroutine);
        }
        _aimMovementCoroutine = StartCoroutine(AimAt(null, speed));
    }


    private void AimAtWorldPosition(Vector3 worldPosition, float speed = 1f)
    {
        if (_aimMovementCoroutine != null)
        {
            StopCoroutine(_aimMovementCoroutine);
        }
        _aimMovementCoroutine = StartCoroutine(AimAt(worldPosition, speed));
    }
    
    private IEnumerator AimAt(Vector3? worldPosition, float speed)
    {
        Vector2 targetNormalizedPosition;
        processedLookInput = Vector2.zero;
    
        if (worldPosition.HasValue)
        {
            Vector3 boundaryCenter = GetEnemySplinePosition();
            Vector3 localTargetOffset = worldPosition.Value - boundaryCenter;
            targetNormalizedPosition = new Vector2(
                Mathf.Clamp(localTargetOffset.x / CrosshairBoundaryX, -1f, 1f),
                Mathf.Clamp(localTargetOffset.y / CrosshairBoundaryY, -1f, 1f)
            );
        }
        else
        {
            targetNormalizedPosition = Vector2.zero;
        }
    
        while (Vector2.Distance(normalizedAimPosition, targetNormalizedPosition) > 0.01f)
        {
            normalizedAimPosition = Vector2.Lerp(
                normalizedAimPosition, 
                targetNormalizedPosition, 
                speed * Time.deltaTime
            );
            yield return null;
        }
    
        normalizedAimPosition = targetNormalizedPosition;
    }

    #endregion Aiming --------------------------------------------------------------------------------------------------------
    
    
    #region Aim Lock --------------------------------------------------------------------------------------------------------

    private void HandleAimLock()
    {
        if (!playerInput.CurrentControlScheme.aimLock || !AllowAiming || !player.Health.IsAlive())
        {
            if (isAimLocked)
            {
                BreakAimLock();
            }
            return;
        }
        

        if (aimLockCooldownTimer > 0)
        {
            aimLockCooldownTimer -= Time.deltaTime;
        }
        
                
        if (isAimLocked)
        {
            if (_currentAimLockTarget == null || !_currentAimLockTarget.Transform.gameObject.activeInHierarchy || !_currentAimLockTarget.IsValidTarget)
            {
                BreakAimLock();
                return;
            }
        
            float distanceToTarget = Vector3.Distance(aimWorldPosition.position, _currentAimLockTarget.Transform.position);
            if (distanceToTarget > playerInput.CurrentControlScheme.aimLockRadius * 2.5f)
            {
                BreakAimLock();
                return;
            }
        

            Vector3 targetWorldPosition = _currentAimLockTarget.Transform.position;
            Vector3 boundaryCenter = GetEnemySplinePosition();
            Vector3 localTargetOffset = targetWorldPosition - boundaryCenter;
        
            Vector2 targetNormalizedPosition = new Vector2(
                Mathf.Clamp(localTargetOffset.x / CrosshairBoundaryX, -1f, 1f),
                Mathf.Clamp(localTargetOffset.y / CrosshairBoundaryY, -1f, 1f)
            );
        
            normalizedAimPosition = Vector2.Lerp(
                normalizedAimPosition,
                targetNormalizedPosition,
                playerInput.CurrentControlScheme.aimLockSpeed * Time.deltaTime
            );
        }
        
        
        if (!isAimLocked && aimLockCooldownTimer <= 0 && noInputTimer > 0.1f)
        {
            TryAimLock();
        }
    }
    
    private void TryAimLock()
    {
        if (isAimLocked) return;
        
        ITargetable newTarget = GetTarget(playerInput.CurrentControlScheme.aimLockRadius);
        if (newTarget is { IsValidTarget: true } && _currentAimLockTarget != newTarget)
        {
            _currentAimLockTarget = newTarget;
            isAimLocked = true;
            OnAimLockStateChange?.Invoke(true, newTarget);
        }
    }
    
    private void BreakAimLock(bool playerBrokeAimLock = false)
    {
        if (!isAimLocked) return;
        
        isAimLocked = false;
        _currentAimLockTarget = null;
        aimLockCooldownTimer = !playerBrokeAimLock ? playerInput.CurrentControlScheme.aimLockCooldown : playerInput.CurrentControlScheme.aimLockCooldown*2;
        OnAimLockStateChange?.Invoke(false, null);
    }
    

    #endregion Aim Lock --------------------------------------------------------------------------------------------------------
    
    
    #region Input Processing --------------------------------------------------------------------------------------------------------



    
    private void OnAttack2(InputAction.CallbackContext context)
    {
        noInputTimer = 0f;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        noInputTimer = 0f;
    }
    
    
    private void OnProcessedLook(Vector2 processedLookInput)
    {
        if (!AllowAiming || !player.Health.IsAlive()) return;

        // Validate input
        if (!IsValidVector2(processedLookInput))
        {
            Debug.LogWarning($"[AIMING] Corrupted input detected: {processedLookInput}");
            return;
        }

        // Safety clamp
        if (processedLookInput.magnitude > playerInput.maxReasonableInput)
        {
            processedLookInput = Vector2.ClampMagnitude(processedLookInput, playerInput.maxReasonableInput);
        }

        this.processedLookInput = processedLookInput;
        noInputTimer = 0f;
    }
    
        
    private void ProcessAimingInput()
    {
        if (isAimLocked && processedLookInput.magnitude <= playerInput.CurrentControlScheme.aimLockStrength)
        {
            processedLookInput = Vector2.zero;
            return;
        }
        
        if (isAimLocked && processedLookInput.magnitude > playerInput.CurrentControlScheme.aimLockStrength)
        {
            BreakAimLock(playerBrokeAimLock: true);
        }

        Vector2 inputDelta = processedLookInput;
        Vector2 positionChange;
        
        // Safety check
        if (!IsValidVector2(inputDelta))
        {
            Debug.LogError($"Corrupted input made it past OnProcessedLook validation: {inputDelta}");
            processedLookInput = Vector2.zero;
            return;
        }
        
        if (playerInput.IsCurrentDeviceGamepad)
        {
            float deadZone = playerInput.CurrentControlScheme.deadZone;
            
            if (inputDelta.magnitude < deadZone)
            {
                inputDelta = Vector2.zero;
            }
            
            Vector2 velocity = inputDelta * (baseSensitivity * playerInput.CurrentControlScheme.aimSensitivity * 2.5f);
            positionChange = velocity * Time.deltaTime; 
        }
        else
        {
            float deadZone = playerInput.CurrentControlScheme.deadZone;
            
            if (inputDelta.magnitude < deadZone)
            {
                inputDelta = Vector2.zero;
            }
            else
            {
                float scaledMagnitude = (inputDelta.magnitude - deadZone) / (1f - deadZone);
                
                if (!float.IsFinite(scaledMagnitude))
                {
                    Debug.LogError($"Non-finite scaled magnitude detected: {scaledMagnitude} from input: {inputDelta}");
                    inputDelta = Vector2.zero;
                }
                else
                {
                    inputDelta = inputDelta.normalized * scaledMagnitude;
                }
            }
            
            if (inputDelta.magnitude > 0)
            {
                float originalMagnitude = inputDelta.magnitude;
                float curvedSensitivity = playerInput.CurrentControlScheme.magnitudeToSensitivityCurve.Evaluate(Mathf.Clamp01(inputDelta.magnitude));
                inputDelta = inputDelta.normalized * (originalMagnitude * curvedSensitivity * baseSensitivity * playerInput.CurrentControlScheme.aimSensitivity);
            }
            
            Vector2 mouseVelocity = inputDelta * 0.1f;
            positionChange = mouseVelocity * Time.deltaTime;
        }
        
        // Final safety check
        if (!IsValidVector2(positionChange))
        {
            Debug.LogError($"Corrupted position change calculated: {positionChange}");
            return;
        }
        
        Vector2 edgeDistance = new Vector2(
            1f - Mathf.Abs(normalizedAimPosition.x),
            1f - Mathf.Abs(normalizedAimPosition.y)
        );
        Vector2 edgeMultiplier = new Vector2(
            Mathf.Lerp(edgeSlowdown, 1f, edgeDistance.x),
            Mathf.Lerp(edgeSlowdown, 1f, edgeDistance.y)
        );
        positionChange.x *= edgeMultiplier.x;
        positionChange.y *= edgeMultiplier.y;
        
        normalizedAimPosition += positionChange;
        normalizedAimPosition.x = Mathf.Clamp(normalizedAimPosition.x, -1f, 1f);
        normalizedAimPosition.y = Mathf.Clamp(normalizedAimPosition.y, -1f, 1f);
    }

    
    #endregion Input Processing --------------------------------------------------------------------------------------------------------

    
    #region Helper Methods -------------------------------------------------------------------------
    
    private bool IsValidVector2(Vector2 vector)
    {
        return !float.IsNaN(vector.x) && !float.IsNaN(vector.y) &&
               !float.IsInfinity(vector.x) && !float.IsInfinity(vector.y);
    }
        
        
    public ITargetable GetTarget(float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(aimWorldPosition.position, radius);
    
        ITargetable closestTarget = null;
        float minDistance = float.MaxValue;
    
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ITargetable target))
            {
                // Skip invalid targets
                if (!target.IsValidTarget) continue;
            
                float distance = Vector3.Distance(aimWorldPosition.position, target.Transform.position);
            
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTarget = target;
                }
            }
        }
    
        return closestTarget;
    }

    public ITargetable[] GetTargets(int maxTargets, float radius)
    {
        List<ITargetable> validTargets = new List<ITargetable>();
        Collider[] hitColliders = Physics.OverlapSphere(aimWorldPosition.position, radius);
    
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ITargetable target) && target.IsValidTarget)
            {
                validTargets.Add(target);
            }
        }
    
        // Sort by distance
        validTargets.Sort((a, b) => 
        {
            float distA = Vector3.Distance(aimWorldPosition.position, a.Transform.position);
            float distB = Vector3.Distance(aimWorldPosition.position, b.Transform.position);
            return distA.CompareTo(distB);
        });
    
        // Return up to maxTargets
        int targetCount = Mathf.Min(maxTargets, validTargets.Count);
        ITargetable[] targets = new ITargetable[targetCount];
        for (int i = 0; i < targetCount; i++)
        {
            targets[i] = validTargets[i];
        }
    
        return targets;
    }
    

    private Vector3 GetEnemySplinePosition()
    {
        return !player.LevelManager ? transform.position : player.LevelManager.EnemyPosition;
    }
    

    #endregion Helper Methods -------------------------------------------------------------------------
    

}