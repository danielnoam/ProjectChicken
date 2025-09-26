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


    private bool _isAimLocked;
    private float _noInputTimer;
    private float _aimLockCooldownTimer;
    private Vector2 _processedLookInput;
    private Vector2 _normalizedAimPosition;
    private ChickenStateController _currentAimLockTarget;
    private Coroutine _autoCenterRoutine;
    private float CrosshairBoundaryX => player.LevelManager ? player.LevelManager.EnemyBoundarySize.x : 25f;
    private float CrosshairBoundaryY => player.LevelManager ? player.LevelManager.EnemyBoundarySize.y : 15f;


    


    public Transform AimWorldPosition => aimWorldPosition;
    public Vector2 NormalizedAimPosition => _normalizedAimPosition;
    
    public event Action<bool, ChickenStateController> OnAimLockStateChange;
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
        _autoCenterRoutine = StartCoroutine(ReturnToCenter());
        OnAllowAimingChanged?.Invoke(AllowAiming);
    }
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        if (_autoCenterRoutine != null) StopCoroutine(_autoCenterRoutine);

        if (AllowAiming != stage.AllowPlayerAiming)
        {
            AllowAiming = stage.AllowPlayerAiming;

            if (!stage.AllowPlayerAiming)
            {
                _autoCenterRoutine = StartCoroutine(ReturnToCenter());
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
            _normalizedAimPosition.x * CrosshairBoundaryX,
            _normalizedAimPosition.y * CrosshairBoundaryY,
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
            _normalizedAimPosition = Vector2.zero;
        }
    }
    
    private void HandleAutoCenter()
    {
        if (!autoCenter || _isAimLocked) return;
    
        bool hasInput = _processedLookInput.magnitude > 0.01f;
    
        if (!hasInput)
        {
            _noInputTimer += Time.deltaTime;
        
            if (_noInputTimer >= autoCenterDelay)
            {
                _normalizedAimPosition = Vector2.Lerp(
                    _normalizedAimPosition, 
                    Vector2.zero, 
                    autoCenterSpeed * Time.deltaTime
                );
            }
        }
    }
    
    
    private IEnumerator ReturnToCenter()
    {
        while (_normalizedAimPosition.magnitude > 0.01f)
        {
            _normalizedAimPosition = Vector2.Lerp(_normalizedAimPosition, Vector2.zero, 1f * Time.deltaTime);
            yield return null;
        }
        _normalizedAimPosition = Vector2.zero;
    }

    #endregion Aiming --------------------------------------------------------------------------------------------------------
    
    
    #region Aim Lock --------------------------------------------------------------------------------------------------------

    private void HandleAimLock()
    {
        if (!playerInput.CurrentControlScheme.aimLock || !AllowAiming || !player.Health.IsAlive())
        {
            if (_isAimLocked)
            {
                BreakAimLock();
            }
            return;
        }
        

        if (_aimLockCooldownTimer > 0)
        {
            _aimLockCooldownTimer -= Time.deltaTime;
        }
        
                
        if (_isAimLocked)
        {
            if (!_currentAimLockTarget || !_currentAimLockTarget.gameObject.activeInHierarchy)
            {
                BreakAimLock();
                return;
            }
        
            float distanceToTarget = Vector3.Distance(aimWorldPosition.position, _currentAimLockTarget.transform.position);
            if (distanceToTarget > playerInput.CurrentControlScheme.aimLockRadius * 2.5f)
            {
                BreakAimLock();
                return;
            }
        

            Vector3 targetWorldPosition = _currentAimLockTarget.transform.position;
            Vector3 boundaryCenter = GetEnemySplinePosition();
            Vector3 localTargetOffset = targetWorldPosition - boundaryCenter;
        
            Vector2 targetNormalizedPosition = new Vector2(
                Mathf.Clamp(localTargetOffset.x / CrosshairBoundaryX, -1f, 1f),
                Mathf.Clamp(localTargetOffset.y / CrosshairBoundaryY, -1f, 1f)
            );
        
            _normalizedAimPosition = Vector2.Lerp(
                _normalizedAimPosition,
                targetNormalizedPosition,
                playerInput.CurrentControlScheme.aimLockSpeed * Time.deltaTime
            );
        }
        
        
        if (!_isAimLocked && _aimLockCooldownTimer <= 0 && _noInputTimer > 0.1f)
        {
            TryAimLock();
        }
    }
    
    private void TryAimLock()
    {
        if (_isAimLocked) return;
        
        ChickenStateController newTarget = GetTarget(playerInput.CurrentControlScheme.aimLockRadius);
        if (newTarget && _currentAimLockTarget != newTarget)
        {
            _currentAimLockTarget = newTarget;
            _isAimLocked = true;
            OnAimLockStateChange?.Invoke(true, newTarget);
        }
    }
    
    private void BreakAimLock(bool playerBrokeAimLock = false)
    {
        if (!_isAimLocked) return;
        
        _isAimLocked = false;
        _currentAimLockTarget = null;
        _aimLockCooldownTimer = !playerBrokeAimLock ? playerInput.CurrentControlScheme.aimLockCooldown : playerInput.CurrentControlScheme.aimLockCooldown*2;
        OnAimLockStateChange?.Invoke(false, null);
    }
    

    #endregion Aim Lock --------------------------------------------------------------------------------------------------------
    
    
    #region Input Processing --------------------------------------------------------------------------------------------------------



    
    private void OnAttack2(InputAction.CallbackContext context)
    {
        _noInputTimer = 0f;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        _noInputTimer = 0f;
    }
    
    
    private void OnProcessedLook(Vector2 processedLookInput)
    {
        if (!AllowAiming || !player.Health.IsAlive()) return;
    
        // Validate input at the source - only check for NaN/Infinity, not magnitude
        if (!IsValidVector2(processedLookInput))
        {
            Debug.LogWarning($"Detected corrupted input in OnProcessedLook: {processedLookInput}, ignoring");
            return; // Don't update _processedLookInput with corrupted data
        }
    
        // Only clamp truly extreme values that indicate corruption (not fast mouse movement)
        if (processedLookInput.magnitude > 10000f)
        {
            Debug.LogWarning($"Detected extremely large input magnitude in OnProcessedLook: {processedLookInput.magnitude}, clamping");
            processedLookInput = processedLookInput.normalized * 1000f; // Keep direction but limit magnitude
        }
    
        _processedLookInput = processedLookInput;
        _noInputTimer = 0f;
    }
    
        
    private void ProcessAimingInput()
    {
        if (_isAimLocked && _processedLookInput.magnitude <= playerInput.CurrentControlScheme.aimLockStrength)
        {
            _processedLookInput = Vector2.zero;
            return;
        }
        
        if (_isAimLocked && _processedLookInput.magnitude > playerInput.CurrentControlScheme.aimLockStrength)
        {
            BreakAimLock(playerBrokeAimLock: true);
        }

        Vector2 inputDelta = _processedLookInput;
        Vector2 positionChange;
        
        // Safety check (should be clean now, but keeping as backup)
        if (!IsValidVector2(inputDelta))
        {
            Debug.LogError($"Corrupted input made it past OnProcessedLook validation: {inputDelta}");
            _processedLookInput = Vector2.zero;
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
                
                // Only catch truly problematic values - normal mouse movement can be large
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
            1f - Mathf.Abs(_normalizedAimPosition.x),
            1f - Mathf.Abs(_normalizedAimPosition.y)
        );
        Vector2 edgeMultiplier = new Vector2(
            Mathf.Lerp(edgeSlowdown, 1f, edgeDistance.x),
            Mathf.Lerp(edgeSlowdown, 1f, edgeDistance.y)
        );
        positionChange.x *= edgeMultiplier.x;
        positionChange.y *= edgeMultiplier.y;
        
        _normalizedAimPosition += positionChange;
        _normalizedAimPosition.x = Mathf.Clamp(_normalizedAimPosition.x, -1f, 1f);
        _normalizedAimPosition.y = Mathf.Clamp(_normalizedAimPosition.y, -1f, 1f);
        _processedLookInput = inputDelta;
    }


    #endregion Input Processing --------------------------------------------------------------------------------------------------------

    
    #region Helper Methods -------------------------------------------------------------------------
    
    private bool IsValidVector2(Vector2 vector)
    {
        return !float.IsNaN(vector.x) && !float.IsNaN(vector.y) &&
               !float.IsInfinity(vector.x) && !float.IsInfinity(vector.y);
    }
        
        
    public ChickenStateController GetTarget(float radius)
    {
        
        Dictionary<ChickenStateController, float> enemyDistances = new Dictionary<ChickenStateController, float>();
        Collider[] hitColliders = Physics.OverlapSphere(aimWorldPosition.position, radius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ChickenStateController enemy))
            {
                float distance = Vector3.Distance(aimWorldPosition.position, enemy.transform.position);
                enemyDistances[enemy] = distance;
            }
        }
        
        if (enemyDistances.Count > 0)
        {
            ChickenStateController closestEnemy = null;
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
    
    public ChickenStateController[] GetTargets(int maxTargets, float radius)
    {
        Dictionary<ChickenStateController, float> enemyDistances = new Dictionary<ChickenStateController, float>();
        Collider[] hitColliders = Physics.OverlapSphere(aimWorldPosition.position, radius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ChickenStateController enemy))
            {
                float distance = Vector3.Distance(aimWorldPosition.position, enemy.transform.position);
                enemyDistances[enemy] = distance;
            }
        }
        
        List<ChickenStateController> sortedEnemies = new List<ChickenStateController>(enemyDistances.Keys);
        sortedEnemies.Sort((a, b) => enemyDistances[a].CompareTo(enemyDistances[b]));
        
        int targetCount = Mathf.Min(maxTargets, sortedEnemies.Count);
        ChickenStateController[] targets = new ChickenStateController[targetCount];
        for (int i = 0; i < targetCount; i++)
        {
            targets[i] = sortedEnemies[i];
        }
        
        return targets;
    }
    

    private Vector3 GetEnemySplinePosition()
    {
        return !player.LevelManager ? transform.position : player.LevelManager.EnemyPosition;
    }
    

    #endregion Helper Methods -------------------------------------------------------------------------
    

}