using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

// FormationManager: Handles enemy formations for a chicken invaders-style game
// 
// Key Concepts:
// - Formation Boundary: A fixed rectangular area centered at the enemy position on the spline
//   * This boundary rotates to match the spline direction
//   * Formations move within this 2D plane, not in 3D space
// - Formation Center: Can move within the boundary based on FormationPosition setting
//   * Movement is constrained to the boundary's local 2D space (X/Y axes of the rotated boundary)
//   * Position offsets are applied in the same rotated space as the boundary
// - Crosshair Boundary: The area where the player can aim (2x LevelManager.EnemyBoundary)
// - Formation Boundary = Crosshair Boundary - (boundaryOffset * 2)
// - Multiple Formations: Can spawn multiple non-overlapping formations
//   * Grid and V-Shape formations are limited to 1 instance
//   * V-Shape auto-scales to fit boundary width and positions at bottom
//   * Other formations can have multiple instances based on formationCount
//   * Formations are automatically positioned to avoid overlap
//
// Features:
// - Multiple formation types (V-Shape, Square, Triangle, Circle, Grid)
// - Multiple formation instances with automatic separation
// - Dynamic slot management with occupation tracking
// - Fixed boundary at enemy position with adjustable offset from crosshair boundary
// - Formation center moves within the fixed boundary's 2D plane
// - Intelligent Spacing Constraint System:
//   * Automatically detects when formation exceeds boundary
//   * Iteratively reduces spacing until all slots fit within bounds
//   * Works with all formation positions (center, corners, edges)
//   * Respects minimum spacing to prevent overlap
//   * Visual feedback shows when auto-adjustment is active
//   * Slots outside bounds are highlighted in red
// - Special Formations:
//   * Grid formations can fill entire boundary
//   * V-Shape formations auto-scale to boundary width and position at bottom
// - Follows spline path using exact enemy position from LevelManager
// - Boundary and formations rotate to match spline direction
//
// Usage:
// 1. Place FormationManager in scene
// 2. Configure formation type and parameters
// 3. Set boundary offset from crosshair boundary
// 4. Set formation count (1-10, Grid and V-Shape always use 1)
// 5. Enable constrainToBoundary for automatic fitting
// 6. Choose formation position (center, corners, etc.) - V-Shape ignores this
// 7. Formations will automatically adjust spacing to fit!

public class FormationManager : MonoBehaviour
{
    #region Enums and Classes

    public enum FormationType
    {
        VShape,
        Square2D,
        Triangle2D,
        Circle,
        Grid
    }

    public enum FormationPosition
    {
        Center,
        Random,
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    [System.Serializable]
    public class FormationSlot
    {
        public Vector3 localPosition;
        public bool isOccupied;
        public GameObject occupant;
        public int formationIndex; // Which formation instance this slot belongs to

        public FormationSlot(Vector3 position, int index = 0)
        {
            localPosition = position;
            isOccupied = false;
            occupant = null;
            formationIndex = index;
        }
    }
    
    [System.Serializable]
    public struct FormationStatistics
    {
        public FormationType formationType;
        public int totalSlots;
        public int occupiedSlots;
        public int availableSlots;
        public float spacingMultiplier;
        public bool isWithinBounds;
        public Vector2 positionOffset;
        public Bounds formationBounds;
        public int activeFormations;
    }
    
    [System.Serializable]
    public class FormationInstance
    {
        public List<FormationSlot> slots = new List<FormationSlot>();
        public Vector2 positionOffset = Vector2.zero;
        public float spacingMultiplier = 1f;
        public int index;
        public Bounds bounds;
        
        public FormationInstance(int idx)
        {
            index = idx;
        }
    }

    #endregion

    #region Inspector Fields

    [Header("Formation Settings")]
    [SerializeField] private FormationType currentFormation = FormationType.VShape;
    [SerializeField] private bool autoUpdateFormation = true;
    [SerializeField]
    [Tooltip("Number of formation instances to spawn. Grid and V-Shape formations always use 1.")]
    [Range(1, 10)] private int formationCount = 1;
    [SerializeField] 
    [Tooltip("Where the formation center should be positioned within the boundary. V-Shape formations ignore this and always position at bottom.")]
    private FormationPosition formationPosition = FormationPosition.Center;
    [SerializeField, Range(0f, 1f)] private float boundaryPadding = 0.1f;
    [SerializeField]
    [Tooltip("Minimum separation between formation instances as a percentage of formation size.")]
    [Range(0.1f, 2f)] private float formationSeparation = 1.2f;
    
    [Header("Boundary Settings")]
    [SerializeField, Min(0f)] 
    [Tooltip("Reduces formation boundary size on all edges compared to crosshair boundary. Use 0 for same size as crosshair.")]
    private float boundaryOffset = 2f; // Default 2 units smaller on each edge

    [Header("Spline Rotation")]
    [SerializeField, Min(0)] private float splineRotationSpeed = 5f;

    [Header("Formation Parameters")]
    [SerializeField, Min(3)] 
    [Tooltip("Number of slots in the V-Shape formation. Should be odd for symmetry.")]
    private int vShapeCount = 19;
    [SerializeField, Min(2)] private int squareSize = 4;
    [SerializeField, Min(3)] private int triangleRows = 4;
    [SerializeField, Min(8)] private int circleCount = 12;
    [SerializeField] private Vector2Int gridSize = new Vector2Int(5, 3);
    [SerializeField] private bool gridFillsBoundary = true;

    [Header("Spacing")]
    [SerializeField] 
    [Tooltip("Horizontal spacing between formation slots. For V-Shape, this controls the width of the V.")]
    private float horizontalSpacing = 2f;
    [SerializeField] 
    [Tooltip("Vertical spacing between formation slots. For V-Shape, this controls the height of the V.")]
    private float verticalSpacing = 2f;
    [SerializeField] private float circleRadius = 5f;
    [SerializeField] private bool constrainToBoundary = true;
    [SerializeField] 
    [Tooltip("When enabled, formations will scale up to fill available boundary space, not just scale down to fit. V-Shape always fills width regardless.")]
    private bool optimizeBoundaryUsage = false;
    [SerializeField, Min(0.1f)] private float minSpacingMultiplier = 0.3f;
    [SerializeField, Min(1f)] private float maxSpacingMultiplier = 2f;
    
