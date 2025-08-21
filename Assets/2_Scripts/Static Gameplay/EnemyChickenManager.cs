using UnityEngine;
using System.Collections.Generic;

public class EnemyChickenManager : MonoBehaviour
{
    [Header("Formation Reference")]
    public FormationCreator formationCreator;
    
    [Header("Debug Visualization")]
    public bool showAssignedSlotConnections = true;
    public bool showAllSubscribedChickens = true;
    public Color assignedSlotColor = Color.green;
    public Color subscribedChickenColor = Color.cyan;
    public float debugSlotSize = 0.3f;
    
    [Header("Formation Change Detection")]
    public bool autoReassignOnFormationChange = true; // Automatically reassign when formation changes
    public float formationCheckInterval = 0.1f; // How often to check for formation changes (seconds)
    
    [Header("Chicken State Synchronization")]
    public bool autoRefreshChickenStates = true; // Automatically refresh chicken states periodically
    public float stateRefreshInterval = 1f; // How often to refresh chicken states (seconds)
    public bool forceStateUpdateOnReassign = true; // Force all chickens to update their states when reassigning

    // Dictionary to track which chicken is assigned to which slot index
    private Dictionary<int, GameObject> slotAssignments = new Dictionary<int, GameObject>();
    
    // List of chickens waiting for assignment
    private List<GameObject> waitingChickens = new List<GameObject>();
    
    // List of all registered chickens
    private List<GameObject> allRegisteredChickens = new List<GameObject>();
    
    // Formation change detection
    private int previousSlotCount = -1; // Initialize to -1 to ensure first check triggers
    private FormationCreator.FormationType previousFormationType;
    private float formationCheckTimer = 0f;
    private float stateRefreshTimer = 0f;
    private bool hasInitializedFormation = false; // Track if we've done initial setup

    void Start()
    {
        if (formationCreator == null)
        {
            formationCreator = GetComponent<FormationCreator>();
            if (formationCreator == null)
            {
                Debug.LogError("EnemyChickenManager: No FormationCreator found! Please assign one in the inspector.");
                return;
            }
        }
        
        // Don't initialize formation tracking here - let Update() handle the first detection
        // This ensures we detect the initial formation as a "change"
        Debug.Log("EnemyChickenManager: Started, waiting for formation initialization...");
    }

    void Update()
    {
        if (formationCreator == null)
            return;
            
        // Check for formation changes (including initial setup)
        if (autoReassignOnFormationChange)
        {
            formationCheckTimer += Time.deltaTime;
            if (formationCheckTimer >= formationCheckInterval)
            {
                CheckForFormationChanges();
                formationCheckTimer = 0f;
            }
        }
        
        // Refresh chicken states periodically
        if (autoRefreshChickenStates)
        {
            stateRefreshTimer += Time.deltaTime;
            if (stateRefreshTimer >= stateRefreshInterval)
            {
                RefreshAllChickenStates();
                stateRefreshTimer = 0f;
            }
        }
    }

    void CheckForFormationChanges()
    {
        int currentSlotCount = formationCreator.GetFormationSlots().Count;
        FormationCreator.FormationType currentFormationType = formationCreator.currentFormation;
        
        // Check if this is the first initialization or if formation changed
        bool isInitialSetup = !hasInitializedFormation;
        bool formationTypeChanged = currentFormationType != previousFormationType;
        bool slotCountChanged = currentSlotCount != previousSlotCount;
        
        if (isInitialSetup || formationTypeChanged || slotCountChanged)
        {
            if (isInitialSetup)
            {
                Debug.Log($"EnemyChickenManager: Initial formation setup detected - {currentFormationType} with {currentSlotCount} slots. Assigning all chickens...");
            }
            else
            {
                Debug.Log($"EnemyChickenManager: Formation changed from {previousFormationType} ({previousSlotCount} slots) to {currentFormationType} ({currentSlotCount} slots). Reassigning all chickens...");
            }
            
            ReassignAllChickens();
            
            // Force all chickens to update their states immediately
            if (forceStateUpdateOnReassign)
            {
                ForceUpdateAllChickenStates();
            }
            
            // Update tracking variables
            previousSlotCount = currentSlotCount;
            previousFormationType = currentFormationType;
            hasInitializedFormation = true;
        }
    }

