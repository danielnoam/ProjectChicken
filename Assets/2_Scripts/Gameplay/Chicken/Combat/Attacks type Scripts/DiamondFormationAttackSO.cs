using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "DiamondFormationAttack", menuName = "Chicken Combat/Attacks/Diamond Formation Attack")]
public class DiamondFormationAttackSO : BaseChickenAttackSO
{
    [Header("Diamond Formation Settings")]
    [SerializeField] private float diamondSize = 5f; // Size of the diamond around the player
    [SerializeField] private int shotsPerSide = 3; // How many shots per side of the diamond (minimum 2)
    [SerializeField] private float offsetFromPlayer = 2f; // How far the diamond should be from the player
    [SerializeField] private bool simultaneousShots = false; // If true, all chickens shoot at once, if false they shoot in sequence
    [SerializeField] private float shotDelay = 0.1f; // Delay between shots when simultaneousShots is false
    [SerializeField] private float rotationOffset = 0f; // Rotation offset in degrees to rotate the entire diamond pattern

    public override AttackType AttackType => AttackType.DiamondFormation;
    public override string AttackName => "Diamond Formation";

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
            LogWarning("Cannot execute Diamond Formation attack!");
            return;
        }

        // Clear any existing warnings to prevent duplicates
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.ClearAllWarnings();
            if (showDebugLogs)
                LogDebug("Cleared existing warnings before creating diamond formation");
        }

        // Calculate diamond positions around the player
        CalculateDiamondPositions(manager.Player.position);

        // Assign chickens to optimal target positions
        AssignChickensToTargets(availableChickens);

        // Debug: Log calculated assignments
        if (showDebugLogs)
        {
            LogDebug($"Diamond formation calculated {assignments.Count} chicken-target assignments:");
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

    private void CalculateDiamondPositions(Vector3 playerPosition)
    {
        targetPositions.Clear();

        // Ensure minimum shots per side
        int actualShotsPerSide = Mathf.Max(2, shotsPerSide);

        // Get canvas boundaries
        Vector2 canvasBounds = GetCanvasBounds();
        if (canvasBounds == Vector2.zero)
        {
            LogWarning("Could not get canvas bounds, using fallback diamond calculation");
            CalculateDiamondPositionsFallback(playerPosition, actualShotsPerSide);
            return;
        }

        // Calculate the diamond center around the player
        Vector3 diamondCenter = playerPosition;

        // Add offset from player (push the diamond outward)
        diamondCenter += Vector3.forward * offsetFromPlayer;

        // Clamp diamond center to be within canvas bounds
        float canvasHalfWidth = canvasBounds.x * 0.5f;
        float canvasHalfHeight = canvasBounds.y * 0.5f;

        diamondCenter.x = Mathf.Clamp(diamondCenter.x, -canvasHalfWidth + diamondSize * 0.5f, canvasHalfWidth - diamondSize * 0.5f);
        diamondCenter.y = Mathf.Clamp(diamondCenter.y, -canvasHalfHeight + diamondSize * 0.5f, canvasHalfHeight - diamondSize * 0.5f);

        // Adjust diamond size if it would extend beyond canvas
        float maxDiamondWidth = Mathf.Min(diamondSize, (canvasHalfWidth - Mathf.Abs(diamondCenter.x)) * 2f);
        float maxDiamondHeight = Mathf.Min(diamondSize, (canvasHalfHeight - Mathf.Abs(diamondCenter.y)) * 2f);
        float adjustedDiamondSize = Mathf.Min(maxDiamondWidth, maxDiamondHeight);
        float adjustedHalfSize = adjustedDiamondSize * 0.5f;

        if (showDebugLogs && adjustedDiamondSize < diamondSize)
        {
            LogDebug($"Diamond size adjusted from {diamondSize} to {adjustedDiamondSize} to fit canvas bounds");
        }

        // Calculate diamond vertices (rotated square - points at cardinal directions)
        List<Vector3> vertices = new List<Vector3>();

        // Base diamond vertices (top, right, bottom, left)
        Vector3[] baseVertices = new Vector3[]
        {
            new Vector3(0f, adjustedHalfSize, 0f),      // Top
            new Vector3(adjustedHalfSize, 0f, 0f),      // Right
            new Vector3(0f, -adjustedHalfSize, 0f),     // Bottom
            new Vector3(-adjustedHalfSize, 0f, 0f)      // Left
        };

        // Apply rotation offset if specified
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];

            if (rotationOffset != 0f)
            {
                float angle = rotationOffset * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float newX = vertex.x * cos - vertex.y * sin;
                float newY = vertex.x * sin + vertex.y * cos;

                vertex = new Vector3(newX, newY, vertex.z);
            }

            vertex += diamondCenter;
            vertex = ClampPositionToCanvas(vertex, canvasBounds);
            vertices.Add(vertex);
        }

        // Generate shots along each side of the diamond
        for (int side = 0; side < 4; side++)
        {
            Vector3 startVertex = vertices[side];
            Vector3 endVertex = vertices[(side + 1) % 4];

            // Place shots along this side
            for (int shot = 0; shot < actualShotsPerSide; shot++)
            {
                float t = (float)shot / (actualShotsPerSide - 1); // 0 to 1

                // Skip the end vertex to avoid duplicates (except for the last side)
                if (shot == actualShotsPerSide - 1 && side < 3)
                    continue;

                Vector3 position = Vector3.Lerp(startVertex, endVertex, t);
                position = ClampPositionToCanvas(position, canvasBounds);
                targetPositions.Add(position);
            }
        }

        LogDebug($"Calculated {targetPositions.Count} target positions for diamond formation (canvas-constrained)");
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

    private void CalculateDiamondPositionsFallback(Vector3 playerPosition, int actualShotsPerSide)
    {
        // Fallback calculation with a smaller, safer diamond size
        float safeDiamondSize = Mathf.Min(diamondSize, 8f); // Max 8 units to stay safe
        float safeHalfSize = safeDiamondSize * 0.5f;
        Vector3 diamondCenter = playerPosition;

        // Add offset from player
        diamondCenter += Vector3.forward * offsetFromPlayer;

        // Calculate diamond vertices (top, right, bottom, left)
        List<Vector3> vertices = new List<Vector3>();

        Vector3[] baseVertices = new Vector3[]
        {
            new Vector3(0f, safeHalfSize, 0f),      // Top
            new Vector3(safeHalfSize, 0f, 0f),      // Right
            new Vector3(0f, -safeHalfSize, 0f),     // Bottom
            new Vector3(-safeHalfSize, 0f, 0f)      // Left
        };

        // Apply rotation offset if specified
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];

            if (rotationOffset != 0f)
            {
                float angle = rotationOffset * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float newX = vertex.x * cos - vertex.y * sin;
                float newY = vertex.x * sin + vertex.y * cos;

                vertex = new Vector3(newX, newY, vertex.z);
            }

            vertex += diamondCenter;
            vertices.Add(vertex);
        }

        // Generate shots along each side of the diamond
        for (int side = 0; side < 4; side++)
        {
            Vector3 startVertex = vertices[side];
            Vector3 endVertex = vertices[(side + 1) % 4];

            // Place shots along this side
            for (int shot = 0; shot < actualShotsPerSide; shot++)
            {
                float t = (float)shot / (actualShotsPerSide - 1); // 0 to 1

                // Skip the end vertex to avoid duplicates (except for the last side)
                if (shot == actualShotsPerSide - 1 && side < 3)
                    continue;

                Vector3 position = Vector3.Lerp(startVertex, endVertex, t);
                targetPositions.Add(position);
            }
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

        LogDebug($"Executed simultaneous diamond formation with {assignments.Count} optimized assignments");
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

        // Use the ShootEggAtPosition method to shoot at the specific diamond formation position
        chicken.ShootEggAtPosition(targetPosition, speed);

        LogDebug($"Chicken {chicken.gameObject.name} shooting towards diamond position {targetPosition}");
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
            Gizmos.color = Color.magenta;

            // Draw the target positions
            foreach (Vector3 pos in targetPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.2f);
            }

            // Draw the diamond outline
            if (targetPositions.Count >= 4)
            {
                for (int i = 0; i < targetPositions.Count; i++)
                {
                    int nextIndex = (i + 1) % targetPositions.Count;
                    Gizmos.DrawLine(targetPositions[i], targetPositions[nextIndex]);
                }
            }

            // Draw diamond center if we have positions
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