using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[CreateAssetMenu(fileName = "FocusedGridAttack", menuName = "Chicken Combat/Attacks/Focused Grid Attack")]
public class FocusedGridAttackSO : BaseChickenAttackSO
{
    [Header("Focused Grid Attack Settings")]
    [Tooltip("The spacing between each egg in the 2x2 grid")]
    [SerializeField] private float gridSpacing = 0.5f; // Distance between grid points
    
    [Header("Multiple Grid Settings")]
    [Tooltip("Number of grid groups to fire (first always targets player, rest are random)")]
    [SerializeField] private int numberOfGridGroups = 4; // Total number of 2x2 grids to fire
    [Tooltip("Time delay between firing each grid group")]
    [SerializeField] private float delayBetweenGroups = 0.5f; // Delay between each group of 4 shots
    
    [Header("Attack Pattern")]
    [SerializeField] private bool simultaneousShots = true; // If true, all 4 shots in a group happen at once
    [SerializeField] private float shotDelay = 0.1f; // Delay between shots when not simultaneous (within a group)
    [SerializeField] private bool randomizeOrder = false; // If true, randomize the shot order within a group
    
    [Header("Offset Settings")]
    [Tooltip("Random offset applied to the grid center to make it less predictable")]
    [SerializeField] private float maxRandomOffset = 0.3f; // Maximum random offset from target position
    [Tooltip("Minimum distance from boundaries when placing random grids")]
    [SerializeField] private float boundaryPadding = 1f; // Keep grids away from edges
    
    [Header("Boundary Settings")]
    [Tooltip("Multiplier for the player boundary size (1.0 = exact size, 2.0 = double size)")]
    [SerializeField, Range(0.5f, 3f)] private float boundarySizeScaler = 2f;
    [Tooltip("Show the calculated boundary area in scene view")]
    [SerializeField] private bool showBoundaryGizmo = true;
    [Tooltip("Minimum distance between grid groups to prevent overlap")]
    [SerializeField, Range(1f, 8f)] private float minDistanceBetweenGroups = 3f;
    
    public override AttackType AttackType => AttackType.FormationShape; // Using FormationShape type, but could add a new type
    public override string AttackName => "Focused Grid Attack";
    
    // Grid group tracking
    private List<GridGroup> gridGroups = new List<GridGroup>();
    private float cachedModifiedSpeed = 0f;
    
    // Cached data for editor gizmos
    [SerializeField, HideInInspector] private Vector2 lastKnownPlayerBounds = new Vector2(40f, 25f);
    [SerializeField, HideInInspector] private Vector3 lastKnownBoundaryCenter = Vector3.zero;
    
    // Helper class to store a grid group
    [System.Serializable]
    public class GridGroup
    {
        public Vector3 centerPosition;
        public List<Vector3> gridPositions = new List<Vector3>();
        public bool hasBeenFired = false;
        
        public GridGroup(Vector3 center)
        {
            centerPosition = center;
        }
    }
    
    public override bool CanExecute(List<ChickenCombatBehaviorV2> availableChickens, ChickenCombatManagerV4 manager)
    {
        // Need at least 4 chickens for the 2x2 grid
        if (availableChickens == null || availableChickens.Count < 4)
        {
            LogDebug($"Not enough chickens available. Required: 4, Available: {(availableChickens?.Count ?? 0)}");
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
            LogWarning("Cannot execute Focused Grid attack!");
            return;
        }
        
        // Cache modified speed
        cachedModifiedSpeed = manager.EggSpeed * eggSpeedMultiplier;
        
        // Clear any existing warnings
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.ClearAllWarnings();
            if (showDebugLogs)
                LogDebug("Cleared existing warnings before creating focused grid groups");
        }
        
        // Select the 4 chickens that will fire all groups
        List<ChickenCombatBehaviorV2> selectedChickens = availableChickens
            .Where(chicken => chicken != null && chicken.CanAttack())
            .Take(4)
            .ToList();
        
        if (selectedChickens.Count < 4)
        {
            LogWarning($"Only {selectedChickens.Count} chickens can attack, need 4 for full grid");
        }
        
        // Calculate all grid group positions
        CalculateAllGridGroups(manager.Player.position);
        
        // Start the attack sequence using coroutine
        manager.StartCoroutine(ExecuteGridGroupSequence(selectedChickens, manager));
        