    // New method to refresh all chicken states without reassigning slots
    void RefreshAllChickenStates()
    {
        foreach (GameObject chicken in allRegisteredChickens)
        {
            if (chicken != null)
            {
                EnemyChickenRegistration registration = chicken.GetComponent<EnemyChickenRegistration>();
                if (registration != null)
                {
                    registration.RefreshState();
                }
            }
        }
    }

    // New method to force all chickens to update their states immediately
    void ForceUpdateAllChickenStates()
    {
        foreach (GameObject chicken in allRegisteredChickens)
        {
            if (chicken != null)
            {
                EnemyChickenRegistration registration = chicken.GetComponent<EnemyChickenRegistration>();
                if (registration != null)
                {
                    registration.ForceStateUpdate();
                }
                
                // Also refresh movement behavior
                ChickenMovementBehavior movement = chicken.GetComponent<ChickenMovementBehavior>();
                if (movement != null)
                {
                    movement.RefreshMovementState();
                }
            }
        }
        
        Debug.Log($"EnemyChickenManager: Forced state update on {allRegisteredChickens.Count} chickens");
    }

    // Method for chickens to register themselves with the manager
    public bool RegisterChicken(GameObject chicken)
    {
        if (chicken == null)
        {
            return false;
        }

        if (allRegisteredChickens.Contains(chicken))
        {
            return false;
        }

        allRegisteredChickens.Add(chicken);
        
        // Try to assign to a slot immediately
        bool assigned = AssignChickenToSlot(chicken);
        
        if (!assigned)
        {
            // Add to waiting list if no slots available
            waitingChickens.Add(chicken);
        }

        // Force the chicken to update its state based on assignment
        // But only if we have initialized the formation
        if (hasInitializedFormation)
        {
            EnemyChickenRegistration registration = chicken.GetComponent<EnemyChickenRegistration>();
            if (registration != null)
            {
                registration.ForceStateUpdate();
            }
        }
        else
        {
            Debug.Log($"EnemyChickenManager: Chicken {chicken.name} registered but formation not yet initialized. Will update state once formation is ready.");
        }

        return true;
    }

    // Method for chickens to unregister themselves (when they die, etc.)
    public bool UnregisterChicken(GameObject chicken)
    {
        if (chicken == null || !allRegisteredChickens.Contains(chicken))
        {
            return false;
        }

        // Remove from all lists
        allRegisteredChickens.Remove(chicken);
        waitingChickens.Remove(chicken);

        // Find and free the slot if this chicken was assigned to one
        int assignedSlot = -1;
        foreach (var kvp in slotAssignments)
        {
            if (kvp.Value == chicken)
            {
                assignedSlot = kvp.Key;
                break;
            }
        }

        if (assignedSlot != -1)
        {
            slotAssignments.Remove(assignedSlot);
            // Try to assign a waiting chicken to the freed slot
            AssignWaitingChickenToSlot(assignedSlot);
        }

        return true;
    }

    // Assign a specific chicken to the first available slot
    bool AssignChickenToSlot(GameObject chicken)
    {
        if (formationCreator == null)
            return false;

        List<Vector3> formationSlots = formationCreator.GetFormationSlots();
        
        // Find first available slot
        for (int i = 0; i < formationSlots.Count; i++)
        {
            if (!slotAssignments.ContainsKey(i))
            {
                slotAssignments[i] = chicken;
                return true;
            }
        }

        return false; // No available slots
    }

    // Try to assign a waiting chicken to a specific slot
    void AssignWaitingChickenToSlot(int slotIndex)
    {
        if (waitingChickens.Count == 0)
            return;

        GameObject waitingChicken = waitingChickens[0];
        waitingChickens.RemoveAt(0);
        slotAssignments[slotIndex] = waitingChicken;
        
        // Force the chicken to update its state (only if formation is initialized)
        if (hasInitializedFormation)
        {
            EnemyChickenRegistration registration = waitingChicken.GetComponent<EnemyChickenRegistration>();
            if (registration != null)
            {
                registration.ForceStateUpdate();
            }
        }
    }

