using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Burst Fire Attack", menuName = "Chicken Combat/Attacks/Burst Fire Attack")]
public class BurstFireAttackSO : BaseChickenAttackSO
{
    [Header("Burst Fire Settings")]
    public int maxSimultaneousAttacks = 2;

    public override AttackType AttackType => AttackType.BurstFire;
    public override string AttackName => "Burst Fire Attack";

    public override bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        bool hasEnoughChickens = availableChickens.Count >= minChickensRequired;
        
        if (!hasEnoughChickens)
            LogDebug($"Not enough chickens available ({availableChickens.Count}/{minChickensRequired})");
            
        return hasEnoughChickens;
    }

    public override void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        if (!CanExecute(availableChickens, manager))
        {
            LogWarning("Cannot execute - insufficient chickens");
            return;
        }

        LogDebug("EXECUTING!");

        // Calculate modified egg speed with multiplier
        float modifiedSpeed = manager.EggSpeed * eggSpeedMultiplier;

        // Select chickens for burst attack
        List<ChickenCombatBehaviorV2> selectedChickens = SelectChickensForBurst(availableChickens);

        // Execute attacks
        foreach (ChickenCombatBehaviorV2 chicken in selectedChickens)
        {
            ExecuteChickenAttack(chicken, modifiedSpeed);
        }

        LogDebug($"Completed - {selectedChickens.Count} chickens fired (Speed: {modifiedSpeed:F1}, Multiplier: {eggSpeedMultiplier:F2}x)");
        foreach (var chicken in selectedChickens)
        {
            LogDebug($"  Burst attacker: {chicken.gameObject.name}");
        }
    }

    List<ChickenCombatBehaviorV2> SelectChickensForBurst(List<ChickenCombatBehaviorV2> availableChickens)
    {
        List<ChickenCombatBehaviorV2> selectedChickens = new List<ChickenCombatBehaviorV2>();
        int chickensToSelect = Mathf.Min(maxSimultaneousAttacks, availableChickens.Count);

        // Create pool to avoid selecting same chicken twice
        List<ChickenCombatBehaviorV2> chickenPool = new List<ChickenCombatBehaviorV2>(availableChickens);

        for (int i = 0; i < chickensToSelect; i++)
        {
            if (chickenPool.Count == 0) break;

            int randomIndex = Random.Range(0, chickenPool.Count);
            ChickenCombatBehaviorV2 selectedChicken = chickenPool[randomIndex];

            selectedChickens.Add(selectedChicken);
            chickenPool.RemoveAt(randomIndex);

            LogDebug($"Selected {selectedChicken.gameObject.name} ({i + 1}/{chickensToSelect})");
        }

        return selectedChickens;
    }

    void ExecuteChickenAttack(ChickenCombatBehaviorV2 chicken, float eggSpeed)
    {
        LogDebug($"Executing attack on {chicken.gameObject.name}");
        chicken.ShootEgg(eggSpeed, deactivateWarningCircle);
        // Play the attack SFX at the chicken's position
        if (audioEvent != null)
        {
            audioEvent.PlayAtPoint(chicken.transform.position);
        }
    }
}