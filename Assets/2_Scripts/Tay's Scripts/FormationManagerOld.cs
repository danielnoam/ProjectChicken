using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using System.Linq;

public class FormationManagerOld : MonoBehaviour
{
    [System.Serializable]
    public enum FormationType
    {
        VShape,
        Triangle,
        Circle,
        Square,
        Grid
    }
    
    // This class represents a slot in the formation
    [System.Serializable]
    public class FormationSlot
    {
        public Vector3 localPosition;      // Position relative to formation center
        public Transform occupant;         // The enemy currently in this slot (null if empty)
        public bool isReserved;           // True if an enemy is moving toward this slot
        
        public FormationSlot(Vector3 position)
        {
            localPosition = position;
            occupant = null;
            isReserved = false;
        }
    }

    [Header("Formation Settings")]
    [SerializeField] private FormationType currentFormation = FormationType.Grid;
    [SerializeField] private float formationSize = 10f;
    [SerializeField] private float enemySpacing = 2f;
    [SerializeField] private int maxSlotsPerFormation = 50; // Maximum slots to create
    
    [Header("Spline Following Settings")]
    [SerializeField] private bool useSplineFollowing = true;
    [SerializeField] private float splineOffset = 15f;
    [SerializeField] private float heightAboveSpline = 5f;
    [SerializeField] private float followSmoothness = 5f;
    
    [Header("Slot Assignment Settings")]
    [SerializeField] private float slotAssignmentRange = 100f; // How far away enemies can be to get assigned
    [SerializeField] private float enemyMoveToSlotSpeed = 8f; // How fast enemies move to their slots
    [SerializeField] private float arrivalThreshold = 0.5f; // How close is "arrived at slot"
    
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private string enemyTag = "Enemy";
    
    // Core data structures
    private List<FormationSlot> formationSlots = new List<FormationSlot>();
    private List<Transform> unassignedEnemies = new List<Transform>();
    private Dictionary<Transform, FormationSlot> enemyToSlotMap = new Dictionary<Transform, FormationSlot>();
    
    // Formation positioning
    private Vector3 formationCenter;
    private Vector3 targetFormationCenter;
    private Quaternion formationRotation;
    private LevelManager levelManager;
    
    void Start()
    {
        levelManager = LevelManager.Instance;
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        // Create initial formation slots
        GenerateFormationSlots();
    }
    
    void Update()
    {
        // Update formation position first
        UpdateFormationPosition();
        
        // Find new enemies and update our tracking
        ScanForEnemies();
        
        // Assign unassigned enemies to empty slots
        AssignEnemiesToSlots();
        
        // Move enemies to their assigned positions
        UpdateEnemyPositions();
        
        // Clean up destroyed enemies
        CleanupDestroyedEnemies();
    }
    
    void GenerateFormationSlots()
    {
        formationSlots.Clear();
        
        switch (currentFormation)
        {
            case FormationType.VShape:
                GenerateVShapeSlots();
                break;
            case FormationType.Triangle:
                GenerateTriangleSlots();
                break;
            case FormationType.Circle:
                GenerateCircleSlots();
                break;
            case FormationType.Square:
                GenerateSquareSlots();
                break;
            case FormationType.Grid:
                GenerateGridSlots();
                break;
        }
    }
    
    // Scan the scene for enemies and categorize them
    void ScanForEnemies()
    {
        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag(enemyTag);
        
        // First, find any new enemies that aren't tracked yet
        foreach (GameObject enemyObj in enemyObjects)
        {
            if (enemyObj != null && enemyObj.activeInHierarchy)
            {
                Transform enemy = enemyObj.transform;
                
                // Check if this enemy is already assigned to a slot
                if (!enemyToSlotMap.ContainsKey(enemy) && !unassignedEnemies.Contains(enemy))
                {
                    // This is a new enemy, add to unassigned list
                    unassignedEnemies.Add(enemy);
                }
            }
        }
    }
    