    // Get the assigned slot index for a specific chicken
    public int GetChickenSlotIndex(GameObject chicken)
    {
        foreach (var kvp in slotAssignments)
        {
            if (kvp.Value == chicken)
                return kvp.Key;
        }
        return -1; // Not assigned to any slot
    }

    // Get the world position of a chicken's assigned slot
    public Vector3? GetChickenSlotPosition(GameObject chicken)
    {
        int slotIndex = GetChickenSlotIndex(chicken);
        if (slotIndex == -1 || formationCreator == null)
            return null;

        List<Vector3> formationSlots = formationCreator.GetFormationSlots();
        if (slotIndex < formationSlots.Count)
            return formationSlots[slotIndex];

        return null;
    }

    // Get the chicken assigned to a specific slot
    public GameObject GetChickenInSlot(int slotIndex)
    {
        return slotAssignments.ContainsKey(slotIndex) ? slotAssignments[slotIndex] : null;
    }

    // Force reassign all chickens (useful when formation changes)
    public void ReassignAllChickens()
    {
        // Clear current assignments
        slotAssignments.Clear();
        waitingChickens.Clear();

        // Re-add all registered chickens to waiting list
        waitingChickens.AddRange(allRegisteredChickens);

        // Assign them to slots
        for (int i = waitingChickens.Count - 1; i >= 0; i--)
        {
            if (AssignChickenToSlot(waitingChickens[i]))
            {
                waitingChickens.RemoveAt(i);
            }
        }
        
        Debug.Log($"EnemyChickenManager: Reassignment complete. {slotAssignments.Count} assigned, {waitingChickens.Count} waiting.");
    }

    // New method to validate and fix any desynchronized chickens
    public void ValidateAndFixChickenStates()
    {
        int fixedCount = 0;
        
        foreach (GameObject chicken in allRegisteredChickens)
        {
            if (chicken == null)
                continue;
                
            EnemyChickenRegistration registration = chicken.GetComponent<EnemyChickenRegistration>();
            ChickenStateController stateController = chicken.GetComponent<ChickenStateController>();
            ChickenMovementBehavior movement = chicken.GetComponent<ChickenMovementBehavior>();
            
            if (registration == null || stateController == null)
                continue;
                
            bool hasSlot = GetChickenSlotIndex(chicken) != -1;
            bool chickenThinksItHasSlot = registration.IsAssignedToSlot();
            var currentState = stateController.CurrentState;
            
            // Check for desynchronization
            if (hasSlot != chickenThinksItHasSlot)
            {
                Debug.LogWarning($"Fixing desynchronized chicken {chicken.name}: Manager says hasSlot={hasSlot}, Chicken says hasSlot={chickenThinksItHasSlot}");
                registration.ForceStateUpdate();
                if (movement != null)
                    movement.RefreshMovementState();
                fixedCount++;
            }
            // Check if chicken has slot but is idle
            else if (hasSlot && currentState == ChickenStateController.ChickenState.Idle)
            {
                Debug.LogWarning($"Fixing stuck chicken {chicken.name}: Has slot but is idle");
                registration.ForceStateUpdate();
                if (movement != null)
                    movement.RefreshMovementState();
                fixedCount++;
            }
            // Check if chicken doesn't have slot but isn't idle
            else if (!hasSlot && currentState != ChickenStateController.ChickenState.Idle)
            {
                Debug.LogWarning($"Fixing stuck chicken {chicken.name}: No slot but not idle (state: {currentState})");
                registration.ForceStateUpdate();
                if (movement != null)
                    movement.RefreshMovementState();
                fixedCount++;
            }
            // Check if chicken should be moving but movement behavior says it's not
            else if (hasSlot && (currentState == ChickenStateController.ChickenState.MovingToSlotOnce || currentState == ChickenStateController.ChickenState.MovingInsideFormation))
            {
                if (movement != null && !movement.IsCurrentlyMoving)
                {
                    Debug.LogWarning($"Fixing movement stuck chicken {chicken.name}: Should be moving but movement behavior is inactive");
                    movement.RefreshMovementState();
                    fixedCount++;
                }
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"EnemyChickenManager: Fixed {fixedCount} stuck chickens");
        }
    }

