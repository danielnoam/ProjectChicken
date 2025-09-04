using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class AttackEntry
{
    public BaseChickenAttackSO attackAsset;
    [Range(0f, 100f)] public float chancePercentage = 10f;
}



public class ChickenCombatManagerV4 : MonoBehaviour
{
    [Header("Attack Configuration")]
    public AttackLootTableSO attackLootTable; // Single loot table reference

    [Header("General Combat Settings")]
    public bool enableCombat = true;
    public float eggSpeed = 10f;
    public float attackPatternChangeCooldown = 8f; // Cooldown duration when switching attack patterns

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showAttackGizmos = false;
    public bool showSelectionLogs = false;
    public bool showPatternChangeLogs = true;

    // Timing management for each attack type
    [System.Serializable]
    public class AttackTiming
    {
        public BaseChickenAttackSO attackAsset;
        public float lastAttackTime = 0f;
        
        public bool IsReady => Time.time - lastAttackTime >= attackAsset.AttackInterval;
        public float TimeUntilReady => Mathf.Max(0f, attackAsset.AttackInterval - (Time.time - lastAttackTime));
        
        public void MarkAttackExecuted()
        {
            lastAttackTime = Time.time;
        }
        
        public void ResetCooldown()
        {
            lastAttackTime = 0f;
        }
    }

    // Private fields
    private List<ChickenCombatBehaviorV2> allCombatChickens = new List<ChickenCombatBehaviorV2>();
    private Dictionary<BaseChickenAttackSO, AttackTiming> attackTimings = new Dictionary<BaseChickenAttackSO, AttackTiming>();
    
    // Pattern change tracking - NEW SYSTEM
    private AttackType currentAttackType = AttackType.None;
    private int currentAttackTypeUseCount = 0;
    private bool isInPatternChangeCooldown = false;
    private float patternChangeCooldownStartTime = 0f;
    private BaseChickenAttackSO nextAttackAfterCooldown = null; // The attack to use after cooldown
    
    private Transform player;

    // Public properties
    public int TotalCombatChickens => allCombatChickens.Count;
    public List<ChickenCombatBehaviorV2> GetAvailableAttackers() => GetAvailableAttackersInternal();
    public int AvailableAttackers => GetAvailableAttackersInternal().Count;
    public AttackType CurrentAttackType => currentAttackType;
    public int CurrentAttackTypeUseCount => currentAttackTypeUseCount;
    public bool IsInPatternChangeCooldown => isInPatternChangeCooldown;
    public float PatternChangeCooldownTimeRemaining => isInPatternChangeCooldown ? 
        Mathf.Max(0f, attackPatternChangeCooldown - (Time.time - patternChangeCooldownStartTime)) : 0f;
    public float EggSpeed => eggSpeed;
    public Transform Player => player;

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

        // Validate loot table
        if (attackLootTable == null)
        {
            Debug.LogError("ChickenCombatManagerV4: No Attack Loot Table assigned! Combat will not work.");
            return;
        }

        // Initialize attack timings from loot table
        InitializeAttackTimings();

        // Find all combat chickens
        RefreshCombatChickens();
        LogLootTableStatus();
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

        if (attackLootTable == null)
        {
            if (showDebugLogs && Time.frameCount % 300 == 0)
                Debug.Log("ChickenCombatManagerV4: No loot table assigned, cannot attack");
            return;
        }

        // Check if we're in pattern change cooldown
        if (isInPatternChangeCooldown)
        {
            UpdatePatternChangeCooldown();
            return; // NO ATTACKS during cooldown
        }

        // Get ready attacks (this includes individual attack cooldowns)
        var readyAttacks = GetReadyAttacksFromLootTable();
        
        if (readyAttacks.Count == 0)
        {
            if (showSelectionLogs && Time.frameCount % 180 == 0)
                Debug.Log("ChickenCombatManagerV4: No attacks ready for execution");
            return;
        }

        // Select attack based on current pattern or initial selection
        var selectedAttack = SelectAttackFromLootTable(readyAttacks);
        
        if (selectedAttack == null)
        {
            if (showSelectionLogs)
                Debug.Log("ChickenCombatManagerV4: No attack selected from loot table");
            return;
        }

