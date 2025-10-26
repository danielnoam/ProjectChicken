using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[CreateAssetMenu(fileName = "Big Square Attack", menuName = "Chicken Combat/Attacks/Big Square Attack")]
public class BigSquareAttackSO : BaseChickenAttackSO
{
    // This attack creates a large square grid around the player
    // All chickens fire simultaneously at their assigned grid positions
    
    [Header("Large Grid Attack Settings")]
    [Tooltip("Size of the grid (e.g., 4 means 4x4 = 16 shots)")]
    [SerializeField, Range(2, 6)] private int gridSize = 4; // NxN grid
    
    [Tooltip("The spacing between each egg in the grid")]
    [SerializeField, Range(0.3f, 8f)] private float gridSpacing = 4f; // Distance between grid points
    
    [Header("Targeting Settings")]
    [Tooltip("Offset the entire grid from the player position")]
    [SerializeField] private Vector3 gridOffset = Vector3.zero;
    
    [Tooltip("Random offset applied to the grid center to make it less predictable")]
    [SerializeField, Range(0f, 1f)] private float maxRandomOffset = 0.3f;
    
    [Tooltip("If true, grid will rotate randomly around the player")]
    [SerializeField] private bool randomRotation = true;
    
    [Tooltip("If false, uses a subset of chickens equal to gridSize x gridSize")]
    [SerializeField] private bool useAllAvailableChickens = false;
    
    [Header("Boundary Settings")]
    [Tooltip("Multiplier for the player boundary size (1.0 = exact size, 2.0 = double size)")]
    [SerializeField, Range(0.5f, 3f)] private float boundarySizeScaler = 2f;
    [Tooltip("Minimum distance from boundaries when placing the grid")]
    [SerializeField] private float boundaryPadding = 1f;
    
    [Tooltip("Show the grid area in scene view")]
    [SerializeField] private bool showGridGizmo = true;
    

    
    public override AttackType AttackType => AttackType.BigSquare;
    public override string AttackName => "Large Grid Attack";
    
    // Grid data
    private List<Vector3> gridPositions = new List<Vector3>();
    private Vector3 calculatedGridCenter;
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
        int chickensNeeded = useAllAvailableChickens ? 1 : (gridSize * gridSize);
        
        // Need enough available chickens for the grid
        if (availableChickens == null || availableChickens.Count < chickensNeeded)
        {
            LogDebug($"Not enough available chickens for attack. Need {chickensNeeded} for {gridSize}x{gridSize} grid, Available: {(availableChickens?.Count ?? 0)}");
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
        int chickensToUse = useAllAvailableChickens ? availableChickens.Count : Mathf.Min(gridPositions.Count, availableChickens.Count);
        List<ChickenCombatBehaviorV2> selectedChickens = availableChickens
            .Where(chicken => chicken != null && chicken.CanAttack())
            .Take(chickensToUse)
            .ToList();
        
        // Start the attack using coroutine
        manager.StartCoroutine(ExecuteGridAttack(selectedChickens, manager));
        
        LogDebug($"Started large grid attack: {gridSize}x{gridSize} = {gridPositions.Count} positions, using {selectedChickens.Count} chickens (Total registered: {manager.TotalCombatChickens})");
    }
    
    private void CalculateGridPositions(Vector3 playerPosition)
    {
        gridPositions.Clear();
        
        // Apply random offset
        Vector3 randomOffset = new Vector3(
            Random.Range(-maxRandomOffset, maxRandomOffset),
            Random.Range(-maxRandomOffset, maxRandomOffset),
            0f
        );
        
        // Calculate grid center
        calculatedGridCenter = playerPosition + gridOffset + randomOffset;
        calculatedGridCenter.z = playerPosition.z;
        
        // Apply random rotation if enabled
        currentRotation = randomRotation ? Random.Range(0f, 360f) : 0f;
        
        // Calculate grid bounds to ensure it fits within player boundaries
        float gridHalfSize = ((gridSize - 1) * gridSpacing) * 0.5f;
        
        // Get boundary limits
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            Vector2 playerBounds = levelManager.PlayerBoundarySize * boundarySizeScaler;
            Vector3 boundaryCenter = levelManager.PlayerPosition;
            
            // Clamp grid center to ensure entire grid stays within bounds
            float minX = boundaryCenter.x - (playerBounds.x * 0.5f) + gridHalfSize + boundaryPadding;
            float maxX = boundaryCenter.x + (playerBounds.x * 0.5f) - gridHalfSize - boundaryPadding;
            float minY = boundaryCenter.y - (playerBounds.y * 0.5f) + gridHalfSize + boundaryPadding;
            float maxY = boundaryCenter.y + (playerBounds.y * 0.5f) - gridHalfSize - boundaryPadding;
            
            calculatedGridCenter.x = Mathf.Clamp(calculatedGridCenter.x, minX, maxX);
            calculatedGridCenter.y = Mathf.Clamp(calculatedGridCenter.y, minY, maxY);
        }
        