    // Display current chicken assignments in a clean list format
    [ContextMenu("Show Chicken List")]
    public void PrintCurrentAssignments()
    {
        Debug.Log("=== CHICKEN MANAGER STATUS ===");
        Debug.Log($"Formation: {(formationCreator != null ? formationCreator.currentFormation.ToString() : "None")}");
        Debug.Log($"Total Slots: {(formationCreator != null ? formationCreator.GetFormationSlots().Count : 0)}");
        Debug.Log($"Chickens: {allRegisteredChickens.Count} registered | {slotAssignments.Count} assigned | {waitingChickens.Count} waiting");
        Debug.Log($"Auto-reassign: {(autoReassignOnFormationChange ? "Enabled" : "Disabled")}");
        Debug.Log($"Auto-refresh: {(autoRefreshChickenStates ? "Enabled" : "Disabled")}");
        Debug.Log($"Formation Initialized: {hasInitializedFormation}");
        Debug.Log("");
        
        // Show assigned chickens
        if (slotAssignments.Count > 0)
        {
            Debug.Log("ASSIGNED CHICKENS:");
            foreach (var kvp in slotAssignments)
            {
                string chickenName = kvp.Value != null ? kvp.Value.name : "NULL";
                string state = "Unknown";
                bool isMoving = false;
                
                // Try to get state if chicken has state controller
                if (kvp.Value != null)
                {
                    var stateController = kvp.Value.GetComponent<ChickenStateController>();
                    if (stateController != null)
                    {
                        state = stateController.CurrentState.ToString();
                    }
                    
                    var movement = kvp.Value.GetComponent<ChickenMovementBehavior>();
                    if (movement != null)
                    {
                        isMoving = movement.IsCurrentlyMoving;
                    }
                }
                
                Debug.Log($"  Slot {kvp.Key}: {chickenName} ({state}) {(isMoving ? "[MOVING]" : "[STATIC]")}");
            }
            Debug.Log("");
        }
        
        // Show waiting chickens
        if (waitingChickens.Count > 0)
        {
            Debug.Log("WAITING CHICKENS:");
            for (int i = 0; i < waitingChickens.Count; i++)
            {
                string chickenName = waitingChickens[i] != null ? waitingChickens[i].name : "NULL";
                string state = "Unknown";
                bool isMoving = false;
                
                // Try to get state if chicken has state controller
                if (waitingChickens[i] != null)
                {
                    var stateController = waitingChickens[i].GetComponent<ChickenStateController>();
                    if (stateController != null)
                    {
                        state = stateController.CurrentState.ToString();
                    }
                    
                    var movement = waitingChickens[i].GetComponent<ChickenMovementBehavior>();
                    if (movement != null)
                    {
                        isMoving = movement.IsCurrentlyMoving;
                    }
                }
                
                Debug.Log($"  {i + 1}: {chickenName} ({state}) {(isMoving ? "[MOVING]" : "[STATIC]")}");
            }
        }
        
        if (slotAssignments.Count == 0 && waitingChickens.Count == 0)
        {
            Debug.Log("No chickens registered.");
        }
    }

    [ContextMenu("Force Reassign All Chickens")]
    public void ForceReassignAllChickens()
    {
        ReassignAllChickens();
        if (forceStateUpdateOnReassign)
        {
            ForceUpdateAllChickenStates();
        }
    }

    [ContextMenu("Validate and Fix All Chicken States")]
    public void ForceValidateAndFixStates()
    {
        ValidateAndFixChickenStates();
    }

    [ContextMenu("Force Refresh All Chicken States")]
    public void ForceRefreshAllStates()
    {
        RefreshAllChickenStates();
    }

