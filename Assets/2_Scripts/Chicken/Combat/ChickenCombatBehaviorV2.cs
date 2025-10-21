using System;
using DNExtensions;
using UnityEngine;

public class ChickenCombatBehaviorV2 : MonoBehaviour
{
    [Header("Egg Shooting Settings")]
    public GameObject eggPrefab;
    public Transform eggSpawnPoint; // Where eggs spawn from (can be empty transform as child)

    [Header("Debug")]
    public bool showDebugLogs = true; // Enable by default to help troubleshoot

    private ChickenStateController stateController;
    private ChickenAnimationController animationController; // NEW: Animation controller reference
    private Transform player;

    void Start()
    {
        stateController = GetComponent<ChickenStateController>();
        animationController = GetComponent<ChickenAnimationController>(); // NEW: Get animation controller

        if (stateController == null)
        {
            Debug.LogError($"ChickenCombatBehaviorV2 on {gameObject.name}: No ChickenStateController found!");
        }

        // NEW: Check for animation controller
        if (animationController == null)
        {
            Debug.LogWarning($"ChickenCombatBehaviorV2 on {gameObject.name}: No ChickenAnimationController found - attack animations will not play!");
        }

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // If no egg spawn point assigned, use this transform
        if (eggSpawnPoint == null)
        {
            eggSpawnPoint = transform;
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Using chicken transform as egg spawn point");
        }

        if (eggPrefab == null)
        {
            Debug.LogError($"ChickenCombatBehaviorV2 on {gameObject.name}: No egg prefab assigned!");
        }
    }

    public bool CanAttack()
    {
        // Can only attack when following slot
        if (stateController == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"ChickenCombatBehaviorV2 on {gameObject.name}: CanAttack = FALSE - No state controller");
            return false;
        }