        // Generate grid positions
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                // Calculate position relative to grid center
                float x = (col - (gridSize - 1) * 0.5f) * gridSpacing;
                float y = (row - (gridSize - 1) * 0.5f) * gridSpacing;
                
                Vector3 localPos = new Vector3(x, y, 0f);
                
                // Apply rotation
                if (currentRotation != 0f)
                {
                    float rad = currentRotation * Mathf.Deg2Rad;
                    float rotatedX = localPos.x * Mathf.Cos(rad) - localPos.y * Mathf.Sin(rad);
                    float rotatedY = localPos.x * Mathf.Sin(rad) + localPos.y * Mathf.Cos(rad);
                    localPos = new Vector3(rotatedX, rotatedY, 0f);
                }
                
                // Add to world position
                Vector3 worldPos = calculatedGridCenter + localPos;
                gridPositions.Add(worldPos);
            }
        }
        
        if (showDebugLogs)
        {
            LogDebug($"Calculated {gridSize}x{gridSize} grid at {calculatedGridCenter} with rotation {currentRotation:F1}°");
        }
    }
    
    private IEnumerator ExecuteGridAttack(List<ChickenCombatBehaviorV2> selectedChickens, ChickenCombatManagerV4 manager)
    {
        LogDebug($"Firing large grid: {gridSize}x{gridSize} = {gridPositions.Count} shots simultaneously");
        
        // Fire all shots at once
        if (useAllAvailableChickens)
        {
            // Each chicken fires at multiple positions if we have fewer chickens than positions
            int positionsPerChicken = Mathf.CeilToInt((float)gridPositions.Count / selectedChickens.Count);
            int positionIndex = 0;
            
            foreach (var chicken in selectedChickens)
            {
                if (chicken != null && chicken.CanAttack())
                {
                    // This chicken fires at its assigned positions
                    for (int i = 0; i < positionsPerChicken && positionIndex < gridPositions.Count; i++)
                    {
                        ShootChickenAtPosition(chicken, gridPositions[positionIndex], cachedModifiedSpeed);
                        positionIndex++;
                    }
                }
            }
        }
        else
        {
            // One chicken per position (up to available chickens)
            for (int i = 0; i < Mathf.Min(selectedChickens.Count, gridPositions.Count); i++)
            {
                var chicken = selectedChickens[i];
                var targetPos = gridPositions[i];
                
                if (chicken != null && chicken.CanAttack())
                {
                    ShootChickenAtPosition(chicken, targetPos, cachedModifiedSpeed);
                }
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
        gridPositions.Clear();
    }
    
    // Gizmo drawing for debugging
    private void OnDrawGizmosSelected()
    {
        if (showGridGizmo && gridPositions != null && gridPositions.Count > 0)
        {
            // Draw grid positions
            Gizmos.color = Color.magenta;
            foreach (Vector3 pos in gridPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.15f);
            }
            
            // Draw grid lines to show the pattern
            if (gridPositions.Count == gridSize * gridSize)
            {
                Gizmos.color = new Color(1f, 0f, 1f, 0.5f); // Semi-transparent magenta
                
                // Draw horizontal lines
                for (int row = 0; row < gridSize; row++)
                {
                    for (int col = 0; col < gridSize - 1; col++)
                    {
                        int index = row * gridSize + col;
                        Gizmos.DrawLine(gridPositions[index], gridPositions[index + 1]);
                    }
                }
                
                // Draw vertical lines
                for (int col = 0; col < gridSize; col++)
                {
                    for (int row = 0; row < gridSize - 1; row++)
                    {
                        int index = row * gridSize + col;
                        int nextIndex = (row + 1) * gridSize + col;
                        Gizmos.DrawLine(gridPositions[index], gridPositions[nextIndex]);
                    }
                }
            }
            
            // Draw grid center and bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(calculatedGridCenter, 0.3f);
            
            // Draw rotation indicator
            if (currentRotation != 0f)
            {
                Vector3 rotationDir = new Vector3(Mathf.Cos(currentRotation * Mathf.Deg2Rad), Mathf.Sin(currentRotation * Mathf.Deg2Rad), 0f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(calculatedGridCenter, calculatedGridCenter + rotationDir * 2f);
            }
            
            // Draw grid size label
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.magenta;
            UnityEditor.Handles.Label(calculatedGridCenter + Vector3.up * 2f, 
                $"Grid: {gridSize}x{gridSize} = {gridSize * gridSize} shots\nSpacing: {gridSpacing:F2}");
            #endif
        }
    }
}