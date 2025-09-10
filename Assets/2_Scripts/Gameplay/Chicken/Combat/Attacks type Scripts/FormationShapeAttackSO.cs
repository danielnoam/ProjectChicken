using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "FormationShapeAttack", menuName = "Chicken Combat/Attacks/Formation Shape Attack")]
public class FormationShapeAttackSO : BaseChickenAttackSO
{
    [Header("Formation Shape Attack Settings")]
    [Tooltip("If true, will automatically detect formation shape from FormationCreator. If false, uses specified formation type.")]
    [SerializeField] private bool autoDetectFormation = true;
    [SerializeField] private FormationCreator.FormationType manualFormationType = FormationCreator.FormationType.Circle;

    [Header("General Formation Settings")]
    [SerializeField] private float offsetFromPlayer = 2f; // How far the formation should be from the player
    [SerializeField] private bool simultaneousShots = false; // If true, all chickens shoot at once, if false they shoot in sequence
    [SerializeField] private float shotDelay = 0.1f; // Delay between shots when simultaneousShots is false

    [Header("Circle Formation Settings")]
    [SerializeField] private float circleRadius = 4f; // Radius of the circle around the player
    [SerializeField] private int circleNumberOfShots = 8; // How many shots around the circle (minimum 3)
    [SerializeField] private float circleRotationOffset = 0f; // Rotation offset in degrees

    [Header("Square Formation Settings")]
    [SerializeField] private float squareSize = 5f; // Size of the square around the player
    [SerializeField] private int squareShotsPerSide = 3; // How many shots per side of the square (minimum 2)

    [Header("Diamond Formation Settings")]
    [SerializeField] private float diamondSize = 5f; // Size of the diamond around the player
    [SerializeField] private int diamondShotsPerSide = 3; // How many shots per side of the diamond (minimum 2)
    [SerializeField] private float diamondRotationOffset = 0f; // Rotation offset in degrees

    public override AttackType AttackType => AttackType.FormationShape;
    public override string AttackName => "Formation Shape Attack";

    private int currentShotIndex = 0;
    private List<Vector3> targetPositions = new List<Vector3>();
    private List<ChickenTargetAssignment> assignments = new List<ChickenTargetAssignment>();
    private float lastShotTime = 0f;
    private FormationCreator.FormationType detectedFormationType;

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

        // Try to detect formation type if auto-detect is enabled
        if (autoDetectFormation)
        {
            FormationCreator formationCreator = FindObjectOfType<FormationCreator>();
            if (formationCreator != null)
            {
                detectedFormationType = formationCreator.currentFormation;
                if (showDebugLogs)
                    LogDebug($"Auto-detected formation type: {detectedFormationType}");
            }
            else
            {
                detectedFormationType = manualFormationType;
                if (showDebugLogs)
                    LogDebug($"No FormationCreator found, using manual formation type: {detectedFormationType}");
            }
        }
        else
        {
            detectedFormationType = manualFormationType;
            if (showDebugLogs)
                LogDebug($"Using manual formation type: {detectedFormationType}");
        }

