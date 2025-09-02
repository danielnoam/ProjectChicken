using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Rapid Fire Attack", menuName = "Chicken Combat/Attacks/Rapid Fire Attack")]
public class RapidFireAttackSO : BaseChickenAttackSO
{
    [Header("Rapid Fire Settings")]
    public int shotsPerChicken = 3;
    public float shotDelay = 0.3f;
    public int maxChickensInvolved = 1;
    public bool useSequentialShots = true;

    public override AttackType AttackType => AttackType.RapidFire;
    public override string AttackName => "Rapid Fire Attack";

    public override bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        bool hasChickens = availableChickens.Count > 0;
        
        if (!hasChickens)
            LogDebug("No chickens available");
            
        return hasChickens;
    }

    public override void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        if (!CanExecute(availableChickens, manager))
        {
            LogWarning("Cannot execute");
            return;
        }

        LogDebug("EXECUTING!");

        // Select chickens for rapid fire
        List<ChickenCombatBehaviorV2> selectedChickens = SelectChickensForRapidFire(availableChickens);

        // Since ScriptableObjects can't use StartCoroutine directly, we ask the manager to run it
        if (useSequentialShots)
        {
            manager.StartCoroutine(ExecuteSequentialRapidFire(selectedChickens, manager));
        }
        else
        {
            manager.StartCoroutine(ExecuteSimultaneousRapidFire(selectedChickens, manager));
        }
    }

    List<ChickenCombatBehaviorV2> SelectChickensForRapidFire(List<ChickenCombatBehaviorV2> availableChickens)
    {
        List<ChickenCombatBehaviorV2> selectedChickens = new List<ChickenCombatBehaviorV2>();
        int chickensToSelect = Mathf.Min(maxChickensInvolved, availableChickens.Count);

        // Create a copy to avoid modifying original list
        List<ChickenCombatBehaviorV2> chickenPool = new List<ChickenCombatBehaviorV2>(availableChickens);

        for (int i = 0; i < chickensToSelect; i++)
        {
            if (chickenPool.Count == 0) break;

            int randomIndex = Random.Range(0, chickenPool.Count);
            selectedChickens.Add(chickenPool[randomIndex]);
            chickenPool.RemoveAt(randomIndex); // Avoid duplicates
        }

        LogDebug($"Selected {selectedChickens.Count} chickens for rapid fire");
        foreach (var chicken in selectedChickens)
        {
            LogDebug($"  Rapid fire chicken: {chicken.gameObject.name}");
        }

        return selectedChickens;
    }

    System.Collections.IEnumerator ExecuteSequentialRapidFire(List<ChickenCombatBehaviorV2> selectedChickens, ChickenCombatManagerV4 manager)
    {
        foreach (var chicken in selectedChickens)
        {
            if (chicken == null) continue;

            LogDebug($"Starting rapid fire sequence on {chicken.gameObject.name}");

            for (int shot = 0; shot < shotsPerChicken; shot++)
            {
                if (chicken != null && chicken.IsReadyToAttack)
                {
                    chicken.ShootEgg(manager.EggSpeed);
                    LogDebug($"{chicken.gameObject.name} fired shot {shot + 1}/{shotsPerChicken}");
                }

                if (shot < shotsPerChicken - 1) // Don't wait after the last shot
                {
                    yield return new WaitForSeconds(shotDelay);
                }
            }

            // Small delay between chickens if multiple are involved
            if (selectedChickens.Count > 1)
            {
                yield return new WaitForSeconds(shotDelay * 0.5f);
            }
        }

        LogDebug("Sequential rapid fire complete!");
    }

    System.Collections.IEnumerator ExecuteSimultaneousRapidFire(List<ChickenCombatBehaviorV2> selectedChickens, ChickenCombatManagerV4 manager)
    {
        for (int shot = 0; shot < shotsPerChicken; shot++)
        {
            // All selected chickens fire simultaneously
            foreach (var chicken in selectedChickens)
            {
                if (chicken != null && chicken.IsReadyToAttack)
                {
                    chicken.ShootEgg(manager.EggSpeed);
                    LogDebug($"{chicken.gameObject.name} fired simultaneous shot {shot + 1}/{shotsPerChicken}");
                }
            }

            if (shot < shotsPerChicken - 1) // Don't wait after the last shot
            {
                yield return new WaitForSeconds(shotDelay);
            }
        }

        LogDebug($"Simultaneous rapid fire complete - {selectedChickens.Count} chickens fired {shotsPerChicken} shots each!");
    }
}