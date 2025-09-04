using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;

[CreateAssetMenu(fileName = "New Attack Loot Table", menuName = "Chicken Combat/Attack Loot Table")]
public class AttackLootTableSO : ScriptableObject
{
    [Header("Attack Loot Table")]
    public ChanceList<BaseChickenAttackSO> attackEntries = new ChanceList<BaseChickenAttackSO>();
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    



    
    public BaseChickenAttackSO SelectRandomAttack()
    {
        if (attackEntries.Count == 0)
        {
            if (showDebugLogs) Debug.LogWarning($"AttackLootTable '{name}': No attack entries available");
            return null;
        }

        return attackEntries.GetRandomItem();
    }
    
    
    public BaseChickenAttackSO SelectRandomAttackThatIsNot(AttackType attackType)
    {

        for (var index = 0; index < attackEntries.Count; index++)
        {
            var entry = attackEntries[index];
            if (!entry)
            {
                if (showDebugLogs) Debug.LogWarning($"AttackLootTable '{name}': Attack asset is null");
                continue;
            }

            if (entry.AttackType == attackType) continue;

            return entry;
        }
        
        if (showDebugLogs) Debug.LogWarning($"AttackLootTable '{name}': No attack entries available");
        return attackEntries.GetRandomItem();
    }
    
    public List<BaseChickenAttackSO> GetValidAttacks()
    {
        var validAttacks = new List<BaseChickenAttackSO>();

        for (var index = 0; index < attackEntries.Count; index++)
        {
            var entry = attackEntries[index];
            if (!entry)
            {
                if (showDebugLogs) Debug.LogWarning($"AttackLootTable '{name}': Attack asset is null");
                continue;
            }

            validAttacks.Add(entry);
        }
        
        return validAttacks;
    }
    
    public float GetAttackChance(BaseChickenAttackSO attackAsset)
    {
        if (attackAsset == null)
        {
            if (showDebugLogs) Debug.LogWarning($"AttackLootTable '{name}': Attack asset is null");
            return 0f;
        }
        
        var index = attackEntries.GetIndex(attackAsset);
        return attackEntries.GetChance(index);
    }
    

    

    


}