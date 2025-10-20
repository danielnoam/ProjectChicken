using UnityEngine;

public class ChickenMovementBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementDuration = 2f;
    public float durationVariationRange = 0.5f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Slot Following")]
    public bool trackSlotChanges = true;
    public float slotChangeDetectionInterval = 0.1f;
    public float slotDistanceThreshold = 1f;
    public float followingSpeed = 8f;
    public float followingDistanceThreshold = 0.1f;
    
    [Header("Movement Failsafe")]
    public int maxMovementResets = 5;
    public float maxMovementTime = 10f;
    public bool enableMovementFailsafe = true;
    public float failsafeMovementSpeed = 12f; // NEW: Speed multiplier for failsafe movement
    
    [Header("Spawn Area Settings")]
    public bool moveToSpawnWhenIdle = true;
    public float spawnAreaArrivalDistance = 0.5f;
    public float spawnCheckDelay = 0.5f;
    public float minDistanceFromBlocker = 0.5f;
    public int maxSpawnAttempts = 30;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool showMovementGizmos = true;
    public Color movementLineColor = Color.yellow;
    public Color failsafeMovementColor = Color.red; // NEW: Color for failsafe movement

    private ChickenStateController stateController;
    private EnemyChickenRegistration registration;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float actualMovementDuration;
    private float movementTimer = 0f;
    private float slotCheckTimer = 0f;
    private bool isMoving = false;
    private bool wasInSpawnArea = false;
    
    // Movement failsafe tracking
    private int movementResetCount = 0;
    private float movingToSlotStartTime = 0f;
    private bool hasTriggeredFailsafe = false;
    
    // Timers
    private float idleStateTimer = 0f;
    private bool hasCheckedSpawnThisFrame = false;

    void Start()
    {
        stateController = GetComponent<ChickenStateController>();
        registration = GetComponent<EnemyChickenRegistration>();

        if (stateController == null)
        {
            Debug.LogError($"ChickenMovementBehavior on {gameObject.name}: No ChickenStateController found!");
        }

        if (registration == null)
        {
            Debug.LogError($"ChickenMovementBehavior on {gameObject.name}: No EnemyChickenRegistration found!");
        }

        if (movementCurve.keys.Length == 0)
        {
            movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
        
        wasInSpawnArea = IsInSpawnArea();
    }

    void Update()
    {
        if (stateController == null || registration == null)
            return;

        hasCheckedSpawnThisFrame = false;
        
        HandleMovement();
        HandleIdleTimer();
    }

    void HandleIdleTimer()
    {
        if (stateController.IsIdle)
        {
            idleStateTimer += Time.deltaTime;
            
            if (!isMoving)
            {
                wasInSpawnArea = IsInSpawnArea();
            }
            
            if (idleStateTimer >= spawnCheckDelay && !hasCheckedSpawnThisFrame)
            {
                CheckAndStartSpawnMovement();
                hasCheckedSpawnThisFrame = true;
            }
        }
        else
        {
            idleStateTimer = 0f;
        }
    }

    void CheckAndStartSpawnMovement()
    {
        if (!moveToSpawnWhenIdle || isMoving)
        {
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Skip spawn movement - moveToSpawnWhenIdle: {moveToSpawnWhenIdle}, isMoving: {isMoving}");
            return;
        }

        bool currentlyInSpawnArea = IsInSpawnArea();
        
        if (showDebugLogs)
        {
            Debug.Log($"Chicken {gameObject.name}: Checking spawn movement - In spawn area: {currentlyInSpawnArea}");
        }

        if (!currentlyInSpawnArea)
        {
            if (EnemySpawner.Instance == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"Chicken {gameObject.name}: No EnemySpawner instance found!");
                return;
            }

            if (!EnemySpawner.Instance.HasValidSpawnAreas())
            {
                if (showDebugLogs)
                    Debug.LogWarning($"Chicken {gameObject.name}: EnemySpawner has no valid spawn areas configured!");
                return;
            }
            
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Not in spawn area, attempting to move to spawn");
            
            StartMovingToSpawn();
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Already in spawn area, no movement needed");
        }
    }

    void HandleMovement()
    {
        // Start moving to slot when state changes to MovingToSlot
        if (stateController.IsMovingToSlot && !isMoving)
        {
            StartMovingToSlot();
        }
        // Handle failsafe movement state - NEW
        else if (stateController.IsFailsafeMovement && !isMoving)
        {
            StartFailsafeMovement();
        }
        // Handle continuous following when in FollowingSlot state
        else if (stateController.IsFollowingSlot)
        {
            HandleSlotFollowing();
        }
        // Stop movement if state changed to something that shouldn't be moving
        else if (!stateController.IsMovingToSlot && !stateController.IsFailsafeMovement && !stateController.IsIdle && isMoving)
        {
            StopMovement();
        }
        
        // Check movement failsafe if enabled and currently moving to slot
        if (enableMovementFailsafe && stateController.IsMovingToSlot && isMoving && !hasTriggeredFailsafe)
        {
            CheckMovementFailsafe();
        }
        
        // Update current movement
        if (isMoving)
        {
            if (stateController.IsMovingToSlot)
            {
                UpdateMovementToSlot();
            }
            else if (stateController.IsFailsafeMovement) // NEW
            {
                UpdateFailsafeMovement();
            }
            else if (stateController.IsIdle)
            {
                UpdateMovementToSpawn();
            }
        }
    }

    void HandleSlotFollowing()
    {
        Vector3? slotPosition = registration.GetAssignedSlotPosition();
        
        if (!slotPosition.HasValue)
        {
            // Lost slot assignment while following
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: Lost slot assignment while following!");
            
            stateController.SetIdle();
            return;
        }
        
        Vector3 targetSlotPos = slotPosition.Value;
        float distanceToSlot = Vector3.Distance(transform.position, targetSlotPos);
        
        // If we're close enough, don't move
        if (distanceToSlot <= followingDistanceThreshold)
        {
            return;
        }
        
        // Move directly towards the slot like a child following its parent
        float moveDistance = followingSpeed * Time.deltaTime;
        
        // Don't overshoot the target
        if (moveDistance >= distanceToSlot)
        {
            transform.position = targetSlotPos;
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Reached perfect slot position");
        }
        else
        {
            Vector3 direction = (targetSlotPos - transform.position).normalized;
            transform.position += direction * moveDistance;
        }
    }

    void CheckMovementFailsafe()
    {
        float timeInMovingState = Time.time - movingToSlotStartTime;
        
        // Check if we've exceeded maximum resets or maximum time
        bool tooManyResets = movementResetCount >= maxMovementResets;
        bool tooMuchTime = timeInMovingState >= maxMovementTime;
        
        if (tooManyResets || tooMuchTime)
        {
            hasTriggeredFailsafe = true;
            
            if (showDebugLogs)
            {
                string reason = tooManyResets ? 
                    $"too many movement resets ({movementResetCount}/{maxMovementResets})" :
                    $"too much time in MovingToSlot state ({timeInMovingState:F2}s/{maxMovementTime:F2}s)";
                    
                Debug.LogWarning($"Chicken {gameObject.name}: Movement failsafe triggered due to {reason}. Entering FailsafeMovement state.");
            }
            
            // Force transition to FailsafeMovement state instead of FollowingSlot - CHANGED
            stateController.SetFailsafeMovement();
            
            // Stop discrete movement - failsafe movement will be handled differently
            StopMovement();
        }
    }
    
    // NEW: Start failsafe movement
    void StartFailsafeMovement()
    {
        Vector3? slotPosition = registration.GetAssignedSlotPosition();
    
        if (slotPosition.HasValue)
        {
            startPosition = transform.position;
            targetPosition = slotPosition.Value;
        
            // Don't calculate duration - we'll use constant speed instead
            // actualMovementDuration is not needed for failsafe movement
        
            movementTimer = 0f;
            slotCheckTimer = 0f;
            isMoving = true;
        
            if (showDebugLogs)
            {
                float distance = Vector3.Distance(startPosition, targetPosition);
                float estimatedTime = distance / failsafeMovementSpeed;
                Debug.Log($"Chicken {gameObject.name}: Started FAILSAFE movement to slot at {targetPosition} (distance: {distance:F2}, estimated time: {estimatedTime:F2}s at speed {failsafeMovementSpeed})");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: No assigned slot position found during failsafe movement!");
        
            // If no slot, just go to following state anyway
            stateController.SetFollowingSlot();
        }
    }
    
    // NEW: Update failsafe movement
    void UpdateFailsafeMovement()
    {
        movementTimer += Time.deltaTime; // Still track time for debug purposes
    
        // Still check for slot changes during failsafe movement
        slotCheckTimer += Time.deltaTime;
        if (slotCheckTimer >= slotChangeDetectionInterval)
        {
            CheckForSlotChangesDuringFailsafe();
            slotCheckTimer = 0f;
        }
    
        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
    
        // Check if we've arrived (within a small threshold)
        if (distanceToTarget <= 0.1f)
        {
            ArrivedAtSlotFromFailsafe();
            return;
        }
    
        // Move at constant speed
        Vector3 direction = (targetPosition - transform.position).normalized;
        float moveDistance = failsafeMovementSpeed * Time.deltaTime;
    
        // Make sure we don't overshoot the target
        if (moveDistance >= distanceToTarget)
        {
            transform.position = targetPosition;
            ArrivedAtSlotFromFailsafe();
        }
        else
        {
            transform.position += direction * moveDistance;
        }
    }
    
    // NEW: Check for slot changes during failsafe movement
    void CheckForSlotChangesDuringFailsafe()
    {
        Vector3? currentSlotPosition = registration.GetAssignedSlotPosition();
    
        if (currentSlotPosition.HasValue)
        {
            Vector3 newTarget = currentSlotPosition.Value;
        
            if (Vector3.Distance(targetPosition, newTarget) > 0.1f)
            {
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Slot changed during failsafe movement. Updating target.");
            
                // Just update the target position - no need to recalculate anything else
                targetPosition = newTarget;
            
                // Optional: Update start position to current position for smoother direction change
                startPosition = transform.position;
            }
        }
    }
    
    // NEW: Handle arrival from failsafe movement
    void ArrivedAtSlotFromFailsafe()
    {
        transform.position = targetPosition;
        
        // Transition to FollowingSlot state after failsafe movement
        stateController.SetFollowingSlot();
        
        isMoving = false;
        movementTimer = 0f;
        slotCheckTimer = 0f;
        
        // Reset failsafe tracking for future use
        ResetFailsafeTracking();
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: Arrived at slot via FAILSAFE movement, now following slot");
    }
    
    void StartMovingToSlot()
    {
        Vector3? slotPosition = registration.GetAssignedSlotPosition();
        
        if (slotPosition.HasValue)
        {
            startPosition = transform.position;
            targetPosition = slotPosition.Value;
            
            float variation = Random.Range(0f, durationVariationRange);
            actualMovementDuration = movementDuration + variation;
            
            movementTimer = 0f;
            slotCheckTimer = 0f;
            isMoving = true;
            idleStateTimer = 0f;
            
            // Initialize failsafe tracking when starting MovingToSlot
            if (!hasTriggeredFailsafe)
            {
                movingToSlotStartTime = Time.time;
                movementResetCount = 0;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"Chicken {gameObject.name}: Started moving to slot at {targetPosition} (duration: {actualMovementDuration:F2}s)");
            }
            
            wasInSpawnArea = false;
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: No assigned slot position found!");
        }
    }

    void StartMovingToSpawn()
    {
        Vector3? spawnPosition = GetValidSpawnPosition();
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: StartMovingToSpawn - Got spawn position: {(spawnPosition.HasValue ? spawnPosition.Value.ToString() : "NULL")}");
        
        if (spawnPosition.HasValue)
        {
            startPosition = transform.position;
            targetPosition = spawnPosition.Value;
            
            float variation = Random.Range(0f, durationVariationRange);
            actualMovementDuration = movementDuration + variation;
            
            movementTimer = 0f;
            isMoving = true;
            
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Started moving to spawn area at {targetPosition} from {startPosition} (duration: {actualMovementDuration:F2}s)");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: Could not get valid spawn position for idle movement!");
        }
    }

    void UpdateMovementToSlot()
    {
        movementTimer += Time.deltaTime;
        
        // Check for slot changes during movement
        if (trackSlotChanges && stateController.IsMovingToSlot)
        {
            slotCheckTimer += Time.deltaTime;
            if (slotCheckTimer >= slotChangeDetectionInterval)
            {
                CheckForSlotChangesWhileMoving();
                slotCheckTimer = 0f;
            }
        }
        
        float normalizedTime = movementTimer / actualMovementDuration;

        if (normalizedTime >= 1f)
        {
            ArrivedAtSlot();
            return;
        }

        float curveValue = movementCurve.Evaluate(normalizedTime);
        Vector3 newPosition = Vector3.LerpUnclamped(startPosition, targetPosition, curveValue);
        transform.position = newPosition;
    }

    void CheckForSlotChangesWhileMoving()
    {
        Vector3? currentSlotPosition = registration.GetAssignedSlotPosition();
        
        if (currentSlotPosition.HasValue)
        {
            Vector3 newTarget = currentSlotPosition.Value;
            
            if (Vector3.Distance(targetPosition, newTarget) > 0.1f)
            {
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Slot changed during movement. Updating target.");
                
                UpdateMovementTarget(newTarget);
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: Lost slot assignment during movement!");
            
            StopMovement();
            stateController.SetIdle();
        }
    }

    void UpdateMovementTarget(Vector3 newTarget)
    {
        startPosition = transform.position;
        targetPosition = newTarget;
        
        float variation = Random.Range(0f, durationVariationRange);
        actualMovementDuration = movementDuration + variation;
        
        movementTimer = 0f;
        
        // Track movement resets for failsafe
        if (enableMovementFailsafe && !hasTriggeredFailsafe)
        {
            movementResetCount++;
            
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Updated movement target to {newTarget} (reset count: {movementResetCount}/{maxMovementResets})");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"Chicken {gameObject.name}: Updated movement target to {newTarget}");
        }
    }

    void ResetFailsafeTracking()
    {
        movementResetCount = 0;
        movingToSlotStartTime = 0f;
        hasTriggeredFailsafe = false;
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: Reset failsafe tracking");
    }

    void UpdateMovementToSpawn()
    {
        movementTimer += Time.deltaTime;
        float normalizedTime = movementTimer / actualMovementDuration;

        if (normalizedTime >= 1f)
        {
            ArrivedAtSpawn();
            return;
        }

        float curveValue = movementCurve.Evaluate(normalizedTime);
        Vector3 newPosition = Vector3.LerpUnclamped(startPosition, targetPosition, curveValue);
        transform.position = newPosition;
    }

    void ArrivedAtSlot()
    {
        transform.position = targetPosition;

        if (stateController != null)
        {
            if (stateController.IsMovingToSlot)
            {
                stateController.SetFollowingSlot();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Arrived at slot, now following slot");
            }
        }

        isMoving = false;
        movementTimer = 0f;
        slotCheckTimer = 0f;
        
        // Reset failsafe tracking on successful arrival
        ResetFailsafeTracking();
    }

    void ArrivedAtSpawn()
    {
        transform.position = targetPosition;
        isMoving = false;
        wasInSpawnArea = true;
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: Arrived at spawn area");
    }

    void StopMovement()
    {
        isMoving = false;
        movementTimer = 0f;
        slotCheckTimer = 0f;
        idleStateTimer = 0f;
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: Stopped movement");
    }

    bool IsInSpawnArea()
    {
        if (EnemySpawner.Instance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: IsInSpawnArea - EnemySpawner.Instance is null!");
            return false;
        }

        bool inSpawnArea = IsPositionInValidSpawnArea(transform.position);
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: IsInSpawnArea - Position {transform.position} is in spawn area: {inSpawnArea}");

        return inSpawnArea;
    }

    bool IsPositionInValidSpawnArea(Vector3 position)
    {
        if (EnemySpawner.Instance == null)
            return false;

        return EnemySpawner.Instance.IsPositionInValidSpawnArea(position);
    }

    Vector3? GetValidSpawnPosition()
    {
        if (EnemySpawner.Instance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: GetValidSpawnPosition - No EnemySpawner instance found!");
            return null;
        }

        Vector3? result = EnemySpawner.Instance.GetValidSpawnPositionForIdle();
        
        if (showDebugLogs)
        {
            if (result.HasValue)
                Debug.Log($"Chicken {gameObject.name}: GetValidSpawnPosition - Found valid position: {result.Value}");
            else
                Debug.LogWarning($"Chicken {gameObject.name}: GetValidSpawnPosition - EnemySpawner returned null position!");
        }

        return result;
    }

    public void RefreshMovementState()
    {
        if (stateController == null || registration == null)
            return;
            
        StopMovement(); // This will NOT reset failsafe tracking anymore
        idleStateTimer = 0f;
        
        if (stateController.IsMovingToSlot)
        {
            StartMovingToSlot();
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: RefreshMovementState - Starting slot movement");
        }
        else if (stateController.IsFailsafeMovement) // NEW
        {
            StartFailsafeMovement();
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: RefreshMovementState - Starting failsafe movement");
        }
        else if (stateController.IsFollowingSlot)
        {
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: RefreshMovementState - Now following slot directly");
        }
        else if (stateController.IsIdle)
        {
            if (moveToSpawnWhenIdle && !IsInSpawnArea())
            {
                idleStateTimer = spawnCheckDelay;
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: RefreshMovementState - Will check spawn movement");
            }
        }
    }

    // Public properties
    public bool IsCurrentlyMoving => isMoving || IsActivelyFollowing;
    public bool IsActivelyFollowing 
    { 
        get 
        {
            if (stateController != null && stateController.IsFollowingSlot && registration != null)
            {
                Vector3? slotPos = registration.GetAssignedSlotPosition();
                if (slotPos.HasValue)
                {
                    float distance = Vector3.Distance(transform.position, slotPos.Value);
                    return distance > followingDistanceThreshold;
                }
            }
            return false;
        }
    }
    public Vector3 CurrentTarget 
    { 
        get 
        {
            if (stateController != null && stateController.IsFollowingSlot && registration != null)
            {
                Vector3? slotPos = registration.GetAssignedSlotPosition();
                return slotPos ?? targetPosition;
            }
            return targetPosition;
        }
    }
    public float MovementProgress => isMoving ? (movementTimer / actualMovementDuration) : 0f;
    public float TimeRemaining => isMoving ? Mathf.Max(0f, actualMovementDuration - movementTimer) : 0f;
    public float ActualDuration => actualMovementDuration;
    public bool WasInSpawnArea => wasInSpawnArea;
    public float IdleTime => idleStateTimer;
    public bool IsFollowingSlot => stateController != null && stateController.IsFollowingSlot;
    public bool IsInFailsafeMovement => stateController != null && stateController.IsFailsafeMovement; // NEW
    
    // Failsafe properties
    public int MovementResetCount => movementResetCount;
    public float TimeInMovingState => (stateController != null && stateController.IsMovingToSlot && movingToSlotStartTime > 0) ? 
        Time.time - movingToSlotStartTime : 0f;
    public bool HasTriggeredFailsafe => hasTriggeredFailsafe;
    public bool IsFailsafeEnabled => enableMovementFailsafe;

    // Context menu methods
    [ContextMenu("Test Idle Movement Logic")]
    void ContextMenuTestIdleMovement()
    {
        Debug.Log($"=== Testing Idle Movement for {gameObject.name} ===");
        Debug.Log($"Current State: {(stateController ? stateController.CurrentState.ToString() : "NULL STATE CONTROLLER")}");
        Debug.Log($"moveToSpawnWhenIdle: {moveToSpawnWhenIdle}");
        Debug.Log($"isMoving: {isMoving}");
        Debug.Log($"Current Position: {transform.position}");
        
        if (EnemySpawner.Instance == null)
        {
            Debug.LogError("EnemySpawner.Instance is NULL!");
            return;
        }
        
        bool inSpawnArea = IsInSpawnArea();
        Debug.Log($"Is currently in spawn area: {inSpawnArea}");
        
        if (EnemySpawner.Instance.HasValidSpawnAreas())
        {
            Bounds? spawnBounds = EnemySpawner.Instance.GetSpawnAreaBounds();
            Bounds? blockerBounds = EnemySpawner.Instance.GetBlockerAreaBounds();
            
            Debug.Log($"Spawn area bounds: {(spawnBounds.HasValue ? spawnBounds.Value.ToString() : "NULL")}");
            Debug.Log($"Blocker area bounds: {(blockerBounds.HasValue ? blockerBounds.Value.ToString() : "NULL")}");
        }
        else
        {
            Debug.LogError("EnemySpawner has no valid spawn areas!");
        }
        
        Vector3? testSpawnPos = GetValidSpawnPosition();
        Debug.Log($"Test spawn position: {(testSpawnPos.HasValue ? testSpawnPos.Value.ToString() : "NULL")}");
        
        // Force idle and test movement
        if (stateController != null)
        {
            stateController.SetIdle();
            idleStateTimer = spawnCheckDelay;
            CheckAndStartSpawnMovement();
        }
    }

    [ContextMenu("Force Start Moving to Spawn")]
    void ContextMenuForceStartMovingToSpawn()
    {
        if (stateController != null)
        {
            stateController.SetIdle();
            idleStateTimer = spawnCheckDelay;
            CheckAndStartSpawnMovement();
        }
    }

    [ContextMenu("Force Start Moving to Slot")]
    void ContextMenuForceStartMoving()
    {
        if (stateController != null)
        {
            stateController.SetMovingToSlot();
        }
    }

    [ContextMenu("Force Start Following Slot")]
    void ContextMenuForceStartFollowing()
    {
        if (stateController != null)
        {
            stateController.SetFollowingSlot();
        }
    }
    
    [ContextMenu("Force Start Failsafe Movement")] // NEW
    void ContextMenuForceFailsafeMovement()
    {
        if (stateController != null)
        {
            hasTriggeredFailsafe = true;
            stateController.SetFailsafeMovement();
        }
    }

    [ContextMenu("Force Stop Moving")]
    void ContextMenuForceStopMoving()
    {
        StopMovement();
        if (stateController != null)
        {
            stateController.SetIdle();
        }
    }
    
    [ContextMenu("Test Failsafe Trigger")] // NEW
    void ContextMenuTestFailsafe()
    {
        if (stateController != null && stateController.IsMovingToSlot)
        {
            // Force trigger the failsafe conditions
            movementResetCount = maxMovementResets + 1;
            CheckMovementFailsafe();
        }
        else
        {
            Debug.Log($"Chicken {gameObject.name}: Must be in MovingToSlot state to test failsafe");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showMovementGizmos)
            return;

        // Show discrete movement
        if (isMoving)
        {
            if (stateController != null)
            {
                if (stateController.IsMovingToSlot)
                {
                    Gizmos.color = movementLineColor;
                    Gizmos.DrawLine(transform.position, targetPosition);
                    Gizmos.DrawWireSphere(targetPosition, 0.2f);
                    
                    // Show movement progress
                    float progress = movementTimer / actualMovementDuration;
                    Vector3 progressPos = Vector3.Lerp(startPosition, targetPosition, progress);
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(progressPos + Vector3.up * 0.3f, 0.05f);
                }
                else if (stateController.IsFailsafeMovement) // NEW
                {
                    Gizmos.color = failsafeMovementColor;
                    Gizmos.DrawLine(transform.position, targetPosition);
                    Gizmos.DrawWireSphere(targetPosition, 0.25f);
                    
                    // Show failsafe movement progress
                    float progress = movementTimer / actualMovementDuration;
                    Vector3 progressPos = Vector3.Lerp(startPosition, targetPosition, progress);
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(progressPos + Vector3.up * 0.4f, 0.08f);
                    
                    // Draw "FAILSAFE" indicator
                    Gizmos.DrawWireCube(transform.position + Vector3.up, Vector3.one * 0.3f);
                }
                else if (stateController.IsIdle)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, targetPosition);
                    Gizmos.DrawWireSphere(targetPosition, spawnAreaArrivalDistance);
                }
            }
        }
        // Show direct following (FollowingSlot)
        else if (stateController != null && stateController.IsFollowingSlot)
        {
            Vector3? slotPosition = registration?.GetAssignedSlotPosition();
            if (slotPosition.HasValue)
            {
                float distance = Vector3.Distance(transform.position, slotPosition.Value);
                
                if (distance <= followingDistanceThreshold)
                {
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.color = Color.yellow;
                }
                
                Gizmos.DrawLine(transform.position, slotPosition.Value);
                Gizmos.DrawWireSphere(slotPosition.Value, followingDistanceThreshold);
                
                if (distance > followingDistanceThreshold)
                {
                    Vector3 direction = (slotPosition.Value - transform.position).normalized;
                    Vector3 arrowPos = transform.position + direction * 0.5f + Vector3.up * 0.5f;
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(arrowPos, 0.05f);
                }
            }
        }
    }
}