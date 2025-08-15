using UnityEngine;

public class EnemyChickenRegistration : MonoBehaviour
{
    [Header("Registration Settings")]
    public bool autoRegisterOnStart = true;
    public bool autoUnregisterOnDestroy = true;
    
    [Header("State Management")]
    public bool autoManageState = true; // Automatically manage chicken state based on slot assignment
    public bool showDebugLogs = false; // Local debug logs for this chicken

    private EnemyChickenManager manager;
    private ChickenStateController stateController;
    private ChickenMovementBehavior movementBehavior;
    private bool isRegistered = false;
    private bool wasAssignedLastFrame = false; // Track assignment status
    private int lastKnownSlotIndex = -1; // Track slot changes
    private Vector3? lastKnownSlotPosition = null; // Track slot position changes

    void Start()
    {
        // Get the required components
        stateController = GetComponent<ChickenStateController>();
        movementBehavior = GetComponent<ChickenMovementBehavior>();
        
        if (stateController == null && autoManageState)
        {
            Debug.LogWarning($"EnemyChickenRegistration on {gameObject.name}: No ChickenStateController found! Auto state management disabled.");
            autoManageState = false;
        }

        if (autoRegisterOnStart)
        {
            RegisterWithManager();
        }
    }

    void Update()
    {
        // Automatically manage state based on slot assignment
        if (autoManageState && isRegistered && stateController != null)
        {
            UpdateStateBasedOnAssignment();
        }
    }

    void OnDestroy()
    {
        if (autoUnregisterOnDestroy && isRegistered)
        {
            UnregisterFromManager();
        }
    }

    // Update chicken state based on slot assignment
    void UpdateStateBasedOnAssignment()
    {
        bool isCurrentlyAssigned = IsAssignedToSlot();
        int currentSlotIndex = GetAssignedSlotIndex();
        Vector3? currentSlotPosition = GetAssignedSlotPosition();

        // Check if assignment status changed
        if (isCurrentlyAssigned != wasAssignedLastFrame)
        {
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Assignment status changed. Now assigned: {isCurrentlyAssigned}");
                
            if (isCurrentlyAssigned)
            {
                // Just got assigned to a slot - transition to MovingToSlotOnce
                stateController.SetMovingToSlotOnce();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Got slot assignment, setting to MovingToSlotOnce");
            }
            else
            {
                // Lost slot assignment (slot became unavailable) - go to idle
                stateController.SetIdle();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Lost slot assignment, setting to Idle");
            }
            
            wasAssignedLastFrame = isCurrentlyAssigned;
            lastKnownSlotIndex = currentSlotIndex;
            lastKnownSlotPosition = currentSlotPosition;
        }
        // Check if slot index or position changed while still assigned
        else if (isCurrentlyAssigned && (currentSlotIndex != lastKnownSlotIndex || 
                (currentSlotPosition.HasValue && lastKnownSlotPosition.HasValue && 
                 Vector3.Distance(currentSlotPosition.Value, lastKnownSlotPosition.Value) > 0.1f)))
        {
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Slot changed from {lastKnownSlotIndex} to {currentSlotIndex}");
                
            // Slot changed - if we're in combat, go to MovingInsideFormation
            if (stateController.IsInCombat)
            {
                stateController.SetMovingInsideFormation();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Slot changed while in combat, setting to MovingInsideFormation");
            }
            else if (stateController.IsIdle)
            {
                // If we're idle but have a slot, we should move to it
                stateController.SetMovingToSlotOnce();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Was idle but got new slot, setting to MovingToSlotOnce");
            }
            
            lastKnownSlotIndex = currentSlotIndex;
            lastKnownSlotPosition = currentSlotPosition;
        }
        // Handle edge cases
        else if (!isCurrentlyAssigned && !stateController.IsIdle)
        {
            // Ensure idle state when not assigned and not already idle
            stateController.SetIdle();
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: No assignment but not idle, forcing to Idle");
        }
        else if (isCurrentlyAssigned && stateController.IsIdle)
        {
            // If we have a slot but we're idle, we should be moving to slot
            // This fixes the bug where chickens get stuck in idle with a slot assigned
            stateController.SetMovingToSlotOnce();
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Has slot but was idle, setting to MovingToSlotOnce");
        }
    }

