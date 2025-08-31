using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Formation Fill Attack", menuName = "Chicken Combat/Attacks/Formation Fill Attack")]
public class FormationFillAttackSO : BaseChickenAttackSO
{
    [Header("Formation Fill Settings")]
    public bool preferClosestChickenToSlot = true;
    [Range(0f, 1f)]
    public float randomnessWeight = 0.3f; // 0 = always closest, 1 = completely random

    public override AttackType AttackType => AttackType.FormationFill;
    public override string AttackName => "Formation Fill Attack";

    public override bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        // Check if there are chickens available to move
        if (availableChickens.Count == 0)
        {
            LogDebug("No chickens available for formation filling");
            return false;
        }

        // Check if there are empty formation slots
        // NOTE: This assumes you have a formation manager component
        // You'll need to implement IFormationManager interface or similar
        bool hasEmptySlots = CheckForEmptyFormationSlots(manager);
        
        if (!hasEmptySlots)
            LogDebug("No empty formation slots available");
            
        return hasEmptySlots;
    }

    public override void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        if (!CanExecute(availableChickens, manager))
        {
            LogWarning("Cannot execute - no chickens available or no empty formation slots");
            return;
        }

        LogDebug("EXECUTING!");

        // Get empty formation slots
        var emptySlots = GetEmptyFormationSlots(manager);
        if (emptySlots.Count == 0)
        {
            LogWarning("No empty slots found during execution");
            return;
        }

        // Select a random empty slot
        Vector3 targetSlot = emptySlots[Random.Range(0, emptySlots.Count)];

        // Select chicken for repositioning
        ChickenCombatBehaviorV2 selectedChicken = SelectChickenForRepositioning(availableChickens, targetSlot);

        if (selectedChicken != null)
        {
            ExecuteFormationFill(selectedChicken, targetSlot, manager);
            LogDebug($"{selectedChicken.gameObject.name} moving to fill formation slot at {targetSlot}");
        }
        else
        {
            LogWarning("Failed to select chicken for formation filling");
        }
    }

    bool CheckForEmptyFormationSlots(ChickenCombatManagerV4 manager)
    {
        // Find the EnemyChickenManager in the scene
        var enemyChickenManager = FindObjectOfType<EnemyChickenManager>();
        if (enemyChickenManager != null)
        {
            bool hasEmptySlots = enemyChickenManager.AvailableSlots > 0;
            LogDebug($"Formation check: {enemyChickenManager.AvailableSlots} empty slots available");
            return hasEmptySlots;
        }
        
        LogDebug("No EnemyChickenManager found - cannot check for empty slots");
        return false;
    }

    List<Vector3> GetEmptyFormationSlots(ChickenCombatManagerV4 manager)
    {
        List<Vector3> emptySlots = new List<Vector3>();
        
        var enemyChickenManager = FindObjectOfType<EnemyChickenManager>();
        if (enemyChickenManager != null && enemyChickenManager.formationCreator != null)
        {
            var allSlots = enemyChickenManager.formationCreator.GetFormationSlots();
            
            // Find slots that don't have chickens assigned
            for (int i = 0; i < allSlots.Count; i++)
            {
                if (enemyChickenManager.GetChickenInSlot(i) == null)
                {
                    emptySlots.Add(allSlots[i]);
                    LogDebug($"Found empty slot {i} at position {allSlots[i]}");
                }
            }
        }
        
        return emptySlots;
    }

    ChickenCombatBehaviorV2 SelectChickenForRepositioning(List<ChickenCombatBehaviorV2> availableChickens, Vector3 targetSlot)
    {
        if (availableChickens.Count == 0)
            return null;

        if (availableChickens.Count == 1)
            return availableChickens[0];

        ChickenCombatBehaviorV2 selectedChicken = null;

        if (preferClosestChickenToSlot && randomnessWeight < 1f)
        {
            selectedChicken = SelectBasedOnDistanceToSlot(availableChickens, targetSlot);
        }
        else
        {
            // Pure random selection
            int randomIndex = Random.Range(0, availableChickens.Count);
            selectedChicken = availableChickens[randomIndex];
        }

        if (selectedChicken != null)
        {
            string selectionMethod = preferClosestChickenToSlot && randomnessWeight < 1f ? "distance-based" : "random";
            LogDebug($"Selected {selectedChicken.gameObject.name} using {selectionMethod} selection for slot at {targetSlot}");
        }

        return selectedChicken;
    }

    ChickenCombatBehaviorV2 SelectBasedOnDistanceToSlot(List<ChickenCombatBehaviorV2> availableChickens, Vector3 targetSlot)
    {
        // Weighted selection between closest and random
        if (Random.Range(0f, 1f) <= randomnessWeight)
        {
            // Random selection
            return availableChickens[Random.Range(0, availableChickens.Count)];
        }
        else
        {
            // Find closest chicken to target slot
            ChickenCombatBehaviorV2 closestChicken = null;
            float closestDistance = float.MaxValue;

            foreach (var chicken in availableChickens)
            {
                float distance = Vector3.Distance(chicken.transform.position, targetSlot);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestChicken = chicken;
                }
            }

            return closestChicken;
        }
    }

    void ExecuteFormationFill(ChickenCombatBehaviorV2 chicken, Vector3 targetSlot, ChickenCombatManagerV4 manager)
    {
        LogDebug($"Executing formation fill - moving {chicken.gameObject.name} to slot at {targetSlot}");
        
        // Get the chicken's state controller and registration
        var stateController = chicken.GetComponent<ChickenStateController>();
        var registration = chicken.GetComponent<EnemyChickenRegistration>();
        var movementBehavior = chicken.GetComponent<ChickenMovementBehavior>();
        
        if (stateController != null)
        {
            // Find the EnemyChickenManager to handle the reassignment
            var enemyChickenManager = FindObjectOfType<EnemyChickenManager>();
            if (enemyChickenManager != null)
            {
                // Find which slot index corresponds to our target position
                var allSlots = enemyChickenManager.formationCreator.GetFormationSlots();
                int targetSlotIndex = -1;
                
                for (int i = 0; i < allSlots.Count; i++)
                {
                    if (Vector3.Distance(allSlots[i], targetSlot) < 0.1f)
                    {
                        targetSlotIndex = i;
                        break;
                    }
                }
                
                if (targetSlotIndex != -1)
                {
                    // Get current slot assignment
                    int currentSlotIndex = registration != null ? registration.GetAssignedSlotIndex() : -1;
                    
                    // Reassign chicken to new slot by updating the manager's assignments
                    if (currentSlotIndex != -1)
                    {
                        // Chicken is currently assigned - we need to swap or move
                        GameObject currentChickenInTargetSlot = enemyChickenManager.GetChickenInSlot(targetSlotIndex);
                        
                        if (currentChickenInTargetSlot == null)
                        {
                            // Target slot is empty - simple reassignment
                            enemyChickenManager.UnregisterChicken(chicken.gameObject);
                            enemyChickenManager.RegisterChicken(chicken.gameObject);
                            
                            LogDebug($"Reassigned {chicken.gameObject.name} from slot {currentSlotIndex} to slot {targetSlotIndex}");
                        }
                        else
                        {
                            // Target slot is occupied - this shouldn't happen since we selected empty slots
                            LogWarning($"Target slot {targetSlotIndex} is occupied by {currentChickenInTargetSlot.name}! Skipping reassignment.");
                            return;
                        }
                    }
                    else
                    {
                        // Chicken is not assigned - just register it normally
                        enemyChickenManager.RegisterChicken(chicken.gameObject);
                        LogDebug($"Assigned unslotted {chicken.gameObject.name} to slot {targetSlotIndex}");
                    }
                    
                    // Force state update to trigger movement
                    if (registration != null)
                    {
                        registration.ForceStateUpdate();
                    }
                    
                    // Refresh movement to start moving to new slot
                    if (movementBehavior != null)
                    {
                        movementBehavior.RefreshMovementState();
                    }
                    
                    LogDebug($"Formation fill completed - {chicken.gameObject.name} will move to new formation slot");
                }
                else
                {
                    LogWarning($"Could not find slot index for target position {targetSlot}");
                }
            }
            else
            {
                LogWarning("No EnemyChickenManager found! Cannot execute formation fill.");
            }
        }
        else
        {
            LogWarning($"No ChickenStateController found on {chicken.gameObject.name}! Cannot execute formation fill.");
        }
    }
}

// Removed fictional interfaces - using existing system instead