        // Execute the attack
        ExecuteAttack(selectedAttack);
    }

    void UpdatePatternChangeCooldown()
    {
        float cooldownTimeRemaining = PatternChangeCooldownTimeRemaining;
        
        if (cooldownTimeRemaining <= 0f)
        {
            // Cooldown finished, switch to the pre-selected new attack type
            CompletePatternChange();
        }
        else
        {
            // Still in cooldown - show periodic debug message
            if (showPatternChangeLogs && Time.frameCount % 180 == 0)
            {
                Debug.Log($"ChickenCombatManagerV4: Pattern change cooldown - {cooldownTimeRemaining:F1}s remaining, switching to {nextAttackAfterCooldown?.AttackName ?? "Unknown"}");
            }
        }
    }

    void CompletePatternChange()
    {
        // Exit cooldown state
        isInPatternChangeCooldown = false;
        
        if (nextAttackAfterCooldown != null)
        {
            currentAttackType = nextAttackAfterCooldown.AttackType;
            currentAttackTypeUseCount = 0;
            
            if (showPatternChangeLogs)
                Debug.Log($"ChickenCombatManagerV4: PATTERN CHANGE COMPLETED - New attack type: {currentAttackType}");
        }
        else
        {
            // Fallback
            currentAttackType = AttackType.None;
            currentAttackTypeUseCount = 0;
            
            if (showPatternChangeLogs)
                Debug.LogWarning("ChickenCombatManagerV4: No next attack selected - resetting to None");
        }
        
        nextAttackAfterCooldown = null;
    }

    void InitializeAttackTimings()
    {
        attackTimings.Clear();
        
        if (attackLootTable == null) return;
        
        var validAttacks = attackLootTable.GetValidAttacks();
        
        foreach (var attackAsset in validAttacks)
        {
            if (attackAsset != null)
            {
                var timing = new AttackTiming
                {
                    attackAsset = attackAsset,
                    lastAttackTime = 0f
                };
                
                attackTimings[attackAsset] = timing;
            }
        }
    }

    List<BaseChickenAttackSO> GetReadyAttacksFromLootTable()
    {
        if (attackLootTable == null) return new List<BaseChickenAttackSO>();
        
        List<BaseChickenAttackSO> readyAttacks = new List<BaseChickenAttackSO>();
        var validAttacks = attackLootTable.GetValidAttacks();
        
        foreach (var attackAsset in validAttacks)
        {
            if (attackAsset != null && attackTimings.ContainsKey(attackAsset))
            {
                var timing = attackTimings[attackAsset];
                
                // Check individual attack cooldown first - THIS IS CRITICAL
                if (!timing.IsReady)
                {
                    if (showSelectionLogs)
                        Debug.Log($"ChickenCombatManagerV4: {attackAsset.AttackName} not ready - individual cooldown: {timing.TimeUntilReady:F1}s remaining");
                    continue;
                }

                // Check if attack can execute (chicken requirements, etc.)
                var availableChickens = GetAvailableAttackersInternal();
                if (attackAsset.CanExecute(availableChickens, this))
                {
                    readyAttacks.Add(attackAsset);
                    
                    if (showSelectionLogs)
                        Debug.Log($"ChickenCombatManagerV4: {attackAsset.AttackName} is READY");
                }
                else
                {
                    if (showSelectionLogs)
                        Debug.Log($"ChickenCombatManagerV4: {attackAsset.AttackName} ready but cannot execute - chicken requirements not met");
                }
            }
        }
        
        return readyAttacks;
    }

    BaseChickenAttackSO SelectAttackFromLootTable(List<BaseChickenAttackSO> readyAttacks)
    {
        if (readyAttacks.Count == 0) return null;
        
        // If we have a current attack type, only use attacks of that type
        if (currentAttackType != AttackType.None)
        {
            var currentTypeAttacks = readyAttacks.Where(x => x.AttackType == currentAttackType).ToList();
            if (currentTypeAttacks.Count > 0)
            {
                // Continue with current attack type - select from current type only
                return SelectFromSpecificAttacks(currentTypeAttacks);
            }
            else
            {
                // Current attack type not available, but we haven't reached use limit yet
                // This means we should wait rather than switch types
                if (showSelectionLogs)
                    Debug.Log($"ChickenCombatManagerV4: Current attack type {currentAttackType} not available, waiting...");
                return null;
            }
        }
        else
        {
            // No current attack type, select any attack (initial selection)
            return SelectFromSpecificAttacks(readyAttacks);
        }
    }


    
    BaseChickenAttackSO SelectFromSpecificAttacks(List<BaseChickenAttackSO> specificAttacks)
    {
        if (specificAttacks.Count == 0) return null;
        if (specificAttacks.Count == 1) return specificAttacks[0];
        
        // Create a temporary loot table with only specified attacks
        var tempEntries = new List<AttackEntry>();
        
        foreach (var attack in specificAttacks)
        {
            float originalChance = attackLootTable.GetAttackChance(attack);
            if (originalChance > 0f)
            {
                tempEntries.Add(new AttackEntry
                {
                    attackAsset = attack,
                    chancePercentage = originalChance
                });
            }
        }
        
        if (tempEntries.Count == 0) return null;
        if (tempEntries.Count == 1) return tempEntries[0].attackAsset;
        
        // Normalize percentages to sum to 100% for available attacks only
        float totalPercentage = tempEntries.Sum(x => x.chancePercentage);
        if (totalPercentage <= 0f) return specificAttacks[0]; // Fallback
        
        // Generate random value between 0 and total percentage
        float randomValue = Random.Range(0f, totalPercentage);
        
        if (showSelectionLogs)
            Debug.Log($"ChickenCombatManagerV4: Attack selection - Random roll: {randomValue:F2} out of {totalPercentage:F2}");
        
        // Find which attack this random value corresponds to
        float cumulativePercentage = 0f;
        foreach (var entry in tempEntries)
        {
            cumulativePercentage += entry.chancePercentage;
            
            if (showSelectionLogs)
                Debug.Log($"  {entry.attackAsset.AttackName}: {entry.chancePercentage:F1}%, Cumulative: {cumulativePercentage:F1}%");
            
            if (randomValue <= cumulativePercentage)
            {
                if (showSelectionLogs)
                    Debug.Log($"  SELECTED: {entry.attackAsset.AttackName}!");
                    
                return entry.attackAsset;
            }
        }
        
        // Fallback
        return tempEntries[tempEntries.Count - 1].attackAsset;
    }

    void ExecuteAttack(BaseChickenAttackSO selectedAttack)
    {
        var availableChickens = GetAvailableAttackersInternal();
        
        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: EXECUTING {selectedAttack.AttackName}!");

        // Execute the attack via ScriptableObject
        selectedAttack.Execute(availableChickens, this);

        // Update individual attack timing - CRITICAL FOR PREVENTING SPAM
        if (attackTimings.ContainsKey(selectedAttack))
        {
            attackTimings[selectedAttack].MarkAttackExecuted();
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: {selectedAttack.AttackName} cooldown started - next available in {selectedAttack.AttackInterval}s");
        }

        // Update attack type usage tracking
        UpdateAttackTypeUsage(selectedAttack);
    }

    void UpdateAttackTypeUsage(BaseChickenAttackSO selectedAttack)
    {
        AttackType attackType = selectedAttack.AttackType;
        
        // If this is a different attack type than current, reset the count
        if (currentAttackType != attackType)
        {
            currentAttackType = attackType;
            currentAttackTypeUseCount = 1;
            
            if (showPatternChangeLogs)
                Debug.Log($"ChickenCombatManagerV4: Started new attack pattern: {attackType} (Use 1)");
        }
        else
        {
            // Same attack type, increment use count
            currentAttackTypeUseCount++;
            
            if (showPatternChangeLogs)
                Debug.Log($"ChickenCombatManagerV4: Continued attack pattern: {attackType} (Use {currentAttackTypeUseCount})");
        }
        
        // Check if we've reached the use limit for this attack type
        int usesBeforeChange = selectedAttack.UsesBeforePatternChange;
        
        if (currentAttackTypeUseCount >= usesBeforeChange)
        {
            StartPatternChangeCooldown(attackType, usesBeforeChange);
        }
        else
        {
            if (showPatternChangeLogs)
            {
                int usesRemaining = usesBeforeChange - currentAttackTypeUseCount;
                Debug.Log($"ChickenCombatManagerV4: {usesRemaining} more uses of {attackType} before pattern change");
            }
        }
    }

    void StartPatternChangeCooldown(AttackType completedAttackType, int totalUses)
    {
        isInPatternChangeCooldown = true;
        patternChangeCooldownStartTime = Time.time;
        
        // Pre-select the next attack type (different from current)
        nextAttackAfterCooldown = attackLootTable.SelectRandomAttackThatIsNot(completedAttackType);
        
        if (showPatternChangeLogs)
        {
            Debug.Log($"ChickenCombatManagerV4: PATTERN CHANGE TRIGGERED!");
            Debug.Log($"  Completed {totalUses} uses of {completedAttackType}");
            Debug.Log($"  Starting {attackPatternChangeCooldown}s cooldown");
            Debug.Log($"  Next attack type will be: {nextAttackAfterCooldown?.AttackType.ToString() ?? "None"}");
        }
    }

    List<ChickenCombatBehaviorV2> GetAvailableAttackersInternal()
    {
        List<ChickenCombatBehaviorV2> available = new List<ChickenCombatBehaviorV2>();


        foreach (ChickenCombatBehaviorV2 chicken in allCombatChickens)
        {
            if (!chicken) continue;

            if (chicken.IsReadyToAttack)
            {
                available.Add(chicken);
            }
        }

        return available;
    }

    void RefreshCombatChickens()
    {
        allCombatChickens.Clear();
        ChickenCombatBehaviorV2[] foundChickens = FindObjectsOfType<ChickenCombatBehaviorV2>();
        allCombatChickens.AddRange(foundChickens);

        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: Found {allCombatChickens.Count} combat chickens");
    }

    void LogLootTableStatus()
    {
        if (!showDebugLogs || attackLootTable == null) return;
            
        Debug.Log($"=== ATTACK LOOT TABLE STATUS: {attackLootTable.name} ===");
        var validAttacks = attackLootTable.GetValidAttacks();
        
        foreach (var attack in validAttacks)
        {
            if (attackTimings.ContainsKey(attack))
            {
                var timing = attackTimings[attack];
                float chance = attackLootTable.GetAttackChance(attack);
                int usesBeforeChange = attack.UsesBeforePatternChange;
                string readyStatus = timing.IsReady ? "READY" : $"Not Ready ({timing.TimeUntilReady:F1}s)";
                
                Debug.Log($"  {attack.AttackName}: {chance:F1}% chance, {usesBeforeChange} uses before change - {readyStatus}");
            }
        }
    }

    // Public management methods
    public void RegisterChicken(ChickenCombatBehaviorV2 chicken)
    {
        if (chicken != null && !allCombatChickens.Contains(chicken))
        {
            allCombatChickens.Add(chicken);
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Registered chicken {chicken.gameObject.name}");
        }
    }

    public void UnregisterChicken(ChickenCombatBehaviorV2 chicken)
    {
        if (allCombatChickens.Contains(chicken))
        {
            allCombatChickens.Remove(chicken);
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Unregistered chicken {chicken.gameObject.name}");
        }
    }

    public void SetLootTable(AttackLootTableSO newLootTable)
    {
        attackLootTable = newLootTable;
        InitializeAttackTimings();
        
        // Reset pattern tracking when changing loot table
        ResetPatternTracking();
        
        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: Set new loot table: {newLootTable?.name ?? "None"}");
    }

    public void ResetAllCooldowns()
    {
        foreach (var timing in attackTimings.Values)
        {
            timing.ResetCooldown();
        }
        
        ResetPatternTracking();
        
        if (showDebugLogs)
            Debug.Log("ChickenCombatManagerV4: All cooldowns and pattern tracking reset");
    }

    public void ResetPatternTracking()
    {
        currentAttackType = AttackType.None;
        currentAttackTypeUseCount = 0;
        isInPatternChangeCooldown = false;
        patternChangeCooldownStartTime = 0f;
        nextAttackAfterCooldown = null;
        
        if (showPatternChangeLogs)
            Debug.Log("ChickenCombatManagerV4: Pattern tracking reset");
    }

    public void ResetAttackCooldown(BaseChickenAttackSO attackAsset)
    {
        if (attackTimings.ContainsKey(attackAsset))
        {
            attackTimings[attackAsset].ResetCooldown();
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Reset cooldown for {attackAsset.AttackName}");
        }
    }

    public void ForceAttackNow(BaseChickenAttackSO attackAsset)
    {
        if (attackAsset == null) return;
        
        var availableChickens = GetAvailableAttackersInternal();
        if (attackAsset.CanExecute(availableChickens, this))
        {
            ExecuteAttack(attackAsset);
            Debug.Log($"ChickenCombatManagerV4: Forced execution of {attackAsset.AttackName}");
        }
        else
        {
            Debug.LogWarning($"ChickenCombatManagerV4: Cannot force execute {attackAsset.AttackName} - requirements not met");
        }
    }

    public void ForcePatternChangeNow()
    {
        if (isInPatternChangeCooldown)
        {
            if (showPatternChangeLogs)
                Debug.Log("ChickenCombatManagerV4: Already in pattern change cooldown");
            return;
        }
        
        StartPatternChangeCooldown(currentAttackType, currentAttackTypeUseCount);
        
        if (showPatternChangeLogs)
            Debug.Log("ChickenCombatManagerV4: Forced pattern change initiated");
    }

    // Context Menu Methods
    [ContextMenu("Refresh Combat Chickens")]
    void ContextMenuRefreshChickens()
    {
        RefreshCombatChickens();
    }

    [ContextMenu("Refresh Attack Timings")]
    void ContextMenuRefreshTimings()
    {
        InitializeAttackTimings();
        LogLootTableStatus();
    }

    [ContextMenu("Reset All Cooldowns")]
    void ContextMenuResetAllCooldowns()
    {
        ResetAllCooldowns();
    }

    [ContextMenu("Reset Pattern Tracking")]
    void ContextMenuResetPatternTracking()
    {
        ResetPatternTracking();
    }

    [ContextMenu("Force Pattern Change")]
    void ContextMenuForcePatternChange()
    {
        ForcePatternChangeNow();
    }

    [ContextMenu("Toggle Combat")]
    void ContextMenuToggleCombat()
    {
        enableCombat = !enableCombat;
        Debug.Log($"ChickenCombatManagerV4: Combat {(enableCombat ? "enabled" : "disabled")}");
    }

    [ContextMenu("Test Loot Table Selection (10 times)")]
    void ContextMenuTestLootTable()
    {
        if (attackLootTable == null)
        {
            Debug.LogError("ChickenCombatManagerV4: No loot table to test!");
            return;
        }

        Debug.Log("=== TESTING LOOT TABLE SELECTION ===");
        Dictionary<string, int> results = new Dictionary<string, int>();
        
        for (int i = 0; i < 10; i++)
        {
            var readyAttacks = GetReadyAttacksFromLootTable();
            var selected = SelectAttackFromLootTable(readyAttacks);
            string attackName = selected?.AttackName ?? "None";
            
            if (!results.ContainsKey(attackName))
                results[attackName] = 0;
            results[attackName]++;
        }
        
        Debug.Log("Results from 10 selections:");
        foreach (var kvp in results)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} times ({kvp.Value * 10f}%)");
        }
    }

    [ContextMenu("Print Combat Status")]
    void ContextMenuPrintStatus()
    {
        Debug.Log("=== CHICKEN COMBAT MANAGER V4 STATUS ===");
        Debug.Log($"Combat Enabled: {enableCombat}");
        Debug.Log($"Total Combat Chickens: {TotalCombatChickens}");
        Debug.Log($"Available Attackers: {AvailableAttackers}");
        Debug.Log($"Pattern Change Cooldown Duration: {attackPatternChangeCooldown}s");
        Debug.Log($"Current Attack Type: {currentAttackType}");
        Debug.Log($"Current Attack Type Uses: {currentAttackTypeUseCount}");
        Debug.Log($"In Pattern Change Cooldown: {isInPatternChangeCooldown}");
        if (isInPatternChangeCooldown)
        {
            Debug.Log($"Pattern Change Cooldown Remaining: {PatternChangeCooldownTimeRemaining:F1}s");
            Debug.Log($"Next Attack Type: {nextAttackAfterCooldown?.AttackType.ToString() ?? "None"}");
        }
        Debug.Log($"Player Found: {(player != null ? "Yes" : "No")}");
        Debug.Log($"Loot Table: {(attackLootTable != null ? attackLootTable.name : "NONE")}");

        LogLootTableStatus();

        if (allCombatChickens.Count > 0)
        {
            Debug.Log("\nCHICKEN DETAILS:");
            foreach (var chicken in allCombatChickens)
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

    void OnDrawGizmos()
    {
        if (!showAttackGizmos) return;

        if (allCombatChickens.Count > 0)
        {
            foreach (var chicken in allCombatChickens)
            {
                if (chicken != null)
                {
                    // Show chicken readiness
                    Gizmos.color = chicken.IsReadyToAttack ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(chicken.transform.position + Vector3.up * 2f, 0.5f);
                }
            }
        }
        
        // Show pattern change cooldown status
        if (isInPatternChangeCooldown)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + Vector3.up * 3f;
            Gizmos.DrawWireCube(center, Vector3.one);
        }
    }
}