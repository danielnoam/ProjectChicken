using UnityEngine;
using System.Collections.Generic;

public class ChickenCombatManagerV4 : MonoBehaviour
{
    [Header("General Combat Settings")]
    public bool enableCombat = true;
    public float eggSpeed = 20f;
    public float PatternChangeCooldown;
    
    [Header("Attack Configuration")]
    public AttackLootTableSO attackLootTable;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showAttackGizmos = false;
    public bool showRegistrationLogs = false;

    [Header("Registered Chickens (Read Only)")]
    [SerializeField] private List<ChickenCombatBehaviorV2> registeredCombatChickens = new List<ChickenCombatBehaviorV2>();
    
    // Combat state management
    private CombatState currentState = CombatState.WaitingForChickens;
    private float stateTimer = 0f;
    private BaseChickenAttackSO currentAttack = null;
    private int currentAttackUses = 0;
    private float nextAttackTime = 0f;
    
    private Transform player;

    // Combat states enum
    public enum CombatState
    {
        WaitingForChickens,    // Waiting for at least 1 chicken to register
        PatternCooldown,       // Waiting for pattern change cooldown to finish
        Attacking              // Currently executing attacks
    }

    // Public properties
    public int TotalCombatChickens => registeredCombatChickens.Count;
    public List<ChickenCombatBehaviorV2> RegisteredChickens => new List<ChickenCombatBehaviorV2>(registeredCombatChickens);
    public List<ChickenCombatBehaviorV2> GetAvailableAttackers() => GetAvailableAttackersInternal();
    public int AvailableAttackers => GetAvailableAttackersInternal().Count;
    public float EggSpeed => eggSpeed;
    public Transform Player => player;
    public AttackLootTableSO AttackLootTable => attackLootTable;
    public CombatState CurrentState => currentState;
    public BaseChickenAttackSO CurrentAttack => currentAttack;
    public int CurrentAttackUses => currentAttackUses;

    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("ChickenCombatManagerV4: No player found with 'Player' tag!");
        }

        // Validate attack loot table
        if (attackLootTable == null)
        {
            Debug.LogWarning("ChickenCombatManagerV4: No attack loot table assigned!");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"ChickenCombatManagerV4: Attack loot table '{attackLootTable.name}' loaded with {attackLootTable.GetValidAttacks().Count} valid attacks");
        }

        // Initialize combat state
        ResetCombatState();
    }

    void Update()
    {
        if (!enableCombat)
        {
            if (showDebugLogs && Time.frameCount % 300 == 0)
                Debug.Log("ChickenCombatManagerV4: Combat is disabled");
            return;
        }

        if (player == null)
        {
            if (showDebugLogs && Time.frameCount % 300 == 0)
                Debug.Log("ChickenCombatManagerV4: No player found, cannot attack");
            return;
        }

        // Update combat state machine
        UpdateCombatStateMachine();
    }

    private void UpdateCombatStateMachine()
    {
        switch (currentState)
        {
            case CombatState.WaitingForChickens:
                UpdateWaitingForChickens();
                break;
                
            case CombatState.PatternCooldown:
                UpdatePatternCooldown();
                break;
                
            case CombatState.Attacking:
                UpdateAttacking();
                break;
        }
    }

    private void UpdateWaitingForChickens()
    {
        // Check if we have at least one chicken registered
        if (TotalCombatChickens > 0)
        {
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: First chicken registered! Starting pattern cooldown ({PatternChangeCooldown}s)");
            
            StartPatternCooldown();
        }
    }

    private void UpdatePatternCooldown()
    {
        stateTimer -= Time.deltaTime;
        
        if (stateTimer <= 0f)
        {
            // Cooldown finished, select new attack
            SelectNewAttack();
        }
    }

    private void UpdateAttacking()
    {
        // Check if we still have chickens
        if (TotalCombatChickens == 0)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: No chickens left, returning to waiting state");
            
            ResetCombatState();
            return;
        }

        // Check if current attack is still valid
        if (currentAttack == null)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: Current attack is null, starting cooldown");
            
            StartPatternCooldown();
            return;
        }

        // Check if we've used up this attack pattern
        if (currentAttackUses >= currentAttack.UsesBeforePatternChange)
        {
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Attack '{currentAttack.AttackName}' used {currentAttackUses} times, starting pattern change cooldown");
            
            currentAttack = null;
            currentAttackUses = 0;
            StartPatternCooldown();
            return;
        }

        // Execute attack if it's time
        if (Time.time >= nextAttackTime)
        {
            ExecuteCurrentAttack();
        }
    }

    private void StartPatternCooldown()
    {
        currentState = CombatState.PatternCooldown;
        stateTimer = PatternChangeCooldown;
        
        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: Starting pattern cooldown for {PatternChangeCooldown} seconds");
    }

    private void SelectNewAttack()
    {
        if (attackLootTable == null)
        {
            Debug.LogWarning("ChickenCombatManagerV4: No attack loot table assigned, cannot select attack");
            ResetCombatState();
            return;
        }

        // Get available attackers
        var availableChickens = GetAvailableAttackersInternal();
        if (availableChickens.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: No available attackers, starting cooldown");
            
            StartPatternCooldown();
            return;
        }

        // Select a random attack that can be executed
        BaseChickenAttackSO selectedAttack = null;
        int attempts = 0;
        int maxAttempts = 10; // Prevent infinite loop

        while (selectedAttack == null && attempts < maxAttempts)
        {
            var potentialAttack = attackLootTable.SelectRandomAttack();
            if (potentialAttack != null && potentialAttack.CanExecute(availableChickens, this))
            {
                selectedAttack = potentialAttack;
            }
            attempts++;
        }

        if (selectedAttack != null)
        {
            currentAttack = selectedAttack;
            currentAttackUses = 0;
            currentState = CombatState.Attacking;
            nextAttackTime = Time.time; // Attack immediately
            
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Selected attack '{currentAttack.AttackName}' (Type: {currentAttack.AttackType})");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"ChickenCombatManagerV4: Could not find executable attack after {maxAttempts} attempts, starting cooldown");
            
            StartPatternCooldown();
        }
    }

    private void ExecuteCurrentAttack()
    {
        if (currentAttack == null) return;

        var availableChickens = GetAvailableAttackersInternal();
        if (availableChickens.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: No available attackers for execution");
            return;
        }

        // Check if attack can still be executed
        if (!currentAttack.CanExecute(availableChickens, this))
        {
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Attack '{currentAttack.AttackName}' can no longer be executed");
            
            StartPatternCooldown();
            return;
        }

        // Execute the attack
        currentAttack.Execute(availableChickens, this);
        currentAttackUses++;
        
        // Set next attack time
        nextAttackTime = Time.time + currentAttack.AttackInterval;
        
        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: Executed '{currentAttack.AttackName}' (Use {currentAttackUses}/{currentAttack.UsesBeforePatternChange})");
    }

    private void ResetCombatState()
    {
        currentState = CombatState.WaitingForChickens;
        currentAttack = null;
        currentAttackUses = 0;
        stateTimer = 0f;
        nextAttackTime = 0f;
        
        if (showDebugLogs)
            Debug.Log("ChickenCombatManagerV4: Combat state reset to WaitingForChickens");
    }

    List<ChickenCombatBehaviorV2> GetAvailableAttackersInternal()
    {
        List<ChickenCombatBehaviorV2> available = new List<ChickenCombatBehaviorV2>();

        foreach (ChickenCombatBehaviorV2 chicken in registeredCombatChickens)
        {
            if (!chicken) continue;

            if (chicken.IsReadyToAttack)
            {
                available.Add(chicken);
            }
        }

        return available;
    }

    // ATTACK SELECTION METHODS
    public BaseChickenAttackSO SelectRandomAttack()
    {
        if (attackLootTable == null)
        {
            Debug.LogWarning("ChickenCombatManagerV4: No attack loot table assigned, cannot select attack");
            return null;
        }

        return attackLootTable.SelectRandomAttack();
    }

    public BaseChickenAttackSO SelectRandomAttackExcluding(AttackType excludeType)
    {
        if (attackLootTable == null)
        {
            Debug.LogWarning("ChickenCombatManagerV4: No attack loot table assigned, cannot select attack");
            return null;
        }

        return attackLootTable.SelectRandomAttackThatIsNot(excludeType);
    }

    // REGISTRATION SYSTEM
    public bool RegisterChickenForCombat(ChickenCombatBehaviorV2 chicken)
    {
        if (chicken == null)
        {
            if (showRegistrationLogs)
                Debug.LogWarning("ChickenCombatManagerV4: Attempted to register null chicken for combat");
            return false;
        }

        if (registeredCombatChickens.Contains(chicken))
        {
            if (showRegistrationLogs)
                Debug.Log($"ChickenCombatManagerV4: Chicken {chicken.gameObject.name} already registered for combat");
            return false;
        }

        registeredCombatChickens.Add(chicken);

        if (showRegistrationLogs)
            Debug.Log($"ChickenCombatManagerV4: Registered chicken {chicken.gameObject.name} for combat ({registeredCombatChickens.Count} total)");

        // Check if this is the first chicken and we should start combat
        if (currentState == CombatState.WaitingForChickens && TotalCombatChickens == 1)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: First chicken registered, starting combat system");
        }

        return true;
    }

    public bool UnregisterChickenFromCombat(ChickenCombatBehaviorV2 chicken)
    {
        if (chicken == null) return false;

        bool wasRegistered = registeredCombatChickens.Remove(chicken);

        if (wasRegistered && showRegistrationLogs)
            Debug.Log($"ChickenCombatManagerV4: Unregistered chicken {chicken.gameObject.name} from combat ({registeredCombatChickens.Count} total)");

        // If no chickens left, reset combat state
        if (TotalCombatChickens == 0 && currentState != CombatState.WaitingForChickens)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: No chickens left, resetting combat state");
            
            ResetCombatState();
        }

        return wasRegistered;
    }

    // Keep this method for backwards compatibility but mark it as obsolete
    [System.Obsolete("Use RegisterChickenForCombat instead. This method is kept for backwards compatibility.")]
    public void RegisterChicken(ChickenCombatBehaviorV2 chicken)
    {
        RegisterChickenForCombat(chicken);
    }

    // Keep this method for backwards compatibility but mark it as obsolete  
    [System.Obsolete("Use UnregisterChickenFromCombat instead. This method is kept for backwards compatibility.")]
    public void UnregisterChicken(ChickenCombatBehaviorV2 chicken)
    {
        UnregisterChickenFromCombat(chicken);
    }

    // Context Menu Methods
    [ContextMenu("Toggle Combat")]
    void ContextMenuToggleCombat()
    {
        enableCombat = !enableCombat;
        Debug.Log($"ChickenCombatManagerV4: Combat {(enableCombat ? "enabled" : "disabled")}");
        
        if (!enableCombat)
        {
            ResetCombatState();
        }
    }

    [ContextMenu("Force Pattern Change")]
    void ContextMenuForcePatternChange()
    {
        if (currentState == CombatState.Attacking)
        {
            Debug.Log("ChickenCombatManagerV4: Forcing pattern change");
            currentAttack = null;
            currentAttackUses = 0;
            StartPatternCooldown();
        }
        else
        {
            Debug.Log($"ChickenCombatManagerV4: Cannot force pattern change in state: {currentState}");
        }
    }

    [ContextMenu("Print Combat Status")]
    void ContextMenuPrintStatus()
    {
        Debug.Log("=== CHICKEN COMBAT MANAGER V4 STATUS ===");
        Debug.Log($"Combat Enabled: {enableCombat}");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"State Timer: {stateTimer:F2}s");
        Debug.Log($"Current Attack: {(currentAttack != null ? currentAttack.AttackName : "None")}");
        Debug.Log($"Attack Uses: {currentAttackUses}");
        Debug.Log($"Next Attack Time: {(nextAttackTime > Time.time ? (nextAttackTime - Time.time).ToString("F2") + "s" : "Ready")}");
        Debug.Log($"Total Combat Chickens: {TotalCombatChickens}");
        Debug.Log($"Available Attackers: {AvailableAttackers}");
        Debug.Log($"Player Found: {(player != null ? "Yes" : "No")}");
        Debug.Log($"Attack Loot Table: {(attackLootTable != null ? attackLootTable.name : "None")}");

        if (attackLootTable != null)
        {
            var validAttacks = attackLootTable.GetValidAttacks();
            Debug.Log($"Valid Attacks in Loot Table: {validAttacks.Count}");
        }

        if (registeredCombatChickens.Count > 0)
        {
            Debug.Log("\nREGISTERED COMBAT CHICKENS:");
            foreach (var chicken in registeredCombatChickens)
            {
                if (chicken != null)
                {
                    var stateController = chicken.GetComponent<ChickenStateController>();
                    var state = stateController?.CurrentState.ToString() ?? "Unknown";
                    Debug.Log($"  {chicken.gameObject.name}: State={state}, CanAttack={chicken.IsReadyToAttack}");
                }
                else
                {
                    Debug.Log($"  NULL CHICKEN FOUND!");
                }
            }
        }
    }

    [ContextMenu("Clean Up Null References")]
    void ContextMenuCleanupNullRefs()
    {
        int originalCount = registeredCombatChickens.Count;
        registeredCombatChickens.RemoveAll(chicken => chicken == null);
        int removedCount = originalCount - registeredCombatChickens.Count;

        if (removedCount > 0)
        {
            Debug.Log($"ChickenCombatManagerV4: Cleaned up {removedCount} null chicken references");
            
            // Reset state if no chickens left
            if (TotalCombatChickens == 0)
            {
                ResetCombatState();
            }
        }
        else
        {
            Debug.Log("ChickenCombatManagerV4: No null references found");
        }
    }

    [ContextMenu("Test Attack Selection")]
    void ContextMenuTestAttackSelection()
    {
        if (attackLootTable == null)
        {
            Debug.LogWarning("ChickenCombatManagerV4: No attack loot table assigned for testing");
            return;
        }

        Debug.Log("=== TESTING ATTACK SELECTION ===");
        for (int i = 0; i < 5; i++)
        {
            var selectedAttack = SelectRandomAttack();
            if (selectedAttack != null)
            {
                Debug.Log($"Test {i + 1}: Selected attack '{selectedAttack.name}' (Type: {selectedAttack.AttackType})");
            }
            else
            {
                Debug.Log($"Test {i + 1}: No attack selected");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showAttackGizmos) return;

        // Show combat state
        Gizmos.color = GetStateColor();
        if (transform.position != Vector3.zero)
        {
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);
        }

        if (registeredCombatChickens.Count > 0)
        {
            foreach (var chicken in registeredCombatChickens)
            {
                if (chicken != null)
                {
                    // Show chicken readiness
                    Gizmos.color = chicken.IsReadyToAttack ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(chicken.transform.position + Vector3.up * 2f, 0.5f);
                }
            }
        }
    }

    private Color GetStateColor()
    {
        switch (currentState)
        {
            case CombatState.WaitingForChickens: return Color.yellow;
            case CombatState.PatternCooldown: return Color.blue;
            case CombatState.Attacking: return Color.red;
            default: return Color.white;
        }
    }
}