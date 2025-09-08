using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "SquareFormationAttack", menuName = "Chicken Combat/Attacks/Square Formation Attack")]
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
    private List<ChickenTargetAssignment> assignments = new List<ChickenTargetAssignment>();
    private float lastShotTime = 0f;

    // Helper class to store chicken-target assignments
    [System.Serializable]
    public class ChickenTargetAssignment
    {
        public ChickenCombatBehaviorV2 chicken;
        public Vector3 targetPosition;
        public float distance;
        public bool hasBeenShot;

        public ChickenTargetAssignment(ChickenCombatBehaviorV2 chicken, Vector3 targetPosition, float distance)
        {
            this.chicken = chicken;
            this.targetPosition = targetPosition;
            this.distance = distance;
            this.hasBeenShot = false;
        }
    }

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

        // Assign chickens to optimal target positions
        AssignChickensToTargets(availableChickens);

        // Debug: Log calculated assignments
        if (showDebugLogs)
        {
            LogDebug($"Square formation calculated {assignments.Count} chicken-target assignments:");
            for (int i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                LogDebug($"  {assignment.chicken.gameObject.name} -> {assignment.targetPosition} (distance: {assignment.distance:F2})");
            }
        }

        if (simultaneousShots)
        {
            ExecuteSimultaneousShots(manager);
        }
        else
        {
            ExecuteSequentialShots(manager);
        }
    }

    private void AssignChickensToTargets(List<ChickenCombatBehaviorV2> availableChickens)
    {
        assignments.Clear();

        if (targetPositions.Count == 0 || availableChickens.Count == 0)
            return;

        // Calculate the center point of all chickens
        Vector3 chickenCenter = Vector3.zero;
        foreach (var chicken in availableChickens)
        {
            chickenCenter += chicken.transform.position;
        }
        chickenCenter /= availableChickens.Count;

        // Calculate the center point of all targets
        Vector3 targetCenter = Vector3.zero;
        foreach (var target in targetPositions)
        {
            targetCenter += target;
        }
        targetCenter /= targetPositions.Count;

        // Sort chickens by their angle relative to chicken center
        var sortedChickens = availableChickens.OrderBy(chicken =>
        {
            Vector3 direction = chicken.transform.position - chickenCenter;
            return Mathf.Atan2(direction.y, direction.x);
        }).ToList();

        // Sort targets by their angle relative to target center  
        var sortedTargets = targetPositions.OrderBy(target =>
        {
            Vector3 direction = target - targetCenter;
            return Mathf.Atan2(direction.y, direction.x);
        }).ToList();

        // Assign chickens to targets based on their angular positions
        // This ensures chickens shoot in directions that maintain the formation shape
        for (int i = 0; i < sortedTargets.Count; i++)
        {
            // Use modulo to cycle through chickens if we have more targets than chickens
            var chicken = sortedChickens[i % sortedChickens.Count];
            var target = sortedTargets[i];
            float distance = Vector3.Distance(chicken.transform.position, target);

            assignments.Add(new ChickenTargetAssignment(chicken, target, distance));
        }

        LogDebug($"Assigned {assignments.Count} spatially-ordered chicken-target pairs using angular sorting");

        // Debug: Log the assignments to verify the spatial ordering
        if (showDebugLogs)
        {
            for (int i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                Vector3 chickenDir = assignment.chicken.transform.position - chickenCenter;
                Vector3 targetDir = assignment.targetPosition - targetCenter;
                float chickenAngle = Mathf.Atan2(chickenDir.y, chickenDir.x) * Mathf.Rad2Deg;
                float targetAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

                LogDebug($"  Assignment {i}: {assignment.chicken.gameObject.name} (angle: {chickenAngle:F1}°) -> Target (angle: {targetAngle:F1}°)");
            }
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
        // PRIORITY CHANGE: Try to get bounds from PlayerBoundaryCanvas first (includes scaler)
        PlayerBoundaryCanvas boundaryCanvas = FindObjectOfType<PlayerBoundaryCanvas>();
        if (boundaryCanvas != null)
        {
            Canvas canvas = boundaryCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                RectTransform rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform != null && rectTransform.sizeDelta != Vector2.zero)
                {
                    if (showDebugLogs)
                        LogDebug($"Got canvas bounds from PlayerBoundaryCanvas: {rectTransform.sizeDelta} (includes scaler)");
                    return rectTransform.sizeDelta;
                }
            }
        }

        // Fallback: get bounds from LevelManager (raw, unscaled)
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            Vector2 playerBounds = levelManager.PlayerBoundarySize;
            if (playerBounds != Vector2.zero)
            {
                if (showDebugLogs)
                    LogDebug($"Got canvas bounds from LevelManager (fallback, no scaler): {playerBounds * 2f}");
                return playerBounds * 2f; // Convert from boundary size to full canvas size
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

    private void ExecuteSimultaneousShots(ChickenCombatManagerV4 manager)
    {
        // All assigned chickens shoot at their optimal positions simultaneously
        foreach (var assignment in assignments)
        {
            if (assignment.chicken != null && assignment.chicken.CanAttack())
            {
                ShootChickenAtPosition(assignment.chicken, assignment.targetPosition, manager.EggSpeed);
                assignment.hasBeenShot = true;
            }
        }

        LogDebug($"Executed simultaneous square formation with {assignments.Count} optimized assignments");
    }

    private void ExecuteSequentialShots(ChickenCombatManagerV4 manager)
    {
        // Check if enough time has passed for the next shot
        if (Time.time - lastShotTime < shotDelay)
        {
            return;
        }

        // Reset if we've completed a full cycle
        if (currentShotIndex >= assignments.Count)
        {
            currentShotIndex = 0;
            // Reset all assignments for next cycle
            foreach (var assignment in assignments)
            {
                assignment.hasBeenShot = false;
            }
        }

        // Find the next unshot assignment
        if (currentShotIndex < assignments.Count)
        {
            var assignment = assignments[currentShotIndex];

            if (assignment.chicken != null && assignment.chicken.CanAttack() && !assignment.hasBeenShot)
            {
                ShootChickenAtPosition(assignment.chicken, assignment.targetPosition, manager.EggSpeed);
                assignment.hasBeenShot = true;

                currentShotIndex++;
                lastShotTime = Time.time;

                LogDebug($"Sequential shot {currentShotIndex}/{assignments.Count} executed - {assignment.chicken.gameObject.name} -> {assignment.targetPosition}");
            }
            else
            {
                // Skip this assignment if chicken can't attack
                currentShotIndex++;
            }
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
        assignments.Clear();
        lastShotTime = 0f;
    }

    // Gizmo drawing for debugging
    private void OnDrawGizmosSelected()
    {
        // Draw target positions
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

        // Draw chicken-target assignments
        if (assignments != null && assignments.Count > 0)
        {
            Gizmos.color = Color.yellow;

            foreach (var assignment in assignments)
            {
                if (assignment.chicken != null)
                {
                    // Draw line from chicken to its assigned target
                    Gizmos.DrawLine(assignment.chicken.transform.position, assignment.targetPosition);

                    // Draw chicken position
                    Gizmos.color = assignment.hasBeenShot ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(assignment.chicken.transform.position, 0.3f);
                    Gizmos.color = Color.yellow;
                }
            }
        }
    }
}
