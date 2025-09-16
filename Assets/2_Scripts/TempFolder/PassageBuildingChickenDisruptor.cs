using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using KBCore.Refs;

[RequireComponent(typeof(BoxCollider))]
public class PassageBuildingChickenDisruptor : MonoBehaviour
{
    [Header("Disruption Settings")]
    [SerializeField] private float recoveryDelay = 3f; // Delay after building exits before chickens recover
    [SerializeField] private bool affectOnlyActiveChickens = true; // Only affect non-idle chickens
    [SerializeField] private bool pauseFormationEffects = true; // Pause formation effects during disruption
    
    [Header("Detection Settings")]
    [SerializeField] private string passageBuildingTag = "Passage Building";
    
    [Header("References")]
    [SerializeField, Scene(Flag.EditableAnywhere)] private FormationEffectManager formationEffectManager;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showDisruptionGizmos = true;
    [SerializeField] private Color disruptionColor = Color.red;
    [SerializeField] private Color recoveryColor = Color.orange;
    
    // State tracking
    private bool isDisrupting = false;
    private bool isInRecoveryDelay = false;
    private float recoveryTimeRemaining = 0f;
    private List<ChickenDisruptionData> disruptedChickens = new List<ChickenDisruptionData>();
    private HashSet<Collider> buildingsInZone = new HashSet<Collider>(); // Track multiple buildings
    private BoxCollider boxCollider;
    
    // Formation effect state tracking
    private bool wasBreathingActiveBeforeDisruption = false;
    private bool wasRotationActiveBeforeDisruption = false;
    private bool hadPendingActivationBeforeDisruption = false;
    
    // References
    private EnemyChickenManager chickenManager;
    
    // Data structure to track disrupted chickens
    [System.Serializable]
    private class ChickenDisruptionData
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
    
