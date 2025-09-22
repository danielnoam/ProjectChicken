using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Scramble Formation Attack", menuName = "Chicken Combat/Attacks/Scramble Formation")]
public class ScrambleFormationSO : BaseChickenAttackSO
{
    [Header("Scramble Formation Settings")]
    [Tooltip("Minimum time between scramble attacks to prevent spam")]
    public float scrambleCooldown = 5f;
    
    [Tooltip("Whether to force state updates on all chickens after scrambling")]
    public bool forceStateUpdate = true;
    
    [Tooltip("Play scramble effect/animation when executing")]
    public bool playScrambleEffect = true;

    public override AttackType AttackType => AttackType.FormationScramble;
    public override string AttackName => "Formation Scramble";

    public override bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        // Check if we have minimum required chickens
        if (availableChickens == null || availableChickens.Count < minChickensRequired)
        {
            LogDebug($"Not enough chickens available. Required: {minChickensRequired}, Available: {(availableChickens?.Count ?? 0)}");
            return false;
        }

        // Try to find the EnemyChickenManager in the scene
        EnemyChickenManager chickenManager = FindEnemyChickenManager();
        if (chickenManager == null)
        {
            LogWarning("No EnemyChickenManager found in scene. Cannot execute scramble.");
            return false;
        }

        // Check if there are chickens registered to scramble
        if (chickenManager.TotalRegisteredChickens == 0)
        {
            LogDebug("No chickens registered in EnemyChickenManager to scramble.");
            return false;
        }

        // Check if formation is properly initialized
        if (!chickenManager.HasInitializedFormation)
        {
            LogDebug("Formation not yet initialized. Cannot scramble.");
            return false;
        }

        LogDebug($"Can execute scramble - {chickenManager.TotalRegisteredChickens} chickens available to scramble.");
        return true;
    }

    public override void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        LogDebug("Executing Formation Scramble attack on assigned chickens only...");

        // Find the EnemyChickenManager
        EnemyChickenManager chickenManager = FindEnemyChickenManager();
        if (chickenManager == null)
        {
            LogWarning("Failed to execute: No EnemyChickenManager found!");
            return;
        }

        // Execute the scramble on only assigned chickens
        chickenManager.ScrambleAssignedChickens();

        // Play scramble effect if enabled
        if (playScrambleEffect)
        {
            PlayScrambleEffect(chickenManager);
        }

        LogDebug($"Formation scramble completed! {chickenManager.AssignedChickensCount} chickens reassigned to new formation positions. {chickenManager.WaitingChickensCount} chickens remained idle.");
    }

    // Helper method to find EnemyChickenManager in the scene
    private EnemyChickenManager FindEnemyChickenManager()
    {
        // First try to find by tag (if you use tags)
        GameObject managerObject = GameObject.FindGameObjectWithTag("EnemyChickenManager");
        if (managerObject != null)
        {
            return managerObject.GetComponent<EnemyChickenManager>();
        }

        // Otherwise search by component type
        return FindFirstObjectByType<EnemyChickenManager>();
    }

    // Optional: Add visual/audio feedback for scramble
    private void PlayScrambleEffect(EnemyChickenManager chickenManager)
    {
        // You can add particle effects, sounds, screen shake, etc. here
        LogDebug("Playing scramble visual effect...");
        
        // Example: Add a brief camera shake or particle effect
        // CameraShake.Instance?.Shake(0.3f, 0.1f);
        // ParticleSystem scrambleEffect = chickenManager.GetComponent<ParticleSystem>();
        // if (scrambleEffect != null) scrambleEffect.Play();
    }
}