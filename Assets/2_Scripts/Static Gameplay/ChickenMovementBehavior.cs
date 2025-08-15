using UnityEngine;

public class ChickenMovementBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float initialMovementDuration = 3f;
    public float formationMovementDuration = 1.8f;
    public float durationVariationRange = 0.5f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Slot Tracking")]
    public bool trackSlotChanges = true;
    public float slotChangeDetectionInterval = 0.1f;
    public bool trackSlotWhileInCombat = true;
    public float combatSlotDistanceThreshold = 1f;
    
    [Header("Spawn Area Settings")]
    public bool moveToSpawnWhenIdle = true;
    public string spawnAreaTag = "ChickenSpawner";
    public float spawnAreaArrivalDistance = 0.5f;
    public float spawnCheckDelay = 0.2f; // Delay before checking spawn movement to avoid rapid state changes
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool showMovementGizmos = true;
    public Color movementLineColor = Color.yellow;

    private ChickenStateController stateController;
    private EnemyChickenRegistration registration;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float actualMovementDuration;
    private float movementTimer = 0f;
    private float slotCheckTimer = 0f;
    private bool isMovingToSlot = false;
    private bool isMovingToSpawn = false;
    private bool wasInSpawnArea = false;
    
    // New variables to fix spawn movement issues
    private float idleStateTimer = 0f; // Track how long we've been idle
    private bool hasCheckedSpawnThisFrame = false; // Prevent multiple spawn checks per frame

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
        
        // Initialize spawn area status
        wasInSpawnArea = IsInSpawnArea();
    }

    void Update()
    {
        if (stateController == null || registration == null)
            return;

        hasCheckedSpawnThisFrame = false; // Reset flag each frame
        
        HandleMovement();
        HandleCombatSlotTracking();
        HandleIdleTimer();
    }

    // New method to handle idle timer and spawn movement
    void HandleIdleTimer()
    {
        if (stateController.IsIdle)
        {
            idleStateTimer += Time.deltaTime;
            
            // Update spawn area status for idle chickens
            if (!isMovingToSpawn)
            {
                wasInSpawnArea = IsInSpawnArea();
            }
            
            // Check for spawn movement after a small delay to avoid rapid state changes
            if (idleStateTimer >= spawnCheckDelay && !hasCheckedSpawnThisFrame)
            {
                CheckAndStartSpawnMovement();
                hasCheckedSpawnThisFrame = true;
            }
        }
        else
        {
            // Reset timer when not idle
            idleStateTimer = 0f;
        }
    }

    // New method to handle spawn movement logic with better debugging
    void CheckAndStartSpawnMovement()
    {
        // Early exit conditions
        if (!moveToSpawnWhenIdle)
        {
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: moveToSpawnWhenIdle is disabled");
            return;
        }
        
        if (isMovingToSlot || isMovingToSpawn)
        {
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Already moving (slot: {isMovingToSlot}, spawn: {isMovingToSpawn})");
            return;
        }

        bool currentlyInSpawnArea = IsInSpawnArea();
        
        if (showDebugLogs)
        {
            Debug.Log($"Chicken {gameObject.name}: Checking spawn movement - In spawn area: {currentlyInSpawnArea}, Was in spawn: {wasInSpawnArea}");
        }

        // Only move to spawn if not currently in a spawn area
        if (!currentlyInSpawnArea)
        {
            // Check if spawn areas exist
            GameObject[] spawnAreas = GameObject.FindGameObjectsWithTag(spawnAreaTag);
            if (spawnAreas.Length == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"Chicken {gameObject.name}: No spawn areas found with tag '{spawnAreaTag}'! Cannot move to spawn.");
                return;
            }
            
            StartMovingToSpawn();
        }
        else if (showDebugLogs)
        {
            Debug.Log($"Chicken {gameObject.name}: Already in spawn area, no movement needed");
        }
    }

    void HandleMovement()
    {
        if (stateController.IsMovingToSlotOnce && !isMovingToSlot && !isMovingToSpawn)
        {
            StartMovingToSlot();
        }
        else if (stateController.IsMovingInsideFormation && !isMovingToSlot && !isMovingToSpawn)
        {
            Vector3? slotPosition = registration.GetAssignedSlotPosition();
            if (slotPosition.HasValue)
            {
                StartMovingInsideFormation(slotPosition.Value);
            }
        }
        else if (!stateController.IsMovingToSlotOnce && !stateController.IsMovingInsideFormation && !stateController.IsIdle && (isMovingToSlot || isMovingToSpawn))
        {
            StopAllMovement();
        }
        else if ((stateController.IsMovingToSlotOnce || stateController.IsMovingInsideFormation) && isMovingToSlot)
        {
            UpdateMovementToSlot();
        }
        else if (stateController.IsIdle && isMovingToSpawn)
        {
            UpdateMovementToSpawn();
        }
    }

    void HandleCombatSlotTracking()
    {
        if (!trackSlotWhileInCombat || !stateController.IsInCombat)
            return;

        slotCheckTimer += Time.deltaTime;
        if (slotCheckTimer >= slotChangeDetectionInterval)
        {
            CheckForCombatSlotChanges();
            slotCheckTimer = 0f;
        }
    }

    void CheckForCombatSlotChanges()
    {
        Vector3? currentSlotPosition = registration.GetAssignedSlotPosition();
        
        if (currentSlotPosition.HasValue)
        {
            Vector3 newTarget = currentSlotPosition.Value;
            float distanceToSlot = Vector3.Distance(transform.position, newTarget);
            
            if (distanceToSlot > combatSlotDistanceThreshold)
            {
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: In combat but slot moved. Distance: {distanceToSlot:F2}, starting MovingInsideFormation");
                
                stateController.SetMovingInsideFormation();
                StartMovingInsideFormation(newTarget);
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: Lost slot assignment while in combat!");
            
            stateController.SetIdle();
        }
    }
    void StartMovingToSlot()
    {
        Vector3? slotPosition = registration.GetAssignedSlotPosition();
        
        if (slotPosition.HasValue)
        {
            startPosition = transform.position;
            targetPosition = slotPosition.Value;
            
            // Always use initial movement duration when moving to slot (coming from outside formation)
            float variation = Random.Range(0f, durationVariationRange);
            actualMovementDuration = initialMovementDuration + variation;
            
            movementTimer = 0f;
            slotCheckTimer = 0f;
            isMovingToSlot = true;
            isMovingToSpawn = false;
            idleStateTimer = 0f; // Reset idle timer
            
            if (showDebugLogs)
            {
                Debug.Log($"Chicken {gameObject.name}: Started moving to slot at {targetPosition} (using initial duration: {actualMovementDuration:F2}s, +{variation:F2}s added)");
            }
            
            // Clear spawn area flag once we start moving to formation
            wasInSpawnArea = false;
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: No assigned slot position found!");
        }
    }

    void StartMovingInsideFormation(Vector3 newSlotPosition)
    {
        startPosition = transform.position;
        targetPosition = newSlotPosition;
        
        float variation = Random.Range(0f, durationVariationRange);
        actualMovementDuration = formationMovementDuration + variation;
        
        movementTimer = 0f;
        slotCheckTimer = 0f;
        isMovingToSlot = true;
        isMovingToSpawn = false;
        idleStateTimer = 0f; // Reset idle timer
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: Started moving inside formation to {targetPosition} (duration: {actualMovementDuration:F2}s, +{variation:F2}s added)");
    }

    void StartMovingToSpawn()
    {
        Vector3? spawnPosition = GetRandomSpawnPosition();
        
        if (spawnPosition.HasValue)
        {
            startPosition = transform.position;
            targetPosition = spawnPosition.Value;
            
            float variation = Random.Range(0f, durationVariationRange);
            actualMovementDuration = formationMovementDuration + variation;
            
            movementTimer = 0f;
            slotCheckTimer = 0f;
            isMovingToSpawn = true;
            isMovingToSlot = false;
            
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Started moving to spawn area at {targetPosition} (duration: {actualMovementDuration:F2}s, +{variation:F2}s added)");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: Failed to get spawn position!");
        }
    }

    bool IsInSpawnArea()
    {
        GameObject[] spawnAreas = GameObject.FindGameObjectsWithTag(spawnAreaTag);
        
        if (spawnAreas.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: No spawn areas found with tag '{spawnAreaTag}' when checking IsInSpawnArea");
            return false;
        }
        
        foreach (GameObject spawnArea in spawnAreas)
        {
            BoxCollider spawnCollider = spawnArea.GetComponent<BoxCollider>();
            if (spawnCollider != null)
            {
                if (spawnCollider.bounds.Contains(transform.position))
                {
                    return true;
                }
            }
            else
            {
                // Fallback: check distance to spawn area center if no collider
                float distanceToSpawn = Vector3.Distance(transform.position, spawnArea.transform.position);
                if (distanceToSpawn <= spawnAreaArrivalDistance)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    Vector3? GetRandomSpawnPosition()
    {
        GameObject[] spawnAreas = GameObject.FindGameObjectsWithTag(spawnAreaTag);
        
        if (spawnAreas.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: No spawn areas found with tag '{spawnAreaTag}'!");
            return null;
        }
        
        GameObject selectedSpawnArea = spawnAreas[Random.Range(0, spawnAreas.Length)];
        BoxCollider spawnCollider = selectedSpawnArea.GetComponent<BoxCollider>();
        
        if (spawnCollider == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Spawn area {selectedSpawnArea.name} has no BoxCollider! Using transform position.");
            return selectedSpawnArea.transform.position;
        }
        
        Bounds bounds = spawnCollider.bounds;
        Vector3 randomPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
        
        return randomPoint;
    }

    void UpdateMovementToSlot()
    {
        movementTimer += Time.deltaTime;
        
        if (trackSlotChanges && stateController.IsMovingToSlotOnce)
        {
            slotCheckTimer += Time.deltaTime;
            if (slotCheckTimer >= slotChangeDetectionInterval)
            {
                CheckForSlotChanges();
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

    void CheckForSlotChanges()
    {
        Vector3? currentSlotPosition = registration.GetAssignedSlotPosition();
        
        if (currentSlotPosition.HasValue)
        {
            Vector3 newTarget = currentSlotPosition.Value;
            
            if (Vector3.Distance(targetPosition, newTarget) > 0.01f)
            {
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Slot position changed during movement. Old: {targetPosition}, New: {newTarget}");
                
                UpdateMovementTarget(newTarget);
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: Lost slot assignment during movement!");
            
            StopAllMovement();
        }
    }

    void UpdateMovementTarget(Vector3 newTarget)
    {
        startPosition = transform.position;
        targetPosition = newTarget;
        
        float variation = Random.Range(0f, durationVariationRange);
        
        // Use appropriate duration based on movement type
        if (stateController != null && stateController.IsMovingToSlotOnce)
        {
            // First time moving to slot - use initial duration
            actualMovementDuration = initialMovementDuration + variation;
        }
        else
        {
            // Moving inside formation - use formation duration
            actualMovementDuration = formationMovementDuration + variation;
        }
        
        movementTimer = 0f;
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: Updated movement target. New full duration: {actualMovementDuration:F2}s (+{variation:F2}s variation)");
    }

    void ArrivedAtSlot()
    {
        transform.position = targetPosition;

        if (stateController != null)
        {
            if (stateController.IsMovingToSlotOnce)
            {
                stateController.SetInCombat();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Arrived at slot for first time, now in combat state");
            }
            else if (stateController.IsMovingInsideFormation)
            {
                stateController.SetInCombat();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Arrived at new slot position, returning to combat state");
            }
        }

        StopMovingToSlot();
    }

    void ArrivedAtSpawn()
    {
        transform.position = targetPosition;
        isMovingToSpawn = false;
        wasInSpawnArea = true;
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: Arrived at spawn area");
    }

    void StopAllMovement()
    {
        isMovingToSlot = false;
        isMovingToSpawn = false;
        movementTimer = 0f;
        slotCheckTimer = 0f;
        idleStateTimer = 0f; // Reset idle timer
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: Stopped all movement");
    }

    void StopMovingToSlot()
    {
        isMovingToSlot = false;
        movementTimer = 0f;
        slotCheckTimer = 0f;
        
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: Stopped moving to slot");
    }

    public void ForceStopMovement()
    {
        StopAllMovement();
    }

    public bool IsCurrentlyMoving
    {
        get { return isMovingToSlot || isMovingToSpawn; }
    }

    public Vector3 CurrentTarget
    {
        get { return targetPosition; }
    }

    public float MovementProgress
    {
        get { return (isMovingToSlot || isMovingToSpawn) ? (movementTimer / actualMovementDuration) : 0f; }
    }

    public float TimeRemaining
    {
        get { return (isMovingToSlot || isMovingToSpawn) ? Mathf.Max(0f, actualMovementDuration - movementTimer) : 0f; }
    }

    public float ActualDuration
    {
        get { return actualMovementDuration; }
    }

    public bool IsSlotTrackingEnabled
    {
        get { return trackSlotChanges; }
    }

    public bool IsCombatSlotTrackingEnabled
    {
        get { return trackSlotWhileInCombat; }
    }

    public bool IsMovingToSpawn
    {
        get { return isMovingToSpawn; }
    }

    public bool WasInSpawnArea
    {
        get { return wasInSpawnArea; }
    }

    // New property to check idle timer
    public float IdleTime
    {
        get { return idleStateTimer; }
    }

    // New method to refresh movement state (called by registration system)
    public void RefreshMovementState()
    {
        if (stateController == null || registration == null)
            return;
            
        // Stop any current movement
        StopAllMovement();
        
        // Reset timers
        idleStateTimer = 0f;
        
        // Check what state we should be in and start appropriate movement
        if (stateController.IsMovingToSlotOnce)
        {
            StartMovingToSlot();
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: RefreshMovementState - Starting slot movement");
        }
        else if (stateController.IsMovingInsideFormation)
        {
            Vector3? slotPosition = registration.GetAssignedSlotPosition();
            if (slotPosition.HasValue)
            {
                StartMovingInsideFormation(slotPosition.Value);
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: RefreshMovementState - Starting formation movement");
            }
        }
        else if (stateController.IsIdle)
        {
            // For idle state, we'll let the normal update cycle handle spawn movement
            // but we can force check it if needed
            if (moveToSpawnWhenIdle && !IsInSpawnArea())
            {
                idleStateTimer = spawnCheckDelay; // Set timer so spawn check happens immediately
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: RefreshMovementState - Will check spawn movement immediately");
            }
        }
    }

    [ContextMenu("Force Start Moving to Slot")]
    void ContextMenuForceStartMoving()
    {
        if (stateController != null)
        {
            stateController.SetMovingToSlotOnce();
        }
    }

    [ContextMenu("Force Start Moving to Spawn")]
    void ContextMenuForceStartMovingToSpawn()
    {
        if (stateController != null)
        {
            stateController.SetIdle();
            // Force spawn movement immediately
            idleStateTimer = spawnCheckDelay;
            CheckAndStartSpawnMovement();
        }
    }

    [ContextMenu("Force Start Moving Inside Formation")]
    void ContextMenuForceStartMovingInsideFormation()
    {
        if (stateController != null)
        {
            stateController.SetMovingInsideFormation();
        }
    }

    [ContextMenu("Force Stop Moving")]
    void ContextMenuForceStopMoving()
    {
        ForceStopMovement();
        if (stateController != null)
        {
            stateController.SetIdle();
        }
    }

    [ContextMenu("Check Spawn Areas")]
    void ContextMenuCheckSpawnAreas()
    {
        GameObject[] spawnAreas = GameObject.FindGameObjectsWithTag(spawnAreaTag);
        Debug.Log($"=== SPAWN AREA CHECK ===");
        Debug.Log($"Spawn Area Tag: '{spawnAreaTag}'");
        Debug.Log($"Found {spawnAreas.Length} spawn areas");
        
        for (int i = 0; i < spawnAreas.Length; i++)
        {
            GameObject spawn = spawnAreas[i];
            BoxCollider collider = spawn.GetComponent<BoxCollider>();
            bool hasCollider = collider != null;
            bool isInside = false;
            
            if (hasCollider)
            {
                isInside = collider.bounds.Contains(transform.position);
            }
            else
            {
                float distance = Vector3.Distance(transform.position, spawn.transform.position);
                isInside = distance <= spawnAreaArrivalDistance;
            }
            
            Debug.Log($"  {i}: {spawn.name} - Has Collider: {hasCollider}, Inside: {isInside}");
        }
        
        Debug.Log($"Current IsInSpawnArea(): {IsInSpawnArea()}");
        Debug.Log($"WasInSpawnArea: {wasInSpawnArea}");
        Debug.Log($"Move to Spawn When Idle: {moveToSpawnWhenIdle}");
        Debug.Log($"Idle Time: {idleStateTimer:F2}s (check delay: {spawnCheckDelay:F2}s)");
    }

    [ContextMenu("Toggle Slot Tracking")]
    void ContextMenuToggleSlotTracking()
    {
        trackSlotChanges = !trackSlotChanges;
        Debug.Log($"Chicken {gameObject.name}: Slot tracking {(trackSlotChanges ? "enabled" : "disabled")}");
    }

    [ContextMenu("Toggle Combat Slot Tracking")]
    void ContextMenuToggleCombatSlotTracking()
    {
        trackSlotWhileInCombat = !trackSlotWhileInCombat;
        Debug.Log($"Chicken {gameObject.name}: Combat slot tracking {(trackSlotWhileInCombat ? "enabled" : "disabled")}");
    }

    [ContextMenu("Toggle Move to Spawn When Idle")]
    void ContextMenuToggleMoveToSpawn()
    {
        moveToSpawnWhenIdle = !moveToSpawnWhenIdle;
        Debug.Log($"Chicken {gameObject.name}: Move to spawn when idle {(moveToSpawnWhenIdle ? "enabled" : "disabled")}");
    }

    [ContextMenu("Force Fix Movement State")]
    void ContextMenuForceFixMovementState()
    {
        if (stateController == null || registration == null)
        {
            Debug.LogError("Missing required components!");
            return;
        }
        
        bool hasSlot = registration.IsAssignedToSlot();
        var currentState = stateController.CurrentState;
        
        Debug.Log($"=== MOVEMENT STATE DEBUG ===");
        Debug.Log($"Chicken: {gameObject.name}");
        Debug.Log($"Has Slot: {hasSlot}");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Is Moving To Slot: {isMovingToSlot}");
        Debug.Log($"Is Moving To Spawn: {isMovingToSpawn}");
        Debug.Log($"Idle Time: {idleStateTimer:F2}s");
        Debug.Log($"Move to Spawn When Idle: {moveToSpawnWhenIdle}");
        Debug.Log($"Is In Spawn Area: {IsInSpawnArea()}");
        
        if (hasSlot && currentState == ChickenStateController.ChickenState.Idle)
        {
            Debug.LogWarning("Fixing: Chicken has slot but is idle - forcing to MovingToSlotOnce");
            stateController.SetMovingToSlotOnce();
            RefreshMovementState();
        }
        else if (!hasSlot && currentState != ChickenStateController.ChickenState.Idle)
        {
            Debug.LogWarning("Fixing: Chicken has no slot but isn't idle - forcing to Idle");
            stateController.SetIdle();
            RefreshMovementState();
        }
        else if (hasSlot && (currentState == ChickenStateController.ChickenState.MovingToSlotOnce || currentState == ChickenStateController.ChickenState.MovingInsideFormation) && !isMovingToSlot)
        {
            Debug.LogWarning("Fixing: Chicken should be moving but movement behavior is inactive");
            RefreshMovementState();
        }
        else if (currentState == ChickenStateController.ChickenState.Idle && !hasSlot && !isMovingToSpawn && moveToSpawnWhenIdle)
        {
            Debug.LogWarning("Fixing: Chicken is idle without slot and should move to spawn");
            RefreshMovementState();
        }
        else
        {
            Debug.Log("No issues detected, refreshing movement state anyway");
            RefreshMovementState();
        }
    }

    [ContextMenu("Check Current Status")]
    void ContextMenuCheckCurrentStatus()
    {
        bool hasSlotAssigned = registration != null && registration.IsAssignedToSlot();
        Vector3? slotPosition = registration != null ? registration.GetAssignedSlotPosition() : null;
        bool inSpawnArea = IsInSpawnArea();
        
        Debug.Log($"=== CHICKEN STATUS DEBUG ===");
        Debug.Log($"Chicken: {gameObject.name}");
        Debug.Log($"Current State: {(stateController != null ? stateController.CurrentState.ToString() : "No State Controller")}");
        Debug.Log($"Has Slot Assigned: {hasSlotAssigned}");
        Debug.Log($"Slot Position: {(slotPosition.HasValue ? slotPosition.Value.ToString() : "None")}");
        Debug.Log($"Is In Spawn Area: {inSpawnArea}");
        Debug.Log($"Was In Spawn Area: {wasInSpawnArea}");
        Debug.Log($"Is Moving To Slot: {isMovingToSlot}");
        Debug.Log($"Is Moving To Spawn: {isMovingToSpawn}");
        Debug.Log($"Idle Time: {idleStateTimer:F2}s (check delay: {spawnCheckDelay:F2}s)");
        Debug.Log($"Move to Spawn When Idle: {moveToSpawnWhenIdle}");
        Debug.Log($"Distance to Slot: {(slotPosition.HasValue ? Vector3.Distance(transform.position, slotPosition.Value).ToString("F2") : "N/A")}");
        
        // Check spawn areas
        GameObject[] spawnAreas = GameObject.FindGameObjectsWithTag(spawnAreaTag);
        Debug.Log($"Spawn Areas Found: {spawnAreas.Length} with tag '{spawnAreaTag}'");
        
        if (hasSlotAssigned && stateController != null && stateController.IsIdle)
        {
            Debug.LogWarning("⚠️ POTENTIAL BUG: Chicken has slot assigned but is in Idle state!");
        }
        
        if (!hasSlotAssigned && stateController != null && stateController.IsIdle && !inSpawnArea && !isMovingToSpawn && moveToSpawnWhenIdle && spawnAreas.Length > 0)
        {
            Debug.LogWarning("⚠️ POTENTIAL BUG: Chicken should be moving to spawn but isn't!");
        }
    }

    [ContextMenu("Print Movement Info")]
    void ContextMenuPrintMovementInfo()
    {
        Debug.Log($"Chicken {gameObject.name} Movement Info:");
        Debug.Log($"  State: {(stateController != null ? stateController.CurrentState.ToString() : "No State Controller")}");
        Debug.Log($"  Is Moving to Slot: {isMovingToSlot}");
        Debug.Log($"  Is Moving to Spawn: {isMovingToSpawn}");
        Debug.Log($"  Was In Spawn Area: {wasInSpawnArea}");
        Debug.Log($"  Idle Time: {idleStateTimer:F2}s");
        Debug.Log($"  Target Position: {targetPosition}");
        Debug.Log($"  Initial Movement Duration: {initialMovementDuration:F2}s");
        Debug.Log($"  Formation Movement Duration: {formationMovementDuration:F2}s");
        Debug.Log($"  Actual Duration: {actualMovementDuration:F2}s");
        Debug.Log($"  Movement Progress: {MovementProgress:P}");
        Debug.Log($"  Time Remaining: {TimeRemaining:F1}s");
        Debug.Log($"  Move to Spawn When Idle: {moveToSpawnWhenIdle}");
        Debug.Log($"  Spawn Check Delay: {spawnCheckDelay:F2}s");
        Debug.Log($"  Spawn Area Tag: '{spawnAreaTag}'");
        Debug.Log($"  Slot Tracking: {(trackSlotChanges ? "Enabled" : "Disabled")}");
        Debug.Log($"  Combat Slot Tracking: {(trackSlotWhileInCombat ? "Enabled" : "Disabled")}");
        Debug.Log($"  Combat Distance Threshold: {combatSlotDistanceThreshold:F2}");
        Debug.Log($"  Assigned Slot: {(registration != null ? registration.GetAssignedSlotIndex().ToString() : "No Registration")}");
    }

    void OnDrawGizmos()
    {
        if (!showMovementGizmos)
            return;

        // Only draw gizmos if actually moving
        if (isMovingToSlot && (stateController.IsMovingToSlotOnce || stateController.IsMovingInsideFormation))
        {
            Gizmos.color = movementLineColor;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }
        else if (isMovingToSpawn && stateController.IsIdle)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, spawnAreaArrivalDistance);
        }
        
        // Debug: Show stuck state with red gizmo
        if (stateController != null && registration != null)
        {
            bool hasSlot = registration.IsAssignedToSlot();
            bool shouldBeMoving = hasSlot && (stateController.IsMovingToSlotOnce || stateController.IsMovingInsideFormation);
            bool shouldMoveToSpawn = !hasSlot && stateController.IsIdle && !IsInSpawnArea() && moveToSpawnWhenIdle && idleStateTimer >= spawnCheckDelay;
            
            if (shouldBeMoving && !isMovingToSlot)
            {
                // Chicken should be moving to slot but isn't - draw red warning
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 1f);
            }
            else if (shouldMoveToSpawn && !isMovingToSpawn)
            {
                // Chicken should be moving to spawn but isn't - draw orange warning
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.8f);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showMovementGizmos)
            return;

        if (isMovingToSlot || isMovingToSpawn)
        {
            #if UNITY_EDITOR
            string movementType = "";
            if (isMovingToSpawn)
            {
                movementType = "To Spawn";
            }
            else if (stateController != null)
            {
                if (stateController.IsMovingToSlotOnce)
                    movementType = "Initial";
                else if (stateController.IsMovingInsideFormation)
                    movementType = "Formation";
            }
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Moving ({movementType})\nProgress: {MovementProgress:P}\nTime: {TimeRemaining:F1}s\nDuration: {ActualDuration:F1}s\nIdle: {IdleTime:F1}s");
            #endif
        }
        else if (stateController != null && stateController.IsIdle)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Idle\nTime: {IdleTime:F1}s\nIn Spawn: {IsInSpawnArea()}\nWill Move: {(moveToSpawnWhenIdle && !IsInSpawnArea())}");
            #endif
        }

        if (isMovingToSlot || isMovingToSpawn)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPosition, 0.1f);
        }
    }
}