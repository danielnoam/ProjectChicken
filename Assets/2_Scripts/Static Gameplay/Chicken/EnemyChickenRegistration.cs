using UnityEngine;

public class EnemyChickenRegistration : MonoBehaviour
{
    [Header("Registration Settings")]
    public bool autoRegisterOnStart = true;
    public bool autoUnregisterOnDestroy = true;
    
    [Header("State Management")]
    public bool autoManageState = true;
    public bool showDebugLogs = false;
    public float majorSlotChangeThreshold = 2f; // Distance threshold to detect major slot repositioning

    private EnemyChickenManager manager;
    private ChickenStateController stateController;
    private ChickenMovementBehavior movementBehavior;
    private bool isRegistered = false;
    private bool wasAssignedLastFrame = false;
    private int lastKnownSlotIndex = -1;
    private Vector3? lastKnownSlotPosition = null;

    void Start()
    {
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
                // Just got assigned to a slot - transition to MovingToSlot
                stateController.SetMovingToSlot();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Got slot assignment, setting to MovingToSlot");
            }
            else
            {
                // Lost slot assignment - go to idle
                stateController.SetIdle();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Lost slot assignment, setting to Idle");
            }
            
            wasAssignedLastFrame = isCurrentlyAssigned;
            lastKnownSlotIndex = currentSlotIndex;
            lastKnownSlotPosition = currentSlotPosition;
        }
        // Check if slot changed while still assigned
        else if (isCurrentlyAssigned && (currentSlotIndex != lastKnownSlotIndex || 
                (currentSlotPosition.HasValue && lastKnownSlotPosition.HasValue && 
                 Vector3.Distance(currentSlotPosition.Value, lastKnownSlotPosition.Value) > 0.1f)))
        {
            float slotMovementDistance = 0f;
            bool isMajorSlotChange = false;
            
            // Calculate how far the slot moved
            if (currentSlotPosition.HasValue && lastKnownSlotPosition.HasValue)
            {
                slotMovementDistance = Vector3.Distance(currentSlotPosition.Value, lastKnownSlotPosition.Value);
                isMajorSlotChange = slotMovementDistance > majorSlotChangeThreshold;
            }
            else if (currentSlotIndex != lastKnownSlotIndex)
            {
                // Slot index changed - always consider this major
                isMajorSlotChange = true;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"Chicken {gameObject.name}: Slot changed from {lastKnownSlotIndex} to {currentSlotIndex}, movement distance: {slotMovementDistance:F2}, major change: {isMajorSlotChange}");
            }
                
            // Slot changed - behavior depends on current state and type of change
            if (stateController.IsFollowingSlot)
            {
                if (isMajorSlotChange)
                {
                    // Major change - go back to MovingToSlot for proper movement
                    stateController.SetMovingToSlot();
                    if (showDebugLogs)
                        Debug.Log($"Chicken {gameObject.name}: Major slot change detected, transitioning to MovingToSlot");
                }
                else
                {
                    // Minor change - stay in FollowingSlot, movement behavior will handle it
                    if (showDebugLogs)
                        Debug.Log($"Chicken {gameObject.name}: Minor slot change, staying in FollowingSlot");
                }
            }
            else if (stateController.IsIdle)
            {
                // If we're idle but have a slot, we should move to it
                stateController.SetMovingToSlot();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Was idle but got new slot, setting to MovingToSlot");
            }
            else if (stateController.IsMovingToSlot)
            {
                // Already moving to slot, movement behavior will update the target
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Slot changed while moving, letting movement behavior handle it");
            }
            
            lastKnownSlotIndex = currentSlotIndex;
            lastKnownSlotPosition = currentSlotPosition;
        }
        // Handle edge cases where state and assignment are out of sync
        else if (!isCurrentlyAssigned && !stateController.IsIdle && !stateController.IsConcussed)
        {
            // No assignment but not idle (and not concussed) - force to idle
            stateController.SetIdle();
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: No assignment but not idle, forcing to Idle");
        }
        else if (isCurrentlyAssigned && stateController.IsIdle)
        {
            // Has assignment but idle - should be moving to slot
            stateController.SetMovingToSlot();
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Has slot but was idle, setting to MovingToSlot");
        }
    }

    public void RegisterWithManager()
    {
        if (isRegistered)
            return;

        if (manager == null)
        {
            manager = FindObjectOfType<EnemyChickenManager>();
        }

        if (manager == null)
        {
            Debug.LogError($"Chicken {gameObject.name}: No EnemyChickenManager found in scene!");
            return;
        }

        if (manager.RegisterChicken(gameObject))
        {
            isRegistered = true;
            
            if (autoManageState && stateController != null)
            {
                wasAssignedLastFrame = IsAssignedToSlot();
                lastKnownSlotIndex = GetAssignedSlotIndex();
                lastKnownSlotPosition = GetAssignedSlotPosition();
                
                if (wasAssignedLastFrame)
                {
                    stateController.SetMovingToSlot();
                    if (showDebugLogs)
                        Debug.Log($"Chicken {gameObject.name}: Registered with slot, setting to MovingToSlot");
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
            
            if (autoManageState && stateController != null)
            {
                stateController.SetIdle();
            }
            
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Successfully unregistered from manager");
        }
    }

    public void ReregisterWithManager()
    {
        if (isRegistered)
        {
            UnregisterFromManager();
        }
        RegisterWithManager();
    }

    public int GetAssignedSlotIndex()
    {
        if (!isRegistered || manager == null)
            return -1;

        return manager.GetChickenSlotIndex(gameObject);
    }

    public Vector3? GetAssignedSlotPosition()
    {
        if (!isRegistered || manager == null)
            return null;

        return manager.GetChickenSlotPosition(gameObject);
    }

    public bool IsAssignedToSlot()
    {
        return GetAssignedSlotIndex() != -1;
    }

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
        
        if (isCurrentlyAssigned)
        {
            // Has slot - determine proper state
            if (stateController.IsIdle)
            {
                // Has slot but idle - should be moving to slot
                stateController.SetMovingToSlot();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Force update - has slot but idle, setting to MovingToSlot");
            }
            // If already MovingToSlot or FollowingSlot, leave it as is
            // If Concussed, leave it as is (concussion should be handled separately)
        }
        else
        {
            // No slot - should be idle (unless concussed)
            if (!stateController.IsIdle && !stateController.IsConcussed)
            {
                stateController.SetIdle();
                if (showDebugLogs)
                    Debug.Log($"Chicken {gameObject.name}: Force update - no slot, setting to Idle");
            }
        }
        
        // Update tracking
        wasAssignedLastFrame = isCurrentlyAssigned;
        lastKnownSlotIndex = currentSlotIndex;
        lastKnownSlotPosition = currentSlotPosition;
        
        // Refresh movement behavior
        if (movementBehavior != null)
        {
            movementBehavior.RefreshMovementState();
        }
    }

    public void RefreshState()
    {
        if (!isRegistered || !autoManageState || stateController == null)
            return;
            
        // Light refresh - just update tracking variables and let Update handle changes
        bool isCurrentlyAssigned = IsAssignedToSlot();
        int currentSlotIndex = GetAssignedSlotIndex();
        Vector3? currentSlotPosition = GetAssignedSlotPosition();
        
        if (isCurrentlyAssigned != wasAssignedLastFrame || 
            currentSlotIndex != lastKnownSlotIndex ||
            (currentSlotPosition.HasValue && lastKnownSlotPosition.HasValue && 
             Vector3.Distance(currentSlotPosition.Value, lastKnownSlotPosition.Value) > 0.1f))
        {
            if (showDebugLogs)
                Debug.Log($"Chicken {gameObject.name}: Refresh detected change - will update state next frame");
        }
    }

    // Public properties
    public bool IsRegistered => isRegistered;
    public EnemyChickenManager Manager => manager;
    public ChickenStateController StateController => stateController;
    public ChickenMovementBehavior MovementBehavior => movementBehavior;

    // Context menu methods
    [ContextMenu("Register with Manager")]
    void ContextMenuRegister() => RegisterWithManager();

    [ContextMenu("Unregister from Manager")]
    void ContextMenuUnregister() => UnregisterFromManager();

    [ContextMenu("Force Fix State")]
    void ContextMenuForceFixState() => ForceStateUpdate();

    [ContextMenu("Refresh State")]
    void ContextMenuRefreshState() => RefreshState();

    [ContextMenu("Toggle Debug Logs")]
    void ContextMenuToggleDebugLogs()
    {
        showDebugLogs = !showDebugLogs;
        Debug.Log($"Chicken {gameObject.name}: Debug logs {(showDebugLogs ? "enabled" : "disabled")}");
    }

    [ContextMenu("Test Major Slot Change")]
    void ContextMenuTestMajorSlotChange()
    {
        if (stateController != null && stateController.IsFollowingSlot)
        {
            // Simulate a major slot change by setting last known position far away
            Vector3? currentPos = GetAssignedSlotPosition();
            if (currentPos.HasValue)
            {
                lastKnownSlotPosition = currentPos.Value + Vector3.right * (majorSlotChangeThreshold + 1f);
                Debug.Log($"Chicken {gameObject.name}: Simulated major slot change for testing");
            }
        }
        else
        {
            Debug.Log($"Chicken {gameObject.name}: Must be in FollowingSlot state to test major slot change");
        }
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
        Debug.Log($"Major Slot Change Threshold: {majorSlotChangeThreshold:F2}");

        if (stateController != null)
        {
            Debug.Log($"Current State: {stateController.CurrentState}");
            Debug.Log($"Can Attack: {stateController.CanAttack}");
        }
        
        if (movementBehavior != null)
        {
            Debug.Log($"Is Moving: {movementBehavior.IsCurrentlyMoving}");
            Debug.Log($"Is Actively Following: {movementBehavior.IsActivelyFollowing}");
        }
    }
}