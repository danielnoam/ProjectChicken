using UnityEngine;
using System.Collections.Generic;

// Interface that all attack types must implement
public interface IChickenAttack
{
    AttackType AttackType { get; }
    string AttackName { get; }
    float AttackInterval { get; }
    int UsesBeforePatternChange { get; }
    bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager);
    void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager);
}

// Base ScriptableObject class for all attack types
public abstract class BaseChickenAttackSO : ScriptableObject, IChickenAttack
{
    [Header("Base Attack Settings")]
    public float attackInterval = 1f; // The time in seconds that takes to attack (0.5f means 2 attacks per seconds)
    [Range(1, 20)]
    public int usesBeforePatternChange = 3; // How many times this attack can be used before triggering pattern change Cooldown
    [Range(1, 20)]
    public int minChickensRequired = 1; //The minimum amount of chickens that needs to be registered to activate this attack
    public bool showDebugLogs = true;


    public abstract AttackType AttackType { get; }
    public abstract string AttackName { get; }
    public virtual float AttackInterval => attackInterval;
    public virtual int UsesBeforePatternChange => usesBeforePatternChange;

    public abstract bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager);
    public abstract void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager);

    protected virtual void LogDebug(string message)
    {
        if (showDebugLogs)
            Debug.Log($"{AttackName}: {message}");
    }

    protected virtual void LogWarning(string message)
    {
        if (showDebugLogs)
            Debug.LogWarning($"{AttackName}: {message}");
    }
}

// Attack type enum (can be expanded)
public enum AttackType
{
    None,
    BurstFire,
    SingleFire,
    RapidFire,
    SniperShot,
    SpreadShot,
    FormationFill
    // Add more attack types as needed
}