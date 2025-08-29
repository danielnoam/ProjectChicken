using UnityEngine;
using System.Collections.Generic;

public class ChickenCombatManagerV2 : MonoBehaviour
{
    [Header("Burst Attack Settings")]
    public float burstAttackInterval = 3f; // How often to trigger burst attacks
    public int maxSimultaneousAttacks = 2; // Max chickens that can attack at once in burst mode

    [Header("Single Fire Attack Settings")]
    public float singleFireInterval = 1f; // How often a single chicken fires (lower cooldown)

    [Header("Attack Type Selection")]
    [Range(1, 100)]
    public int burstFireChance = 30; // If random 1-100 falls within this range, use burst fire

    [Header("General Combat Settings")]
    public bool enableCombat = true;
    public float eggSpeed = 10f; // Speed of all eggs shot by chickens
    public float attackPatternChangeCooldown = 3f; // Cooldown when switching between attack types

    [Header("Debug")]
    public bool showDebugLogs = true; // Enable by default to help troubleshoot
    public bool showAttackGizmos = false;

    public enum AttackType
    {
        None,
        BurstFire,
        SingleFire
    }

    private List<ChickenCombatBehaviorV2> allCombatChickens = new List<ChickenCombatBehaviorV2>();
    private float lastBurstAttackTime = 0f;
    private float lastSingleFireTime = 0f;
    private float lastAttackPatternChangeTime = 0f;
    private AttackType lastAttackType = AttackType.None;
    private Transform player;

