using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "New Attack Loot Table", menuName = "Chicken Combat/Attack Loot Table")]
public class AttackLootTableSO : ScriptableObject
{
    [Header("Attack Loot Table")]
    public List<AttackEntry> attackEntries = new List<AttackEntry>();
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    [System.Serializable]
    public class AttackEntry
    {
        public BaseChickenAttackSO attackAsset;
        [Range(0f, 100f)]
        public float chancePercentage = 10f;
        public bool lockPercentage = false; // Lock this percentage from auto-balancing
        

        
        // Helper properties
        public string AttackName => attackAsset?.AttackName ?? "None";
        public bool IsValid => attackAsset != null;
    }
    
    // Cached values for performance
    private float totalPercentage = 100f;
    private bool needsRecalculation = true;
    
    void OnValidate()
    {
        // Auto-balance percentages when values change in inspector
        AutoBalancePercentages();
    }
    
    public void AutoBalancePercentages()
    {
        if (attackEntries.Count <= 1) return;
        
        // Remove invalid entries
        attackEntries.RemoveAll(x => x.attackAsset == null);
        
        if (attackEntries.Count == 0) return;
        
        // Get locked and unlocked entries
        var lockedEntries = attackEntries.Where(x => x.lockPercentage).ToList();
        var unlockedEntries = attackEntries.Where(x => !x.lockPercentage).ToList();
        
        // Calculate total locked percentage
        float totalLockedPercentage = lockedEntries.Sum(x => x.chancePercentage);
        
        // Check if locked percentages exceed 100%
        if (totalLockedPercentage > 100f)
        {
            Debug.LogWarning($"AttackLootTable '{name}': Locked percentages total {totalLockedPercentage:F1}%, which exceeds 100%. Scaling down locked entries.");
            
            // Scale down locked entries proportionally
            float lockScaleFactor = 100f / totalLockedPercentage;
            foreach (var entry in lockedEntries)
            {
                entry.chancePercentage *= lockScaleFactor;
            }
            
            // Set all unlocked entries to 0
            foreach (var entry in unlockedEntries)
            {
                entry.chancePercentage = 0f;
            }
            
            return;
        }
        
        // Calculate remaining percentage for unlocked entries
        float remainingPercentage = 100f - totalLockedPercentage;
        
        if (unlockedEntries.Count == 0)
        {
            // All entries are locked, ensure they total 100%
            if (!Mathf.Approximately(totalLockedPercentage, 100f))
            {
                Debug.LogWarning($"AttackLootTable '{name}': All entries are locked but don't total 100% ({totalLockedPercentage:F1}%). Consider unlocking some entries.");
            }
            return;
        }
        
        // Distribute remaining percentage among unlocked entries
        if (remainingPercentage <= 0f)
        {
            // No room left for unlocked entries
            foreach (var entry in unlockedEntries)
            {
                entry.chancePercentage = 0f;
            }
        }
        else
        {
            // Calculate current total of unlocked entries
            float currentUnlockedTotal = unlockedEntries.Sum(x => x.chancePercentage);
            
            if (currentUnlockedTotal <= 0f)
            {
                // Distribute equally among unlocked entries
                float equalShare = remainingPercentage / unlockedEntries.Count;
                foreach (var entry in unlockedEntries)
                {
                    entry.chancePercentage = equalShare;
                }
            }
            else
            {
                // Scale unlocked entries proportionally to fit remaining space
                float scaleFactor = remainingPercentage / currentUnlockedTotal;
                foreach (var entry in unlockedEntries)
                {
                    entry.chancePercentage *= scaleFactor;
                }
            }
        }
        
        // Round all percentages to 1 decimal place
        foreach (var entry in attackEntries)
        {
            entry.chancePercentage = Mathf.Round(entry.chancePercentage * 10f) / 10f;
        }
        
        // Fix floating point precision issues and ensure exact 100.0%
        float actualTotal = attackEntries.Sum(x => x.chancePercentage);
        if (!Mathf.Approximately(actualTotal, 100f) && unlockedEntries.Count > 0)
        {
            float difference = 100f - actualTotal;
            unlockedEntries[0].chancePercentage = Mathf.Round((unlockedEntries[0].chancePercentage + difference) * 10f) / 10f;
        }
        
        needsRecalculation = true;
    }
    
    public BaseChickenAttackSO SelectRandomAttack()
    {
        return SelectRandomAttack(null); // Use overload with no exclusions
    }
    
    public BaseChickenAttackSO SelectRandomAttack(AttackType? excludeAttackType)
    {
        if (attackEntries.Count == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"AttackLootTable '{name}': No attack entries available");
            return null;
        }
        
        // Remove invalid entries and excluded attack types
        var validEntries = attackEntries.Where(x => x.IsValid && x.chancePercentage > 0f).ToList();
        
        if (excludeAttackType.HasValue)
        {
            validEntries = validEntries.Where(x => x.attackAsset.AttackType != excludeAttackType.Value).ToList();
            
            if (showDebugLogs)
                Debug.Log($"AttackLootTable '{name}': Excluding {excludeAttackType.Value} attacks from selection");
        }
        