    // Find and register with the manager
    public void RegisterWithManager()
    {
        if (isRegistered)
            return;

        // Try to find the manager in the scene
        if (manager == null)
        {
            manager = FindObjectOfType<EnemyChickenManager>();
        }

        if (manager == null)
        {
            Debug.LogError($"Chicken {gameObject.name}: No EnemyChickenManager found in scene!");
            return;
        }

        // Register with the manager
        if (manager.RegisterChicken(gameObject))
        {
            isRegistered = true;
            
            // Initialize state based on assignment
            if (autoManageState && stateController != null)
            {
                wasAssignedLastFrame = IsAssignedToSlot();
                lastKnownSlotIndex = GetAssignedSlotIndex();
                lastKnownSlotPosition = GetAssignedSlotPosition();
                
                if (wasAssignedLastFrame)
                {
                    stateController.SetMovingToSlotOnce();
                    if (showDebugLogs)
                        Debug.Log($"Chicken {gameObject.name}: Registered with slot, setting to MovingToSlotOnce");
                }
                else
                {
                    stateController.SetIdle();
                    if (showDebugLogs)
                        Debug.Log($"Chicken {gameObject.name}: Registered without slot, setting to Idle");
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Successfully registered with manager");
        }
        else
        {
            Debug.LogError($"Chicken {gameObject.name} failed to register with manager!");
        }
    }

    // Unregister from the manager
    public void UnregisterFromManager()
    {
        if (!isRegistered || manager == null)
            return;

        if (manager.UnregisterChicken(gameObject))
        {
            isRegistered = false;
            wasAssignedLastFrame = false;
            lastKnownSlotIndex = -1;
            lastKnownSlotPosition = null;
            
            // Set to idle when unregistered
            if (autoManageState && stateController != null)
            {
                stateController.SetIdle();
            }
            
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Successfully unregistered from manager");
        }
    }

    // Force unregister and re-register (useful for respawning)
    public void ReregisterWithManager()
    {
        if (isRegistered)
        {
            UnregisterFromManager();
        }
        RegisterWithManager();
    }

    // Get the assigned slot index from the manager
    public int GetAssignedSlotIndex()
    {
        if (!isRegistered || manager == null)
            return -1;

        return manager.GetChickenSlotIndex(gameObject);
    }

    // Get the world position of the assigned slot
    public Vector3? GetAssignedSlotPosition()
    {
        if (!isRegistered || manager == null)
            return null;

        return manager.GetChickenSlotPosition(gameObject);
    }

    // Check if this chicken is assigned to a slot
    public bool IsAssignedToSlot()
    {
        return GetAssignedSlotIndex() != -1;
    }

    // Force a state update (useful when assignments change)
    public void ForceStateUpdate()
    {
        if (!isRegistered || !autoManageState || stateController == null)
            return;
        
        bool isCurrentlyAssigned = IsAssignedToSlot();
        int currentSlotIndex = GetAssignedSlotIndex();
        Vector3? currentSlotPosition = GetAssignedSlotPosition();
        
        if (showDebugLogs)
        {
            Debug.Log($"Chicken {gameObject.name}: Force state update - Assigned: {isCurrentlyAssigned}, Slot: {currentSlotIndex}, Current State: {stateController.CurrentState}");
        }
        
        if (isCurrentlyAssigned && (stateController.IsIdle || (!stateController.IsMovingToSlotOnce && !stateController.IsMovingInsideFormation && !stateController.IsInCombat)))
        {
            // Has slot but not in proper state - force to MovingToSlotOnce
            stateController.SetMovingToSlotOnce();
            wasAssignedLastFrame = true;
            lastKnownSlotIndex = currentSlotIndex;
            lastKnownSlotPosition = currentSlotPosition;
            
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Force update - setting to MovingToSlotOnce");
        }
        else if (!isCurrentlyAssigned && !stateController.IsIdle)
        {
            // No slot but not idle - force to idle
            stateController.SetIdle();
            wasAssignedLastFrame = false;
            lastKnownSlotIndex = -1;
            lastKnownSlotPosition = null;
            
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Force update - setting to Idle");
        }
        else
        {
            // Update tracking
            wasAssignedLastFrame = isCurrentlyAssigned;
            lastKnownSlotIndex = currentSlotIndex;
            lastKnownSlotPosition = currentSlotPosition;
        }
        
        // Also refresh movement behavior if available
        if (movementBehavior != null)
        {
            movementBehavior.RefreshMovementState();
        }
    }

    // Refresh state without forcing changes (lighter than ForceStateUpdate)
    public void RefreshState()
    {
        if (!isRegistered || !autoManageState || stateController == null)
            return;
            
        // Just update our tracking variables and let normal Update handle state changes
        bool isCurrentlyAssigned = IsAssignedToSlot();
        int currentSlotIndex = GetAssignedSlotIndex();
        Vector3? currentSlotPosition = GetAssignedSlotPosition();
        
        // Only update if something actually changed
        if (isCurrentlyAssigned != wasAssignedLastFrame || 
            currentSlotIndex != lastKnownSlotIndex ||
            (currentSlotPosition.HasValue && lastKnownSlotPosition.HasValue && 
             Vector3.Distance(currentSlotPosition.Value, lastKnownSlotPosition.Value) > 0.1f))
        {
            // Let the next Update() cycle handle the state change
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Refresh detected change - will update state next frame");
        }
    }

    // Public properties
    public bool IsRegistered => isRegistered;
    public EnemyChickenManager Manager => manager;
    public ChickenStateController StateController => stateController;
    public ChickenMovementBehavior MovementBehavior => movementBehavior;

    // Context menu for testing in editor
    [ContextMenu("Register with Manager")]
    void ContextMenuRegister()
    {
        RegisterWithManager();
    }

    [ContextMenu("Unregister from Manager")]
    void ContextMenuUnregister()
    {
        UnregisterFromManager();
    }

    [ContextMenu("Force Fix State")]
    void ContextMenuForceFixState()
    {
        ForceStateUpdate();
    }

    [ContextMenu("Refresh State")]
    void ContextMenuRefreshState()
    {
        RefreshState();
    }

    [ContextMenu("Toggle Debug Logs")]
    void ContextMenuToggleDebugLogs()
    {
        showDebugLogs = !showDebugLogs;
        Debug.Log($"Chicken {gameObject.name}: Debug logs {(showDebugLogs ? "enabled" : "disabled")}");
    }

    [ContextMenu("Print Assignment Info")]
    void ContextMenuPrintInfo()
    {
        if (!isRegistered)
        {
            Debug.Log($"Chicken {gameObject.name}: Not registered");
            return;
        }

        int slotIndex = GetAssignedSlotIndex();
        Vector3? slotPosition = GetAssignedSlotPosition();

        Debug.Log($"=== CHICKEN ASSIGNMENT INFO ===");
        Debug.Log($"Chicken: {gameObject.name}");
        Debug.Log($"Registered: {isRegistered}");
        Debug.Log($"Assigned Slot: {(slotIndex == -1 ? "None (waiting)" : slotIndex.ToString())}");
        Debug.Log($"Slot Position: {(slotPosition.HasValue ? slotPosition.Value.ToString() : "None")}");
        Debug.Log($"Was Assigned Last Frame: {wasAssignedLastFrame}");
        Debug.Log($"Last Known Slot: {lastKnownSlotIndex}");
        Debug.Log($"Last Known Position: {(lastKnownSlotPosition.HasValue ? lastKnownSlotPosition.Value.ToString() : "None")}");

        if (stateController != null)
        {
            Debug.Log($"Current State: {stateController.CurrentState}");
        }
        
        if (movementBehavior != null)
        {
            Debug.Log($"Is Moving: {movementBehavior.IsCurrentlyMoving}");
            Debug.Log($"Moving To Spawn: {movementBehavior.IsMovingToSpawn}");
        }
    }
}