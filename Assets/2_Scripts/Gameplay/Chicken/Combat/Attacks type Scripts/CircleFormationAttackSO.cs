using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Circle Formation Attack", menuName = "Chicken Combat/Attacks/Circle Formation Attack")]
public class CircleFormationAttackSO : BaseChickenAttackSO
{
    [Header("Circle Formation Settings")]
    [SerializeField] private float circleRadius = 4f; // Radius of the circle around the player
    [SerializeField] private int numberOfShots = 8; // How many shots around the circle (minimum 3)
    [SerializeField] private float offsetFromPlayer = 2f; // How far the circle should be from the player
    [SerializeField] private bool simultaneousShots = false; // If true, all chickens shoot at once, if false they shoot in sequence
    [SerializeField] private float shotDelay = 0.1f; // Delay between shots when simultaneousShots is false
    [SerializeField] private float rotationOffset = 0f; // Rotation offset in degrees to rotate the entire circle pattern

    public override AttackType AttackType => AttackType.CircleFormation;
    public override string AttackName => "Circle Formation";

    private int currentShotIndex = 0;
    private List<Vector3> targetPositions = new List<Vector3>();
    private float lastShotTime = 0f;

    public override bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        if (availableChickens == null || availableChickens.Count < minChickensRequired)
        {
            LogDebug($"Not enough chickens available. Required: {minChickensRequired}, Available: {(availableChickens?.Count ?? 0)}");
            return false;
        }

        if (manager.Player == null)
        {
            LogWarning("No player target found!");
            return false;
        }

