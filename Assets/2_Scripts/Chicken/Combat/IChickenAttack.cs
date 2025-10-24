using UnityEngine;
using System.Collections.Generic;
using DNExtensions;

// Interface that all attack types must implement
public interface IChickenAttack
{
    AttackType AttackType { get; }
    string AttackName { get; }
    float AttackInterval { get; }
    int UsesBeforePatternChange { get; }
    bool DeactivateWarningCircle { get; }
    float EggSpeedMultiplier { get; } // New property for speed multiplier
    SOAudioEvent AudioEvent { get; }
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
    public int minChickensRequired = 1; // The minimum amount of chickens that needs to be registered to activate this attack

    [Header("Egg Speed Modifier")]
    [Tooltip("Multiplier applied to the base egg speed for this attack. 1.0 = normal speed, 1.5 = 50% faster, 0.5 = 50% slower")]
    [Range(0.1f, 3f)]
    public float eggSpeedMultiplier = 1f; // Multiplier for egg speed (default 1.0 means no change)

    [Header("Warning Settings")]
    public bool deactivateWarningCircle = false; // Controls whether this attack disables warning circles

    [Header("Audio")]
    public SOAudioEvent audioEvent;
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    public abstract AttackType AttackType { get; }
    public abstract string AttackName { get; }
    public virtual float AttackInterval => attackInterval;
    public virtual int UsesBeforePatternChange => usesBeforePatternChange;
    public virtual bool DeactivateWarningCircle => deactivateWarningCircle;
    public virtual float EggSpeedMultiplier => eggSpeedMultiplier;
    public virtual SOAudioEvent AudioEvent => audioEvent;

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
    FormationScramble,
    SquareFormation,
    CircleFormation,
    DiamondFormation,
    FormationShape,
    // Add more attack types as needed
}