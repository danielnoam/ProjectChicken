using System;
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
    [SerializeField, Range(0f, 1f), Tooltip("Reduces sensitivity near boundaries to prevent wall sliding (1 = no slowdown, 0 = full slowdown)")] private float edgeSlowdown = 0.3f;
    [SerializeField, Tooltip("Use screen-relative input for consistent feel across different resolutions")] private bool useScreenSpaceInput;
    [SerializeField, ShowIf("useScreenSpaceInput"), Tooltip("Screen pixel equivalent for mouse movement normalization")] private Vector2 screenSensitivity = new Vector2(800f, 600f);[EndIf]
    
    [Header("Auto Center")]
    [SerializeField] private bool autoCenter = true;
    [EnableIf("autoCenter")]
    [SerializeField, Min(0)] private float autoCenterDelay = 5f;
    [SerializeField, Min(0)] private float autoCenterSpeed = 1f;
    [EndIf]
    
    [Header("References")]
    [SerializeField] private Transform aimWorldPosition;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem playerWeapon;
    [SerializeField, Self, HideInInspector] private ControllerRumbleSource controllerRumbleSource;


    private bool _isAimLocked;
    private bool _allowAiming;
    private float _noInputTimer;
    private float _aimLockCooldownTimer;
    private Vector2 _processedLookInput;
    private Vector2 _normalizedAimPosition;
    private Vector3 _aimDirection;
    private ChickenController _currentAimLockTarget;
    private float CrosshairBoundaryX => player.LevelManager ? player.LevelManager.EnemyBoundary.x : 25f;
    private float CrosshairBoundaryY => player.LevelManager ? player.LevelManager.EnemyBoundary.y : 15f;


    public Vector3 AimDirection => _aimDirection;
    public Transform AimWorldPosition => aimWorldPosition;
    public Vector2 NormalizedAimPosition => _normalizedAimPosition;
    public ChickenController CurrentAimLockTarget => _currentAimLockTarget;
    
    public event Action<bool, ChickenController> OnAimLockStateChange; 

    
    
    
    private void OnValidate() { this.ValidateRefs(); }

    private void Awake()
    {
        _allowAiming = true;
        
        // Take the aim world position out of the player's transform so when the player moves it will not affect the position setting of the aim position
        // Don't change for now.
        // The aim world position is also used in the player weapon system to hold the reticles
        if (aimWorldPosition) aimWorldPosition.SetParent(null);
    }

    private void OnEnable()
    {
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
    
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        _allowAiming = stage.AllowPlayerAim;
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


        if (player.AlignToSplineDirection)
        {
            localOffset = player.SplineRotation * localOffset;
        }


        aimWorldPosition.position = boundaryCenter + localOffset;
        aimWorldPosition.rotation = player.AlignToSplineDirection ? player.SplineRotation : Quaternion.identity;
        _aimDirection = (aimWorldPosition.position - transform.position).normalized;
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

    #endregion Aiming --------------------------------------------------------------------------------------------------------
    
    
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
            if (player.AlignToSplineDirection)
            {
                localTargetOffset = Quaternion.Inverse(player.SplineRotation) * localTargetOffset;
            }
        
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
        
        ChickenController newTarget = GetEnemyTarget(playerInput.CurrentControlScheme.aimLockRadius);
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

    private void OnProcessedLook(Vector2 processedLookInput)
    {
        if (!_allowAiming || !player.IsAlive()) return;
        _processedLookInput = processedLookInput;
        _noInputTimer = 0f;
    }
    
    private void OnAttack2(InputAction.CallbackContext context)
    {
        _noInputTimer = 0f;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
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
                inputDelta = inputDelta.normalized * scaledMagnitude;
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
    
    public ChickenController GetEnemyTarget(float radius)
    {
        
        Dictionary<ChickenController, float> enemyDistances = new Dictionary<ChickenController, float>();
        Collider[] hitColliders = Physics.OverlapSphere(aimWorldPosition.position, radius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ChickenController enemy))
            {
                float distance = Vector3.Distance(aimWorldPosition.position, enemy.transform.position);
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
        Collider[] hitColliders = Physics.OverlapSphere(aimWorldPosition.position, radius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out ChickenController enemy))
            {
                float distance = Vector3.Distance(aimWorldPosition.position, enemy.transform.position);
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
    

    private Vector3 GetEnemySplinePosition()
    {
        return !player.LevelManager ? transform.position : player.LevelManager.EnemyPosition;
    }
    

    #endregion Helper Methods -------------------------------------------------------------------------

    
    #region Editor -------------------------------------------------------------------------
    #if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        // Draw boundaries from spline position 
        if (player.LevelManager)
        {
            Gizmos.color = Color.blue;
            Vector3 crosshairSplinePosition = GetEnemySplinePosition();
            
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
                    worldCorners[i] = crosshairSplinePosition + (player.SplineRotation * localCorners[i]);
                }
                
                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;
                    Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
                }
                
                
                if (Application.isPlaying)
                {
                    string debugText = $"Normalized Position: ({_normalizedAimPosition.x:F2}, {_normalizedAimPosition.y:F2})";
                    if (_isAimLocked && _currentAimLockTarget)
                    {
                        debugText += $"\nAim Locked: {_currentAimLockTarget.name}";
                    }
                    else if (_aimLockCooldownTimer > 0)
                    {
                        debugText += $"\nCooldown: {_aimLockCooldownTimer:F1}s";
                    }

                    debugText += $"\nCrosshair Boundaries";
                
                    UnityEditor.Handles.Label(crosshairSplinePosition + (player.SplineRotation * Vector3.up * (CrosshairBoundaryY + 0.5f)), debugText);
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
                Gizmos.DrawWireSphere(aimWorldPosition.position, playerInput.CurrentControlScheme.aimLockRadius);
            }
        }
    }
    
    #endif 
    #endregion Editor -------------------------------------------------------------------------
}