using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Square Formation Attack", menuName = "Chicken Combat/Attacks/Square Formation Attack")]
public class SquareFormationAttackSO : BaseChickenAttackSO
{
    [Header("Square Formation Settings")]
    [SerializeField] private float squareSize = 5f; // Size of the square around the player
    [SerializeField] private int shotsPerSide = 3; // How many shots per side of the square (minimum 2)
    [SerializeField] private float offsetFromPlayer = 2f; // How far the square should be from the player
    [SerializeField] private bool simultaneousShots = false; // If true, all chickens shoot at once, if false they shoot in sequence
    [SerializeField] private float shotDelay = 0.1f; // Delay between shots when simultaneousShots is false

    public override AttackType AttackType => AttackType.SquareFormation;
    public override string AttackName => "Square Formation";

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
            LogWarning("Cannot execute Square Formation attack!");
            return;
        }

        // Clear any existing warnings to prevent duplicates
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.ClearAllWarnings();
            if (showDebugLogs)
                LogDebug("Cleared existing warnings before creating square formation");
        }

        // Calculate square positions around the player
        CalculateSquarePositions(manager.Player.position);

        // Debug: Log calculated positions
        if (showDebugLogs)
        {
            LogDebug($"Square formation calculated {targetPositions.Count} positions:");
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

    private void CalculateSquarePositions(Vector3 playerPosition)
    {
        targetPositions.Clear();

        // Ensure minimum shots per side
        int actualShotsPerSide = Mathf.Max(2, shotsPerSide);

        // Get canvas boundaries
        Vector2 canvasBounds = GetCanvasBounds();
        if (canvasBounds == Vector2.zero)
        {
            LogWarning("Could not get canvas bounds, using fallback square calculation");
            CalculateSquarePositionsFallback(playerPosition, actualShotsPerSide);
            return;
        }

        // Calculate the actual square bounds around the player
        float halfSize = squareSize * 0.5f;
        Vector3 squareCenter = playerPosition;

        // Add offset from player (push the square outward)
        squareCenter += Vector3.forward * offsetFromPlayer;

        // Clamp square center to be within canvas bounds
        float canvasHalfWidth = canvasBounds.x * 0.5f;
        float canvasHalfHeight = canvasBounds.y * 0.5f;

        squareCenter.x = Mathf.Clamp(squareCenter.x, -canvasHalfWidth + halfSize, canvasHalfWidth - halfSize);
        squareCenter.y = Mathf.Clamp(squareCenter.y, -canvasHalfHeight + halfSize, canvasHalfHeight - halfSize);

        // Adjust square size if it would extend beyond canvas
        float maxSquareWidth = Mathf.Min(squareSize, (canvasHalfWidth - Mathf.Abs(squareCenter.x)) * 2f);
        float maxSquareHeight = Mathf.Min(squareSize, (canvasHalfHeight - Mathf.Abs(squareCenter.y)) * 2f);
        float adjustedSquareSize = Mathf.Min(maxSquareWidth, maxSquareHeight);
        float adjustedHalfSize = adjustedSquareSize * 0.5f;

        if (showDebugLogs && adjustedSquareSize < squareSize)
        {
            LogDebug($"Square size adjusted from {squareSize} to {adjustedSquareSize} to fit canvas bounds");
        }

        // Calculate positions for each side of the square with canvas bounds

        // Top side (left to right)
        for (int i = 0; i < actualShotsPerSide; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1); // 0 to 1
            float x = Mathf.Lerp(-adjustedHalfSize, adjustedHalfSize, t);
            Vector3 position = squareCenter + new Vector3(x, adjustedHalfSize, 0f);
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }

        // Right side (top to bottom, excluding corners)
        for (int i = 1; i < actualShotsPerSide - 1; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1); // 0 to 1
            float y = Mathf.Lerp(adjustedHalfSize, -adjustedHalfSize, t);
            Vector3 position = squareCenter + new Vector3(adjustedHalfSize, y, 0f);
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }

        // Bottom side (right to left)
        for (int i = 0; i < actualShotsPerSide; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1); // 0 to 1
            float x = Mathf.Lerp(adjustedHalfSize, -adjustedHalfSize, t);
            Vector3 position = squareCenter + new Vector3(x, -adjustedHalfSize, 0f);
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }

        // Left side (bottom to top, excluding corners)
        for (int i = 1; i < actualShotsPerSide - 1; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1); // 0 to 1
            float y = Mathf.Lerp(-adjustedHalfSize, adjustedHalfSize, t);
            Vector3 position = squareCenter + new Vector3(-adjustedHalfSize, y, 0f);
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }

        LogDebug($"Calculated {targetPositions.Count} target positions for square formation (canvas-constrained)");
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

    private void CalculateSquarePositionsFallback(Vector3 playerPosition, int actualShotsPerSide)
    {
        // Fallback calculation with a smaller, safer square size
        float safeSquareSize = Mathf.Min(squareSize, 8f); // Max 8 units to stay safe
        float halfSize = safeSquareSize * 0.5f;
        Vector3 squareCenter = playerPosition;

        // Add offset from player
        squareCenter += Vector3.forward * offsetFromPlayer;

        // Calculate positions for each side of the square (same as before but with safe size)

        // Top side (left to right)
        for (int i = 0; i < actualShotsPerSide; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float x = Mathf.Lerp(-halfSize, halfSize, t);
            Vector3 position = squareCenter + new Vector3(x, halfSize, 0f);
            targetPositions.Add(position);
        }

        // Right side (top to bottom, excluding corners)
        for (int i = 1; i < actualShotsPerSide - 1; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float y = Mathf.Lerp(halfSize, -halfSize, t);
            Vector3 position = squareCenter + new Vector3(halfSize, y, 0f);
            targetPositions.Add(position);
        }

        // Bottom side (right to left)
        for (int i = 0; i < actualShotsPerSide; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float x = Mathf.Lerp(halfSize, -halfSize, t);
            Vector3 position = squareCenter + new Vector3(x, -halfSize, 0f);
            targetPositions.Add(position);
        }

        // Left side (bottom to top, excluding corners)
        for (int i = 1; i < actualShotsPerSide - 1; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float y = Mathf.Lerp(-halfSize, halfSize, t);
            Vector3 position = squareCenter + new Vector3(-halfSize, y, 0f);
            targetPositions.Add(position);
        }

        LogDebug($"Used fallback calculation for {targetPositions.Count} target positions");
    }

    private void ExecuteSimultaneousShots(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        // All chickens shoot at random positions in the square simultaneously
        for (int i = 0; i < availableChickens.Count && i < targetPositions.Count; i++)
        {
            ChickenCombatBehaviorV2 chicken = availableChickens[i];
            Vector3 targetPos = targetPositions[i % targetPositions.Count];

            ShootChickenAtPosition(chicken, targetPos, manager.EggSpeed);
        }

        LogDebug($"Executed simultaneous square formation with {availableChickens.Count} chickens");
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

        // Use the new ShootEggAtPosition method to shoot at the specific square formation position
        chicken.ShootEggAtPosition(targetPosition, speed);

        LogDebug($"Chicken {chicken.gameObject.name} shooting towards square position {targetPosition}");
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
            Gizmos.color = Color.cyan;

            // Draw the target positions
            foreach (Vector3 pos in targetPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.2f);
            }

            // Draw the square outline
            if (targetPositions.Count >= 4)
            {
                for (int i = 0; i < targetPositions.Count; i++)
                {
                    int nextIndex = (i + 1) % targetPositions.Count;
                    Gizmos.DrawLine(targetPositions[i], targetPositions[nextIndex]);
                }
            }
        }
    }
}