    void Awake()
    {
        // Ensure we have a BoxCollider set as trigger
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError($"PassageBuildingChickenDisruptor on {gameObject.name}: BoxCollider component is required!");
        }
    }
    
    void Start()
    {
        // Find the chicken manager
        chickenManager = FindFirstObjectByType<EnemyChickenManager>();
        if (chickenManager == null && showDebugLogs)
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
            Debug.Log($"PassageBuildingChickenDisruptor on {gameObject.name}: Initialized. Will disrupt chickens while '{passageBuildingTag}' objects are in zone, with {recoveryDelay}s recovery delay after exit.");
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
        // Check if the object has the correct tag
        if (!other.CompareTag(passageBuildingTag))
        {
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: {passageBuildingTag} '{other.gameObject.name}' entered trigger zone!");
        }
        
        // Add building to tracking set
        buildingsInZone.Add(other);
        
        // Start disruption if this is the first building
        if (buildingsInZone.Count == 1)
        {
            StartDisruption();
        }
        else if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: Additional building entered zone. Total buildings: {buildingsInZone.Count}");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Check if the object has the correct tag
        if (!other.CompareTag(passageBuildingTag))
        {
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: {passageBuildingTag} '{other.gameObject.name}' exited trigger zone!");
        }
        
        // Remove building from tracking set
        buildingsInZone.Remove(other);
        
        // If no more buildings in zone, start recovery delay
        if (buildingsInZone.Count == 0)
        {
            StartRecoveryDelay();
        }
        else if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: Building exited, but others remain. Total buildings: {buildingsInZone.Count}");
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
        
        // Pause formation effects before disrupting chickens
        if (pauseFormationEffects)
        {
            PauseFormationEffects();
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
        // Formation effects remain paused during recovery
    }
    
    void UpdateRecoveryDelay()
    {
        recoveryTimeRemaining -= Time.deltaTime;
        
        if (recoveryTimeRemaining <= 0f)
        {
            EndDisruption();
        }
    }
    
    void PauseFormationEffects()
    {
        if (!formationEffectManager) return;
        
        // Store current effect states
        wasBreathingActiveBeforeDisruption = formationEffectManager.IsBreathingActive;
        wasRotationActiveBeforeDisruption = formationEffectManager.IsRotationActive;
        
        // Check if there was a pending activation
        var pendingField = formationEffectManager.GetType().GetField("isWaitingForEffectActivation", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        hadPendingActivationBeforeDisruption = pendingField?.GetValue(formationEffectManager) is bool pending && pending;
        
        if (showDebugLogs)
        {
            string stateMsg = "Formation effects paused: ";
            stateMsg += $"Breathing={wasBreathingActiveBeforeDisruption}, Rotation={wasRotationActiveBeforeDisruption}";
            if (hadPendingActivationBeforeDisruption)
            {
                stateMsg += ", PendingActivation=true";
            }
            Debug.Log($"PassageBuildingChickenDisruptor: {stateMsg}");
        }
        
        // Force stop all effects (this will also cancel any pending activations)
        formationEffectManager.ForceStopAllEffects();
    }
    
    void ResumeFormationEffects()
    {
        if (!formationEffectManager || !pauseFormationEffects) return;
        
        if (showDebugLogs)
        {
            string resumeMsg = "Resuming formation effects: ";
            resumeMsg += $"Breathing={wasBreathingActiveBeforeDisruption}, Rotation={wasRotationActiveBeforeDisruption}";
            if (hadPendingActivationBeforeDisruption)
            {
                resumeMsg += ", RestartPending=true";
            }
            Debug.Log($"PassageBuildingChickenDisruptor: {resumeMsg}");
        }
        
        // If effects were active or pending before disruption, restart them
        if (wasBreathingActiveBeforeDisruption || wasRotationActiveBeforeDisruption || hadPendingActivationBeforeDisruption)
        {
            if (formationEffectManager.UseStageBasedActivation)
            {
                // In stage-based mode, force a new roll for effects
                // This will restart the delay timer and potentially activate effects again
                formationEffectManager.ForceRollForEffects();
            }
            else
            {
                // In manual mode, restore the previous states
                if (wasBreathingActiveBeforeDisruption)
                    formationEffectManager.StartBreathing();
                if (wasRotationActiveBeforeDisruption)
                    formationEffectManager.StartRotation();
                
                if (wasBreathingActiveBeforeDisruption || wasRotationActiveBeforeDisruption)
                    formationEffectManager.StartEffects();
            }
        }
        
        // Clear stored states
        wasBreathingActiveBeforeDisruption = false;
        wasRotationActiveBeforeDisruption = false;
        hadPendingActivationBeforeDisruption = false;
    }
    
    void DisruptAllChickens()
    {
        disruptedChickens.Clear();
        
        if (chickenManager == null)
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
                disruptedChickens.Add(disruptionData);
                
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
            Debug.Log($"PassageBuildingChickenDisruptor: Recovery delay complete, restoring {disruptedChickens.Count} chickens");
        }
        
        int restoredCount = 0;
        
        // Restore all disrupted chickens
        foreach (var disruptionData in disruptedChickens)
        {
            if (!disruptionData.IsValid())
            {
                continue; // Skip invalid chickens (might have been destroyed)
            }
            
            // Re-enable auto state management if it was originally enabled
            disruptionData.registration.autoManageState = disruptionData.wasAutoManaging;
            
            // Force a state update to let the chicken determine its proper state
            if (disruptionData.wasAutoManaging)
            {
                disruptionData.registration.ForceStateUpdate();
            }
            else
            {
                // If auto management was disabled, restore the original state
                disruptionData.stateController.ChangeState(disruptionData.originalState);
            }
            
            restoredCount++;
            
            if (showDebugLogs)
            {
                Debug.Log($"PassageBuildingChickenDisruptor: Restored chicken '{disruptionData.stateController.gameObject.name}' (auto-manage: {disruptionData.wasAutoManaging})");
            }
        }
        
        // Clean up
        disruptedChickens.Clear();
        isDisrupting = false;
        isInRecoveryDelay = false;
        recoveryTimeRemaining = 0f;
        
        // Resume formation effects after chicken restoration
        if (pauseFormationEffects)
        {
            // Wait a frame to ensure chickens are fully restored before resuming effects
            StartCoroutine(ResumeFormationEffectsDelayed());
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"PassageBuildingChickenDisruptor: Disruption ended, restored {restoredCount} chickens");
        }
    }
    
    // Wait one frame before resuming effects to ensure chicken states are stable
    IEnumerator ResumeFormationEffectsDelayed()
    {
        yield return null; // Wait one frame
        ResumeFormationEffects();
    }
    
    // Force end disruption (useful for testing or special cases)
    [ContextMenu("Force End Disruption")]
    public void ForceEndDisruption()
    {
        if (isDisrupting || isInRecoveryDelay)
        {
            buildingsInZone.Clear();
            EndDisruption();
        }
        else
        {
            Debug.Log("PassageBuildingChickenDisruptor: No active disruption to end");
        }
    }
    
    // Start disruption manually (useful for testing)
    [ContextMenu("Force Start Disruption")]
    public void ForceStartDisruption()
    {
        // Simulate a building entering
        buildingsInZone.Add(boxCollider); // Use own collider as dummy
        StartDisruption();
    }
    
    // Simulate building exit for testing
    [ContextMenu("Force Start Recovery")]
    public void ForceStartRecovery()
    {
        if (isDisrupting && !isInRecoveryDelay)
        {
            buildingsInZone.Clear();
            StartRecoveryDelay();
        }
        else
        {
            Debug.Log("PassageBuildingChickenDisruptor: Must be actively disrupting (not in recovery) to start recovery");
        }
    }
    
    // Public properties
    public bool IsDisrupting => isDisrupting;
    public bool IsInRecoveryDelay => isInRecoveryDelay;
    public float RecoveryTimeRemaining => recoveryTimeRemaining;
    public int DisruptedChickensCount => disruptedChickens.Count;
    public int BuildingsInZone => buildingsInZone.Count;
    public float RecoveryDelay => recoveryDelay;
    public bool IsPausingFormationEffects => pauseFormationEffects;
    
    // Status for UI/debugging
    public string GetStatusString()
    {
        if (!isDisrupting)
            return "Inactive";
        
        if (isInRecoveryDelay)
            return $"Recovery Delay ({recoveryTimeRemaining:F1}s remaining)";
            
        return $"Active Disruption ({buildingsInZone.Count} building{(buildingsInZone.Count != 1 ? "s" : "")} present)";
    }
    
    public string GetFormationEffectStatus()
    {
        if (!pauseFormationEffects || !formationEffectManager)
            return "Not managing effects";
            
        if (isDisrupting)
            return "Effects PAUSED during disruption";
            
        return "Effects available for normal operation";
    }
    
    void OnDrawGizmos()
    {
        if (!showDisruptionGizmos) return;
        
        // Draw the trigger area
        if (boxCollider != null)
        {
            Color gizmoColor;
            if (isInRecoveryDelay)
                gizmoColor = recoveryColor;
            else if (isDisrupting)
                gizmoColor = disruptionColor;
            else
                gizmoColor = Color.yellow;
                
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (isDisrupting)
            {
                // Draw filled box when disrupting
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                
                // Draw wireframe
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else
            {
                // Draw wireframe only when not disrupting
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            
            Gizmos.matrix = Matrix4x4.identity;
        }
        
        // Draw connections to disrupted chickens
        if (isDisrupting && disruptedChickens.Count > 0)
        {
            Color connectionColor = isInRecoveryDelay ? recoveryColor : disruptionColor;
            Gizmos.color = connectionColor;
            
            foreach (var disruptionData in disruptedChickens)
            {
                if (disruptionData.IsValid())
                {
                    Gizmos.DrawLine(transform.position, disruptionData.stateController.transform.position);
                    Gizmos.DrawWireSphere(disruptionData.stateController.transform.position + Vector3.up * 2f, 0.5f);
                }
            }
        }
        
        // Draw buildings in zone
        if (buildingsInZone.Count > 0)
        {
            Gizmos.color = Color.white;
            foreach (var building in buildingsInZone)
            {
                if (building != null)
                {
                    Gizmos.DrawLine(transform.position, building.transform.position);
                    Gizmos.DrawWireSphere(building.transform.position + Vector3.up * 1f, 0.3f);
                }
            }
        }
        
        // Draw formation effect status indicator
        if (pauseFormationEffects && formationEffectManager && isDisrupting)
        {
            // Draw a cross or pause symbol above the disruptor
            Gizmos.color = Color.magenta;
            Vector3 iconPos = transform.position + Vector3.up * 3f;
            Gizmos.DrawLine(iconPos + Vector3.left * 0.5f, iconPos + Vector3.right * 0.5f);
            Gizmos.DrawLine(iconPos + Vector3.forward * 0.5f, iconPos + Vector3.back * 0.5f);
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