        LogDebug($"Started attack sequence with {gridGroups.Count} grid groups and {selectedChickens.Count} chickens");
    }
    
    private void CalculateAllGridGroups(Vector3 playerPosition)
    {
        gridGroups.Clear();
        
        // Get player bounds from LevelManager
        Vector2 playerBounds = Vector2.zero;
        Vector3 boundaryCenter = Vector3.zero;
        
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            playerBounds = levelManager.PlayerBoundarySize * boundarySizeScaler; // Apply scaler
            boundaryCenter = levelManager.PlayerPosition;
            // Cache for editor gizmos
            lastKnownPlayerBounds = playerBounds;
            lastKnownBoundaryCenter = boundaryCenter;
        }
        else
        {
            LogWarning("No LevelManager found, using default bounds");
            playerBounds = new Vector2(40f, 25f) * boundarySizeScaler; // Apply scaler
            boundaryCenter = Vector3.zero;
        }
        
        // Keep track of all group centers to ensure spacing
        List<Vector3> existingGroupCenters = new List<Vector3>();
        
        for (int groupIndex = 0; groupIndex < numberOfGridGroups; groupIndex++)
        {
            Vector3 groupCenter;
            
            if (groupIndex == 0)
            {
                // First group always targets the player with small random offset
                Vector3 randomOffset = new Vector3(
                    Random.Range(-maxRandomOffset, maxRandomOffset),
                    Random.Range(-maxRandomOffset, maxRandomOffset),
                    0f
                );
                groupCenter = playerPosition + randomOffset;
                
                // Ensure the player-targeted grid stays within bounds
                float halfGridSize = gridSpacing * 0.5f;
                float minX = boundaryCenter.x - (playerBounds.x * 0.5f) + halfGridSize + boundaryPadding;
                float maxX = boundaryCenter.x + (playerBounds.x * 0.5f) - halfGridSize - boundaryPadding;
                float minY = boundaryCenter.y - (playerBounds.y * 0.5f) + halfGridSize + boundaryPadding;
                float maxY = boundaryCenter.y + (playerBounds.y * 0.5f) - halfGridSize - boundaryPadding;
                
                groupCenter.x = Mathf.Clamp(groupCenter.x, minX, maxX);
                groupCenter.y = Mathf.Clamp(groupCenter.y, minY, maxY);
                groupCenter.z = playerPosition.z;
            }
            else
            {
                // Subsequent groups need to maintain minimum distance from existing groups
                bool validPosition = false;
                int attempts = 0;
                int maxAttempts = 50; // Prevent infinite loops
                
                do
                {
                    groupCenter = GetRandomPositionInBounds(playerBounds, playerPosition);
                    validPosition = true;
                    
                    // Check distance from all existing group centers
                    foreach (Vector3 existingCenter in existingGroupCenters)
                    {
                        float distance = Vector3.Distance(groupCenter, existingCenter);
                        if (distance < minDistanceBetweenGroups)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                    
                    attempts++;
                    if (attempts >= maxAttempts)
                    {
                        LogWarning($"Could not find valid position for group {groupIndex} after {maxAttempts} attempts. Using last position.");
                        break;
                    }
                } while (!validPosition);
            }
            
            // Add this center to the list of existing centers
            existingGroupCenters.Add(groupCenter);
            
            GridGroup group = new GridGroup(groupCenter);
            
            // Calculate the 4 positions for this grid
            float halfSpacing = gridSpacing * 0.5f;
            
            // Top-left
            group.gridPositions.Add(new Vector3(groupCenter.x - halfSpacing, groupCenter.y + halfSpacing, groupCenter.z));
            // Top-right
            group.gridPositions.Add(new Vector3(groupCenter.x + halfSpacing, groupCenter.y + halfSpacing, groupCenter.z));
            // Bottom-left
            group.gridPositions.Add(new Vector3(groupCenter.x - halfSpacing, groupCenter.y - halfSpacing, groupCenter.z));
            // Bottom-right
            group.gridPositions.Add(new Vector3(groupCenter.x + halfSpacing, groupCenter.y - halfSpacing, groupCenter.z));
            
            // Randomize order within group if enabled
            if (randomizeOrder && !simultaneousShots)
            {
                for (int i = 0; i < group.gridPositions.Count; i++)
                {
                    Vector3 temp = group.gridPositions[i];
                    int randomIndex = Random.Range(i, group.gridPositions.Count);
                    group.gridPositions[i] = group.gridPositions[randomIndex];
                    group.gridPositions[randomIndex] = temp;
                }
            }
            
            gridGroups.Add(group);
        }
        
        LogDebug($"Calculated {gridGroups.Count} grid groups with {gridGroups.Count * 4} total target positions");
        if (showDebugLogs && levelManager != null)
        {
            LogDebug($"Player bounds: {playerBounds} (scaler: {boundarySizeScaler}), Boundary center: {boundaryCenter}");
            LogDebug($"Min distance between groups: {minDistanceBetweenGroups}");
        }
    }
    
    private IEnumerator ExecuteGridGroupSequence(List<ChickenCombatBehaviorV2> selectedChickens, ChickenCombatManagerV4 manager)
    {
        // Execute each grid group in sequence
        for (int groupIndex = 0; groupIndex < gridGroups.Count; groupIndex++)
        {
            GridGroup currentGroup = gridGroups[groupIndex];
            
            LogDebug($"Firing grid group {groupIndex + 1}/{gridGroups.Count} at {currentGroup.centerPosition}");
            
            if (simultaneousShots)
            {
                // Fire all 4 shots in this group at once
                ExecuteSimultaneousGroupShots(currentGroup, selectedChickens);
            }
            else
            {
                // Execute sequential shots for this group
                yield return ExecuteSequentialGroupShots(currentGroup, selectedChickens);
            }
            
            currentGroup.hasBeenFired = true;
            
            // Wait before firing next group (except after the last group)
            if (groupIndex < gridGroups.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenGroups);
            }
        }
        
        LogDebug("Completed all grid groups!");
        ResetAttackState();
    }
    
    private void ExecuteSimultaneousGroupShots(GridGroup group, List<ChickenCombatBehaviorV2> selectedChickens)
    {
        for (int i = 0; i < Mathf.Min(selectedChickens.Count, group.gridPositions.Count); i++)
        {
            var chicken = selectedChickens[i];
            var targetPos = group.gridPositions[i];
            
            if (chicken != null && chicken.CanAttack())
            {
                ShootChickenAtPosition(chicken, targetPos, cachedModifiedSpeed);
            }
        }
    }
    
    private IEnumerator ExecuteSequentialGroupShots(GridGroup group, List<ChickenCombatBehaviorV2> selectedChickens)
    {
        for (int i = 0; i < Mathf.Min(selectedChickens.Count, group.gridPositions.Count); i++)
        {
            var chicken = selectedChickens[i];
            var targetPos = group.gridPositions[i];
            
            if (chicken != null && chicken.CanAttack())
            {
                ShootChickenAtPosition(chicken, targetPos, cachedModifiedSpeed);
                
                // Wait before next shot (except after the last shot)
                if (i < group.gridPositions.Count - 1)
                {
                    yield return new WaitForSeconds(shotDelay);
                }
            }
        }
    }
    
    private Vector3 GetRandomPositionInBounds(Vector2 canvasBounds, Vector3 playerPosition)
    {
        // Get the player boundary size from LevelManager (this is the actual play area)
        Vector2 playerBounds = Vector2.zero;
        Vector3 boundaryCenter = Vector3.zero;
        
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            playerBounds = levelManager.PlayerBoundarySize * boundarySizeScaler; // Apply scaler
            boundaryCenter = levelManager.PlayerPosition; // This includes the boundary offset
            // Cache for editor gizmos
            lastKnownPlayerBounds = playerBounds;
            lastKnownBoundaryCenter = boundaryCenter;
        }
        else
        {
            // Fallback if no LevelManager
            playerBounds = canvasBounds * 0.5f * boundarySizeScaler; // Apply scaler
            boundaryCenter = Vector3.zero;
        }
        
        // Calculate the actual bounds accounting for grid size and padding
        float halfWidth = (playerBounds.x * 0.5f) - boundaryPadding - (gridSpacing * 0.5f);
        float halfHeight = (playerBounds.y * 0.5f) - boundaryPadding - (gridSpacing * 0.5f);
        
        // Ensure we have valid bounds
        halfWidth = Mathf.Max(halfWidth, gridSpacing);
        halfHeight = Mathf.Max(halfHeight, gridSpacing);
        
        // Generate random position within the bounds
        float randomX = Random.Range(-halfWidth, halfWidth);
        float randomY = Random.Range(-halfHeight, halfHeight);
        
        // Return position relative to the boundary center (which includes offset)
        Vector3 randomPosition = boundaryCenter + new Vector3(randomX, randomY, 0f);
        
        // Use the same Z position as the player
        randomPosition.z = playerPosition.z;
        
        if (showDebugLogs)
        {
            LogDebug($"Random position: {randomPosition} (bounds: {playerBounds}, center: {boundaryCenter}, scaler: {boundarySizeScaler})");
        }
        
        return randomPosition;
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
        
        LogDebug($"Chicken {chicken.gameObject.name} shooting at position {targetPosition}");
    }
    
    // Reset attack state
    public void ResetAttackState()
    {
        gridGroups.Clear();
    }
    
    // Helper method to draw a wire circle in the XY plane
    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
    
    // Gizmo drawing for debugging
    private void OnDrawGizmosSelected()
    {
        // Draw the calculated boundary area if enabled
        if (showBoundaryGizmo)
        {
            Vector2 playerBounds;
            Vector3 boundaryCenter;
            
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager != null)
            {
                playerBounds = levelManager.PlayerBoundarySize * boundarySizeScaler;
                boundaryCenter = levelManager.PlayerPosition;
            }
            else
            {
                // Use cached data in editor
                playerBounds = lastKnownPlayerBounds * boundarySizeScaler;
                boundaryCenter = lastKnownBoundaryCenter;
            }
            
            // Draw the outer boundary
            Gizmos.color = Color.cyan;
            Vector3[] corners = new Vector3[4];
            float halfWidth = playerBounds.x * 0.5f;
            float halfHeight = playerBounds.y * 0.5f;
            
            corners[0] = boundaryCenter + new Vector3(-halfWidth, -halfHeight, 0);
            corners[1] = boundaryCenter + new Vector3(halfWidth, -halfHeight, 0);
            corners[2] = boundaryCenter + new Vector3(halfWidth, halfHeight, 0);
            corners[3] = boundaryCenter + new Vector3(-halfWidth, halfHeight, 0);
            
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }
            
            // Draw the inner boundary (after padding)
            Gizmos.color = new Color(0, 1, 1, 0.5f); // Semi-transparent cyan
            float innerHalfWidth = halfWidth - boundaryPadding - (gridSpacing * 0.5f);
            float innerHalfHeight = halfHeight - boundaryPadding - (gridSpacing * 0.5f);
            
            corners[0] = boundaryCenter + new Vector3(-innerHalfWidth, -innerHalfHeight, 0);
            corners[1] = boundaryCenter + new Vector3(innerHalfWidth, -innerHalfHeight, 0);
            corners[2] = boundaryCenter + new Vector3(innerHalfWidth, innerHalfHeight, 0);
            corners[3] = boundaryCenter + new Vector3(-innerHalfWidth, innerHalfHeight, 0);
            
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }
            
            // Draw center cross
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawLine(boundaryCenter + Vector3.left * innerHalfWidth, boundaryCenter + Vector3.right * innerHalfWidth);
            Gizmos.DrawLine(boundaryCenter + Vector3.down * innerHalfHeight, boundaryCenter + Vector3.up * innerHalfHeight);
            
            // Draw scaler text label
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(boundaryCenter + Vector3.up * (halfHeight + 1f), 
                $"Boundary Scaler: {boundarySizeScaler:F2}x\nSize: {playerBounds.x:F1} x {playerBounds.y:F1}");
            #endif
        }
        
        // Draw grid groups
        if (gridGroups != null && gridGroups.Count > 0)
        {
            for (int groupIndex = 0; groupIndex < gridGroups.Count; groupIndex++)
            {
                var group = gridGroups[groupIndex];
                
                // Different colors for different groups
                if (groupIndex == 0)
                    Gizmos.color = Color.red; // Player-targeted group
                else
                    Gizmos.color = Color.HSVToRGB((float)groupIndex / gridGroups.Count, 1f, 1f);
                
                // Draw grid positions
                foreach (Vector3 pos in group.gridPositions)
                {
                    Gizmos.DrawWireSphere(pos, 0.15f);
                    if (group.hasBeenFired)
                        Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
                }
                
                // Draw grid outline
                if (group.gridPositions.Count == 4)
                {
                    Gizmos.DrawLine(group.gridPositions[0], group.gridPositions[1]); // Top line
                    Gizmos.DrawLine(group.gridPositions[2], group.gridPositions[3]); // Bottom line
                    Gizmos.DrawLine(group.gridPositions[0], group.gridPositions[2]); // Left line
                    Gizmos.DrawLine(group.gridPositions[1], group.gridPositions[3]); // Right line
                    
                    // Draw group center
                    Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
                    Gizmos.DrawWireSphere(group.centerPosition, 0.25f);
                    
                    // Draw minimum distance circle (debug visualization)
                    if (showDebugLogs && groupIndex > 0)
                    {
                        Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Transparent yellow
                        DrawWireCircle(group.centerPosition, minDistanceBetweenGroups, 16);
                    }
                }
            }
        }
    }
}