    void Start()
    {
        Debug.Log("ChickenCombatManagerV2: Starting up...");

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"ChickenCombatManagerV2: Found player: {player.name}");
        }
        else
        {
            Debug.LogWarning("ChickenCombatManagerV2: No player found with 'Player' tag!");
        }

        // Find all combat chickens in the scene
        RefreshCombatChickens();
        Debug.Log($"ChickenCombatManagerV2: Initialization complete. Combat enabled: {enableCombat}");
        Debug.Log($"ChickenCombatManagerV2: Burst fire chance: {burstFireChance}% (1-{burstFireChance})");
    }

    void Update()
    {
        if (!enableCombat)
        {
            if (showDebugLogs && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
                Debug.Log("ChickenCombatManagerV2: Combat is disabled");
            return;
        }

        if (player == null)
        {
            if (showDebugLogs && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
                Debug.Log("ChickenCombatManagerV2: No player found, cannot attack");
            return;
        }

        // Check for burst attack timing
        float timeSinceBurstAttack = Time.time - lastBurstAttackTime;
        bool burstReady = timeSinceBurstAttack >= burstAttackInterval;

        // Check for single fire timing
        float timeSinceSingleFire = Time.time - lastSingleFireTime;
        bool singleFireReady = timeSinceSingleFire >= singleFireInterval;

        // Only proceed if at least one attack type is ready
        if (!burstReady && !singleFireReady)
            return;

        // Choose attack type randomly
        int randomRoll = Random.Range(1, 101); // 1-100 inclusive
        AttackType selectedAttackType = randomRoll <= burstFireChance ? AttackType.BurstFire : AttackType.SingleFire;

        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV2: Attack decision - Roll: {randomRoll}, Burst chance: 1-{burstFireChance}, Selected: {selectedAttackType}");

        // Check if we're switching attack patterns
        bool isPatternChange = lastAttackType != AttackType.None && lastAttackType != selectedAttackType;
        float timeSincePatternChange = Time.time - lastAttackPatternChangeTime;
        bool patternChangeCooldownReady = timeSincePatternChange >= attackPatternChangeCooldown;

        if (isPatternChange && !patternChangeCooldownReady)
        {
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV2: Pattern change blocked - switching from {lastAttackType} to {selectedAttackType}, cooldown: {(attackPatternChangeCooldown - timeSincePatternChange):F1}s remaining");
            return;
        }

        // Execute the chosen attack type if it's ready
        bool attackExecuted = false;
        if (selectedAttackType == AttackType.BurstFire && burstReady)
        {
            TriggerBurstAttack();
            lastBurstAttackTime = Time.time;
            attackExecuted = true;
        }
        else if (selectedAttackType == AttackType.SingleFire && singleFireReady)
        {
            TriggerSingleFireAttack();
            lastSingleFireTime = Time.time;
            attackExecuted = true;
        }

        if (attackExecuted)
        {
            // Update pattern change tracking
            if (isPatternChange)
            {
                lastAttackPatternChangeTime = Time.time;
                if (showDebugLogs)
                    Debug.Log($"ChickenCombatManagerV2: PATTERN CHANGE - From {lastAttackType} to {selectedAttackType}");
            }
            lastAttackType = selectedAttackType;
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV2: Selected attack type {selectedAttackType} not ready - Burst ready: {burstReady}, Single ready: {singleFireReady}");
        }
    }

    void RefreshCombatChickens()
    {
        allCombatChickens.Clear();
        ChickenCombatBehaviorV2[] foundChickens = FindObjectsOfType<ChickenCombatBehaviorV2>();
        allCombatChickens.AddRange(foundChickens);

        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV2: Found {allCombatChickens.Count} combat chickens");
    }

    void TriggerBurstAttack()
    {
        if (showDebugLogs)
            Debug.Log("ChickenCombatManagerV2: EXECUTING BURST ATTACK!");

        // Get chickens that can attack
        List<ChickenCombatBehaviorV2> availableAttackers = GetAvailableAttackers();

        if (availableAttackers.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV2: No chickens available for burst attack");
            return;
        }

        // Randomly select chickens for burst attack
        List<ChickenCombatBehaviorV2> selectedChickens = SelectRandomChickens(availableAttackers, maxSimultaneousAttacks);

        foreach (ChickenCombatBehaviorV2 chicken in selectedChickens)
        {
            ExecuteEggAttack(chicken);
        }

        if (showDebugLogs)
        {
            Debug.Log($"ChickenCombatManagerV2: BURST ATTACK - {selectedChickens.Count} chickens fired from {availableAttackers.Count} available");
            foreach (var chicken in selectedChickens)
            {
                Debug.Log($"  Burst attacker: {chicken.gameObject.name}");
            }
        }
    }

    void TriggerSingleFireAttack()
    {
        if (showDebugLogs)
            Debug.Log("ChickenCombatManagerV2: EXECUTING SINGLE FIRE ATTACK!");

        // Get chickens that can attack
        List<ChickenCombatBehaviorV2> availableAttackers = GetAvailableAttackers();

        if (availableAttackers.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV2: No chickens available for single fire attack");
            return;
        }

        // Randomly select one chicken for single fire attack
        ChickenCombatBehaviorV2 selectedChicken = SelectRandomChicken(availableAttackers);

        if (selectedChicken != null)
        {
            ExecuteEggAttack(selectedChicken);

            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV2: SINGLE FIRE ATTACK - {selectedChicken.gameObject.name} fired randomly from {availableAttackers.Count} available");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV2: Failed to select chicken for single fire attack");
        }
    }

    // NEW: Select a random chicken from available list
    ChickenCombatBehaviorV2 SelectRandomChicken(List<ChickenCombatBehaviorV2> availableChickens)
    {
        if (availableChickens.Count == 0)
            return null;

        int randomIndex = Random.Range(0, availableChickens.Count);
        ChickenCombatBehaviorV2 selectedChicken = availableChickens[randomIndex];

        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV2: Randomly selected chicken {selectedChicken.gameObject.name} (index {randomIndex} of {availableChickens.Count})");

        return selectedChicken;
    }

    // NEW: Select multiple random chickens from available list (for burst attacks)
    List<ChickenCombatBehaviorV2> SelectRandomChickens(List<ChickenCombatBehaviorV2> availableChickens, int maxCount)
    {
        List<ChickenCombatBehaviorV2> selectedChickens = new List<ChickenCombatBehaviorV2>();

        if (availableChickens.Count == 0)
            return selectedChickens;

        int chickensToSelect = Mathf.Min(maxCount, availableChickens.Count);

        // Create a copy of the available chickens list to avoid modifying the original
        List<ChickenCombatBehaviorV2> chickenPool = new List<ChickenCombatBehaviorV2>(availableChickens);

        // Randomly select chickens without duplicates
        for (int i = 0; i < chickensToSelect; i++)
        {
            if (chickenPool.Count == 0)
                break;

            int randomIndex = Random.Range(0, chickenPool.Count);
            ChickenCombatBehaviorV2 selectedChicken = chickenPool[randomIndex];

            selectedChickens.Add(selectedChicken);
            chickenPool.RemoveAt(randomIndex); // Remove to avoid selecting the same chicken twice

            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV2: Randomly selected chicken {selectedChicken.gameObject.name} for burst attack ({i + 1} of {chickensToSelect})");
        }

        return selectedChickens;
    }

    List<ChickenCombatBehaviorV2> GetAvailableAttackers()
    {
        List<ChickenCombatBehaviorV2> available = new List<ChickenCombatBehaviorV2>();

        if (showDebugLogs && Time.frameCount % 600 == 0) // Reduced frequency for availability logging
            Debug.Log($"ChickenCombatManagerV2: Checking {allCombatChickens.Count} combat chickens for availability...");

        foreach (ChickenCombatBehaviorV2 chicken in allCombatChickens)
        {
            if (chicken == null)
            {
                if (showDebugLogs)
                    Debug.Log("ChickenCombatManagerV2: Found null chicken in list, skipping");
                continue;
            }

            // Check if chicken can attack (only needs to be in FollowingSlot state)
            bool isReadyToAttack = chicken.IsReadyToAttack;

            if (!isReadyToAttack)
            {
                // Get more detailed state info for debugging (less frequent logging)
                if (showDebugLogs && Time.frameCount % 600 == 0)
                {
                    var stateController = chicken.GetComponent<ChickenStateController>();
                    if (stateController != null)
                    {
                        Debug.Log($"ChickenCombatManagerV2: {chicken.gameObject.name} not ready - Current state: {stateController.CurrentState}");
                    }
                }
                continue;
            }

            available.Add(chicken);
        }

        if (showDebugLogs && Time.frameCount % 600 == 0) // Less frequent logging
            Debug.Log($"ChickenCombatManagerV2: Found {available.Count} available attackers out of {allCombatChickens.Count} total chickens");

        return available;
    }

    void ExecuteEggAttack(ChickenCombatBehaviorV2 chicken)
    {
        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV2: EXECUTING EGG ATTACK on {chicken.gameObject.name}!");

        chicken.ShootEgg(eggSpeed);

        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV2: ShootEgg() called on {chicken.gameObject.name} with speed {eggSpeed}");
    }

    // Public methods for external control
    public void RegisterChicken(ChickenCombatBehaviorV2 chicken)
    {
        if (chicken != null && !allCombatChickens.Contains(chicken))
        {
            allCombatChickens.Add(chicken);

            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV2: Registered chicken {chicken.gameObject.name} for combat");
        }
    }

    public void UnregisterChicken(ChickenCombatBehaviorV2 chicken)
    {
        if (allCombatChickens.Contains(chicken))
        {
            allCombatChickens.Remove(chicken);

            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV2: Unregistered chicken {chicken.gameObject.name} from combat");
        }
    }

    public void ForceBurstAttackNow()
    {
        lastBurstAttackTime = 0f; // Reset timer to trigger attack immediately
        lastAttackPatternChangeTime = 0f; // Reset pattern change cooldown
        if (showDebugLogs)
            Debug.Log("ChickenCombatManagerV2: Forced burst attack reset");
    }

    public void ForceSingleFireNow()
    {
        lastSingleFireTime = 0f; // Reset timer to trigger attack immediately
        lastAttackPatternChangeTime = 0f; // Reset pattern change cooldown
        if (showDebugLogs)
            Debug.Log("ChickenCombatManagerV2: Forced single fire reset");
    }

    public void ResetPatternChangeCooldown()
    {
        lastAttackPatternChangeTime = 0f;
        if (showDebugLogs)
            Debug.Log("ChickenCombatManagerV2: Pattern change cooldown reset");
    }

    // Properties
    public int TotalCombatChickens => allCombatChickens.Count;
    public int AvailableAttackers => GetAvailableAttackers().Count;
    public float NextBurstAttackTime => lastBurstAttackTime + burstAttackInterval;
    public float NextSingleFireTime => lastSingleFireTime + singleFireInterval;
    public float NextPatternChangeTime => lastAttackPatternChangeTime + attackPatternChangeCooldown;
    public AttackType LastAttackType => lastAttackType;

    // Context menu methods
    [ContextMenu("Refresh Combat Chickens")]
    void ContextMenuRefreshChickens()
    {
        RefreshCombatChickens();
    }

    [ContextMenu("Force Burst Attack Now")]
    void ContextMenuForceBurstAttack()
    {
        TriggerBurstAttack();
        lastBurstAttackTime = Time.time;
        lastAttackType = AttackType.BurstFire;
        Debug.Log("ChickenCombatManagerV2: Forced burst attack execution");
    }

    [ContextMenu("Force Single Fire Now")]
    void ContextMenuForceSingleFire()
    {
        TriggerSingleFireAttack();
        lastSingleFireTime = Time.time;
        lastAttackType = AttackType.SingleFire;
        Debug.Log("ChickenCombatManagerV2: Forced single fire execution");
    }

    [ContextMenu("Reset Pattern Change Cooldown")]
    void ContextMenuResetPatternChange()
    {
        ResetPatternChangeCooldown();
        Debug.Log("ChickenCombatManagerV2: Pattern change cooldown manually reset");
    }

    [ContextMenu("Toggle Combat")]
    void ContextMenuToggleCombat()
    {
        enableCombat = !enableCombat;
        Debug.Log($"ChickenCombatManagerV2: Combat {(enableCombat ? "enabled" : "disabled")}");
    }

    [ContextMenu("Print Combat Status")]
    void ContextMenuPrintStatus()
    {
        Debug.Log("=== CHICKEN COMBAT MANAGER STATUS ===");
        Debug.Log($"Combat Enabled: {enableCombat}");
        Debug.Log($"Total Combat Chickens: {TotalCombatChickens}");
        Debug.Log($"Available Attackers: {AvailableAttackers}");
        Debug.Log($"BURST ATTACK - Interval: {burstAttackInterval}s, Max Simultaneous: {maxSimultaneousAttacks}");
        Debug.Log($"SINGLE FIRE - Interval: {singleFireInterval}s, Random Selection: ENABLED");
        Debug.Log($"PATTERN CHANGE - Cooldown: {attackPatternChangeCooldown}s, Last Attack Type: {lastAttackType}");
        Debug.Log($"Burst Fire Chance: {burstFireChance}% (1-{burstFireChance})");
        Debug.Log($"Next Burst Attack In: {(NextBurstAttackTime - Time.time):F1}s");
        Debug.Log($"Next Single Fire In: {(NextSingleFireTime - Time.time):F1}s");
        Debug.Log($"Next Pattern Change Available In: {(NextPatternChangeTime - Time.time):F1}s");
        Debug.Log($"Player Found: {(player != null ? "Yes" : "No")}");

        if (allCombatChickens.Count > 0)
        {
            Debug.Log("\nCHICKEN DETAILS:");
            foreach (var chicken in allCombatChickens)
            {
                if (chicken != null)
                {
                    var stateController = chicken.GetComponent<ChickenStateController>();
                    var state = stateController != null ? stateController.CurrentState.ToString() : "Unknown";
                    var canAttack = chicken.IsReadyToAttack;
                    Debug.Log($"  {chicken.gameObject.name}: State={state}, CanAttack={canAttack}");
                }
                else
                {
                    Debug.Log($"  NULL CHICKEN FOUND IN LIST!");
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showAttackGizmos)
            return;

        // Draw simple indicators for combat chickens
        if (allCombatChickens.Count > 0)
        {
            for (int i = 0; i < allCombatChickens.Count; i++)
            {
                var chicken = allCombatChickens[i];
                if (chicken != null)
                {
                    // Show combat-ready chickens with green gizmo
                    bool canAttack = chicken.IsReadyToAttack;
                    Gizmos.color = canAttack ? Color.green : Color.red;

                    Gizmos.DrawWireSphere(chicken.transform.position + Vector3.up * 2f, 0.5f);
                }
            }
        }
    }
}