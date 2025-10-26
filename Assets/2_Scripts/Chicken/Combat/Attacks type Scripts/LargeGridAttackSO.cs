using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[CreateAssetMenu(fileName = "LargeGridAttack", menuName = "Chicken Combat/Attacks/Large Grid Attack")]
public class LargeGridAttackSO : BaseChickenAttackSO
{
    // This attack creates a large square grid around the player with optional side grids
    // Default configuration: 4x4 main grid (16 shots) + 2x 3x3 side grids (18 shots) = 34 total shots
    // ALL grid positions are ALWAYS fired at - chickens will fire multiple times if needed
    // 
    // Chicken Requirements:
    // - minChickensRequired: Total registered chickens needed (checked against manager.TotalCombatChickens)
    // - Available chickens: Only needs at least 1 chicken that can attack to execute
    // - useAllAvailableChickens = true: Uses ALL available chickens to share the shots
    // - useAllAvailableChickens = false: Uses up to maxChickensWhenLimited chickens to share the shots
    
    [Header("Large Grid Attack Settings")]
    [Tooltip("Size of the grid (e.g., 4 means 4x4 = 16 shots)")]
    [SerializeField, Range(2, 6)] private int gridSize = 4; // NxN grid
    
    [Tooltip("The spacing between each egg in the grid")]
    [SerializeField, Range(0.3f, 2f)] private float gridSpacing = 0.8f; // Distance between grid points
    
    [Header("Targeting Settings")]
    [Tooltip("Offset the entire grid from the player position")]
    [SerializeField] private Vector3 gridOffset = Vector3.zero;
    
    [Tooltip("Random offset applied to the grid center to make it less predictable")]
    [SerializeField, Range(0f, 1f)] private float maxRandomOffset = 0.3f;
    
    [Tooltip("If true, grid will rotate randomly around the player")]
    [SerializeField] private bool randomRotation = true;
    
    [Tooltip("If true, uses ALL available chickens. If false, uses up to maxChickensWhenLimited chickens. All grid positions are always fired at regardless of chicken count.")]
    [SerializeField] private bool useAllAvailableChickens = true;
    
    [Tooltip("When useAllAvailableChickens is false, limits the attack to this many chickens")]
    [SerializeField, Range(1, 20)] private int maxChickensWhenLimited = 8;
    
    [Header("Side Grids Settings")]
    [Tooltip("Enable additional 3x3 grids on left and right sides")]
    [SerializeField] private bool enableSideGrids = true;
    
    [Tooltip("Size of the side grids")]
    [SerializeField, Range(2, 5)] private int sideGridSize = 3;
    
    [Tooltip("Distance between main grid and side grids")]
    [SerializeField, Range(0.5f, 15f)] private float sideGridDistance = 1.5f;
    
    [Tooltip("Vertical offset for side grids (positive = up, negative = down)")]
    [SerializeField, Range(-2f, 2f)] private float sideGridVerticalOffset = 0f;
    
    [Header("Boundary Settings")]
    [Tooltip("Multiplier for the player boundary size (1.0 = exact size, 2.0 = double size)")]
    [SerializeField, Range(0.5f, 3f)] private float boundarySizeScaler = 2.5f;
    
    [Tooltip("Minimum distance from boundaries when placing the grid")]
    [SerializeField] private float boundaryPadding = 1f;
    
    [Tooltip("Show the grid area in scene view")]
    [SerializeField] private bool showGridGizmo = true;
    
    public override AttackType AttackType => AttackType.FormationShape;
    public override string AttackName => "Large Grid Attack";
    
    // Grid data
    private List<Vector3> mainGridPositions = new List<Vector3>();
    private List<Vector3> leftGridPositions = new List<Vector3>();
    private List<Vector3> rightGridPositions = new List<Vector3>();
    private List<Vector3> allGridPositions = new List<Vector3>(); // Combined list for easy iteration
    private Vector3 calculatedGridCenter;
    private Vector3 leftGridCenter;
    private Vector3 rightGridCenter;
    private float cachedModifiedSpeed = 0f;
    private float currentRotation = 0f;
    
