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
    private Transform player;

    void Start()
    {
        stateController = GetComponent<ChickenStateController>();

        if (stateController == null)
        {
            Debug.LogError($"ChickenCombatBehaviorV2 on {gameObject.name}: No ChickenStateController found!");
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

    public void ShootEgg(float speed)
    {
        if (showDebugLogs)
            Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: ShootEgg() called with speed {speed}!");

        if (!CanAttack())
        {
            if (showDebugLogs)
                Debug.LogWarning($"ChickenCombatBehaviorV2 on {gameObject.name}: Cannot attack - State: {(stateController != null ? stateController.CurrentState.ToString() : "null")}");
            return;
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
        if (eggScript != null)
        {
            eggScript.Initialize(shootDirection, speed);
            if (showDebugLogs)
                Debug.Log($"ChickenCombatBehaviorV2 on {gameObject.name}: Egg initialized with ChickenEggV2 script");
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

    // Public properties for the combat manager
    public bool IsReadyToAttack => CanAttack();
    public Transform Player => player;

    // Context menu for testing
    [ContextMenu("Test Shoot Egg")]
    void ContextMenuShootEgg()
    {
        // Use default speed for testing since speed comes from manager now
        float testSpeed = 10f;
        ShootEgg(testSpeed);
    }
}