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
    public bool autoReassignOnFormationChange = true;
    public float formationCheckInterval = 0.1f;
    public float majorPositionChangeThreshold = 3f; // Distance threshold to detect formation repositioning
    
    [Header("Chicken State Synchronization")]
    public bool autoRefreshChickenStates = true;
    public float stateRefreshInterval = 1f;
    public bool forceStateUpdateOnReassign = true;

    private Dictionary<int, GameObject> slotAssignments = new Dictionary<int, GameObject>();
    private List<GameObject> waitingChickens = new List<GameObject>();
    private List<GameObject> allRegisteredChickens = new List<GameObject>();
    
    private int previousSlotCount = -1;
    private FormationCreator.FormationType previousFormationType;
    private Vector3 previousFormationCenter = Vector3.zero;
    private float formationCheckTimer = 0f;
    private float stateRefreshTimer = 0f;
    private bool hasInitializedFormation = false;

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
    }

    void Update()
    {
        if (formationCreator == null)
            return;
            
        if (autoReassignOnFormationChange)
        {
            formationCheckTimer += Time.deltaTime;
            if (formationCheckTimer >= formationCheckInterval)
            {
                CheckForFormationChanges();
                formationCheckTimer = 0f;
            }
        }
        
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
        Vector3 currentFormationCenter = GetFormationCenter();
        
        bool isInitialSetup = !hasInitializedFormation;
        bool formationTypeChanged = currentFormationType != previousFormationType;
        bool slotCountChanged = currentSlotCount != previousSlotCount;
        bool majorPositionChange = false;
        
        // Check for major position changes (formation repositioning)
        if (hasInitializedFormation)
        {
            float centerDistance = Vector3.Distance(currentFormationCenter, previousFormationCenter);
            majorPositionChange = centerDistance > majorPositionChangeThreshold;
            
            if (majorPositionChange)
            {
                Debug.Log($"EnemyChickenManager: Major formation position change detected. Distance: {centerDistance:F2} (threshold: {majorPositionChangeThreshold:F2})");
            }
        }
        
        if (isInitialSetup || formationTypeChanged || slotCountChanged || majorPositionChange)
        {
            if (isInitialSetup)
            {
                Debug.Log($"EnemyChickenManager: Initial formation setup detected - {currentFormationType} with {currentSlotCount} slots. Assigning all chickens...");
            }
            else if (formationTypeChanged)
            {
                Debug.Log($"EnemyChickenManager: Formation type changed from {previousFormationType} to {currentFormationType}. Reassigning all chickens...");
            }
            else if (slotCountChanged)
            {
                Debug.Log($"EnemyChickenManager: Formation slot count changed from {previousSlotCount} to {currentSlotCount}. Reassigning all chickens...");
            }
            else if (majorPositionChange)
            {
                Debug.Log($"EnemyChickenManager: Formation repositioned significantly. Reassigning all chickens...");
            }
            
            // For major changes, force chickens back to MovingToSlot state
            if (formationTypeChanged || majorPositionChange)
            {
                ForceAllChickensToMovingToSlot();
            }
            
            ReassignAllChickens();
            
            if (forceStateUpdateOnReassign)
            {
                ForceUpdateAllChickenStates();
            }
            
            previousSlotCount = currentSlotCount;
            previousFormationType = currentFormationType;
            previousFormationCenter = currentFormationCenter;
            hasInitializedFormation = true;
        }
        else
        {
            // Update center position for minor changes (like formation resizing)
            previousFormationCenter = currentFormationCenter;
        }
    }

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
                
                ChickenMovementBehavior movement = chicken.GetComponent<ChickenMovementBehavior>();
                if (movement != null)
                {
                    movement.RefreshMovementState();
                }
            }
        }
    }

    Vector3 GetFormationCenter()
    {
        if (formationCreator == null)
            return Vector3.zero;
            
        List<Vector3> formationSlots = formationCreator.GetFormationSlots();
        if (formationSlots.Count == 0)
            return Vector3.zero;
            
        Vector3 center = Vector3.zero;
        foreach (Vector3 slot in formationSlots)
        {
            center += slot;
        }
        center /= formationSlots.Count;
        
        return center;
    }

    void ForceAllChickensToMovingToSlot()
    {
        int forcedCount = 0;
        
        foreach (GameObject chicken in allRegisteredChickens)
        {
            if (chicken != null)
            {
                ChickenStateController stateController = chicken.GetComponent<ChickenStateController>();
                if (stateController != null && stateController.IsFollowingSlot)
                {
                    stateController.SetMovingToSlot();
                    forcedCount++;
                    
                    // Also refresh movement behavior to start proper movement
                    ChickenMovementBehavior movement = chicken.GetComponent<ChickenMovementBehavior>();
                    if (movement != null)
                    {
                        movement.RefreshMovementState();
                    }
                }
            }
        }
        
        if (forcedCount > 0)
        {
            Debug.Log($"EnemyChickenManager: Forced {forcedCount} chickens from FollowingSlot back to MovingToSlot due to major formation change");
        }
    }

    public bool RegisterChicken(GameObject chicken)
    {
        if (chicken == null || allRegisteredChickens.Contains(chicken))
        {
            return false;
        }

        allRegisteredChickens.Add(chicken);
        
        bool assigned = AssignChickenToSlot(chicken);
        
        if (!assigned)
        {
            waitingChickens.Add(chicken);
        }

        if (hasInitializedFormation)
        {
            EnemyChickenRegistration registration = chicken.GetComponent<EnemyChickenRegistration>();
            if (registration != null)
            {
                registration.ForceStateUpdate();
            }
        }

        return true;
    }

    public bool UnregisterChicken(GameObject chicken)
    {
        if (chicken == null || !allRegisteredChickens.Contains(chicken))
        {
            return false;
        }

        allRegisteredChickens.Remove(chicken);
        waitingChickens.Remove(chicken);

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
            AssignWaitingChickenToSlot(assignedSlot);
        }

        return true;
    }

    bool AssignChickenToSlot(GameObject chicken)
    {
        if (formationCreator == null)
            return false;

        List<Vector3> formationSlots = formationCreator.GetFormationSlots();
        
        for (int i = 0; i < formationSlots.Count; i++)
        {
            if (!slotAssignments.ContainsKey(i))
            {
                slotAssignments[i] = chicken;
                return true;
            }
        }

        return false;
    }

    void AssignWaitingChickenToSlot(int slotIndex)
    {
        if (waitingChickens.Count == 0)
            return;

        GameObject waitingChicken = waitingChickens[0];
        waitingChickens.RemoveAt(0);
        slotAssignments[slotIndex] = waitingChicken;
        
        if (hasInitializedFormation)
        {
            EnemyChickenRegistration registration = waitingChicken.GetComponent<EnemyChickenRegistration>();
            if (registration != null)
            {
                registration.ForceStateUpdate();
            }
        }
    }

    public int GetChickenSlotIndex(GameObject chicken)
    {
        foreach (var kvp in slotAssignments)
        {
            if (kvp.Value == chicken)
                return kvp.Key;
        }
        return -1;
    }

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

    public GameObject GetChickenInSlot(int slotIndex)
    {
        return slotAssignments.ContainsKey(slotIndex) ? slotAssignments[slotIndex] : null;
    }

    public void ReassignAllChickens()
    {
        slotAssignments.Clear();
        waitingChickens.Clear();

        waitingChickens.AddRange(allRegisteredChickens);

        for (int i = waitingChickens.Count - 1; i >= 0; i--)
        {
            if (AssignChickenToSlot(waitingChickens[i]))
            {
                waitingChickens.RemoveAt(i);
            }
        }
        
        Debug.Log($"EnemyChickenManager: Reassignment complete. {slotAssignments.Count} assigned, {waitingChickens.Count} waiting.");
    }

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
            // Check if chicken doesn't have slot but isn't idle (unless concussed)
            else if (!hasSlot && currentState != ChickenStateController.ChickenState.Idle && currentState != ChickenStateController.ChickenState.Concussed)
            {
                Debug.LogWarning($"Fixing stuck chicken {chicken.name}: No slot but not idle (state: {currentState})");
                registration.ForceStateUpdate();
                if (movement != null)
                    movement.RefreshMovementState();
                fixedCount++;
            }
            // Check if chicken should be moving but movement behavior says it's not
            else if (hasSlot && currentState == ChickenStateController.ChickenState.MovingToSlot)
            {
                if (movement != null && !movement.IsCurrentlyMoving)
                {
                    Debug.LogWarning($"Fixing movement stuck chicken {chicken.name}: Should be moving to slot but movement behavior is inactive");
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

    [ContextMenu("Show Chicken List")]
    public void PrintCurrentAssignments()
    {
        Debug.Log("=== CHICKEN MANAGER STATUS ===");
        Debug.Log($"Formation: {(formationCreator != null ? formationCreator.currentFormation.ToString() : "None")}");
        Debug.Log($"Total Slots: {(formationCreator != null ? formationCreator.GetFormationSlots().Count : 0)}");
        Debug.Log($"Formation Center: {GetFormationCenter()}");
        Debug.Log($"Major Position Threshold: {majorPositionChangeThreshold:F2}");
        Debug.Log($"Chickens: {allRegisteredChickens.Count} registered | {slotAssignments.Count} assigned | {waitingChickens.Count} waiting");
        Debug.Log($"Auto-reassign: {(autoReassignOnFormationChange ? "Enabled" : "Disabled")}");
        Debug.Log($"Auto-refresh: {(autoRefreshChickenStates ? "Enabled" : "Disabled")}");
        Debug.Log($"Formation Initialized: {hasInitializedFormation}");
        Debug.Log("");
        
        if (slotAssignments.Count > 0)
        {
            Debug.Log("ASSIGNED CHICKENS:");
            foreach (var kvp in slotAssignments)
            {
                string chickenName = kvp.Value != null ? kvp.Value.name : "NULL";
                string state = "Unknown";
                bool isMoving = false;
                
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
                        // Check different types of movement
                        if (stateController != null && stateController.IsFollowingSlot)
                        {
                            if (movement.IsActivelyFollowing)
                            {
                                isMoving = true;
                                state += " [FOLLOWING]"; // Actively moving to catch up
                            }
                            else
                            {
                                isMoving = false;
                                state += " [TRACKING]"; // At perfect position, not moving
                            }
                        }
                        else
                        {
                            isMoving = movement.IsCurrentlyMoving;
                        }
                    }
                }
                
                Debug.Log($"  Slot {kvp.Key}: {chickenName} ({state}) {(isMoving ? "[MOVING]" : "[STATIC]")}");
            }
            Debug.Log("");
        }
        
        if (waitingChickens.Count > 0)
        {
            Debug.Log("WAITING CHICKENS:");
            for (int i = 0; i < waitingChickens.Count; i++)
            {
                string chickenName = waitingChickens[i] != null ? waitingChickens[i].name : "NULL";
                string state = "Unknown";
                bool isMoving = false;
                
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
                        // Check different types of movement
                        if (stateController != null && stateController.IsFollowingSlot)
                        {
                            if (movement.IsActivelyFollowing)
                            {
                                isMoving = true;
                                state += " [FOLLOWING]"; // Actively moving to catch up
                            }
                            else
                            {
                                isMoving = false;
                                state += " [TRACKING]"; // At perfect position, not moving
                            }
                        }
                        else
                        {
                            isMoving = movement.IsCurrentlyMoving;
                        }
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

    [ContextMenu("Force Formation Initialization")]
    public void ForceFormationInitialization()
    {
        hasInitializedFormation = false;
        previousSlotCount = -1;
        Debug.Log("EnemyChickenManager: Reset formation initialization. Next update will detect formation as new.");
    }

    [ContextMenu("Test Major Position Change")]
    public void TestMajorPositionChange()
    {
        if (formationCreator != null)
        {
            // Simulate a major position change by updating the previous center
            Vector3 currentCenter = GetFormationCenter();
            previousFormationCenter = currentCenter + Vector3.right * (majorPositionChangeThreshold + 1f);
            Debug.Log($"EnemyChickenManager: Simulated major position change. Next formation check will detect it as major change.");
        }
    }

    [ContextMenu("Force All Chickens to MovingToSlot")]
    public void ForceAllChickensToMovingToSlotMenu()
    {
        ForceAllChickensToMovingToSlot();
    }

    void OnDrawGizmos()
    {
        if (formationCreator == null)
            return;

        List<Vector3> formationSlots = formationCreator.GetFormationSlots();

        if (showAssignedSlotConnections && slotAssignments.Count > 0)
        {
            Gizmos.color = assignedSlotColor;
            foreach (var kvp in slotAssignments)
            {
                if (kvp.Key < formationSlots.Count && kvp.Value != null)
                {
                    Vector3 slotPosition = formationSlots[kvp.Key];
                    
                    Gizmos.DrawSphere(slotPosition, debugSlotSize);
                    Gizmos.DrawLine(kvp.Value.transform.position, slotPosition);
                }
            }
        }

        if (showAllSubscribedChickens && allRegisteredChickens.Count > 0)
        {
            foreach (GameObject chicken in allRegisteredChickens)
            {
                if (chicken != null)
                {
                    bool hasSlot = GetChickenSlotIndex(chicken) != -1;
                    ChickenStateController stateController = chicken.GetComponent<ChickenStateController>();
                    ChickenMovementBehavior movement = chicken.GetComponent<ChickenMovementBehavior>();
                    
                    if (hasSlot && stateController != null && stateController.IsIdle)
                    {
                        // Chicken has slot but is idle - potential bug (red)
                        Gizmos.color = Color.red;
                    }
                    else if (!hasSlot && stateController != null && !stateController.IsIdle && !stateController.IsConcussed)
                    {
                        // Chicken doesn't have slot but isn't idle (and not concussed) - potential bug (orange)
                        Gizmos.color = Color.orange;
                    }
                    else if (hasSlot && stateController != null && stateController.IsMovingToSlot && movement != null && !movement.IsCurrentlyMoving)
                    {
                        // Should be moving to slot but isn't - potential bug (yellow)
                        Gizmos.color = Color.yellow;
                    }
                    else if (hasSlot && stateController != null && stateController.IsFollowingSlot && movement != null)
                    {
                        // Following slot - different colors based on following state
                        if (movement.IsActivelyFollowing)
                        {
                            Gizmos.color = Color.green; // Actively following (moving directly)
                        }
                        else
                        {
                            Gizmos.color = Color.blue; // Following but static (at perfect position)
                        }
                    }
                    else
                    {
                        // Normal state (cyan)
                        Gizmos.color = subscribedChickenColor;
                    }
                    
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
    public bool HasInitializedFormation => hasInitializedFormation;
}