    public override bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        // Check if manager exists
        if (manager == null)
        {
            LogWarning("No combat manager found!");
            return false;
        }
        
        // Check total registered chickens against minimum required
        int totalRegisteredChickens = manager.TotalCombatChickens;
        if (totalRegisteredChickens < minChickensRequired)
        {
            LogDebug($"Not enough registered chickens. Required: {minChickensRequired}, Registered: {totalRegisteredChickens}");
            return false;
        }
        
        // Calculate how many chickens we need for the grid
        int mainGridShots = gridSize * gridSize;
        int sideGridShots = enableSideGrids ? (sideGridSize * sideGridSize * 2) : 0; // 2 side grids
        int totalShots = mainGridShots + sideGridShots;
        
        // We need at least 1 available chicken to execute the attack
        // The chickens can fire multiple shots
        int chickensNeeded = 1;
        
        // Need enough available chickens to execute
        if (availableChickens == null || availableChickens.Count < chickensNeeded)
        {
            LogDebug($"Not enough available chickens for attack. Need at least {chickensNeeded} to execute (Total shots planned: {totalShots}), Available: {(availableChickens?.Count ?? 0)}");
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
            LogWarning("Cannot execute Large Grid attack!");
            return;
        }
        
        // Cache modified speed
        cachedModifiedSpeed = manager.EggSpeed * eggSpeedMultiplier;
        