    // Try to assign unassigned enemies to empty slots
    void AssignEnemiesToSlots()
    {
        // Remove any null entries from unassigned list
        unassignedEnemies.RemoveAll(e => e == null);
        
        // Try to assign each unassigned enemy to an empty slot
        for (int i = unassignedEnemies.Count - 1; i >= 0; i--)
        {
            Transform enemy = unassignedEnemies[i];
            if (enemy == null) continue;
            
            // Find the nearest empty slot
            FormationSlot bestSlot = FindNearestEmptySlot(enemy.position);
            
            if (bestSlot != null)
            {
                // Assign the enemy to this slot
                AssignEnemyToSlot(enemy, bestSlot);
                unassignedEnemies.RemoveAt(i);
            }
        }
    }
    
    FormationSlot FindNearestEmptySlot(Vector3 enemyPosition)
    {
        FormationSlot nearestSlot = null;
        float nearestDistance = float.MaxValue;
        
        foreach (FormationSlot slot in formationSlots)
        {
            // Skip occupied or reserved slots
            if (slot.occupant != null || slot.isReserved) continue;
            
            // Calculate world position of this slot
            Vector3 slotWorldPos = GetSlotWorldPosition(slot);
            float distance = Vector3.Distance(enemyPosition, slotWorldPos);
            
            // Only consider slots within assignment range
            if (distance < nearestDistance && distance < slotAssignmentRange)
            {
                nearestDistance = distance;
                nearestSlot = slot;
            }
        }
        
        return nearestSlot;
    }
    
    void AssignEnemyToSlot(Transform enemy, FormationSlot slot)
    {
        slot.occupant = enemy;
        slot.isReserved = true;
        enemyToSlotMap[enemy] = slot;
    }
    
    // Update positions of all assigned enemies
    void UpdateEnemyPositions()
    {
        List<Transform> enemiesToRemove = new List<Transform>();
        
        foreach (var kvp in enemyToSlotMap)
        {
            Transform enemy = kvp.Key;
            FormationSlot slot = kvp.Value;
            
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }
            
            // Calculate target position for this enemy
            Vector3 targetPosition = GetSlotWorldPosition(slot);
            
            // Move enemy toward its slot
            float distance = Vector3.Distance(enemy.position, targetPosition);
            
            if (distance > arrivalThreshold)
            {
                // Enemy is still moving to its slot
                enemy.position = Vector3.Lerp(enemy.position, targetPosition, Time.deltaTime * enemyMoveToSlotSpeed);
            }
            else
            {
                // Enemy has arrived, ensure it stays in formation
                enemy.position = Vector3.Lerp(enemy.position, targetPosition, Time.deltaTime * enemyMoveToSlotSpeed * 2f);
            }
        }
        
