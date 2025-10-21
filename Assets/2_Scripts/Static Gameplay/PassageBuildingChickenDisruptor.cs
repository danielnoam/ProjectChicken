using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DNExtensions;
using KBCore.Refs;
using VInspector;


[System.Serializable]
public class ChickenDisruptionData
{
    public EnemyChickenRegistration registration;
    public ChickenStateController stateController;
    public ChickenStateController.ChickenState originalState;
    public bool wasAutoManaging;
        
    public ChickenDisruptionData(EnemyChickenRegistration reg, ChickenStateController state)
    {
        registration = reg;
        stateController = state;
        originalState = state.CurrentState;
        wasAutoManaging = reg.autoManageState;
    }
        
    public bool IsValid()
    {
        return registration != null && stateController != null;
    }
}


[RequireComponent(typeof(BoxCollider))]
public class PassageBuildingChickenDisruptor : MonoBehaviour
{
    [Header("Disruption Settings")]
    [SerializeField] private float recoveryDelay = 3f; // Delay after building exits before chickens recover
    [SerializeField] private bool affectOnlyActiveChickens = true; // Only affect non-idle chickens
    [SerializeField] private bool pauseFormationEffects = true; // Pause formation effects during disruption
    
    [Header("References")]
    [SerializeField, Scene(Flag.EditableAnywhere)] private FormationEffectManager formationEffectManager;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField, VInspector.ReadOnly] private bool isDisrupting;
    [SerializeField, VInspector.ReadOnly] private bool isInRecoveryDelay;
    [SerializeField, VInspector.ReadOnly] private float recoveryTimeRemaining;
    
    
    private readonly List<ChickenDisruptionData> _disruptedChickens = new List<ChickenDisruptionData>();
    private readonly HashSet<Collider> _buildingsInZone = new HashSet<Collider>(); 
    private BoxCollider _boxCollider;
    private EnemyChickenManager _chickenManager;
    

    
    void Awake()
    {
        // Ensure we have a BoxCollider set as trigger
        _boxCollider = GetComponent<BoxCollider>();
        if (_boxCollider != null)
        {
            _boxCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError($"PassageBuildingChickenDisruptor on {gameObject.name}: BoxCollider component is required!");
        }
    }
    
    void Start()
    {
        // Find the chicken manager
        _chickenManager = FindFirstObjectByType<EnemyChickenManager>();
        if (_chickenManager == null && showDebugLogs)
        {
            Debug.LogWarning($"PassageBuildingChickenDisruptor on {gameObject.name}: No EnemyChickenManager found in scene!");
        }
        
        // Find formation effect manager if not assigned
        if (!formationEffectManager)
        {
            formationEffectManager = FindFirstObjectByType<FormationEffectManager>();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor on {gameObject.name}: Initialized. Will disrupt chickens while objects are in zone, with {recoveryDelay}s recovery delay after exit.");
            if (pauseFormationEffects && formationEffectManager)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Formation effects will be paused during disruption");
            }
        }
    }
    
    void Update()
    {
        if (isInRecoveryDelay)
        {
            UpdateRecoveryDelay();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
    
        if (other.gameObject.TryGetComponent(out PassthroughObstacle passthroughObstacle))
        {
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: '{passthroughObstacle.name}' entered trigger zone!");
            }
    
            if (!_buildingsInZone.Add(other)) return;

            if (_buildingsInZone.Count == 1)
            {
                StartDisruption();
            }
            else if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Additional building entered zone. Total buildings: {_buildingsInZone.Count}");
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Check if the object has the correct tag
        if (other.gameObject.TryGetComponent(out PassthroughObstacle passthroughObstacle))
        {
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor:'{passthroughObstacle.name}' exited trigger zone!");
            }
            
            // Remove building from tracking set
            if (!_buildingsInZone.Contains(other)) return;
            _buildingsInZone.Remove(other);
        
            // If no more buildings in zone, start recovery delay
            if (_buildingsInZone.Count == 0)
            {
                StartRecoveryDelay();
            }
            else if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Building exited, but others remain. Total buildings: {_buildingsInZone.Count}");
            }
        }
    }
    
    void StartDisruption()
    {
        if (isDisrupting)
        {
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Disruption already active");
            }
            return;
        }
        
        // Cancel any ongoing recovery delay
        if (isInRecoveryDelay)
        {
            isInRecoveryDelay = false;
            recoveryTimeRemaining = 0f;
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Cancelled recovery delay - building re-entered zone");
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: Starting chicken disruption (active while building present)");
        }
        
        // Freeze slot assignments to prevent ANY chickens from taking slots
        if (_chickenManager != null)
        {
            _chickenManager.SetAutoUpdatesEnabled(false);
            _chickenManager.FreezeSlotAssignments();
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Disabled auto-updates and froze slot assignments");
            }
        }
        
        // Stop formation effects during disruption
        if (pauseFormationEffects && formationEffectManager != null)
        {
            formationEffectManager.ForceStopAllEffects();
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Stopped all formation effects");
            }
        }
        
        isDisrupting = true;
        
        // Find and disrupt chickens
        DisruptAllChickens();
    }
    
    void StartRecoveryDelay()
    {
        if (!isDisrupting)
        {
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: No disruption active to recover from");
            }
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: All buildings exited zone. Starting recovery delay: {recoveryDelay} seconds");
        }
        
        isInRecoveryDelay = true;
        recoveryTimeRemaining = recoveryDelay;
        
        // Note: We keep isDisrupting = true during recovery delay
        // Chickens remain disrupted until recovery completes
        // Formation effects remain stopped during recovery
        // Manager auto-updates remain disabled during recovery
    }
    
    void UpdateRecoveryDelay()
    {
        recoveryTimeRemaining -= Time.deltaTime;
        
        if (recoveryTimeRemaining <= 0f)
        {
            EndDisruption();
        }
    }
    
    void DisruptAllChickens()
    {
        _disruptedChickens.Clear();
        
        if (_chickenManager == null)
        {
            Debug.LogWarning("PassageBuildingChickenDisruptor: No chicken manager found, cannot disrupt chickens");
            return;
        }
        
        // Find all registered chickens
        List<GameObject> allChickens = new List<GameObject>();
        
        // Get chickens from all registrations
        EnemyChickenRegistration[] allRegistrations = FindObjectsByType<EnemyChickenRegistration>(FindObjectsSortMode.None);
        
        foreach (var registration in allRegistrations)
        {
            if (registration.IsRegistered)
            {
                allChickens.Add(registration.gameObject);
            }
        }
        
        int disruptedCount = 0;
        
        foreach (GameObject chickenObj in allChickens)
        {
            EnemyChickenRegistration registration = chickenObj.GetComponent<EnemyChickenRegistration>();
            ChickenStateController stateController = chickenObj.GetComponent<ChickenStateController>();
            
            if (registration == null || stateController == null)
            {
                continue;
            }
            
            // Check if we should affect this chicken
            bool shouldDisrupt = true;
            
            if (affectOnlyActiveChickens)
            {
                // Only disrupt chickens that are not already idle
                shouldDisrupt = !stateController.IsIdle;
            }
            
            if (shouldDisrupt)
            {
                // Store the chicken's current state
                ChickenDisruptionData disruptionData = new ChickenDisruptionData(registration, stateController);
                _disruptedChickens.Add(disruptionData);
                
                // Disable auto state management temporarily
                registration.autoManageState = false;
                
                // Force chicken to idle state
                stateController.SetIdle();
                
                disruptedCount++;
                
                if (showDebugLogs)
                {
                    Debug.Log($"PassageBuildingChickenDisruptor: Disrupted chicken '{chickenObj.name}' from {disruptionData.originalState} to Idle");
                }
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: Disrupted {disruptedCount} chickens out of {allChickens.Count} total chickens");
        }
    }
    
    void EndDisruption()
    {
        if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: Recovery delay complete, restoring {_disruptedChickens.Count} chickens");
        }
        
        int restoredCount = 0;
        
        // Restore all disrupted chickens
        foreach (var disruptionData in _disruptedChickens)
        {
            if (!disruptionData.IsValid())
            {
                continue;
            }
            
            // IMPORTANT: Reset tracking variables BEFORE re-enabling auto management
            // This ensures the system treats the slot assignment as "new"
            var registrationType = disruptionData.registration.GetType();
            var wasAssignedField = registrationType.GetField("wasAssignedLastFrame", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lastSlotIndexField = registrationType.GetField("lastKnownSlotIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lastSlotPosField = registrationType.GetField("lastKnownSlotPosition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (wasAssignedField != null)
                wasAssignedField.SetValue(disruptionData.registration, false);
            if (lastSlotIndexField != null)
                lastSlotIndexField.SetValue(disruptionData.registration, -1);
            if (lastSlotPosField != null)
                lastSlotPosField.SetValue(disruptionData.registration, null);
            
            // Re-enable auto state management if it was originally enabled
            disruptionData.registration.autoManageState = disruptionData.wasAutoManaging;
            
            // Set to idle first, then let the system detect the assignment
            disruptionData.stateController.SetIdle();
            
            restoredCount++;
            
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Restored chicken '{disruptionData.stateController.gameObject.name}' (auto-manage: {disruptionData.wasAutoManaging})");
            }
        }
        
        // Clean up
        _disruptedChickens.Clear();
        isDisrupting = false;
        isInRecoveryDelay = false;
        recoveryTimeRemaining = 0f;
        
        // Unfreeze slot assignments and re-enable auto-updates
        if (_chickenManager != null)
        {
            _chickenManager.UnfreezeSlotAssignments();
            _chickenManager.SetAutoUpdatesEnabled(true);
            
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Unfroze slot assignments and re-enabled auto-updates");
            }
        }
        
        // Update formation effects to current chicken count
        if (pauseFormationEffects && formationEffectManager != null)
        {
            // Wait a frame to ensure chickens are fully restored before updating formation effects
            StartCoroutine(UpdateFormationEffectsDelayed());
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: Disruption ended, restored {restoredCount} chickens");
        }
    }
    
    // Wait one frame before updating effects to ensure chicken states are stable
    IEnumerator UpdateFormationEffectsDelayed()
    {
        yield return null; // Wait one frame
        
        if (formationEffectManager != null && formationEffectManager.UseStageBasedActivation)
        {
            // Force a re-evaluation of effects based on current chicken count
            formationEffectManager.ForceUpdateChickenCount();
            
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Forced formation effect manager to update chicken count");
            }
        }
    }
    
    // Force end disruption (useful for testing or special cases)
    [Button("Force End Disruption")]
    public void ForceEndDisruption()
    {
        if (isDisrupting || isInRecoveryDelay)
        {
            _buildingsInZone.Clear();
            EndDisruption();
        }
        else
        {
            Debug.Log("PassageBuildingChickenDisruptor: No active disruption to end");
        }
    }
    
    // Start disruption manually (useful for testing)
    [Button("Force Start Disruption")]
    public void ForceStartDisruption()
    {
        // Simulate a building entering
        _buildingsInZone.Add(_boxCollider); // Use own collider as dummy
        StartDisruption();
    }
    
    // Simulate building exit for testing
    [Button("Force Start Recovery")]
    public void ForceStartRecovery()
    {
        if (isDisrupting && !isInRecoveryDelay)
        {
            _buildingsInZone.Clear();
            StartRecoveryDelay();
        }
        else
        {
            Debug.Log("PassageBuildingChickenDisruptor: Must be actively disrupting (not in recovery) to start recovery");
        }
    }
    
    
    
   
    
    void OnValidate()
    {
        // Ensure recovery delay is positive
        recoveryDelay = Mathf.Max(0f, recoveryDelay);
        
        // Find formation effect manager if not assigned
        if (!formationEffectManager)
        {
            formationEffectManager = FindFirstObjectByType<FormationEffectManager>();
        }
        
        this.ValidateRefs();
    }
}