        // Clear any existing warnings
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.ClearAllWarnings();
            if (showDebugLogs)
                LogDebug("Cleared existing warnings before creating large grid");
        }
        
        // Calculate grid positions
        CalculateGridPositions(manager.Player.position);
        
        // Select chickens for the attack
        List<ChickenCombatBehaviorV2> selectedChickens;
        
        if (useAllAvailableChickens)
        {
            // Use all available chickens that can attack
            selectedChickens = availableChickens
                .Where(chicken => chicken != null && chicken.CanAttack())
                .ToList();
        }
        else
        {
            // Use a limited number of chickens (but they'll fire multiple times to cover all spots)
            int maxChickensToUse = Mathf.Min(maxChickensWhenLimited, availableChickens.Count);
            selectedChickens = availableChickens
                .Where(chicken => chicken != null && chicken.CanAttack())
                .Take(maxChickensToUse)
                .ToList();
        }
        
        // Start the attack using coroutine
        manager.StartCoroutine(ExecuteGridAttack(selectedChickens, manager));
        
        int totalPositions = allGridPositions.Count;
        int shotsPerChicken = selectedChickens.Count > 0 ? Mathf.CeilToInt((float)totalPositions / selectedChickens.Count) : 0;
        
        LogDebug($"Started large grid attack: Main {gridSize}x{gridSize}" + 
            (enableSideGrids ? $" + 2x{sideGridSize}x{sideGridSize} side grids" : "") +
            $" = {totalPositions} total positions. Using {selectedChickens.Count} chickens, ~{shotsPerChicken} shots per chicken (Total registered: {manager.TotalCombatChickens})");
    }
    
    private void CalculateGridPositions(Vector3 playerPosition)
    {
        mainGridPositions.Clear();
        leftGridPositions.Clear();
        rightGridPositions.Clear();
        allGridPositions.Clear();
        
        // Apply random offset
        Vector3 randomOffset = new Vector3(
            Random.Range(-maxRandomOffset, maxRandomOffset),
            Random.Range(-maxRandomOffset, maxRandomOffset),
            0f
        );
        
        // Calculate main grid center
        calculatedGridCenter = playerPosition + gridOffset + randomOffset;
        calculatedGridCenter.z = playerPosition.z;
        
        // Apply random rotation if enabled
        currentRotation = randomRotation ? Random.Range(0f, 360f) : 0f;
        
        // Calculate grid bounds to ensure all grids fit within player boundaries
        float mainGridHalfSize = ((gridSize - 1) * gridSpacing) * 0.5f;
        float sideGridHalfSize = ((sideGridSize - 1) * gridSpacing) * 0.5f;
        
        // Calculate total width needed if side grids are enabled
        float totalWidthNeeded = enableSideGrids ? 
            (mainGridHalfSize * 2) + (sideGridHalfSize * 2 * 2) + (sideGridDistance * 2) : 
            (mainGridHalfSize * 2);
        
        // Get boundary limits
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            Vector2 playerBounds = levelManager.PlayerBoundarySize * boundarySizeScaler;
            Vector3 boundaryCenter = levelManager.PlayerPosition;
            
            // Clamp grid center to ensure entire formation stays within bounds
            float minX = boundaryCenter.x - (playerBounds.x * 0.5f) + (totalWidthNeeded * 0.5f) + boundaryPadding;
            float maxX = boundaryCenter.x + (playerBounds.x * 0.5f) - (totalWidthNeeded * 0.5f) - boundaryPadding;
            float minY = boundaryCenter.y - (playerBounds.y * 0.5f) + mainGridHalfSize + boundaryPadding;
            float maxY = boundaryCenter.y + (playerBounds.y * 0.5f) - mainGridHalfSize - boundaryPadding;
            
            calculatedGridCenter.x = Mathf.Clamp(calculatedGridCenter.x, minX, maxX);
            calculatedGridCenter.y = Mathf.Clamp(calculatedGridCenter.y, minY, maxY);
        }
        
        // Generate main grid positions
        GenerateGridPositions(mainGridPositions, calculatedGridCenter, gridSize, gridSpacing, currentRotation);
        
        // Generate side grids if enabled
        if (enableSideGrids)
        {
            // Calculate side grid centers
            float sideOffset = mainGridHalfSize + sideGridHalfSize + sideGridDistance;
            
            // Apply rotation to side grid offset
            Vector3 leftOffset = new Vector3(-sideOffset, sideGridVerticalOffset, 0f);
            Vector3 rightOffset = new Vector3(sideOffset, sideGridVerticalOffset, 0f);
            
            if (currentRotation != 0f)
            {
                float rad = currentRotation * Mathf.Deg2Rad;
                
                // Rotate left offset
                float rotatedLeftX = leftOffset.x * Mathf.Cos(rad) - leftOffset.y * Mathf.Sin(rad);
                float rotatedLeftY = leftOffset.x * Mathf.Sin(rad) + leftOffset.y * Mathf.Cos(rad);
                leftOffset = new Vector3(rotatedLeftX, rotatedLeftY, 0f);
                
                // Rotate right offset
                float rotatedRightX = rightOffset.x * Mathf.Cos(rad) - rightOffset.y * Mathf.Sin(rad);
                float rotatedRightY = rightOffset.x * Mathf.Sin(rad) + rightOffset.y * Mathf.Cos(rad);
                rightOffset = new Vector3(rotatedRightX, rotatedRightY, 0f);
            }
            
            leftGridCenter = calculatedGridCenter + leftOffset;
            rightGridCenter = calculatedGridCenter + rightOffset;
            
            // Generate side grid positions
            GenerateGridPositions(leftGridPositions, leftGridCenter, sideGridSize, gridSpacing, currentRotation);
            GenerateGridPositions(rightGridPositions, rightGridCenter, sideGridSize, gridSpacing, currentRotation);
        }
        
        // Combine all positions
        allGridPositions.AddRange(mainGridPositions);
        allGridPositions.AddRange(leftGridPositions);
        allGridPositions.AddRange(rightGridPositions);
        
        if (showDebugLogs)
        {
            LogDebug($"Calculated grids: Main {gridSize}x{gridSize} at {calculatedGridCenter}" + 
                (enableSideGrids ? $", Left {sideGridSize}x{sideGridSize} at {leftGridCenter}, Right {sideGridSize}x{sideGridSize} at {rightGridCenter}" : "") +
                $" with rotation {currentRotation:F1}°");
            LogDebug($"Total shots: {allGridPositions.Count}");
        }
    }
    
    private void GenerateGridPositions(List<Vector3> positions, Vector3 center, int size, float spacing, float rotation)
    {
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                // Calculate position relative to grid center
                float x = (col - (size - 1) * 0.5f) * spacing;
                float y = (row - (size - 1) * 0.5f) * spacing;
                
                Vector3 localPos = new Vector3(x, y, 0f);
                
                // Apply rotation
                if (rotation != 0f)
                {
                    float rad = rotation * Mathf.Deg2Rad;
                    float rotatedX = localPos.x * Mathf.Cos(rad) - localPos.y * Mathf.Sin(rad);
                    float rotatedY = localPos.x * Mathf.Sin(rad) + localPos.y * Mathf.Cos(rad);
                    localPos = new Vector3(rotatedX, rotatedY, 0f);
                }
                
                // Add to world position
                Vector3 worldPos = center + localPos;
                positions.Add(worldPos);
            }
        }
    }
    
    private IEnumerator ExecuteGridAttack(List<ChickenCombatBehaviorV2> selectedChickens, ChickenCombatManagerV4 manager)
    {
        LogDebug($"Firing large grid formation: {allGridPositions.Count} shots simultaneously");
        
        // Always fire at all grid positions
        // Distribute shots among available chickens
        if (selectedChickens.Count > 0)
        {
            int positionsPerChicken = Mathf.CeilToInt((float)allGridPositions.Count / selectedChickens.Count);
            int positionIndex = 0;
            
            // Each chicken fires at multiple positions to cover all grid spots
            foreach (var chicken in selectedChickens)
            {
                if (chicken != null && chicken.CanAttack())
                {
                    // This chicken fires at its assigned positions
                    for (int i = 0; i < positionsPerChicken && positionIndex < allGridPositions.Count; i++)
                    {
                        ShootChickenAtPosition(chicken, allGridPositions[positionIndex], cachedModifiedSpeed);
                        positionIndex++;
                    }
                }
            }
            
            // If any positions remain (due to rounding), assign them to chickens that can still attack
            int chickenIndex = 0;
            while (positionIndex < allGridPositions.Count)
            {
                var chicken = selectedChickens[chickenIndex % selectedChickens.Count];
                if (chicken != null && chicken.CanAttack())
                {
                    ShootChickenAtPosition(chicken, allGridPositions[positionIndex], cachedModifiedSpeed);
                    positionIndex++;
                }
                chickenIndex++;
            }
        }
        
        yield return null; // Single frame yield to ensure all shots are processed
        
        LogDebug("Large grid attack complete!");
        ResetAttackState();
    }
    
    private void ShootChickenAtPosition(ChickenCombatBehaviorV2 chicken, Vector3 targetPosition, float speed)
    {
        if (chicken == null || !chicken.CanAttack())
        {
            LogWarning($"Chicken {chicken?.gameObject.name ?? "null"} cannot attack!");
            return;
        }
        
        chicken.ShootEggAtPosition(targetPosition, speed, deactivateWarningCircle);
        
        // Play the attack SFX
        if (audioEvent != null)
        {
            audioEvent.PlayAtPoint(chicken.transform.position);
        }
        
        LogDebug($"Chicken {chicken.gameObject.name} shooting at grid position {targetPosition}");
    }
    
    // Reset attack state
    public void ResetAttackState()
    {
        mainGridPositions.Clear();
        leftGridPositions.Clear();
        rightGridPositions.Clear();
        allGridPositions.Clear();
    }
    
    // Gizmo drawing for debugging
    private void OnDrawGizmosSelected()
    {
        if (showGridGizmo)
        {
            // Draw main grid
            if (mainGridPositions != null && mainGridPositions.Count > 0)
            {
                DrawGrid(mainGridPositions, gridSize, Color.magenta, "Main Grid");
            }
            
            // Draw side grids
            if (enableSideGrids)
            {
                if (leftGridPositions != null && leftGridPositions.Count > 0)
                {
                    DrawGrid(leftGridPositions, sideGridSize, Color.cyan, "Left Grid");
                }
                
                if (rightGridPositions != null && rightGridPositions.Count > 0)
                {
                    DrawGrid(rightGridPositions, sideGridSize, Color.green, "Right Grid");
                }
            }
            
            // Draw grid centers
            if (mainGridPositions.Count > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(calculatedGridCenter, 0.3f);
                
                if (enableSideGrids)
                {
                    Gizmos.color = new Color(0, 1, 1, 0.5f); // Semi-transparent cyan
                    Gizmos.DrawWireSphere(leftGridCenter, 0.25f);
                    Gizmos.color = new Color(0, 1, 0, 0.5f); // Semi-transparent green
                    Gizmos.DrawWireSphere(rightGridCenter, 0.25f);
                    
                    // Draw connections between grids
                    Gizmos.color = new Color(1, 1, 0, 0.3f); // Semi-transparent yellow
                    Gizmos.DrawLine(calculatedGridCenter, leftGridCenter);
                    Gizmos.DrawLine(calculatedGridCenter, rightGridCenter);
                }
                
                // Draw rotation indicator
                if (currentRotation != 0f)
                {
                    Vector3 rotationDir = new Vector3(Mathf.Cos(currentRotation * Mathf.Deg2Rad), Mathf.Sin(currentRotation * Mathf.Deg2Rad), 0f);
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(calculatedGridCenter, calculatedGridCenter + rotationDir * 2f);
                }
                
                // Draw grid info label
                #if UNITY_EDITOR
                UnityEditor.Handles.color = Color.magenta;
                string infoText = $"Main: {gridSize}x{gridSize} = {mainGridPositions.Count} shots";
                if (enableSideGrids)
                {
                    infoText += $"\nSides: 2x{sideGridSize}x{sideGridSize} = {leftGridPositions.Count + rightGridPositions.Count} shots";
                    infoText += $"\nTotal: {allGridPositions.Count} shots";
                }
                infoText += $"\nSpacing: {gridSpacing:F2}";
                if (currentRotation != 0f)
                {
                    infoText += $"\nRotation: {currentRotation:F1}°";
                }
                UnityEditor.Handles.Label(calculatedGridCenter + Vector3.up * 3f, infoText);
                #endif
            }
        }
    }
    
    private void DrawGrid(List<Vector3> positions, int size, Color color, string label)
    {
        if (positions == null || positions.Count == 0) return;
        
        // Draw grid positions
        Gizmos.color = color;
        foreach (Vector3 pos in positions)
        {
            Gizmos.DrawWireSphere(pos, 0.15f);
        }
        
        // Draw grid lines
        if (positions.Count == size * size)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.5f); // Semi-transparent version
            
            // Draw horizontal lines
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size - 1; col++)
                {
                    int index = row * size + col;
                    if (index < positions.Count - 1 && (index + 1) < positions.Count)
                    {
                        Gizmos.DrawLine(positions[index], positions[index + 1]);
                    }
                }
            }
            
            // Draw vertical lines
            for (int col = 0; col < size; col++)
            {
                for (int row = 0; row < size - 1; row++)
                {
                    int index = row * size + col;
                    int nextIndex = (row + 1) * size + col;
                    if (index < positions.Count && nextIndex < positions.Count)
                    {
                        Gizmos.DrawLine(positions[index], positions[nextIndex]);
                    }
                }
            }
        }
    }
}