        return true;
    }

    public override void Execute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        if (!CanExecute(availableChickens, manager))
        {
            LogWarning("Cannot execute Formation Shape attack!");
            return;
        }

        // Clear any existing warnings to prevent duplicates
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.ClearAllWarnings();
            if (showDebugLogs)
                LogDebug($"Cleared existing warnings before creating {detectedFormationType} formation");
        }

        // Calculate positions based on detected formation type
        CalculateFormationPositions(manager.Player.position);

        // Assign chickens to optimal target positions
        AssignChickensToTargets(availableChickens);

        // Debug: Log calculated assignments
        if (showDebugLogs)
        {
            LogDebug($"{detectedFormationType} formation calculated {assignments.Count} chicken-target assignments:");
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

    private void CalculateFormationPositions(Vector3 playerPosition)
    {
        targetPositions.Clear();

        switch (detectedFormationType)
        {
            case FormationCreator.FormationType.Circle:
                CalculateCirclePositions(playerPosition);
                break;
            case FormationCreator.FormationType.Square:
                CalculateSquarePositions(playerPosition);
                break;
            default:
                // Default to circle if unknown formation type
                LogWarning($"Unknown formation type: {detectedFormationType}, defaulting to Circle");
                CalculateCirclePositions(playerPosition);
                break;
        }

        LogDebug($"Calculated {targetPositions.Count} target positions for {detectedFormationType} formation");
    }

    private void CalculateCirclePositions(Vector3 playerPosition)
    {
        // Ensure minimum shots around circle
        int actualNumberOfShots = Mathf.Max(3, circleNumberOfShots);

        // Get canvas boundaries
        Vector2 canvasBounds = GetCanvasBounds();
        if (canvasBounds == Vector2.zero)
        {
            LogWarning("Could not get canvas bounds, using fallback circle calculation");
            CalculateCirclePositionsFallback(playerPosition, actualNumberOfShots);
            return;
        }

        // Calculate the circle center around the player
        Vector3 circleCenter = playerPosition + Vector3.forward * offsetFromPlayer;

        // Clamp circle center and adjust radius for canvas bounds
        float canvasHalfWidth = canvasBounds.x * 0.5f;
        float canvasHalfHeight = canvasBounds.y * 0.5f;

        circleCenter.x = Mathf.Clamp(circleCenter.x, -canvasHalfWidth + circleRadius, canvasHalfWidth - circleRadius);
        circleCenter.y = Mathf.Clamp(circleCenter.y, -canvasHalfHeight + circleRadius, canvasHalfHeight - circleRadius);

        float maxAllowedRadius = Mathf.Min(
            canvasHalfWidth - Mathf.Abs(circleCenter.x),
            canvasHalfHeight - Mathf.Abs(circleCenter.y)
        );

        float adjustedRadius = Mathf.Min(circleRadius, maxAllowedRadius);

        // Calculate positions around the circle
        float angleStep = 360f / actualNumberOfShots;
        float baseRotation = circleRotationOffset;

        for (int i = 0; i < actualNumberOfShots; i++)
        {
            float angle = (baseRotation + (i * angleStep)) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * adjustedRadius;
            float y = Mathf.Sin(angle) * adjustedRadius;

            Vector3 position = circleCenter + new Vector3(x, y, 0f);
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }
    }

    private void CalculateCirclePositionsFallback(Vector3 playerPosition, int actualNumberOfShots)
    {
        float safeRadius = Mathf.Min(circleRadius, 6f);
        Vector3 circleCenter = playerPosition + Vector3.forward * offsetFromPlayer;

        float angleStep = 360f / actualNumberOfShots;
        float baseRotation = circleRotationOffset;

        for (int i = 0; i < actualNumberOfShots; i++)
        {
            float angle = (baseRotation + (i * angleStep)) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * safeRadius;
            float y = Mathf.Sin(angle) * safeRadius;

            Vector3 position = circleCenter + new Vector3(x, y, 0f);
            targetPositions.Add(position);
        }
    }

    private void CalculateSquarePositions(Vector3 playerPosition)
    {
        int actualShotsPerSide = Mathf.Max(2, squareShotsPerSide);
        Vector2 canvasBounds = GetCanvasBounds();
        
        if (canvasBounds == Vector2.zero)
        {
            CalculateSquarePositionsFallback(playerPosition, actualShotsPerSide);
            return;
        }

        float halfSize = squareSize * 0.5f;
        Vector3 squareCenter = playerPosition + Vector3.forward * offsetFromPlayer;

        // Clamp and adjust square size for canvas bounds
        float canvasHalfWidth = canvasBounds.x * 0.5f;
        float canvasHalfHeight = canvasBounds.y * 0.5f;

        squareCenter.x = Mathf.Clamp(squareCenter.x, -canvasHalfWidth + halfSize, canvasHalfWidth - halfSize);
        squareCenter.y = Mathf.Clamp(squareCenter.y, -canvasHalfHeight + halfSize, canvasHalfHeight - halfSize);

        float maxSquareWidth = Mathf.Min(squareSize, (canvasHalfWidth - Mathf.Abs(squareCenter.x)) * 2f);
        float maxSquareHeight = Mathf.Min(squareSize, (canvasHalfHeight - Mathf.Abs(squareCenter.y)) * 2f);
        float adjustedSquareSize = Mathf.Min(maxSquareWidth, maxSquareHeight);
        float adjustedHalfSize = adjustedSquareSize * 0.5f;

        // Generate square positions (same logic as SquareFormationAttackSO)
        // Top side
        for (int i = 0; i < actualShotsPerSide; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float x = Mathf.Lerp(-adjustedHalfSize, adjustedHalfSize, t);
            Vector3 position = squareCenter + new Vector3(x, adjustedHalfSize, 0f);
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }

        // Right side (excluding corners)
        for (int i = 1; i < actualShotsPerSide - 1; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float y = Mathf.Lerp(adjustedHalfSize, -adjustedHalfSize, t);
            Vector3 position = squareCenter + new Vector3(adjustedHalfSize, y, 0f);
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }

        // Bottom side
        for (int i = 0; i < actualShotsPerSide; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float x = Mathf.Lerp(adjustedHalfSize, -adjustedHalfSize, t);
            Vector3 position = squareCenter + new Vector3(x, -adjustedHalfSize, 0f);
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }

        // Left side (excluding corners)
        for (int i = 1; i < actualShotsPerSide - 1; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float y = Mathf.Lerp(-adjustedHalfSize, adjustedHalfSize, t);
            Vector3 position = squareCenter + new Vector3(-adjustedHalfSize, y, 0f);
            position = ClampPositionToCanvas(position, canvasBounds);
            targetPositions.Add(position);
        }
    }

    private void CalculateSquarePositionsFallback(Vector3 playerPosition, int actualShotsPerSide)
    {
        float safeSquareSize = Mathf.Min(squareSize, 8f);
        float halfSize = safeSquareSize * 0.5f;
        Vector3 squareCenter = playerPosition + Vector3.forward * offsetFromPlayer;

        // Same square generation logic but with safe size
        for (int i = 0; i < actualShotsPerSide; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float x = Mathf.Lerp(-halfSize, halfSize, t);
            targetPositions.Add(squareCenter + new Vector3(x, halfSize, 0f));
        }

        for (int i = 1; i < actualShotsPerSide - 1; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float y = Mathf.Lerp(halfSize, -halfSize, t);
            targetPositions.Add(squareCenter + new Vector3(halfSize, y, 0f));
        }

        for (int i = 0; i < actualShotsPerSide; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float x = Mathf.Lerp(halfSize, -halfSize, t);
            targetPositions.Add(squareCenter + new Vector3(x, -halfSize, 0f));
        }

        for (int i = 1; i < actualShotsPerSide - 1; i++)
        {
            float t = (float)i / (actualShotsPerSide - 1);
            float y = Mathf.Lerp(-halfSize, halfSize, t);
            targetPositions.Add(squareCenter + new Vector3(-halfSize, y, 0f));
        }
    }
    private void AssignChickensToTargets(List<ChickenCombatBehaviorV2> availableChickens)
    {
        assignments.Clear();

        if (targetPositions.Count == 0 || availableChickens.Count == 0)
            return;

        // Calculate center points for angular sorting
        Vector3 chickenCenter = Vector3.zero;
        foreach (var chicken in availableChickens)
        {
            chickenCenter += chicken.transform.position;
        }
        chickenCenter /= availableChickens.Count;

        Vector3 targetCenter = Vector3.zero;
        foreach (var target in targetPositions)
        {
            targetCenter += target;
        }
        targetCenter /= targetPositions.Count;

        // Sort chickens and targets by angle
        var sortedChickens = availableChickens.OrderBy(chicken =>
        {
            Vector3 direction = chicken.transform.position - chickenCenter;
            return Mathf.Atan2(direction.y, direction.x);
        }).ToList();

        var sortedTargets = targetPositions.OrderBy(target =>
        {
            Vector3 direction = target - targetCenter;
            return Mathf.Atan2(direction.y, direction.x);
        }).ToList();

        // Assign chickens to targets
        for (int i = 0; i < sortedTargets.Count; i++)
        {
            var chicken = sortedChickens[i % sortedChickens.Count];
            var target = sortedTargets[i];
            float distance = Vector3.Distance(chicken.transform.position, target);

            assignments.Add(new ChickenTargetAssignment(chicken, target, distance));
        }

        LogDebug($"Assigned {assignments.Count} spatially-ordered chicken-target pairs for {detectedFormationType} formation");
    }

    private Vector2 GetCanvasBounds()
    {
        // Try to get bounds from PlayerBoundaryCanvas first
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
                        LogDebug($"Got canvas bounds from PlayerBoundaryCanvas: {rectTransform.sizeDelta}");
                    return rectTransform.sizeDelta;
                }
            }
        }

        // Fallback: get bounds from LevelManager
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            Vector2 playerBounds = levelManager.PlayerBoundarySize;
            if (playerBounds != Vector2.zero)
            {
                if (showDebugLogs)
                    LogDebug($"Got canvas bounds from LevelManager: {playerBounds * 2f}");
                return playerBounds * 2f;
            }
        }

        LogWarning("Could not determine canvas bounds");
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

    private void ExecuteSimultaneousShots(ChickenCombatManagerV4 manager)
    {
        foreach (var assignment in assignments)
        {
            if (assignment.chicken != null && assignment.chicken.CanAttack())
            {
                ShootChickenAtPosition(assignment.chicken, assignment.targetPosition, manager.EggSpeed);
                assignment.hasBeenShot = true;
            }
        }

        LogDebug($"Executed simultaneous {detectedFormationType} formation with {assignments.Count} assignments");
    }

    private void ExecuteSequentialShots(ChickenCombatManagerV4 manager)
    {
        if (Time.time - lastShotTime < shotDelay)
            return;

        if (currentShotIndex >= assignments.Count)
        {
            currentShotIndex = 0;
            foreach (var assignment in assignments)
            {
                assignment.hasBeenShot = false;
            }
        }

        if (currentShotIndex < assignments.Count)
        {
            var assignment = assignments[currentShotIndex];

            if (assignment.chicken != null && assignment.chicken.CanAttack() && !assignment.hasBeenShot)
            {
                ShootChickenAtPosition(assignment.chicken, assignment.targetPosition, manager.EggSpeed);
                assignment.hasBeenShot = true;

                currentShotIndex++;
                lastShotTime = Time.time;

                LogDebug($"Sequential {detectedFormationType} shot {currentShotIndex}/{assignments.Count} executed");
            }
            else
            {
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

        chicken.ShootEggAtPosition(targetPosition, speed, deactivateWarningCircle);
        LogDebug($"Chicken {chicken.gameObject.name} shooting towards {detectedFormationType} position {targetPosition}");
    }

    // Reset attack state when pattern changes
    public void ResetAttackState()
    {
        targetPositions.Clear();
        assignments.Clear();
    }

    // Gizmo drawing for debugging
    private void OnDrawGizmosSelected()
    {
        if (targetPositions != null && targetPositions.Count > 0)
        {
            // Choose color based on formation type
            switch (detectedFormationType)
            {
                case FormationCreator.FormationType.Circle:
                    Gizmos.color = Color.green;
                    break;
                case FormationCreator.FormationType.Square:
                    Gizmos.color = Color.cyan;
                    break;
                case FormationCreator.FormationType.Triangle:
                    Gizmos.color = Color.blue;
                    break;
                case FormationCreator.FormationType.VShape:
                    Gizmos.color = Color.red;
                    break;
                default:
                    Gizmos.color = Color.white;
                    break;
            }

            // Draw target positions
            foreach (Vector3 pos in targetPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.2f);
            }

            // Draw formation outline
            if (targetPositions.Count >= 3)
            {
                for (int i = 0; i < targetPositions.Count; i++)
                {
                    int nextIndex = (i + 1) % targetPositions.Count;
                    Gizmos.DrawLine(targetPositions[i], targetPositions[nextIndex]);
                }
            }

            // Draw formation center
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
            Gizmos.color = Color.white;

            foreach (var assignment in assignments)
            {
                if (assignment.chicken != null)
                {
                    Gizmos.DrawLine(assignment.chicken.transform.position, assignment.targetPosition);

                    Gizmos.color = assignment.hasBeenShot ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(assignment.chicken.transform.position, 0.3f);
                    Gizmos.color = Color.white;
                }
            }
        }
    }
}