        // Clean up any destroyed enemies
        foreach (Transform enemy in enemiesToRemove)
        {
            RemoveEnemyFromSlot(enemy);
        }
    }
    
    void CleanupDestroyedEnemies()
    {
        // Check all slots for destroyed enemies
        foreach (FormationSlot slot in formationSlots)
        {
            if (slot.occupant != null && slot.occupant == null) // Unity's destroyed check
            {
                // The enemy was destroyed, clear the slot
                slot.occupant = null;
                slot.isReserved = false;
            }
        }
        
        // Clean up the enemy-to-slot map
        List<Transform> keysToRemove = enemyToSlotMap.Keys.Where(enemy => enemy == null).ToList();
        foreach (Transform key in keysToRemove)
        {
            enemyToSlotMap.Remove(key);
        }
    }
    
    void RemoveEnemyFromSlot(Transform enemy)
    {
        if (enemyToSlotMap.ContainsKey(enemy))
        {
            FormationSlot slot = enemyToSlotMap[enemy];
            slot.occupant = null;
            slot.isReserved = false;
            enemyToSlotMap.Remove(enemy);
        }
    }
    
    Vector3 GetSlotWorldPosition(FormationSlot slot)
    {
        // Transform the local slot position to world position
        return formationCenter 
            + GetRightVector() * slot.localPosition.x 
            + GetUpVector() * slot.localPosition.y 
            + GetForwardVector() * slot.localPosition.z;
    }
    
    void UpdateFormationPosition()
    {
        if (player == null) return;
        
        if (useSplineFollowing && levelManager != null && levelManager.LevelPath != null)
        {
            UpdateSplineBasedPosition();
        }
        else
        {
            targetFormationCenter = player.position + player.forward * splineOffset;
            formationRotation = player.rotation;
        }
        
        formationCenter = Vector3.Lerp(formationCenter, targetFormationCenter, Time.deltaTime * followSmoothness);
    }
    
    void UpdateSplineBasedPosition()
    {
        float playerT = levelManager.GetCurrentSplineT();
        SplinePath<Spline> splinePath = new SplinePath<Spline>(levelManager.LevelPath.Splines);
        float splineLength = splinePath.GetLength();
        float normalizedOffset = splineOffset / splineLength;
        float targetT = playerT + normalizedOffset;
        
        if (levelManager.LevelPath.Splines.Count > 0)
        {
            if (targetT > 1f)
                targetT = targetT % 1f;
        }
        
        Vector3 splinePosition = levelManager.LevelPath.EvaluatePosition(targetT);
        Vector3 splineTangent = levelManager.LevelPath.EvaluateTangent(targetT);
        Vector3 splineUp = levelManager.LevelPath.EvaluateUpVector(targetT);
        
        targetFormationCenter = splinePosition + splineUp * heightAboveSpline;
        
        if (splineTangent != Vector3.zero)
        {
            formationRotation = Quaternion.LookRotation(splineTangent, splineUp);
        }
    }
    
    Vector3 GetRightVector()
    {
        return useSplineFollowing && levelManager != null ? formationRotation * Vector3.right : player.right;
    }
    
    Vector3 GetUpVector()
    {
        return useSplineFollowing && levelManager != null ? formationRotation * Vector3.up : Vector3.up;
    }
    
    Vector3 GetForwardVector()
    {
        return useSplineFollowing && levelManager != null ? formationRotation * Vector3.forward : player.forward;
    }
    
    // Formation generation methods
    void GenerateVShapeSlots()
    {
        formationSlots.Clear();
        int totalSlots = Mathf.Min(maxSlotsPerFormation, 20); // V-shape looks good with ~20 slots
        
        for (int i = 0; i < totalSlots; i++)
        {
            bool isLeftSide = i % 2 == 0;
            int positionIndex = i / 2;
            
            float horizontalOffset = positionIndex * enemySpacing * 0.7f;
            float verticalOffset = positionIndex * enemySpacing;
            
            if (isLeftSide)
                horizontalOffset *= -1;
            
            Vector3 localPosition = new Vector3(horizontalOffset, verticalOffset, 0);
            formationSlots.Add(new FormationSlot(localPosition));
        }
    }
    
    void GenerateTriangleSlots()
    {
        formationSlots.Clear();
        int rows = Mathf.CeilToInt(Mathf.Sqrt(maxSlotsPerFormation * 2));
        
        for (int row = 0; row < rows; row++)
        {
            int slotsInRow = row + 1;
            float rowWidth = (slotsInRow - 1) * enemySpacing;
            
            for (int col = 0; col < slotsInRow; col++)
            {
                float xOffset = -rowWidth / 2f + col * enemySpacing;
                float yOffset = row * enemySpacing;
                
                Vector3 localPosition = new Vector3(xOffset, yOffset, 0);
                formationSlots.Add(new FormationSlot(localPosition));
                
                if (formationSlots.Count >= maxSlotsPerFormation)
                    return;
            }
        }
    }
    
    void GenerateCircleSlots()
    {
        formationSlots.Clear();
        int slotCount = Mathf.Min(maxSlotsPerFormation, 24); // Circles look good with 24 slots
        float radius = formationSize / 2f;
        float angleStep = 360f / slotCount;
        
        for (int i = 0; i < slotCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            
            Vector3 localPosition = new Vector3(x, y, 0);
            formationSlots.Add(new FormationSlot(localPosition));
        }
    }
    
    void GenerateSquareSlots()
    {
        formationSlots.Clear();
        float halfSize = formationSize / 2f;
        int slotsPerSide = Mathf.CeilToInt(maxSlotsPerFormation / 4f);
        
        // Generate slots for each edge
        for (int side = 0; side < 4; side++)
        {
            for (int i = 0; i < slotsPerSide; i++)
            {
                float t = slotsPerSide > 1 ? (float)i / (slotsPerSide - 1) : 0.5f;
                Vector3 localPosition = Vector3.zero;
                
                switch (side)
                {
                    case 0: // Top edge
                        localPosition = new Vector3(Mathf.Lerp(-halfSize, halfSize, t), halfSize, 0);
                        break;
                    case 1: // Right edge
                        localPosition = new Vector3(halfSize, Mathf.Lerp(halfSize, -halfSize, t), 0);
                        break;
                    case 2: // Bottom edge
                        localPosition = new Vector3(Mathf.Lerp(halfSize, -halfSize, t), -halfSize, 0);
                        break;
                    case 3: // Left edge
                        localPosition = new Vector3(-halfSize, Mathf.Lerp(-halfSize, halfSize, t), 0);
                        break;
                }
                
                formationSlots.Add(new FormationSlot(localPosition));
                
                if (formationSlots.Count >= maxSlotsPerFormation)
                    return;
            }
        }
    }
    
    void GenerateGridSlots()
    {
        formationSlots.Clear();
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(maxSlotsPerFormation));
        float offset = (gridSize - 1) * enemySpacing * 0.5f;
        
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                float xPos = (col * enemySpacing) - offset;
                float yPos = (row * enemySpacing) - offset;
                
                Vector3 localPosition = new Vector3(xPos, yPos, 0);
                formationSlots.Add(new FormationSlot(localPosition));
                
                if (formationSlots.Count >= maxSlotsPerFormation)
                    return;
            }
        }
    }
    
    // Public control methods
    public void SetFormationType(FormationType newFormation)
    {
        if (currentFormation != newFormation)
        {
            currentFormation = newFormation;
            
            // Clear all assignments when changing formation
            foreach (var slot in formationSlots)
            {
                slot.occupant = null;
                slot.isReserved = false;
            }
            enemyToSlotMap.Clear();
            
            // Move all assigned enemies back to unassigned
            GameObject[] allEnemies = GameObject.FindGameObjectsWithTag(enemyTag);
            unassignedEnemies.Clear();
            unassignedEnemies.AddRange(allEnemies.Select(e => e.transform));
            
            // Generate new formation slots
            GenerateFormationSlots();
        }
    }
    
    public void SetFormationSize(float newSize)
    {
        formationSize = Mathf.Max(1f, newSize);
        enemySpacing = formationSize / Mathf.Max(5f, Mathf.Sqrt(maxSlotsPerFormation));
        GenerateFormationSlots(); // Regenerate with new spacing
    }
    
    public void CycleFormation()
    {
        int currentIndex = (int)currentFormation;
        int formationCount = System.Enum.GetValues(typeof(FormationType)).Length;
        currentIndex = (currentIndex + 1) % formationCount;
        SetFormationType((FormationType)currentIndex);
    }
    
    // Status methods
    public FormationType GetCurrentFormation() => currentFormation;
    public int GetAssignedEnemyCount() => enemyToSlotMap.Count;
    public int GetUnassignedEnemyCount() => unassignedEnemies.Count;
    public int GetEmptySlotCount() => formationSlots.Count(s => s.occupant == null && !s.isReserved);
    public int GetTotalSlotCount() => formationSlots.Count;
    
    // Debug visualization
    void OnDrawGizmos()
    {
        // Draw formation center
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(formationCenter, 1f);
        
        // Draw all slots
        foreach (FormationSlot slot in formationSlots)
        {
            Vector3 slotWorldPos = GetSlotWorldPosition(slot);
            
            if (slot.occupant != null)
            {
                // Occupied slot - green
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(slotWorldPos, 0.3f);
            }
            else if (slot.isReserved)
            {
                // Reserved but enemy hasn't arrived - yellow
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(slotWorldPos, 0.3f);
            }
            else
            {
                // Empty slot - red
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(slotWorldPos, 0.2f);
            }
        }
        
        // Draw unassigned enemies
        Gizmos.color = Color.cyan;
        foreach (Transform enemy in unassignedEnemies)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireCube(enemy.position, Vector3.one * 0.5f);
                Gizmos.DrawLine(enemy.position, formationCenter);
            }
        }
    }
}