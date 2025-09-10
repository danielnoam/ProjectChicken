using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

        // Assign chickens to optimal target positions
        AssignChickensToTargets(availableChickens);

        // Debug: Log calculated assignments
        if (showDebugLogs)
        {
            LogDebug($"Circle formation calculated {assignments.Count} chicken-target assignments:");
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

                LogDebug($"  Assignment {i}: {assignment.chicken.gameObject.name} (angle: {chickenAngle:F1}�) -> Target (angle: {targetAngle:F1}�)");
            }
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

        circleCenter.x = Mathf.Clamp(circleCenter.x, -canvasHalfWidth + circleRadius, canvasHalfWidth - circleRadius);
        circleCenter.y = Mathf.Clamp(circleCenter.y, -canvasHalfHeight + circleRadius, canvasHalfHeight - circleRadius);

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

        // Calculate positions around the circle with canvas bounds
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
        // PRIORITY CHANGE: Try to get bounds from PlayerBoundaryCanvas first (includes scaler)
        PlayerBoundaryCanvas boundaryCanvas = FindFirstObjectByType<PlayerBoundaryCanvas>();
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

        LogDebug($"Executed simultaneous circle formation with {assignments.Count} optimized assignments");
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

        // Use the ShootEggAtPosition method to shoot at the specific circle formation position
        chicken.ShootEggAtPosition(targetPosition, speed, deactivateWarningCircle);

        LogDebug($"Chicken {chicken.gameObject.name} shooting towards circle position {targetPosition}");
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

        // Draw chicken-target assignments
        if (assignments != null && assignments.Count > 0)
        {
            Gizmos.color = Color.cyan;

            foreach (var assignment in assignments)
            {
                if (assignment.chicken != null)
                {
                    // Draw line from chicken to its assigned target
                    Gizmos.DrawLine(assignment.chicken.transform.position, assignment.targetPosition);

                    // Draw chicken position
                    Gizmos.color = assignment.hasBeenShot ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(assignment.chicken.transform.position, 0.3f);
                    Gizmos.color = Color.cyan;
                }
            }
        }
    }
}