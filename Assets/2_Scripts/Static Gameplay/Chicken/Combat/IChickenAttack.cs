using UnityEngine;
using System.Collections.Generic;

// Interface that all attack types must implement
public interface IChickenAttack
{
    AttackType AttackType { get; }
    string AttackName { get; }
    float AttackInterval { get; }
    bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager);
    void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager);
}

// Base ScriptableObject class for all attack types
public abstract class BaseChickenAttackSO : ScriptableObject, IChickenAttack
{
    [Header("Base Attack Settings")]
    public float attackInterval = 1f;
    public bool showDebugLogs = true;

    public abstract AttackType AttackType { get; }
    public abstract string AttackName { get; }
    public virtual float AttackInterval => attackInterval;

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
    SpreadShot
    // Add more attack types as needed
}