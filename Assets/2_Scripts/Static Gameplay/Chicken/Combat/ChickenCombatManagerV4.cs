using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ChickenCombatManagerV4 : MonoBehaviour
{
    [Header("Attack Configuration")]
    public AttackLootTableSO attackLootTable; // Single loot table reference

    [Header("General Combat Settings")]
    public bool enableCombat = true;
    public float eggSpeed = 10f;
    public float attackPatternChangeCooldown = 3f;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showAttackGizmos = false;
    public bool showSelectionLogs = false;

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
    private float lastAttackPatternChangeTime = 0f;
    private AttackType lastAttackType = AttackType.None;
    private Transform player;

    // Public properties
    public int TotalCombatChickens => allCombatChickens.Count;
    public List<ChickenCombatBehaviorV2> GetAvailableAttackers() => GetAvailableAttackersInternal();
    public int AvailableAttackers => GetAvailableAttackersInternal().Count;
    public float NextPatternChangeTime => lastAttackPatternChangeTime + attackPatternChangeCooldown;
    public AttackType LastAttackType => lastAttackType;
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

        // Get ready attacks from loot table (this now includes pattern change cooldown check)
        var readyAttacks = GetReadyAttacksFromLootTable();
        
        if (readyAttacks.Count == 0)
        {
            if (showSelectionLogs && Time.frameCount % 180 == 0) // Every 3 seconds at 60fps
                Debug.Log("ChickenCombatManagerV4: No attacks ready for execution");
            return;
        }

        // Use loot table to select attack with proper percentage chances
        var selectedAttack = SelectAttackFromLootTable(readyAttacks);
        
        if (selectedAttack == null)
        {
            if (showSelectionLogs)
                Debug.Log("ChickenCombatManagerV4: No attack selected from loot table");
            return;
        }

        // At this point, we know the attack can execute (including pattern change cooldown)
        // Determine if this is a pattern change BEFORE executing
        bool isPatternChange = lastAttackType != AttackType.None && 
                              lastAttackType != selectedAttack.AttackType;

        // Execute the attack
        ExecuteAttack(selectedAttack, isPatternChange);
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
                
                // Check individual attack cooldown first
                if (!timing.IsReady)
                {
                    if (showSelectionLogs)
                        Debug.Log($"ChickenCombatManagerV4: {attackAsset.AttackName} not ready - individual cooldown: {timing.TimeUntilReady:F1}s remaining");
                    continue;
                }

                // Check if this would be a pattern change and if pattern change cooldown is ready
                bool wouldBePatternChange = lastAttackType != AttackType.None && 
                                          lastAttackType != attackAsset.AttackType;
                
                if (wouldBePatternChange)
                {
                    float timeSincePatternChange = Time.time - lastAttackPatternChangeTime;
                    bool patternChangeCooldownReady = timeSincePatternChange >= attackPatternChangeCooldown;
                    
                    if (!patternChangeCooldownReady)
                    {
                        if (showSelectionLogs)
                            Debug.Log($"ChickenCombatManagerV4: {attackAsset.AttackName} blocked - pattern change cooldown: {(attackPatternChangeCooldown - timeSincePatternChange):F1}s remaining");
                        continue;
                    }
                }

                // Check if attack can execute (chicken requirements, etc.)
                var availableChickens = GetAvailableAttackersInternal();
                if (attackAsset.CanExecute(availableChickens, this))
                {
                    readyAttacks.Add(attackAsset);
                    
                    if (showSelectionLogs)
                    {
                        string changeNote = wouldBePatternChange ? $" (PATTERN CHANGE from {lastAttackType})" : "";
                        Debug.Log($"ChickenCombatManagerV4: {attackAsset.AttackName} is READY{changeNote}");
                    }
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
        
        // Create a temporary loot table with only ready attacks
        var tempEntries = new List<AttackLootTableSO.AttackEntry>();
        
        foreach (var readyAttack in readyAttacks)
        {
            float originalChance = attackLootTable.GetAttackChance(readyAttack);
            if (originalChance > 0f)
            {
                tempEntries.Add(new AttackLootTableSO.AttackEntry
                {
                    attackAsset = readyAttack,
                    chancePercentage = originalChance
                });
            }
        }
        
        if (tempEntries.Count == 0) return null;
        if (tempEntries.Count == 1) return tempEntries[0].attackAsset;
        
        // Normalize percentages to sum to 100% for ready attacks only
        float totalPercentage = tempEntries.Sum(x => x.chancePercentage);
        if (totalPercentage <= 0f) return readyAttacks[0]; // Fallback
        
        // Scale to 100%
        foreach (var entry in tempEntries)
        {
            entry.chancePercentage = (entry.chancePercentage / totalPercentage) * 100f;
        }
        
        // Select using normalized percentages
        float randomValue = Random.Range(0f, 100f);
        
        if (showSelectionLogs)
            Debug.Log($"ChickenCombatManagerV4: Loot table selection - Random roll: {randomValue:F2}% from {tempEntries.Count} ready attacks");
        
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

    void ExecuteAttack(BaseChickenAttackSO selectedAttack, bool isPatternChange)
    {
        var availableChickens = GetAvailableAttackersInternal();
        
        // Update pattern change tracking BEFORE execution if it's a pattern change
        if (isPatternChange)
        {
            lastAttackPatternChangeTime = Time.time;
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: PATTERN CHANGE INITIATED - From {lastAttackType} to {selectedAttack.AttackType}");
        }
        
        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: EXECUTING {selectedAttack.AttackName}!");

        // Execute the attack via ScriptableObject
        selectedAttack.Execute(availableChickens, this);

        // Update individual attack timing
        if (attackTimings.ContainsKey(selectedAttack))
        {
            attackTimings[selectedAttack].MarkAttackExecuted();
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: {selectedAttack.AttackName} cooldown started - next available in {selectedAttack.AttackInterval}s");
        }

        // Update last attack type
        lastAttackType = selectedAttack.AttackType;
        
        if (showDebugLogs && isPatternChange)
            Debug.Log($"ChickenCombatManagerV4: PATTERN CHANGE COMPLETED - Now on {selectedAttack.AttackType}, next pattern change available in {attackPatternChangeCooldown}s");
    }

    List<ChickenCombatBehaviorV2> GetAvailableAttackersInternal()
    {
        List<ChickenCombatBehaviorV2> available = new List<ChickenCombatBehaviorV2>();

        foreach (ChickenCombatBehaviorV2 chicken in allCombatChickens)
        {
            if (chicken == null) continue;

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
                string readyStatus = timing.IsReady ? "READY" : $"Not Ready ({timing.TimeUntilReady:F1}s)";
                
                // Check pattern change status
                bool wouldBePatternChange = lastAttackType != AttackType.None && 
                                          lastAttackType != attack.AttackType;
                string patternStatus = "";
                if (wouldBePatternChange)
                {
                    float timeSincePatternChange = Time.time - lastAttackPatternChangeTime;
                    bool patternChangeCooldownReady = timeSincePatternChange >= attackPatternChangeCooldown;
                    patternStatus = patternChangeCooldownReady ? " + Pattern Change Ready" : $" + Pattern Change Blocked ({(attackPatternChangeCooldown - timeSincePatternChange):F1}s)";
                }
                
                Debug.Log($"  {attack.AttackName}: {chance:F1}% chance - {readyStatus}{patternStatus}");
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
        
        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: Set new loot table: {newLootTable?.name ?? "None"}");
    }

    public void ResetAllCooldowns()
    {
        foreach (var timing in attackTimings.Values)
        {
            timing.ResetCooldown();
        }
        lastAttackPatternChangeTime = 0f;
        
        if (showDebugLogs)
            Debug.Log("ChickenCombatManagerV4: All cooldowns reset");
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
            bool isPatternChange = lastAttackType != AttackType.None && 
                                  lastAttackType != attackAsset.AttackType;
            ExecuteAttack(attackAsset, isPatternChange);
            Debug.Log($"ChickenCombatManagerV4: Forced execution of {attackAsset.AttackName}");
        }
        else
        {
            Debug.LogWarning($"ChickenCombatManagerV4: Cannot force execute {attackAsset.AttackName} - requirements not met");
        }
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
        Debug.Log($"Attack Pattern Change Cooldown: {attackPatternChangeCooldown}s");
        Debug.Log($"Last Attack Type: {lastAttackType}");
        Debug.Log($"Next Pattern Change Available In: {(NextPatternChangeTime - Time.time):F1}s");
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
                    Gizmos.color = chicken.IsReadyToAttack ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(chicken.transform.position + Vector3.up * 2f, 0.5f);
                }
            }
        }
    }
}