    [SerializeField, Min(0f)] private float vShapeBottomPadding = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color openSlotColor = Color.green;
    [SerializeField] private Color occupiedSlotColor = Color.red;
    [SerializeField] private float gizmoSize = 0.5f;
    [SerializeField] private bool showFormationBounds = true;
    [SerializeField] private Color[] formationColors = new Color[] 
    { 
        Color.cyan, Color.magenta, Color.yellow, Color.blue, Color.green,
        Color.red, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f), new Color(0f, 1f, 0.5f), Color.white
    };

    #endregion

    #region Private Fields

    private List<FormationSlot> formationSlots = new List<FormationSlot>();
    private List<FormationInstance> formations = new List<FormationInstance>();
    private FormationType lastFormationType;
    private int lastFormationCount = 1;
    private Vector3 formationCenter; // World position at enemy location on spline
    private float currentSpacingMultiplier = 1f;
    private Quaternion splineRotation = Quaternion.identity;

    #endregion

    #region Properties

    public List<FormationSlot> FormationSlots => formationSlots;
    public FormationType CurrentFormation => currentFormation;
    public Vector3 FormationCenter => formationCenter;
    public float CurrentSpacingMultiplier => currentSpacingMultiplier;
    public bool IsGridFillingBoundary => currentFormation == FormationType.Grid && gridFillsBoundary;
    public bool IsVShapeFormation => currentFormation == FormationType.VShape;
    public Vector3 FormationWorldCenter
    {
        get
        {
            var offset = formations.Count > 0 ? formations[0].positionOffset : Vector2.zero;
            return formationCenter + (splineRotation * new Vector3(offset.x, offset.y, 0));
        }
    }
    public FormationPosition CurrentPosition => formationPosition;
    public Quaternion SplineRotation => splineRotation;
    public Vector2 PositionOffset => formations.Count > 0 ? formations[0].positionOffset : Vector2.zero;
    public int FormationCount => (currentFormation == FormationType.Grid || currentFormation == FormationType.VShape) ? 1 : formationCount;
    public List<FormationInstance> Formations => formations;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        GenerateFormations();
        lastFormationType = currentFormation;
        lastFormationCount = formationCount;
    }

    private void Update()
    {
        HandleSplineRotation();
        UpdateFormationCenter();

        bool needsRegeneration = false;
        
        if (autoUpdateFormation && lastFormationType != currentFormation)
        {
            needsRegeneration = true;
            lastFormationType = currentFormation;
        }
        
        if (lastFormationCount != formationCount)
        {
            needsRegeneration = true;
            lastFormationCount = formationCount;
        }
        
        if (needsRegeneration)
        {
            GenerateFormations();
        }
        
        // Runtime boundary check - useful if boundary size changes
        if (Application.isPlaying && constrainToBoundary && formationSlots.Count > 0)
        {
            // Only check periodically for performance
            if (Time.frameCount % 30 == 0) // Check every 30 frames
            {
                foreach (var formation in formations)
                {
                    if (!IsFormationInstanceWithinBounds(formation))
                    {
                        GenerateFormations();
                        break;
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        HandleSplineRotation();
        UpdateFormationCenter();
        
        DrawBoundary();
        DrawFormationSlots();
        DrawFormationInfo();
    }

    #endregion

    #region Spline Rotation

    private void HandleSplineRotation()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.SplineContainer)
        {
            splineRotation = Quaternion.identity;
            return;
        }

        Vector3 splineForward = GetSplineDirection();
        
        if (splineForward != Vector3.zero)
        {
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineForward, Vector3.up);
            splineRotation = Quaternion.Slerp(splineRotation, targetSplineRotation, splineRotationSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetSplineDirection()
    {
        return !LevelManager.Instance ? Vector3.forward : LevelManager.Instance.GetEnemyDirectionOnSpline(LevelManager.Instance.EnemyPosition);
    }

    #endregion

    #region Formation Generation

    public void GenerateFormations()
    {
        currentSpacingMultiplier = 1f;
        formations.Clear();
        formationSlots.Clear();
        
        int actualCount = (currentFormation == FormationType.Grid || currentFormation == FormationType.VShape) ? 1 : formationCount;
        
        // Generate formations
        for (int i = 0; i < actualCount; i++)
        {
            var formationInstance = new FormationInstance(i);
            formations.Add(formationInstance);
            GenerateFormationInstance(formationInstance);
        }
        
        // Position formations to avoid overlap
        PositionMultipleFormations();
        
        // Rebuild the main slot list
        RebuildSlotList();
    }
    
    private void GenerateFormationInstance(FormationInstance formation)
    {
        formation.slots.Clear();
        formation.spacingMultiplier = 1f;
        
        switch (currentFormation)
        {
            case FormationType.VShape:
                GenerateVShape(formation);
                break;
            case FormationType.Square2D:
                GenerateSquare2D(formation);
                break;
            case FormationType.Triangle2D:
                GenerateTriangle2D(formation);
                break;
            case FormationType.Circle:
                GenerateCircle(formation);
                break;
            case FormationType.Grid:
                GenerateGrid(formation);
                break;
        }
        
        formation.bounds = CalculateFormationInstanceBounds(formation);
    }
    
    private void PositionMultipleFormations()
    {
        if (formations.Count <= 1)
        {
            if (formations.Count == 1)
            {
                ApplyFormationPosition(formations[0]);
                
                if (constrainToBoundary && !IsGridFillingBoundary)
                {
                    ApplyBoundaryConstraintsAtPosition(formations[0]);
                }
            }
            return;
        }
        
        // Multiple formations - distribute them to avoid overlap
        Vector2 boundary = GetFormationBoundary();
        
        float maxFormationWidth = 0f;
        float maxFormationHeight = 0f;
        
        foreach (var formation in formations)
        {
            maxFormationWidth = Mathf.Max(maxFormationWidth, formation.bounds.size.x);
            maxFormationHeight = Mathf.Max(maxFormationHeight, formation.bounds.size.y);
        }
        
        float separationDistance = maxFormationWidth * formationSeparation;
        
        // Position formations based on count
        if (formations.Count == 2)
        {
            formations[0].positionOffset = new Vector2(-separationDistance * 0.5f, 0);
            formations[1].positionOffset = new Vector2(separationDistance * 0.5f, 0);
        }
        else if (formations.Count == 3)
        {
            formations[0].positionOffset = new Vector2(-separationDistance * 2f, 0);
            formations[1].positionOffset = new Vector2(0, 0);
            formations[2].positionOffset = new Vector2(separationDistance * 2f, 0);
        }
        else if (formations.Count == 4)
        {
            float halfSep = separationDistance * 0.5f;
            formations[0].positionOffset = new Vector2(-halfSep, halfSep);
            formations[1].positionOffset = new Vector2(halfSep, halfSep);
            formations[2].positionOffset = new Vector2(-halfSep, -halfSep);
            formations[3].positionOffset = new Vector2(halfSep, -halfSep);
        }
        else
        {
            // Grid layout for more formations
            int cols = Mathf.CeilToInt(Mathf.Sqrt(formations.Count));
            int rows = Mathf.CeilToInt((float)formations.Count / cols);
            
            float colSpacing = separationDistance;
            float rowSpacing = separationDistance * 0.8f;
            
            float totalLayoutWidth = (cols - 1) * colSpacing;
            float totalLayoutHeight = (rows - 1) * rowSpacing;
            
            int index = 0;
            for (int row = 0; row < rows && index < formations.Count; row++)
            {
                for (int col = 0; col < cols && index < formations.Count; col++)
                {
                    float x = (col * colSpacing) - (totalLayoutWidth * 0.5f);
                    float y = (row * rowSpacing) - (totalLayoutHeight * 0.5f);
                    formations[index].positionOffset = new Vector2(x, y);
                    index++;
                }
            }
        }
        
        // Apply boundary constraints to each formation
        if (constrainToBoundary)
        {
            bool allFit = true;
            foreach (var formation in formations)
            {
                if (!IsFormationInstanceWithinBounds(formation))
                {
                    allFit = false;
                    break;
                }
            }
            
            if (!allFit)
            {
                float scaleReduction = 0.9f;
                int maxIterations = 10;
                int iteration = 0;
                
                while (!allFit && iteration < maxIterations)
                {
                    iteration++;
                    
                    foreach (var formation in formations)
                    {
                        formation.spacingMultiplier *= scaleReduction;
                        GenerateFormationInstance(formation);
                    }
                    
                    separationDistance *= scaleReduction;
                    PositionFormationsWithSeparation(separationDistance);
                    
                    allFit = true;
                    foreach (var formation in formations)
                    {
                        if (!IsFormationInstanceWithinBounds(formation))
                        {
                            allFit = false;
                            break;
                        }
                    }
                    
                    if (formations.Count > 0)
                    {
                        currentSpacingMultiplier = formations[0].spacingMultiplier;
                    }
                }
                
                if (!allFit && Application.isPlaying)
                {
                    Debug.LogWarning($"Multiple formations cannot fit within bounds even at minimum spacing!");
                }
            }
        }
    }
    
    private void PositionFormationsWithSeparation(float separationDistance)
    {
        if (formations.Count == 2)
        {
            formations[0].positionOffset = new Vector2(-separationDistance * 0.5f, 0);
            formations[1].positionOffset = new Vector2(separationDistance * 0.5f, 0);
        }
        else if (formations.Count == 3)
        {
            formations[0].positionOffset = new Vector2(-separationDistance * 2f, 0);
            formations[1].positionOffset = new Vector2(0, 0);
            formations[2].positionOffset = new Vector2(separationDistance * 2f, 0);
        }
        else if (formations.Count == 4)
        {
            float halfSep = separationDistance * 0.5f;
            formations[0].positionOffset = new Vector2(-halfSep, halfSep);
            formations[1].positionOffset = new Vector2(halfSep, halfSep);
            formations[2].positionOffset = new Vector2(-halfSep, -halfSep);
            formations[3].positionOffset = new Vector2(halfSep, -halfSep);
        }
        else
        {
            int cols = Mathf.CeilToInt(Mathf.Sqrt(formations.Count));
            int rows = Mathf.CeilToInt((float)formations.Count / cols);
            
            float colSpacing = separationDistance;
            float rowSpacing = separationDistance * 0.8f;
            
            float totalLayoutWidth = (cols - 1) * colSpacing;
            float totalLayoutHeight = (rows - 1) * rowSpacing;
            
            int index = 0;
            for (int row = 0; row < rows && index < formations.Count; row++)
            {
                for (int col = 0; col < cols && index < formations.Count; col++)
                {
                    float x = (col * colSpacing) - (totalLayoutWidth * 0.5f);
                    float y = (row * rowSpacing) - (totalLayoutHeight * 0.5f);
                    formations[index].positionOffset = new Vector2(x, y);
                    index++;
                }
            }
        }
    }
    
    private void RebuildSlotList()
    {
        formationSlots.Clear();
        foreach (var formation in formations)
        {
            formationSlots.AddRange(formation.slots);
        }
    }
    
    private bool IsFormationInstanceWithinBounds(FormationInstance formation)
    {
        Vector2 boundary = GetFormationBoundary();
        
        foreach (var slot in formation.slots)
        {
            Vector3 worldPos = GetSlotWorldPosition(slot);
            Vector3 localPos = Quaternion.Inverse(splineRotation) * (worldPos - formationCenter);
            
            if (Mathf.Abs(localPos.x) > boundary.x * 0.5f || Mathf.Abs(localPos.y) > boundary.y * 0.5f)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private Bounds CalculateFormationInstanceBounds(FormationInstance formation)
    {
        if (formation.slots.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);
        
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        
        foreach (var slot in formation.slots)
        {
            Vector3 pos = slot.localPosition;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }
        
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        
        return new Bounds(center, size);
    }

    private void GenerateVShape(FormationInstance formation)
    {
        int halfCount = vShapeCount / 2;
        var spacing = GetAdjustedSpacing(formation.spacingMultiplier);

        for (int i = 0; i < vShapeCount; i++)
        {
            float xPos = (i - halfCount) * spacing.x;
            float yPos = Mathf.Abs(i - halfCount) * spacing.y;
            AddSlot(formation, new Vector3(xPos, yPos, 0));
        }
        
        // Normalize so lowest point is at y=0
        if (formation.slots.Count > 0)
        {
            float minY = float.MaxValue;
            foreach (var slot in formation.slots)
            {
                minY = Mathf.Min(minY, slot.localPosition.y);
            }
            
            if (minY != 0)
            {
                foreach (var slot in formation.slots)
                {
                    slot.localPosition = new Vector3(
                        slot.localPosition.x,
                        slot.localPosition.y - minY,
                        slot.localPosition.z
                    );
                }
            }
        }
    }

    private void GenerateSquare2D(FormationInstance formation)
    {
        float halfSize = (squareSize - 1) * 0.5f;
        var spacing = GetAdjustedSpacing(formation.spacingMultiplier);

        for (int y = 0; y < squareSize; y++)
        {
            for (int x = 0; x < squareSize; x++)
            {
                float xPos = (x - halfSize) * spacing.x;
                float yPos = (y - halfSize) * spacing.y;
                AddSlot(formation, new Vector3(xPos, yPos, 0));
            }
        }
    }

    private void GenerateTriangle2D(FormationInstance formation)
    {
        var spacing = GetAdjustedSpacing(formation.spacingMultiplier);

        for (int row = 0; row < triangleRows; row++)
        {
            int slotsInRow = row + 1;
            float rowWidth = (slotsInRow - 1) * spacing.x;
            float startX = -rowWidth * 0.5f;
            float yPos = row * spacing.y;

            for (int i = 0; i < slotsInRow; i++)
            {
                float xPos = startX + (i * spacing.x);
                AddSlot(formation, new Vector3(xPos, yPos, 0));
            }
        }
    }

    private void GenerateCircle(FormationInstance formation)
    {
        float angleStep = 360f / circleCount;
        float adjustedRadius = circleRadius * formation.spacingMultiplier;

        for (int i = 0; i < circleCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float xPos = Mathf.Cos(angle) * adjustedRadius;
            float yPos = Mathf.Sin(angle) * adjustedRadius;
            AddSlot(formation, new Vector3(xPos, yPos, 0));
        }
    }

    private void GenerateGrid(FormationInstance formation)
    {
        int columns = Mathf.Max(1, gridSize.x);
        int rows = Mathf.Max(1, gridSize.y);

        if (gridFillsBoundary && LevelManager.Instance)
        {
            Vector2 boundary = GetFormationBoundary();
            
            float safetyMargin = constrainToBoundary ? 0.95f : 1f;
            Vector2 safeBoundary = boundary * safetyMargin;

            float horizontalSpace = columns > 1 ? safeBoundary.x / (columns - 1) : 0;
            float verticalSpace = rows > 1 ? safeBoundary.y / (rows - 1) : 0;

            horizontalSpace *= formation.spacingMultiplier;
            verticalSpace *= formation.spacingMultiplier;

            float gridWidth = (columns - 1) * horizontalSpace;
            float gridHeight = (rows - 1) * verticalSpace;

            float startX = -gridWidth * 0.5f;
            float startY = gridHeight * 0.5f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    float xPos = startX + x * horizontalSpace;
                    float yPos = startY - y * verticalSpace;
                    AddSlot(formation, new Vector3(xPos, yPos, 0));
                }
            }
        }
        else
        {
            var spacing = GetAdjustedSpacing(formation.spacingMultiplier);
            float halfWidth = (columns - 1) * 0.5f;
            float halfHeight = (rows - 1) * 0.5f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    float xPos = (x - halfWidth) * spacing.x;
                    float yPos = (y - halfHeight) * spacing.y;
                    AddSlot(formation, new Vector3(xPos, yPos, 0));
                }
            }
        }
    }

    #endregion

    #region Boundary Management
    
    private void ApplyBoundaryConstraintsAtPosition(FormationInstance formation)
    {
        if (!LevelManager.Instance) return;
        
        Vector2 boundary = GetFormationBoundary();
        
        var currentBounds = CalculateFormationBoundsWithOffset(formation);
        float currentWidth = currentBounds.z - currentBounds.x;
        float currentHeight = currentBounds.w - currentBounds.y;
        
        if (Application.isPlaying)
        {
            Debug.Log($"[FormationConstraints] Checking {currentFormation} - Boundary: {boundary.x:F1}x{boundary.y:F1}, Formation: {currentWidth:F1}x{currentHeight:F1}");
        }
        
        int maxIterations = 20;
        int iteration = 0;
        
        while (!IsFormationInstanceWithinBounds(formation) && iteration < maxIterations)
        {
            iteration++;
            
            float maxExceedX = 0f;
            float maxExceedY = 0f;
            
            foreach (var slot in formation.slots)
            {
                Vector3 worldPos = GetSlotWorldPosition(slot);
                Vector3 localPos = Quaternion.Inverse(splineRotation) * (worldPos - formationCenter);
                
                float exceedX = Mathf.Abs(localPos.x) - (boundary.x * 0.5f);
                float exceedY = Mathf.Abs(localPos.y) - (boundary.y * 0.5f);
                
                maxExceedX = Mathf.Max(maxExceedX, exceedX);
                maxExceedY = Mathf.Max(maxExceedY, exceedY);
            }
            
            var bounds = CalculateFormationBoundsWithOffset(formation);
            float formationWidth = bounds.z - bounds.x;
            float formationHeight = bounds.w - bounds.y;
            
            float reductionFactor = 1f;
            
            if (formationWidth > boundary.x)
            {
                float requiredScaleX = boundary.x / formationWidth;
                reductionFactor = Mathf.Min(reductionFactor, requiredScaleX);
            }
            
            if (formationHeight > boundary.y)
            {
                float requiredScaleY = boundary.y / formationHeight;
                reductionFactor = Mathf.Min(reductionFactor, requiredScaleY);
            }
            
            if (maxExceedX > 0 || maxExceedY > 0)
            {
                float currentScale = formation.spacingMultiplier;
                float targetWidth = boundary.x - (maxExceedX * 2f);
                float targetHeight = boundary.y - (maxExceedY * 2f);
                
                if (formationWidth > 0 && targetWidth > 0)
                {
                    float targetScaleX = targetWidth / (formationWidth / currentScale);
                    reductionFactor = Mathf.Min(reductionFactor, targetScaleX);
                }
                
                if (formationHeight > 0 && targetHeight > 0)
                {
                    float targetScaleY = targetHeight / (formationHeight / currentScale);
                    reductionFactor = Mathf.Min(reductionFactor, targetScaleY);
                }
            }
            
            reductionFactor = Mathf.Min(reductionFactor * 0.9f, 0.95f);
            
            formation.spacingMultiplier *= reductionFactor;
            formation.spacingMultiplier = Mathf.Max(formation.spacingMultiplier, minSpacingMultiplier);
            
            if (Application.isPlaying && iteration <= 5)
            {
                Debug.Log($"[FormationConstraints] Iteration {iteration}: Spacing {(formation.spacingMultiplier * 100):F0}%, Exceed X:{maxExceedX:F1} Y:{maxExceedY:F1}");
            }
            
            GenerateFormationInstance(formation);
            
            if (formation.spacingMultiplier <= minSpacingMultiplier)
            {
                break;
            }
        }
        
        currentSpacingMultiplier = formation.spacingMultiplier;
        
        if (Application.isPlaying)
        {
            bool fits = IsFormationInstanceWithinBounds(formation);
            Debug.Log($"[FormationConstraints] Final result: Fits={fits}, Spacing={(currentSpacingMultiplier * 100):F0}%, Iterations={iteration}");
            
            if (!fits)
            {
                Debug.LogWarning($"Formation '{currentFormation}' at position '{formationPosition}' cannot fit within bounds even at minimum spacing!");
            }
        }
    }
    
    private Vector4 CalculateFormationBoundsWithOffset(FormationInstance formation)
    {
        if (formation.slots.Count == 0) return Vector4.zero;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var slot in formation.slots)
        {
            float x = slot.localPosition.x + formation.positionOffset.x;
            float y = slot.localPosition.y + formation.positionOffset.y;
            
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }

        return new Vector4(minX, minY, maxX, maxY);
    }

    #endregion

    #region Formation Positioning
    
    private void ApplyFormationPosition(FormationInstance formation)
    {
        if (!LevelManager.Instance) return;

        if (IsGridFillingBoundary)
        {
            formation.positionOffset = Vector2.zero;
            return;
        }
        
        Vector2 boundary = GetFormationBoundary();
        var bounds = formation.bounds;
        
        if (currentFormation == FormationType.VShape)
        {
            float bottomBoundary = -(boundary.y * 0.5f);
            float targetCenterY = bottomBoundary + vShapeBottomPadding;
            formation.positionOffset = new Vector2(0, targetCenterY);
            
            if (Application.isPlaying)
            {
                Debug.Log($"[V-Shape Positioning] Boundary: {boundary.y:F1} (bottom at {bottomBoundary:F1}), " +
                         $"Target Y: {targetCenterY:F1}, Padding: {vShapeBottomPadding:F1}");
            }
            
            return;
        }

        float formationWidth = bounds.size.x;
        float formationHeight = bounds.size.y;

        float safetyMargin = constrainToBoundary ? 0.95f : 1f;
        
        float maxOffsetX = Mathf.Max(0, ((boundary.x * safetyMargin) - formationWidth) * 0.5f);
        float maxOffsetY = Mathf.Max(0, ((boundary.y * safetyMargin) - formationHeight) * 0.5f);

        float padding = boundaryPadding * Mathf.Min(boundary.x, boundary.y) * 0.5f;
        maxOffsetX -= padding;
        maxOffsetY -= padding;

        if (maxOffsetX <= 0 || maxOffsetY <= 0)
        {
            formation.positionOffset = Vector2.zero;
            return;
        }

        switch (formationPosition)
        {
            case FormationPosition.Center:
                formation.positionOffset = Vector2.zero;
                break;
            case FormationPosition.Random:
                formation.positionOffset = new Vector2(
                    UnityEngine.Random.Range(-maxOffsetX, maxOffsetX),
                    UnityEngine.Random.Range(-maxOffsetY, maxOffsetY)
                );
                break;
            case FormationPosition.TopLeft:
                formation.positionOffset = new Vector2(-maxOffsetX, maxOffsetY);
                break;
            case FormationPosition.TopCenter:
                formation.positionOffset = new Vector2(0, maxOffsetY);
                break;
            case FormationPosition.TopRight:
                formation.positionOffset = new Vector2(maxOffsetX, maxOffsetY);
                break;
            case FormationPosition.MiddleLeft:
                formation.positionOffset = new Vector2(-maxOffsetX, 0);
                break;
            case FormationPosition.MiddleRight:
                formation.positionOffset = new Vector2(maxOffsetX, 0);
                break;
            case FormationPosition.BottomLeft:
                formation.positionOffset = new Vector2(-maxOffsetX, -maxOffsetY);
                break;
            case FormationPosition.BottomCenter:
                formation.positionOffset = new Vector2(0, -maxOffsetY);
                break;
            case FormationPosition.BottomRight:
                formation.positionOffset = new Vector2(maxOffsetX, -maxOffsetY);
                break;
        }
    }

    #endregion

    #region Slot Management

    public FormationSlot TryOccupySlot(GameObject occupant)
    {
        foreach (var slot in formationSlots)
        {
            if (!slot.isOccupied)
            {
                slot.isOccupied = true;
                slot.occupant = occupant;
                return slot;
            }
        }
        return null;
    }
    
    public FormationSlot TryOccupySlotInFormation(GameObject occupant, int formationIndex)
    {
        if (formationIndex < 0 || formationIndex >= formations.Count) return null;
        
        foreach (var slot in formations[formationIndex].slots)
        {
            if (!slot.isOccupied)
            {
                slot.isOccupied = true;
                slot.occupant = occupant;
                return slot;
            }
        }
        return null;
    }

    public void ReleaseSlot(FormationSlot slot)
    {
        if (slot != null)
        {
            slot.isOccupied = false;
            slot.occupant = null;
        }
    }

    public FormationSlot GetNearestAvailableSlot(Vector3 worldPosition)
    {
        FormationSlot nearestSlot = null;
        float nearestDistance = float.MaxValue;

        foreach (var slot in formationSlots)
        {
            if (!slot.isOccupied)
            {
                float distance = Vector3.Distance(worldPosition, GetSlotWorldPosition(slot));
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSlot = slot;
                }
            }
        }

        return nearestSlot;
    }

    public List<FormationSlot> GetAvailableSlots()
    {
        return formationSlots.FindAll(slot => !slot.isOccupied);
    }
    
    public List<FormationSlot> GetAvailableSlotsInFormation(int formationIndex)
    {
        if (formationIndex < 0 || formationIndex >= formations.Count) return new List<FormationSlot>();
        return formations[formationIndex].slots.FindAll(slot => !slot.isOccupied);
    }

    public Vector3 GetSlotWorldPosition(FormationSlot slot)
    {
        FormationInstance formationInstance = null;
        foreach (var formation in formations)
        {
            if (formation.slots.Contains(slot))
            {
                formationInstance = formation;
                break;
            }
        }
        
        if (formationInstance == null)
        {
            return formationCenter + slot.localPosition;
        }
        
        Vector3 worldPosition = formationCenter;
        Vector3 rotatedOffset = splineRotation * new Vector3(formationInstance.positionOffset.x, formationInstance.positionOffset.y, 0);
        worldPosition += rotatedOffset;
        worldPosition += splineRotation * slot.localPosition;
        
        return worldPosition;
    }
    
    public Vector3 GetSlotLocalPosition(FormationSlot slot)
    {
        FormationInstance formationInstance = null;
        foreach (var formation in formations)
        {
            if (formation.slots.Contains(slot))
            {
                formationInstance = formation;
                break;
            }
        }
        
        if (formationInstance == null)
        {
            return slot.localPosition;
        }
        
        return slot.localPosition + new Vector3(formationInstance.positionOffset.x, formationInstance.positionOffset.y, 0);
    }
    
    public Vector3 LocalToWorldPosition(Vector2 localPos)
    {
        return formationCenter + (splineRotation * new Vector3(localPos.x, localPos.y, 0));
    }
    
    public Vector2 WorldToLocalPosition(Vector3 worldPos)
    {
        Vector3 localPos3D = Quaternion.Inverse(splineRotation) * (worldPos - formationCenter);
        return new Vector2(localPos3D.x, localPos3D.y);
    }

    #endregion

    #region Helper Methods

    private void UpdateFormationCenter()
    {
        if (!LevelManager.Instance) return;
        formationCenter = LevelManager.Instance.EnemyPosition;
    }

    private Vector2 GetFormationBoundary()
    {
        if (!LevelManager.Instance) return new Vector2(50f, 30f);
        
        Vector2 crosshairBoundary = LevelManager.Instance.EnemyBoundary * 2f;
        
        return new Vector2(
            Mathf.Max(1f, crosshairBoundary.x - (boundaryOffset * 2f)),
            Mathf.Max(1f, crosshairBoundary.y - (boundaryOffset * 2f))
        );
    }

    private Vector2 GetAdjustedSpacing(float multiplier)
    {
        return new Vector2(
            horizontalSpacing * multiplier,
            verticalSpacing * multiplier
        );
    }
    
    public Vector2 GetCurrentSpacing()
    {
        return GetAdjustedSpacing(currentSpacingMultiplier);
    }
    
    public float GetCurrentCircleRadius()
    {
        return circleRadius * currentSpacingMultiplier;
    }
    
    private void AddSlot(FormationInstance formation, Vector3 position)
    {
        var slot = new FormationSlot(position, formation.index);
        formation.slots.Add(slot);
    }

    public Vector2Int GetCurrentGridDimensions()
    {
        return currentFormation == FormationType.Grid ? gridSize : Vector2Int.zero;
    }

    public void SetFormationPosition(FormationPosition position)
    {
        formationPosition = position;
        
        if (currentFormation == FormationType.VShape)
        {
            Debug.LogWarning("V-Shape formations always position at the bottom of the boundary and ignore FormationPosition settings.");
        }
        
        if (formationSlots.Count > 0)
        {
            GenerateFormations();
        }
    }
    
    public void SetPositionOffset(Vector2 offset)
    {
        if (formations.Count > 0)
        {
            formations[0].positionOffset = offset;
            
            if (constrainToBoundary && !IsGridFillingBoundary)
            {
                if (!IsFormationInstanceWithinBounds(formations[0]))
                {
                    ApplyBoundaryConstraintsAtPosition(formations[0]);
                }
            }
            
            RebuildSlotList();
        }
    }
    
    public Bounds GetFormationWorldBounds()
    {
        if (formationSlots.Count == 0) return new Bounds(formationCenter, Vector3.zero);
        
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        
        foreach (var slot in formationSlots)
        {
            Vector3 worldPos = GetSlotWorldPosition(slot);
            min = Vector3.Min(min, worldPos);
            max = Vector3.Max(max, worldPos);
        }
        
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        
        return new Bounds(center, size);
    }
    
    public Vector2 GetMaximumOffset()
    {
        if (formations.Count == 0) return Vector2.zero;
        
        Vector2 boundary = GetFormationBoundary();
        
        float maxFormationWidth = 0f;
        float maxFormationHeight = 0f;
        
        foreach (var formation in formations)
        {
            maxFormationWidth = Mathf.Max(maxFormationWidth, formation.bounds.size.x);
            maxFormationHeight = Mathf.Max(maxFormationHeight, formation.bounds.size.y);
        }
        
        if (formations.Count > 1)
        {
            float separationDistance = maxFormationWidth * formationSeparation;
            maxFormationWidth += separationDistance * (formations.Count - 1);
            maxFormationHeight += separationDistance * (formations.Count - 1);
        }
        
        float maxOffsetX = Mathf.Max(0, (boundary.x - maxFormationWidth) * 0.5f);
        float maxOffsetY = Mathf.Max(0, (boundary.y - maxFormationHeight) * 0.5f);
        
        return new Vector2(maxOffsetX, maxOffsetY);
    }
    
    public bool IsCurrentFormationWithinBounds()
    {
        foreach (var formation in formations)
        {
            if (!IsFormationInstanceWithinBounds(formation))
            {
                return false;
            }
        }
        return true;
    }
    
    public FormationStatistics GetFormationStatistics()
    {
        var stats = new FormationStatistics();
        stats.formationType = currentFormation;
        stats.totalSlots = formationSlots.Count;
        stats.occupiedSlots = formationSlots.FindAll(s => s.isOccupied).Count;
        stats.availableSlots = stats.totalSlots - stats.occupiedSlots;
        stats.spacingMultiplier = currentSpacingMultiplier;
        stats.isWithinBounds = IsCurrentFormationWithinBounds();
        stats.positionOffset = formations.Count > 0 ? formations[0].positionOffset : Vector2.zero;
        stats.formationBounds = GetFormationWorldBounds();
        stats.activeFormations = formations.Count;
        return stats;
    }

    #endregion

    #region Editor Methods

    [Button]
    private void ChangeFormation()
    {
        int nextFormation = ((int)currentFormation + 1) % Enum.GetValues(typeof(FormationType)).Length;
        currentFormation = (FormationType)nextFormation;
        GenerateFormations();
    }

    [Button]
    private void RegenerateFormation()
    {
        GenerateFormations();
    }

    [Button]
    private void RandomizePosition()
    {
        if (currentFormation == FormationType.VShape)
        {
            Debug.Log("V-Shape formations always position at bottom - randomization has no effect.");
            GenerateFormations();
            return;
        }
        
        if (formationSlots.Count > 0)
        {
            var previousPosition = formationPosition;
            formationPosition = FormationPosition.Random;
            GenerateFormations();
            formationPosition = previousPosition;
        }
    }

    [Button]
    private void CyclePosition()
    {
        if (currentFormation == FormationType.VShape)
        {
            Debug.Log("V-Shape formations always position at bottom - position cycling has no effect.");
            return;
        }
        
        int nextPosition = ((int)formationPosition + 1) % Enum.GetValues(typeof(FormationPosition)).Length;
        formationPosition = (FormationPosition)nextPosition;
        
        if (formationSlots.Count > 0)
        {
            GenerateFormations();
        }
    }

    [Button]
    private void RecalculateBoundaryConstraints()
    {
        GenerateFormations();
    }
    
    [Button]
    private void DebugPositionSpace()
    {
        Debug.Log($"=== Position Space Debug ===");
        Debug.Log($"Boundary is rotated: {splineRotation.eulerAngles}");
        Debug.Log($"Formation world center: {FormationWorldCenter}");
        Debug.Log($"Boundary center: {formationCenter}");
        Debug.Log($"Active Formations: {formations.Count}");
    }

    #endregion

    #region Gizmo Drawing

    private void DrawBoundary()
    {
        if (!LevelManager.Instance) return;
        
        bool isWithinBounds = true;
        if (formationSlots.Count > 0)
        {
            isWithinBounds = IsCurrentFormationWithinBounds();
        }
        
        if (!isWithinBounds)
        {
            Gizmos.color = Color.red;
        }
        else if (constrainToBoundary)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.gray;
        }
        
        Vector3 boundaryCenter = formationCenter;
        Vector2 formationBoundary = GetFormationBoundary();
        
        Vector3 boundarySize = new Vector3(formationBoundary.x, formationBoundary.y, 0.1f);

        Vector3[] localCorners = new Vector3[]
        {
            new Vector3(-boundarySize.x * 0.5f, -boundarySize.y * 0.5f, 0),
            new Vector3(boundarySize.x * 0.5f, -boundarySize.y * 0.5f, 0),
            new Vector3(boundarySize.x * 0.5f, boundarySize.y * 0.5f, 0),
            new Vector3(-boundarySize.x * 0.5f, boundarySize.y * 0.5f, 0)
        };

        Vector3[] worldCorners = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            worldCorners[i] = boundaryCenter + (splineRotation * localCorners[i]);
        }

        for (int i = 0; i < 4; i++)
        {
            int nextIndex = (i + 1) % 4;
            Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
        }
        
        Gizmos.color *= 0.3f;
        Gizmos.DrawLine(worldCorners[0], worldCorners[2]);
        Gizmos.DrawLine(worldCorners[1], worldCorners[3]);
        
        #if UNITY_EDITOR
        Vector3 planeLabel = boundaryCenter + (splineRotation * Vector3.up * (boundarySize.y * 0.5f + 1f));
        UnityEditor.Handles.Label(planeLabel, "2D Boundary Plane");
        
        if (showGizmos && formationPosition != FormationPosition.Center)
        {
            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.05f);
            UnityEditor.Handles.DrawSolidRectangleWithOutline(worldCorners, new Color(1f, 1f, 0f, 0.05f), Color.clear);
        }
        #endif
    }

    private void DrawFormationSlots()
    {
        Vector2 boundary = GetFormationBoundary();
        
        if (showFormationBounds && formations.Count > 0)
        {
            foreach (var formation in formations)
            {
                var bounds = formation.bounds;
                Vector3 boundsSize = new Vector3(bounds.size.x, bounds.size.y, 0.1f);
                Vector3 formationWorldCenter = formationCenter;
                
                formationWorldCenter += splineRotation * new Vector3(formation.positionOffset.x, formation.positionOffset.y, 0);

                Color boundsColor = GetFormationColor(formation.index);
                boundsColor.a = 0.3f;
                Gizmos.color = boundsColor;
                
                Vector3[] localCorners = new Vector3[]
                {
                    new Vector3(-boundsSize.x * 0.5f, -boundsSize.y * 0.5f, 0),
                    new Vector3(boundsSize.x * 0.5f, -boundsSize.y * 0.5f, 0),
                    new Vector3(boundsSize.x * 0.5f, boundsSize.y * 0.5f, 0),
                    new Vector3(-boundsSize.x * 0.5f, boundsSize.y * 0.5f, 0)
                };

                Vector3[] worldCorners = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    worldCorners[i] = formationWorldCenter + (splineRotation * localCorners[i]);
                }

                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;
                    Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
                }
                
                Gizmos.color = GetFormationColor(formation.index);
                Gizmos.DrawWireSphere(formationWorldCenter, gizmoSize * 0.75f);
            }
        }

        foreach (var slot in formationSlots)
        {
            Vector3 worldPos = GetSlotWorldPosition(slot);
            
            Vector3 localPos = Quaternion.Inverse(splineRotation) * (worldPos - formationCenter);
            
            bool slotInBounds = Mathf.Abs(localPos.x) <= boundary.x * 0.5f && 
                               Mathf.Abs(localPos.y) <= boundary.y * 0.5f;
            
            if (!slotInBounds)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Color baseColor = slot.isOccupied ? occupiedSlotColor : openSlotColor;
                Color formationColor = GetFormationColor(slot.formationIndex);
                Gizmos.color = Color.Lerp(baseColor, formationColor, 0.5f);
            }
            
            Gizmos.DrawSphere(worldPos, gizmoSize);

            if (formations.Count > 1 && slot.formationIndex < formations.Count)
            {
                var formation = formations[slot.formationIndex];
                Vector3 formationWorldCenter = formationCenter;
                
                formationWorldCenter += splineRotation * new Vector3(formation.positionOffset.x, formation.positionOffset.y, 0);
                
                Gizmos.color = GetFormationColor(formation.index) * 0.3f;
                Gizmos.DrawLine(formationWorldCenter, worldPos);
            }
        }

        Gizmos.color = Color.cyan;
        Vector3 forwardDirection = splineRotation * Vector3.forward;
        Gizmos.DrawRay(formationCenter, forwardDirection * 3f);
        
        Vector3 arrowTip = formationCenter + forwardDirection * 3f;
        Vector3 arrowLeft = arrowTip - (splineRotation * (Vector3.forward * 0.5f + Vector3.left * 0.3f));
        Vector3 arrowRight = arrowTip - (splineRotation * (Vector3.forward * 0.5f + Vector3.right * 0.3f));
        
        Gizmos.DrawLine(arrowTip, arrowLeft);
        Gizmos.DrawLine(arrowTip, arrowRight);
    }

    private void DrawFormationInfo()
    {
        #if UNITY_EDITOR
        Vector3 baseInfoPos = formationCenter;

        if (constrainToBoundary && currentSpacingMultiplier < 1f)
        {
            Vector3 infoPos = baseInfoPos + Vector3.up * 5f;
            UnityEditor.Handles.Label(infoPos, $"Spacing: {(currentSpacingMultiplier * 100f):F0}% (Auto-adjusted)");
        }

        if (currentFormation == FormationType.Grid && gridFillsBoundary && formationSlots.Count > 0)
        {
            Vector3 gridInfoPos = baseInfoPos + Vector3.up * 6f;
            UnityEditor.Handles.Label(gridInfoPos, $"Grid: {gridSize.x}x{gridSize.y} = {gridSize.x * gridSize.y} slots");
        }
        
        if (currentFormation == FormationType.VShape && formations.Count > 0)
        {
            Vector3 vInfoPos = baseInfoPos + Vector3.up * 6f;
            UnityEditor.Handles.Label(vInfoPos, $"V-Shape: {vShapeCount} slots (Auto-positioned at bottom)");
        }

        if (formations.Count > 1)
        {
            Vector3 countInfoPos = baseInfoPos + Vector3.up * 7f;
            UnityEditor.Handles.Label(countInfoPos, $"Active Formations: {formations.Count}");
        }

        Vector3 splineInfoPos = baseInfoPos + Vector3.up * 8f;
        UnityEditor.Handles.Label(splineInfoPos, "Spline Aligned");
        
        Vector2 boundary = GetFormationBoundary();
        Vector3 boundaryInfoPos = formationCenter + Vector3.down * (boundary.y * 0.5f + 2f);
        UnityEditor.Handles.Label(boundaryInfoPos, $"Formation Boundary: {boundary.x:F1} x {boundary.y:F1}");
        
        bool allWithinBounds = true;
        int totalSlotsOutside = 0;
        foreach (var formation in formations)
        {
            foreach (var slot in formation.slots)
            {
                Vector3 worldPos = GetSlotWorldPosition(slot);
                Vector3 localPos = Quaternion.Inverse(splineRotation) * (worldPos - formationCenter);
                
                if (Mathf.Abs(localPos.x) > boundary.x * 0.5f || Mathf.Abs(localPos.y) > boundary.y * 0.5f)
                {
                    allWithinBounds = false;
                    totalSlotsOutside++;
                }
            }
        }
        
        if (!allWithinBounds)
        {
            Vector3 warningPos = formationCenter + Vector3.up * (boundary.y * 0.5f + 3f);
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(warningPos, $"⚠ {totalSlotsOutside} SLOTS OUTSIDE BOUNDS!");
            UnityEditor.Handles.color = Color.white;
        }
        else if (constrainToBoundary && currentSpacingMultiplier < 0.99f)
        {
            Vector3 successPos = formationCenter + Vector3.up * (boundary.y * 0.5f + 2f);
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(successPos, "✓ Formations auto-fitted to boundary");
            UnityEditor.Handles.color = Color.white;
        }
        else if (currentFormation == FormationType.VShape)
        {
            Vector3 successPos = formationCenter + Vector3.up * (boundary.y * 0.5f + 2f);
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(successPos, "✓ V-Shape positioned at bottom boundary");
            UnityEditor.Handles.color = Color.white;
        }
        #endif
    }
    
    private Color GetFormationColor(int index)
    {
        if (formationColors == null || formationColors.Length == 0)
            return Color.white;
        
        return formationColors[index % formationColors.Length];
    }

    #endregion
}