        if (player == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"ChickenCombatBehaviorV2 on {gameObject.name}: CanAttack = FALSE - No player found");
            return false;
        }

        bool isFollowingSlot = stateController.IsFollowingSlot;
        if (showDebugLogs)
        {
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: CanAttack check - Current State: {stateController.CurrentState}, IsFollowingSlot: {isFollowingSlot}, Result: {isFollowingSlot}");
        }

        return isFollowingSlot;
    }

    public void ShootEgg(float speed, bool deactivateWarning)
    {
        if (showDebugLogs)
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: ShootEgg() called with speed {speed}!");

        if (!CanAttack())
        {
            if (showDebugLogs)
                Debug.LogWarning($"ChickenCombatBehaviorV2 on {gameObject.name}: Cannot attack - State: {(stateController != null ? stateController.CurrentState.ToString() : "null")}");
            return;
        }

        // NEW: Trigger attack animation
        if (animationController != null)
        {
            animationController.PlayAttackAnimation();
            if (showDebugLogs)
                Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Triggered attack animation");
        }

        if (eggPrefab == null)
        {
            Debug.LogError($"ChickenCombatBehaviorV2 on {gameObject.name}: No egg prefab assigned!");
            return;
        }

        if (player == null)
        {
            Debug.LogError($"ChickenCombatBehaviorV2 on {gameObject.name}: No player target!");
            return;
        }

        // Calculate direction to player
        Vector3 targetPosition = player.position;
        Vector3 shootDirection = (targetPosition - eggSpawnPoint.position).normalized;

        if (showDebugLogs)
        {
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Shooting egg!");
            Debug.Log($"  Spawn Position: {eggSpawnPoint.position}");
            Debug.Log($"  Target Position: {targetPosition}");
            Debug.Log($"  Shoot Direction: {shootDirection}");
            Debug.Log($"  Egg Speed: {speed}");
        }

        // Spawn egg
        GameObject egg = ObjectPooler.GetObjectFromPool(eggPrefab, eggSpawnPoint.position, Quaternion.LookRotation(shootDirection));

        if (egg == null)
        {
            Debug.LogError($"ChickenCombatBehaviorV2 on {gameObject.name}: Failed to instantiate egg!");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Egg instantiated successfully: {egg.name}");

        // Set egg velocity
        ChickenEggV2 eggScript = egg.GetComponent<ChickenEggV2>();
        if (eggScript != null )
        {
            eggScript.Initialize(shootDirection, speed);
            if (showDebugLogs)
                Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Egg initialized with ChickenEggV2 script");

            // Create warning at the exact target position AFTER initializing egg
            if (EggWarningSystem.Instance != null && !deactivateWarning)
            {
                EggWarningSystem.Instance.CreateWarningAtTarget(eggScript, targetPosition);
                if (showDebugLogs)
                    Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Created warning at target position {targetPosition}");
            }

        }
        else
        {
            // Fallback: use rigidbody if no ChickenEgg script
            Rigidbody rb = egg.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = shootDirection * speed;
                if (showDebugLogs)
                    Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Egg velocity set via Rigidbody: {rb.linearVelocity}");
            }
            else
            {
                Debug.LogWarning($"ChickenCombatBehaviorV2 on {gameObject.name}: Egg has no ChickenEggV2 script or Rigidbody - it won't move!");
            }
        }

        if (showDebugLogs)
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: EGG SHOT SUCCESSFULLY!");
    }

    // Shoots an egg towards a specific target position instead of the player
    public void ShootEggAtPosition(Vector3 targetPosition, float speed, bool deactivateWarning)
    {
        if (showDebugLogs)
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: ShootEggAtPosition() called with target {targetPosition} and speed {speed}!");

        if (!CanAttack())
        {
            if (showDebugLogs)
                Debug.LogWarning($"ChickenCombatBehaviorV2 on {gameObject.name}: Cannot attack - State: {(stateController != null ? stateController.CurrentState.ToString() : "null")}");
            return;
        }

        // NEW: Trigger attack animation
        if (animationController != null)
        {
            animationController.PlayAttackAnimation();
            if (showDebugLogs)
                Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Triggered attack animation");
        }

        if (eggPrefab == null)
        {
            Debug.LogError($"ChickenCombatBehaviorV2 on {gameObject.name}: No egg prefab assigned!");
            return;
        }

        // Calculate direction to target position
        Vector3 shootDirection = (targetPosition - eggSpawnPoint.position).normalized;

        if (showDebugLogs)
        {
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Shooting egg at custom position!");
            Debug.Log($"  Spawn Position: {eggSpawnPoint.position}");
            Debug.Log($"  Target Position: {targetPosition}");
            Debug.Log($"  Shoot Direction: {shootDirection}");
            Debug.Log($"  Egg Speed: {speed}");
        }

        // Spawn egg
        GameObject egg = ObjectPooler.GetObjectFromPool(eggPrefab, eggSpawnPoint.position, Quaternion.LookRotation(shootDirection));

        if (egg == null)
        {
            Debug.LogError($"ChickenCombatBehaviorV2 on {gameObject.name}: Failed to instantiate egg!");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Egg instantiated successfully: {egg.name}");

        // Set egg velocity
        ChickenEggV2 eggScript = egg.GetComponent<ChickenEggV2>();
        if (eggScript != null)
        {
            // IMPORTANT: Initialize with skipWarning = true to prevent automatic warnings
            eggScript.Initialize(shootDirection, speed, true);

            // Create warning at the exact target position AFTER initializing egg
            if (EggWarningSystem.Instance != null && !deactivateWarning)
            {
                EggWarningSystem.Instance.CreateWarningAtTarget(eggScript, targetPosition);
                if (showDebugLogs)
                    Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Created warning at target position {targetPosition}");
            }

            if (showDebugLogs)
                Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Egg initialized with ChickenEggV2 script (no auto warning)");
        }
      
    }

    // Public properties for the combat manager
    public bool IsReadyToAttack => CanAttack();
    public Transform Player => player;
    public ChickenAnimationController AnimationController => animationController; // NEW: Expose animation controller
}