    [ContextMenu("Force Update All Chicken States")]
    public void ForceUpdateAllStates()
    {
        ForceUpdateAllChickenStates();
    }

    [ContextMenu("Toggle Auto-Reassign")]
    public void ToggleAutoReassign()
    {
        autoReassignOnFormationChange = !autoReassignOnFormationChange;
        Debug.Log($"EnemyChickenManager: Auto-reassign on formation change {(autoReassignOnFormationChange ? "enabled" : "disabled")}");
    }

    [ContextMenu("Toggle Auto-Refresh")]
    public void ToggleAutoRefresh()
    {
        autoRefreshChickenStates = !autoRefreshChickenStates;
        Debug.Log($"EnemyChickenManager: Auto-refresh chicken states {(autoRefreshChickenStates ? "enabled" : "disabled")}");
    }

    // NEW: Manual method to force formation initialization detection
    [ContextMenu("Force Formation Initialization")]
    public void ForceFormationInitialization()
    {
        hasInitializedFormation = false;
        previousSlotCount = -1;
        Debug.Log("EnemyChickenManager: Reset formation initialization. Next update will detect formation as new.");
    }

    // Draw gizmos for assigned slots and subscribed chickens
    void OnDrawGizmos()
    {
        if (formationCreator == null)
            return;

        List<Vector3> formationSlots = formationCreator.GetFormationSlots();

        // Draw connections for assigned chickens
        if (showAssignedSlotConnections && slotAssignments.Count > 0)
        {
            Gizmos.color = assignedSlotColor;
            foreach (var kvp in slotAssignments)
            {
                if (kvp.Key < formationSlots.Count && kvp.Value != null)
                {
                    Vector3 slotPosition = formationSlots[kvp.Key];
                    
                    // Draw slot sphere
                    Gizmos.DrawSphere(slotPosition, debugSlotSize);
                    
                    // Draw line from chicken to assigned slot
                    Gizmos.DrawLine(kvp.Value.transform.position, slotPosition);
                }
            }
        }

        // Draw indicators for all subscribed chickens
        if (showAllSubscribedChickens && allRegisteredChickens.Count > 0)
        {
            foreach (GameObject chicken in allRegisteredChickens)
            {
                if (chicken != null)
                {
                    // Determine color based on chicken state
                    bool hasSlot = GetChickenSlotIndex(chicken) != -1;
                    ChickenStateController stateController = chicken.GetComponent<ChickenStateController>();
                    ChickenMovementBehavior movement = chicken.GetComponent<ChickenMovementBehavior>();
                    
                    if (hasSlot && stateController != null && stateController.IsIdle)
                    {
                        // Chicken has slot but is idle - potential bug (red)
                        Gizmos.color = Color.red;
                    }
                    else if (!hasSlot && stateController != null && !stateController.IsIdle)
                    {
                        // Chicken doesn't have slot but isn't idle - potential bug (orange)
                        Gizmos.color = Color.orange;
                    }
                    else if (hasSlot && movement != null && (stateController.IsMovingToSlotOnce || stateController.IsMovingInsideFormation) && !movement.IsCurrentlyMoving)
                    {
                        // Should be moving but isn't - potential bug (yellow)
                        Gizmos.color = Color.yellow;
                    }
                    else
                    {
                        // Normal state (cyan)
                        Gizmos.color = subscribedChickenColor;
                    }
                    
                    // Draw wireframe sphere above each subscribed chicken
                    Vector3 indicatorPos = chicken.transform.position + Vector3.up * 1.5f;
                    Gizmos.DrawWireSphere(indicatorPos, 0.4f);
                }
            }
        }
    }

    // Properties for external access
    public int TotalRegisteredChickens => allRegisteredChickens.Count;
    public int AssignedChickensCount => slotAssignments.Count;
    public int WaitingChickensCount => waitingChickens.Count;
    public int AvailableSlots => formationCreator != null ? formationCreator.GetFormationSlots().Count - slotAssignments.Count : 0;
    public bool HasInitializedFormation => hasInitializedFormation; // NEW: Public property to check initialization state
}