using UnityEngine;
using System.Collections.Generic;

public class ChickenCombatManagerV2 : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackInterval = 3f; // How often to trigger attacks
    public int maxSimultaneousAttacks = 2; // Max chickens that can attack at once
    public bool enableCombat = true;
    public float eggSpeed = 10f; // Speed of all eggs shot by chickens

    [Header("Debug")]
    public bool showDebugLogs = true; // Enable by default to help troubleshoot
    public bool showAttackGizmos = false;

    private List<ChickenCombatBehaviorV2> allCombatChickens = new List<ChickenCombatBehaviorV2>();
    private float lastAttackTime = 0f;
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

        // Check if it's time to attack
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (timeSinceLastAttack >= attackInterval)
        {
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV2: Attack timer triggered! Time since last attack: {timeSinceLastAttack:F2}s");

            TriggerAttacks();
            lastAttackTime = Time.time;
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

    void TriggerAttacks()
    {
        // Get chickens that can attack
        List<ChickenCombatBehaviorV2> availableAttackers = GetAvailableAttackers();

        if (availableAttackers.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV2: No chickens available to attack");
            return;
        }

        // Take up to maxSimultaneousAttacks chickens and make them attack
        int attackersToUse = Mathf.Min(maxSimultaneousAttacks, availableAttackers.Count);

        for (int i = 0; i < attackersToUse; i++)
        {
            ExecuteEggAttack(availableAttackers[i]);
        }

        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV2: Commanded {attackersToUse} chickens to attack from {availableAttackers.Count} available");
    }

    List<ChickenCombatBehaviorV2> GetAvailableAttackers()
    {
        List<ChickenCombatBehaviorV2> available = new List<ChickenCombatBehaviorV2>();

        if (showDebugLogs)
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
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV2: {chicken.gameObject.name} - IsReadyToAttack: {isReadyToAttack}");

            if (!isReadyToAttack)
            {
                // Get more detailed state info for debugging
                var stateController = chicken.GetComponent<ChickenStateController>();
                if (stateController != null)
                {
                    if (showDebugLogs)
                        Debug.Log($"ChickenCombatManagerV2: {chicken.gameObject.name} not ready - Current state: {stateController.CurrentState}");
                }
                else
                {
                    if (showDebugLogs)
                        Debug.Log($"ChickenCombatManagerV2: {chicken.gameObject.name} has no ChickenStateController!");
                }
                continue;
            }

            available.Add(chicken);
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV2: {chicken.gameObject.name} AVAILABLE TO ATTACK!");
        }

        if (showDebugLogs)
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

    public void ForceAttackNow()
    {
        lastAttackTime = 0f; // Reset timer to trigger attack immediately
    }

    // Properties
    public int TotalCombatChickens => allCombatChickens.Count;
    public int AvailableAttackers => GetAvailableAttackers().Count;
    public float NextAttackTime => lastAttackTime + attackInterval;

    // Context menu methods
    [ContextMenu("Refresh Combat Chickens")]
    void ContextMenuRefreshChickens()
    {
        RefreshCombatChickens();
    }

    [ContextMenu("Force Attack Now")]
    void ContextMenuForceAttack()
    {
        ForceAttackNow();
        Debug.Log("ChickenCombatManagerV2: Forced immediate attack");
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
        Debug.Log($"Attack Interval: {attackInterval}s");
        Debug.Log($"Max Simultaneous Attacks: {maxSimultaneousAttacks}");
        Debug.Log($"Next Attack In: {(NextAttackTime - Time.time):F1}s");
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
            foreach (var chicken in allCombatChickens)
            {
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