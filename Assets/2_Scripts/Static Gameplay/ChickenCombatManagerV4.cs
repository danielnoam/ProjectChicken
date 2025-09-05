using UnityEngine;
using System.Collections.Generic;

public class ChickenCombatManagerV4 : MonoBehaviour
{
    [Header("General Combat Settings")]
    public bool enableCombat = true;
    public float eggSpeed = 20f;
    public float attackCooldown;
    
    [Header("Attack Configuration")]
    public AttackLootTableSO attackLootTable;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showAttackGizmos = false;
    public bool showRegistrationLogs = false;

    [Header("Registered Chickens (Read Only)")]
    [SerializeField] private List<ChickenCombatBehaviorV2> registeredCombatChickens = new List<ChickenCombatBehaviorV2>();
    
    private Transform player;

    // Public properties
    public int TotalCombatChickens => registeredCombatChickens.Count;
    public List<ChickenCombatBehaviorV2> RegisteredChickens => new List<ChickenCombatBehaviorV2>(registeredCombatChickens);
    public List<ChickenCombatBehaviorV2> GetAvailableAttackers() => GetAvailableAttackersInternal();
    public int AvailableAttackers => GetAvailableAttackersInternal().Count;
    public float EggSpeed => eggSpeed;
    public Transform Player => player;
    public AttackLootTableSO AttackLootTable => attackLootTable;

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

        // Combat manager is now ready for external attack triggering
        // Attack selection logic has been removed
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

        return true;
    }

    public bool UnregisterChickenFromCombat(ChickenCombatBehaviorV2 chicken)
    {
        if (chicken == null) return false;

        bool wasRegistered = registeredCombatChickens.Remove(chicken);

        if (wasRegistered && showRegistrationLogs)
            Debug.Log($"ChickenCombatManagerV4: Unregistered chicken {chicken.gameObject.name} from combat ({registeredCombatChickens.Count} total)");

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
    }

    [ContextMenu("Print Combat Status")]
    void ContextMenuPrintStatus()
    {
        Debug.Log("=== CHICKEN COMBAT MANAGER V4 STATUS ===");
        Debug.Log($"Combat Enabled: {enableCombat}");
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
}