        return true;
    }

    public override void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        if (!CanExecute(availableChickens, manager))
        {
            LogWarning("Cannot execute Circle Formation attack!");
            return;
        }

        // Clear any existing warnings to prevent duplicates
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.ClearAllWarnings();
            if (showDebugLogs)
                LogDebug("Cleared existing warnings before creating circle formation");
        }

        // Calculate circle positions around the player
        CalculateCirclePositions(manager.Player.position);

        // Debug: Log calculated positions
        if (showDebugLogs)
        {
            LogDebug($"Circle formation calculated {targetPositions.Count} positions:");
            for (int i = 0; i < targetPositions.Count; i++)
            {
                LogDebug($"  Position {i}: {targetPositions[i]}");
            }
        }

        if (simultaneousShots)
        {
            ExecuteSimultaneousShots(availableChickens, manager);
        }
        else
        {
            ExecuteSequentialShots(availableChickens, manager);
        }
    }

    private void CalculateCirclePositions(Vector3 playerPosition)
    {
        targetPositions.Clear();

        // Ensure minimum shots around circle
        int actualNumberOfShots = Mathf.Max(3, numberOfShots);

        // Get canvas boundaries
        Vector2 canvasBounds = GetCanvasBounds();
        if (canvasBounds == Vector2.zero)
        {
            LogWarning("Could not get canvas bounds, using fallback circle calculation");
            CalculateCirclePositionsFallback(playerPosition, actualNumberOfShots);
            return;
        }

        // Calculate the circle center around the player
        Vector3 circleCenter = playerPosition;

        // Add offset from player (push the circle outward)
        circleCenter += Vector3.forward * offsetFromPlayer;

        // Clamp circle center to be within canvas bounds
        float canvasHalfWidth = canvasBounds.x * 0.5f;
        float canvasHalfHeight = canvasBounds.y * 0.5f;

        // Calculate maximum allowed radius based on canvas bounds
        float maxAllowedRadius = Mathf.Min(
            canvasHalfWidth - Mathf.Abs(circleCenter.x),
            canvasHalfHeight - Mathf.Abs(circleCenter.y)
        );

        // Adjust circle radius if it would extend beyond canvas
        float adjustedRadius = Mathf.Min(circleRadius, maxAllowedRadius);

        if (showDebugLogs && adjustedRadius < circleRadius)
        {
            LogDebug($"Circle radius adjusted from {circleRadius} to {adjustedRadius} to fit canvas bounds");
        }

        // Calculate positions around the circle
        float angleStep = 360f / actualNumberOfShots;
        float baseRotation = rotationOffset; // Apply rotation offset

        for (int i = 0; i < actualNumberOfShots; i++)
        {
            // Calculate angle in radians
            float angle = (baseRotation + (i * angleStep)) * Mathf.Deg2Rad;

            // Calculate position using trigonometry
            float x = Mathf.Cos(angle) * adjustedRadius;
            float y = Mathf.Sin(angle) * adjustedRadius;

            Vector3 position = circleCenter + new Vector3(x, y, 0f);

            // Clamp position to canvas bounds
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }

        LogDebug($"Calculated {targetPositions.Count} target positions for circle formation (canvas-constrained)");
    }

    private Vector2 GetCanvasBounds()
    {
        // Try to get bounds from LevelManager first
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            Vector2 playerBounds = levelManager.PlayerBoundarySize;
            if (playerBounds != Vector2.zero)
            {
                return playerBounds * 2f; // Convert from boundary size to full canvas size
            }
        }

        // Fallback: try to get bounds from PlayerBoundaryCanvas
        PlayerBoundaryCanvas boundaryCanvas = FindObjectOfType<PlayerBoundaryCanvas>();
        if (boundaryCanvas != null)
        {
            Canvas canvas = boundaryCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                RectTransform rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    return rectTransform.sizeDelta;
                }
            }
        }

        LogWarning("Could not determine canvas bounds - no LevelManager or PlayerBoundaryCanvas found");
        return Vector2.zero;
    }

    private Vector3 ClampPositionToCanvas(Vector3 position, Vector2 canvasBounds)
    {
        float canvasHalfWidth = canvasBounds.x * 0.5f;
        float canvasHalfHeight = canvasBounds.y * 0.5f;

        position.x = Mathf.Clamp(position.x, -canvasHalfWidth, canvasHalfWidth);
        position.y = Mathf.Clamp(position.y, -canvasHalfHeight, canvasHalfHeight);

        return position;
    }

    private void CalculateCirclePositionsFallback(Vector3 playerPosition, int actualNumberOfShots)
    {
        // Fallback calculation with a smaller, safer circle radius
        float safeRadius = Mathf.Min(circleRadius, 6f); // Max 6 units to stay safe
        Vector3 circleCenter = playerPosition;

        // Add offset from player
        circleCenter += Vector3.forward * offsetFromPlayer;

        // Calculate positions around the circle
        float angleStep = 360f / actualNumberOfShots;
        float baseRotation = rotationOffset;

        for (int i = 0; i < actualNumberOfShots; i++)
        {
            // Calculate angle in radians
            float angle = (baseRotation + (i * angleStep)) * Mathf.Deg2Rad;

            // Calculate position using trigonometry
            float x = Mathf.Cos(angle) * safeRadius;
            float y = Mathf.Sin(angle) * safeRadius;

            Vector3 position = circleCenter + new Vector3(x, y, 0f);
            targetPositions.Add(position);
        }

        LogDebug($"Used fallback calculation for {targetPositions.Count} target positions");
    }

    private void ExecuteSimultaneousShots(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        // All chickens shoot at positions in the circle simultaneously
        for (int i = 0; i < availableChickens.Count && i < targetPositions.Count; i++)
        {
            ChickenCombatBehaviorV2 chicken = availableChickens[i];
            Vector3 targetPos = targetPositions[i % targetPositions.Count];

            ShootChickenAtPosition(chicken, targetPos, manager.EggSpeed);
        }

        LogDebug($"Executed simultaneous circle formation with {availableChickens.Count} chickens");
    }

    private void ExecuteSequentialShots(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        // Check if enough time has passed for the next shot
        if (Time.time - lastShotTime < shotDelay)
        {
            return;
        }

        // Reset if we've completed a full cycle
        if (currentShotIndex >= targetPositions.Count)
        {
            currentShotIndex = 0;
        }

        // Find an available chicken to shoot
        if (availableChickens.Count > 0 && currentShotIndex < targetPositions.Count)
        {
            // Use different chickens in rotation
            ChickenCombatBehaviorV2 chicken = availableChickens[currentShotIndex % availableChickens.Count];
            Vector3 targetPos = targetPositions[currentShotIndex];

            ShootChickenAtPosition(chicken, targetPos, manager.EggSpeed);

            currentShotIndex++;
            lastShotTime = Time.time;

            LogDebug($"Sequential shot {currentShotIndex}/{targetPositions.Count} executed");
        }
    }

    private void ShootChickenAtPosition(ChickenCombatBehaviorV2 chicken, Vector3 targetPosition, float speed)
    {
        if (chicken == null || !chicken.CanAttack())
        {
            LogWarning($"Chicken {chicken?.gameObject.name ?? "null"} cannot attack!");
            return;
        }

        // Use the ShootEggAtPosition method to shoot at the specific circle formation position
        chicken.ShootEggAtPosition(targetPosition, speed);

        LogDebug($"Chicken {chicken.gameObject.name} shooting towards circle position {targetPosition}");
    }

    // Reset attack state when pattern changes
    public void ResetAttackState()
    {
        currentShotIndex = 0;
        targetPositions.Clear();
        lastShotTime = 0f;
    }

    // Gizmo drawing for debugging
    private void OnDrawGizmosSelected()
    {
        if (targetPositions != null && targetPositions.Count > 0)
        {
            Gizmos.color = Color.green;

            // Draw the target positions
            foreach (Vector3 pos in targetPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.2f);
            }

            // Draw the circle outline
            if (targetPositions.Count >= 3)
            {
                for (int i = 0; i < targetPositions.Count; i++)
                {
                    int nextIndex = (i + 1) % targetPositions.Count;
                    Gizmos.DrawLine(targetPositions[i], targetPositions[nextIndex]);
                }
            }

            // Draw circle center if we have positions
            if (targetPositions.Count > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (Vector3 pos in targetPositions)
                {
                    center += pos;
                }
                center /= targetPositions.Count;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(center, 0.3f);
            }
        }
    }
}