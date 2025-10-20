using System;
using UnityEngine;
using System.Collections.Generic;
using KBCore.Refs;
using VInspector;
using Random = UnityEngine.Random;

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
    public float majorPositionChangeThreshold = 3f;
    
    [Header("Chicken State Synchronization")]
    public bool autoRefreshChickenStates = true;
    public float stateRefreshInterval = 1f;
    public bool forceStateUpdateOnReassign = true;
    
    [Header("References")]
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;

    private Dictionary<int, GameObject> slotAssignments = new Dictionary<int, GameObject>();
    private List<GameObject> waitingChickens = new List<GameObject>();
    private List<GameObject> allRegisteredChickens = new List<GameObject>();
    
    private int previousSlotCount = -1;
    private FormationCreator.FormationType previousFormationType;
    private Vector3 previousFormationCenter = Vector3.zero;
    private float formationCheckTimer = 0f;
    private float stateRefreshTimer = 0f;
    private bool hasInitializedFormation = false;
    
    // Freeze system to prevent slot assignments during disruption
    private bool isSlotAssignmentFrozen = false;


    private void OnValidate()
    {
        if (!levelManager) levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (!player) player = FindFirstObjectByType<RailPlayer>(FindObjectsInactive.Include);
        
        this.ValidateRefs();
    }


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


    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnRestartedFromSavePoint += OnRestartedFromSavePoint;
        }

        if (player)
        {
            player.Health.OnDeath += OnPlayerDeath;
        }
    }
    
    private void OnDisable()
    {
        if (levelManager)
        {
            levelManager.OnRestartedFromSavePoint -= OnRestartedFromSavePoint;
        }
        
        if (player)
        {
            player.Health.OnDeath -= OnPlayerDeath;
        }
    }
    
    void OnRestartedFromSavePoint(SavePointData savePointData)
    {
        SetAutoUpdatesEnabled(true);
        UnfreezeSlotAssignments(); // Ensure slots are unfrozen after restart
    }
    
    void OnPlayerDeath()
    {
        SetAutoUpdatesEnabled(false);
        
        // Immediately stop any active formation effects
        var formationEffectManager = FindFirstObjectByType<FormationEffectManager>();
        if (formationEffectManager)
        {
            formationEffectManager.StopAllEffects();
        }
        
        // Find all active chickens and force them to cleanup
        var allChickens = FindObjectsByType<EnemyChickenRegistration>(FindObjectsSortMode.None);

        int cleanedCount = 0;
        foreach (var chicken in allChickens)
        {
            if (chicken != null && chicken.gameObject.activeInHierarchy)
            {
                chicken.ForceCompleteUnregister();
                cleanedCount++;
            }
        }

        ForceCompleteReset();
        ValidateAndFixChickenStates();
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
        
        if (hasInitializedFormation)
        {
            float centerDistance = Vector3.Distance(currentFormationCenter, previousFormationCenter);
            majorPositionChange = centerDistance > majorPositionChangeThreshold;
        }
        
        if (isInitialSetup || formationTypeChanged || slotCountChanged || majorPositionChange)
        {
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

    public void ForceUpdateAllChickenStates()
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

    public void ForceCompleteReset()
    {
        // Debug.Log("EnemyChickenManager: Force complete reset initiated");

        slotAssignments.Clear();
        waitingChickens.Clear();
        allRegisteredChickens.Clear();
        
        previousSlotCount = -1;
        previousFormationType = FormationCreator.FormationType.Square;
        previousFormationCenter = Vector3.zero;
        hasInitializedFormation = false;
        
        formationCheckTimer = 0f;
        stateRefreshTimer = 0f;
        
        // Reset freeze state
        isSlotAssignmentFrozen = false;
        
        // Debug.Log("EnemyChickenManager: Complete reset finished");
    }

    public void SetAutoUpdatesEnabled(bool enabled)
    {
        autoReassignOnFormationChange = enabled;
        autoRefreshChickenStates = enabled;
        
        if (!enabled)
        {
            // Debug.Log("EnemyChickenManager: Auto-updates disabled for cleanup");
        }
        else
        {
            // Debug.Log("EnemyChickenManager: Auto-updates re-enabled");
        }
    }

    // Freeze slot assignments - prevents ANY chickens from being assigned to slots
    public void FreezeSlotAssignments()
    {
        if (isSlotAssignmentFrozen)
            return;
            
        isSlotAssignmentFrozen = true;
        // Debug.Log("EnemyChickenManager: Slot assignments FROZEN - no chickens can be assigned to formation slots");
    }

    // Unfreeze slot assignments - allows normal operation and reassigns all chickens
    public void UnfreezeSlotAssignments()
    {
        if (!isSlotAssignmentFrozen)
            return;
            
        isSlotAssignmentFrozen = false;
        // Debug.Log("EnemyChickenManager: Slot assignments UNFROZEN - normal operation resumed");
        
        // Force complete reassignment to fill any empty slots created during freeze
        // (e.g., if chickens died while frozen)
        ReassignAllChickens();
        // Debug.Log($"EnemyChickenManager: Force reassigned all chickens after unfreeze. {slotAssignments.Count} assigned, {waitingChickens.Count} waiting");
    }

    public bool RegisterChicken(GameObject chicken)
    {
        if (chicken == null || allRegisteredChickens.Contains(chicken))
        {
            return false;
        }

        allRegisteredChickens.Add(chicken);
        
        // Check if slot assignments are frozen
        if (isSlotAssignmentFrozen)
        {
            // If frozen, add to waiting list without attempting assignment
            waitingChickens.Add(chicken);
            Debug.Log($"EnemyChickenManager: Chicken '{chicken.name}' registered but added to waiting list (slots frozen)");
            return true;
        }
        
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
            
            // Only try to assign waiting chicken if not frozen
            if (!isSlotAssignmentFrozen)
            {
                AssignWaitingChickenToSlot(assignedSlot);
            }
        }

        return true;
    }

    bool AssignChickenToSlot(GameObject chicken)
    {
        // Don't assign if frozen
        if (isSlotAssignmentFrozen)
            return false;
            
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
        // Don't assign if frozen
        if (isSlotAssignmentFrozen)
            return;
            
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
        // Don't reassign if frozen
        if (isSlotAssignmentFrozen)
        {
            Debug.Log("EnemyChickenManager: Cannot reassign chickens - slot assignments are frozen");
            return;
        }
            
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
            
            if (hasSlot != chickenThinksItHasSlot)
            {
                Debug.LogWarning($"Fixing desynchronized chicken {chicken.name}: Manager says hasSlot={hasSlot}, Chicken says hasSlot={chickenThinksItHasSlot}");
                registration.ForceStateUpdate();
                if (movement != null)
                    movement.RefreshMovementState();
                fixedCount++;
            }
            else if (hasSlot && currentState == ChickenStateController.ChickenState.Idle)
            {
                Debug.LogWarning($"Fixing stuck chicken {chicken.name}: Has slot but is idle");
                registration.ForceStateUpdate();
                if (movement != null)
                    movement.RefreshMovementState();
                fixedCount++;
            }
            else if (!hasSlot && currentState != ChickenStateController.ChickenState.Idle && currentState != ChickenStateController.ChickenState.Concussed)
            {
                Debug.LogWarning($"Fixing stuck chicken {chicken.name}: No slot but not idle (state: {currentState})");
                registration.ForceStateUpdate();
                if (movement != null)
                    movement.RefreshMovementState();
                fixedCount++;
            }
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
        Debug.Log($"Slot Assignments FROZEN: {isSlotAssignmentFrozen}");
        Debug.Log($"Chickens: {allRegisteredChickens.Count} registered | {slotAssignments.Count} assigned | {waitingChickens.Count} waiting");
        Debug.Log("");
        
        if (slotAssignments.Count > 0)
        {
            Debug.Log("ASSIGNED CHICKENS:");
            foreach (var kvp in slotAssignments)
            {
                string chickenName = kvp.Value != null ? kvp.Value.name : "NULL";
                string state = "Unknown";
                
                if (kvp.Value != null)
                {
                    var stateController = kvp.Value.GetComponent<ChickenStateController>();
                    if (stateController != null)
                    {
                        state = stateController.CurrentState.ToString();
                    }
                }
                
                Debug.Log($"  Slot {kvp.Key}: {chickenName} ({state})");
            }
        }
        
        if (waitingChickens.Count > 0)
        {
            Debug.Log("WAITING CHICKENS:");
            for (int i = 0; i < waitingChickens.Count; i++)
            {
                string chickenName = waitingChickens[i] != null ? waitingChickens[i].name : "NULL";
                Debug.Log($"  {i + 1}: {chickenName}");
            }
        }
    }

    public void ScrambleAssignedChickens()
    {
        if (isSlotAssignmentFrozen)
        {
            Debug.Log("EnemyChickenManager: Cannot scramble - slot assignments are frozen");
            return;
        }
            
        if (slotAssignments.Count == 0)
        {
            Debug.Log("EnemyChickenManager: No assigned chickens to scramble.");
            return;
        }

        if (formationCreator == null)
        {
            Debug.LogError("EnemyChickenManager: Cannot scramble - no FormationCreator assigned!");
            return;
        }

        List<Vector3> formationSlots = formationCreator.GetFormationSlots();
        if (formationSlots.Count == 0)
        {
            Debug.LogWarning("EnemyChickenManager: Cannot scramble - no formation slots available!");
            return;
        }

        List<GameObject> assignedChickens = new List<GameObject>();
        foreach (var kvp in slotAssignments)
        {
            if (kvp.Value != null)
            {
                assignedChickens.Add(kvp.Value);
            }
        }

        slotAssignments.Clear();

        List<int> availableSlotIndices = new List<int>();
        for (int i = 0; i < formationSlots.Count && availableSlotIndices.Count < assignedChickens.Count; i++)
        {
            availableSlotIndices.Add(i);
        }

        ShuffleList(availableSlotIndices);

        for (int i = 0; i < assignedChickens.Count; i++)
        {
            if (i < availableSlotIndices.Count)
            {
                int randomSlotIndex = availableSlotIndices[i];
                slotAssignments[randomSlotIndex] = assignedChickens[i];
            }
            else
            {
                waitingChickens.Add(assignedChickens[i]);
            }
        }

        if (forceStateUpdateOnReassign)
        {
            ForceUpdateAssignedChickenStates();
        }
        else
        {
            RefreshAssignedChickenStates();
        }
    }

    void ForceUpdateAssignedChickenStates()
    {
        foreach (var kvp in slotAssignments)
        {
            GameObject chicken = kvp.Value;
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

    void RefreshAssignedChickenStates()
    {
        foreach (var kvp in slotAssignments)
        {
            GameObject chicken = kvp.Value;
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

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    [ContextMenu("Freeze Slot Assignments")]
    public void FreezeSlotAssignmentsMenu()
    {
        FreezeSlotAssignments();
    }

    [ContextMenu("Unfreeze Slot Assignments")]
    public void UnfreezeSlotAssignmentsMenu()
    {
        UnfreezeSlotAssignments();
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
                        Gizmos.color = Color.red;
                    }
                    else if (!hasSlot && stateController != null && !stateController.IsIdle && !stateController.IsConcussed)
                    {
                        Gizmos.color = Color.orange;
                    }
                    else if (hasSlot && stateController != null && stateController.IsMovingToSlot && movement != null && !movement.IsCurrentlyMoving)
                    {
                        Gizmos.color = Color.yellow;
                    }
                    else if (hasSlot && stateController != null && stateController.IsFollowingSlot && movement != null)
                    {
                        if (movement.IsActivelyFollowing)
                        {
                            Gizmos.color = Color.green;
                        }
                        else
                        {
                            Gizmos.color = Color.blue;
                        }
                    }
                    else
                    {
                        Gizmos.color = subscribedChickenColor;
                    }
                    
                    Vector3 indicatorPos = chicken.transform.position + Vector3.up * 1.5f;
                    Gizmos.DrawWireSphere(indicatorPos, 0.4f);
                }
            }
        }
    }

    public int TotalRegisteredChickens => allRegisteredChickens.Count;
    public int AssignedChickensCount => slotAssignments.Count;
    public int WaitingChickensCount => waitingChickens.Count;
    public int AvailableSlots => formationCreator != null ? formationCreator.GetFormationSlots().Count - slotAssignments.Count : 0;
    public bool HasInitializedFormation => hasInitializedFormation;
    public bool IsSlotAssignmentFrozen => isSlotAssignmentFrozen;
}