        if (validEntries.Count == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"AttackLootTable '{name}': No valid attack entries with >0% chance (after exclusions)");
            return null;
        }
        
        if (validEntries.Count == 1)
        {
            if (showDebugLogs)
                Debug.Log($"AttackLootTable '{name}': Only one valid attack, selecting {validEntries[0].AttackName}");
            return validEntries[0].attackAsset;
        }
        
        // Normalize percentages for available entries
        float totalValidPercentage = validEntries.Sum(x => x.chancePercentage);
        if (totalValidPercentage <= 0f) return validEntries[0].attackAsset;
        
        // Generate random value between 0 and total valid percentage
        float randomValue = Random.Range(0f, totalValidPercentage);
        
        if (showDebugLogs)
            Debug.Log($"AttackLootTable '{name}': Random roll: {randomValue:F2} out of {totalValidPercentage:F2}");
        
        // Find which attack this random value corresponds to
        float cumulativePercentage = 0f;
        foreach (var entry in validEntries)
        {
            cumulativePercentage += entry.chancePercentage;
            
            if (showDebugLogs)
                Debug.Log($"  {entry.AttackName}: {entry.chancePercentage:F1}%, Cumulative: {cumulativePercentage:F1}%");
            
            if (randomValue <= cumulativePercentage)
            {
                if (showDebugLogs)
                    Debug.Log($"  Selected: {entry.AttackName}!");
                    
                return entry.attackAsset;
            }
        }
        
        // Fallback (should not happen, but just in case)
        var fallback = validEntries[validEntries.Count - 1];
        if (showDebugLogs)
            Debug.Log($"AttackLootTable '{name}': Fallback selection: {fallback.AttackName}");
        return fallback.attackAsset;
    }
    
    public List<BaseChickenAttackSO> GetValidAttacks()
    {
        return attackEntries
            .Where(x => x.IsValid && x.chancePercentage > 0f)
            .Select(x => x.attackAsset)
            .ToList();
    }
    
    public float GetAttackChance(BaseChickenAttackSO attackAsset)
    {
        var entry = attackEntries.FirstOrDefault(x => x.attackAsset == attackAsset);
        return entry?.chancePercentage ?? 0f;
    }
    

    
    public void SetAttackChance(BaseChickenAttackSO attackAsset, float percentage)
    {
        var entry = attackEntries.FirstOrDefault(x => x.attackAsset == attackAsset);
        if (entry != null)
        {
            entry.chancePercentage = Mathf.Clamp(percentage, 0f, 100f);
            AutoBalancePercentages();
        }
    }
    
    public void AddAttack(BaseChickenAttackSO attackAsset, float percentage = 10f)
    {
        if (attackAsset == null) return;
        
        // Check if already exists
        if (attackEntries.Any(x => x.attackAsset == attackAsset))
        {
            Debug.LogWarning($"AttackLootTable '{name}': Attack {attackAsset.AttackName} already exists");
            return;
        }
        
        var newEntry = new AttackEntry
        {
            attackAsset = attackAsset,
            chancePercentage = percentage
        };
        
        attackEntries.Add(newEntry);
        AutoBalancePercentages();
        
        if (showDebugLogs)
            Debug.Log($"AttackLootTable '{name}': Added {attackAsset.AttackName} with {percentage}% chance");
    }
    
    public void RemoveAttack(BaseChickenAttackSO attackAsset)
    {
        int removedCount = attackEntries.RemoveAll(x => x.attackAsset == attackAsset);
        if (removedCount > 0)
        {
            AutoBalancePercentages();
            if (showDebugLogs)
                Debug.Log($"AttackLootTable '{name}': Removed {attackAsset?.AttackName}");
        }
    }
    
    [ContextMenu("Auto Balance Percentages")]
    void ContextMenuAutoBalance()
    {
        AutoBalancePercentages();
        Debug.Log($"AttackLootTable '{name}': Auto-balanced percentages");
    }
    
    [ContextMenu("Print Loot Table")]
    void ContextMenuPrintTable()
    {
        Debug.Log($"=== ATTACK LOOT TABLE: {name} ===");
        float total = 0f;
        int lockedCount = 0;
        
        foreach (var entry in attackEntries)
        {
            if (entry.IsValid)
            {
                string lockStatus = entry.lockPercentage ? " (LOCKED)" : "";
                Debug.Log($"  {entry.AttackName}: {entry.chancePercentage:F1}%{lockStatus}");
                total += entry.chancePercentage;
                if (entry.lockPercentage) lockedCount++;
            }
            else
            {
                Debug.Log($"  INVALID ENTRY: {entry.chancePercentage:F1}%");
            }
        }
        
        Debug.Log($"Total: {total:F1}% ({lockedCount} entries locked)");
        
        if (!Mathf.Approximately(total, 100f))
        {
            Debug.LogWarning($"Total does not equal 100%! Difference: {(100f - total):F2}%");
        }
    }
    
    [ContextMenu("Test Random Selection (10 times)")]
    void ContextMenuTestSelection()
    {
        Debug.Log($"=== TESTING RANDOM SELECTION: {name} ===");
        
        Dictionary<string, int> results = new Dictionary<string, int>();
        
        for (int i = 0; i < 10; i++)
        {
            var selected = SelectRandomAttack();
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
}