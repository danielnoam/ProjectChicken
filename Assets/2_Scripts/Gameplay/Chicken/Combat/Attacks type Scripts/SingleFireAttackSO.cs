using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Single Fire Attack", menuName = "Chicken Combat/Attacks/Single Fire Attack")]
public class SingleFireAttackSO : BaseChickenAttackSO
{
    public override AttackType AttackType => AttackType.SingleFire;
    public override string AttackName => "Single Fire Attack";

    public override bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        bool hasChickens = availableChickens.Count > 0;
        
        if (!hasChickens)
            LogDebug("No chickens available for attack");
            
        return hasChickens;
    }

    public override void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        if (!CanExecute(availableChickens, manager))
        {
            LogWarning("Cannot execute - no chickens available");
            return;
        }

        LogDebug("EXECUTING!");

        // Always select a random chicken from the available pool
        ChickenCombatBehaviorV2 selectedChicken = SelectRandomChicken(availableChickens);

        if (selectedChicken != null)
        {
            ExecuteChickenAttack(selectedChicken, manager);
            LogDebug($"{selectedChicken.gameObject.name} fired from {availableChickens.Count} available chickens");
        }
        else
        {
            LogWarning("Failed to select chicken for attack");
        }
    }

    ChickenCombatBehaviorV2 SelectRandomChicken(List<ChickenCombatBehaviorV2> availableChickens)
    {
        if (availableChickens.Count == 0)
            return null;

        // Pure random selection - same chicken can be selected multiple times
        int randomIndex = Random.Range(0, availableChickens.Count);
        ChickenCombatBehaviorV2 selectedChicken = availableChickens[randomIndex];
        
        LogDebug($"Selected {selectedChicken.gameObject.name} (index {randomIndex}) using random selection");
        
        return selectedChicken;
    }

    void ExecuteChickenAttack(ChickenCombatBehaviorV2 chicken, ChickenCombatManagerV4 manager)
    {
        LogDebug($"Executing attack on {chicken.gameObject.name}");
        chicken.ShootEgg(manager.EggSpeed);
    }
}