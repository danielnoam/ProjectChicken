using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Single Fire Attack", menuName = "Chicken Combat/Attacks/Single Fire Attack")]
public class SingleFireAttackSO : BaseChickenAttackSO
{
    [Header("Single Fire Settings")]
    public bool preferClosestToPlayer = false;
    [Range(0f, 1f)]
    public float randomnessWeight = 1f; // 1 = completely random, 0 = always closest

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

        // Select chicken for attack
        ChickenCombatBehaviorV2 selectedChicken = SelectChickenForAttack(availableChickens, manager);

        if (selectedChicken != null)
        {
            ExecuteChickenAttack(selectedChicken, manager);
            LogDebug($"{selectedChicken.gameObject.name} fired from {availableChickens.Count} available");
        }
        else
        {
            LogWarning("Failed to select chicken for attack");
        }
    }

    ChickenCombatBehaviorV2 SelectChickenForAttack(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        if (availableChickens.Count == 0)
            return null;

        if (availableChickens.Count == 1)
            return availableChickens[0];

        ChickenCombatBehaviorV2 selectedChicken = null;

        if (preferClosestToPlayer && manager.Player != null && randomnessWeight < 1f)
        {
            selectedChicken = SelectBasedOnDistance(availableChickens, manager.Player);
        }
        else
        {
            // Pure random selection
            int randomIndex = Random.Range(0, availableChickens.Count);
            selectedChicken = availableChickens[randomIndex];
        }

        if (selectedChicken != null)
        {
            string selectionMethod = preferClosestToPlayer && randomnessWeight < 1f ? "distance-based" : "random";
            LogDebug($"Selected {selectedChicken.gameObject.name} using {selectionMethod} selection");
        }

        return selectedChicken;
    }

    ChickenCombatBehaviorV2 SelectBasedOnDistance(List<ChickenCombatBehaviorV2> availableChickens, Transform player)
    {
        // Weighted selection between closest and random
        if (Random.Range(0f, 1f) <= randomnessWeight)
        {
            // Random selection
            return availableChickens[Random.Range(0, availableChickens.Count)];
        }
        else
        {
            // Find closest chicken to player
            ChickenCombatBehaviorV2 closestChicken = null;
            float closestDistance = float.MaxValue;

            foreach (var chicken in availableChickens)
            {
                float distance = Vector3.Distance(chicken.transform.position, player.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestChicken = chicken;
                }
            }

            return closestChicken;
        }
    }

    void ExecuteChickenAttack(ChickenCombatBehaviorV2 chicken, ChickenCombatManagerV4 manager)
    {
        LogDebug($"Executing attack on {chicken.gameObject.name}");
        chicken.ShootEgg(manager.EggSpeed);
    }
}