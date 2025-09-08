using UnityEngine;
using System.Collections.Generic;

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

        // Debug: Log calculated positions
        if (showDebugLogs)
        {
            LogDebug($"Diamond formation calculated {targetPositions.Count} positions:");
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

    private void ExecuteSimultaneousShots(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        // All chickens shoot at positions in the diamond simultaneously
        for (int i = 0; i < availableChickens.Count && i < targetPositions.Count; i++)
        {
            ChickenCombatBehaviorV2 chicken = availableChickens[i];
            Vector3 targetPos = targetPositions[i % targetPositions.Count];

            ShootChickenAtPosition(chicken, targetPos, manager.EggSpeed);
        }

        LogDebug($"Executed simultaneous diamond formation with {availableChickens.Count} chickens");
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

        // Use the ShootEggAtPosition method to shoot at the specific diamond formation position
        chicken.ShootEggAtPosition(targetPosition, speed);

        LogDebug($"Chicken {chicken.gameObject.name} shooting towards diamond position {targetPosition}");
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
    }
}