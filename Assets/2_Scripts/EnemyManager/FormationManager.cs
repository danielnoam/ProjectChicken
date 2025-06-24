using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;


[Serializable]
public class FormationSlot
{
    public Vector3 localPosition;
    public bool isOccupied;
    public GameObject occupant;
    public int formationIndex;

    public FormationSlot(Vector3 position, int index = 0)
    {
        localPosition = position;
        isOccupied = false;
        occupant = null;
        formationIndex = index;
    }
}
    
    
[Serializable]
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


// FormationManager: Handles enemy formations for a chicken invaders-style game
public class FormationManager : MonoBehaviour
{

    #region Inspector Fields

    [Header("Formation Settings")]
    [SerializeField, Range(0f, 1f)] private float boundaryPadding = 0.1f;
    [Tooltip("Minimum separation between formation instances as a percentage of formation size.")]
    [SerializeField][Range(0.1f, 2f)] private float formationSeparation = 1.2f;
    [Tooltip("Reduces formation boundary size on all edges compared to crosshair boundary. Use 0 for same size as crosshair.")]
    [SerializeField, Min(0f)] private float boundaryOffset = 2f;
    [SerializeField, Min(0)] private float splineRotationSpeed = 5f;
    
    [Header("Spacing")]
    [Tooltip("Horizontal spacing between formation slots. For V-Shape, this controls the width of the V.")]
    [SerializeField] private float horizontalSpacing = 2f;
    [Tooltip("Vertical spacing between formation slots. For V-Shape, this controls the height of the V.")]
    [SerializeField] private float verticalSpacing = 2f;
    [SerializeField] private float circleRadius = 5f;
    [Tooltip("When enabled, formations will scale up to fill available boundary space, not just scale down to fit. V-Shape always fills width regardless.")]
    [SerializeField] private bool constrainToBoundary = true;
    [SerializeField, Min(0.1f)] private float minSpacingMultiplier = 0.3f;
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
    
    [Header("References")]
    [SerializeField] private LevelManager levelManager;

    #endregion

    
    #region Private Fields && Properties

    private readonly List<FormationSlot> _formationSlots = new List<FormationSlot>();
    private readonly List<FormationInstance> _formations = new List<FormationInstance>();
    private float _currentSpacingMultiplier = 1f;
    private Quaternion _splineRotation = Quaternion.identity;
    private FormationSettings _currentFormationSettings = new FormationSettings();
    private Vector3 FormationCenter => levelManager ? levelManager.EnemyPosition : Vector3.zero;
    
    public List<FormationSlot> FormationSlots => _formationSlots;
    public static event Action OnFormationChanged;

    #endregion
    

    
    
    #region Unity Lifecycle

    private void OnValidate()
    {
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
    }

    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
        }
    }

    private void OnDisable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }
    }
    
    private void OnStageChanged(SOLevelStage newStage)
    {
        if (newStage?.FormationSettings == null || newStage.StageType != StageType.EnemyWave) return;
        
        var newSettings = newStage.FormationSettings;
        
        // Check if settings actually changed
        bool settingsChanged = !FormationSettings.AreFormationSettingsEqual(_currentFormationSettings, newSettings);
        
        if (settingsChanged)
        {
            // Update to new settings
            _currentFormationSettings = new FormationSettings(newSettings);
            GenerateFormations(true);
        }
    }
    

    private void Update()
    {
        HandleSplineRotation();
    }

    #endregion

    #region Spline Rotation

    private void HandleSplineRotation()
    {
        if (!levelManager)
        {
            _splineRotation = Quaternion.identity;
            return;
        }

        Vector3 splineForward = GetSplineDirection();
        
        if (splineForward != Vector3.zero)
        {
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineForward, Vector3.up);
            _splineRotation = Quaternion.Slerp(_splineRotation, targetSplineRotation, splineRotationSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetSplineDirection()
    {
        return !levelManager ? Vector3.forward : levelManager.GetDirectionOnSpline(levelManager.CurrentPositionOnPath.position);
    }

    #endregion

    #region Formation Generation

    public void GenerateFormations(bool notifyChickens = true)
    {
        // Store previous occupants before clearing
        List<GameObject> previousOccupants = new List<GameObject>();
        foreach (var slot in _formationSlots)
        {
            if (slot.isOccupied && slot.occupant)
            {
                previousOccupants.Add(slot.occupant);
            }
        }
        
        _currentSpacingMultiplier = 1f;
        _formations.Clear();
        _formationSlots.Clear();
        
        int actualCount = (_currentFormationSettings.FormationType == FormationType.Grid || _currentFormationSettings.FormationType == FormationType.VShape) ? 1 : _currentFormationSettings.FormationCount;
        
        // Generate formations
        for (int i = 0; i < actualCount; i++)
        {
            var formationInstance = new FormationInstance(i);
            _formations.Add(formationInstance);
            GenerateFormationInstance(formationInstance);
        }
        
        // Position formations to avoid overlap
        PositionMultipleFormations();
        
        // Rebuild the main slot list
        RebuildSlotList();
        
        // Notify all chickens about formation change
        if (notifyChickens && Application.isPlaying)
        {
            OnFormationChanged?.Invoke();
        
            // Also find all chickens directly and notify them
            var allChickens = FindObjectsByType<ChickenFollowFormation>(FindObjectsSortMode.None);
            foreach (var chicken in allChickens)
            {
                chicken.OnFormationChangedNotification();
            }
        }
    }
    
    
    private void GenerateFormationInstance(FormationInstance formation)
    {
        formation.slots.Clear();
        formation.spacingMultiplier = 1f;
        
        switch (_currentFormationSettings.FormationType)
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
        if (_formations.Count <= 1)
        {
            if (_formations.Count == 1)
            {
                ApplyFormationPosition(_formations[0]);
            
                if (constrainToBoundary && !_currentFormationSettings.IsGridFillingBoundary)
                {
                    ApplyBoundaryConstraintsAtPosition(_formations[0]);
                }
            }
            return;
        }
    
        // Multiple formations - calculate available space first
        Vector2 boundary = GetFormationBoundary();
    
        float maxFormationWidth = 0f;
        float maxFormationHeight = 0f;
    
        foreach (var formation in _formations)
        {
            maxFormationWidth = Mathf.Max(maxFormationWidth, formation.bounds.size.x);
            maxFormationHeight = Mathf.Max(maxFormationHeight, formation.bounds.size.y);
        }
    
        // FIX: Calculate required space BEFORE positioning
        float baseSeparationDistance = maxFormationWidth * formationSeparation;
        float totalRequiredWidth = GetTotalLayoutWidth(_formations.Count, baseSeparationDistance);
        float totalRequiredHeight = GetTotalLayoutHeight(_formations.Count, baseSeparationDistance);
    
        // FIX: Scale down separation if it won't fit
        float widthScale = boundary.x > 0 ? Mathf.Min(1f, boundary.x / totalRequiredWidth) : 1f;
        float heightScale = boundary.y > 0 ? Mathf.Min(1f, boundary.y / totalRequiredHeight) : 1f;
        float scaleReduction = Mathf.Min(widthScale, heightScale);
    
        float adjustedSeparationDistance = baseSeparationDistance * scaleReduction;
    
        // Position formations with adjusted separation
        PositionFormationsWithAdjustedSeparation(adjustedSeparationDistance);
    
        // Apply boundary constraints to each formation
        if (constrainToBoundary)
        {
            ValidateAndAdjustAllFormations();
        }
    }
    
    private void PositionFormationsWithAdjustedSeparation(float separationDistance)
    {
        if (_formations.Count == 2)
        {
            _formations[0].positionOffset = new Vector2(-separationDistance * 1.25f, 0);
            _formations[1].positionOffset = new Vector2(separationDistance * 1.25f, 0);
        }
        else if (_formations.Count == 3)
        {
            _formations[0].positionOffset = new Vector2(-separationDistance * 2f, 0);
            _formations[1].positionOffset = new Vector2(0, 0);
            _formations[2].positionOffset = new Vector2(separationDistance * 2f, 0);
        }
        else if (_formations.Count == 4)
        {
            float halfSep = separationDistance * 0.5f;
            _formations[0].positionOffset = new Vector2(-halfSep, halfSep);
            _formations[1].positionOffset = new Vector2(halfSep, halfSep);
            _formations[2].positionOffset = new Vector2(-halfSep, -halfSep);
            _formations[3].positionOffset = new Vector2(halfSep, -halfSep);
        }
        else
        {
            // Grid layout for more formations
            int cols = Mathf.CeilToInt(Mathf.Sqrt(_formations.Count));
            int rows = Mathf.CeilToInt((float)_formations.Count / cols);
        
            float colSpacing = separationDistance;
            float rowSpacing = separationDistance * 0.8f;
        
            float totalLayoutWidth = (cols - 1) * colSpacing;
            float totalLayoutHeight = (rows - 1) * rowSpacing;
        
            int index = 0;
            for (int row = 0; row < rows && index < _formations.Count; row++)
            {
                for (int col = 0; col < cols && index < _formations.Count; col++)
                {
                    float x = (col * colSpacing) - (totalLayoutWidth * 0.5f);
                    float y = (row * rowSpacing) - (totalLayoutHeight * 0.5f);
                    _formations[index].positionOffset = new Vector2(x, y);
                    index++;
                }
            }
        }
    }
    
    private void ValidateAndAdjustAllFormations()
    {
        bool allFit = false;
        int maxIterations = 15;
        int iteration = 0;

        while (!allFit && iteration < maxIterations)
        {
            iteration++;
            allFit = true;
            
            foreach (var formation in _formations)
            {
                if (!IsFormationInstanceWithinBounds(formation))
                {
                    allFit = false;
                    break;
                }
            }
            
            if (!allFit)
            {
                // Apply progressive scaling
                float scaleReduction = Mathf.Lerp(0.95f, 0.85f, (float)iteration / maxIterations);
                
                foreach (var formation in _formations)
                {
                    formation.spacingMultiplier *= scaleReduction;
                    formation.spacingMultiplier = Mathf.Max(formation.spacingMultiplier, minSpacingMultiplier);
                    GenerateFormationInstance(formation);
                }
                
                // Also reduce separation distances
                float separationReduction = scaleReduction;
                Vector2 boundary = GetFormationBoundary();
                float maxFormationWidth = 0f;
                
                foreach (var formation in _formations)
                {
                    maxFormationWidth = Mathf.Max(maxFormationWidth, formation.bounds.size.x);
                }
                
                float newSeparationDistance = maxFormationWidth * formationSeparation * separationReduction;
                PositionFormationsWithAdjustedSeparation(newSeparationDistance);
            }
            
            if (_formations.Count > 0)
            {
                _currentSpacingMultiplier = _formations[0].spacingMultiplier;
            }
        }

        if (!allFit && Application.isPlaying)
        {
            Debug.LogWarning($"FormationManager: Could not fit all {_formations.Count} formations within bounds after {iteration} iterations!");
            
            // Emergency fallback: Center all formations at origin with minimal spacing
            foreach (var formation in _formations)
            {
                formation.positionOffset = Vector2.zero;
                formation.spacingMultiplier = minSpacingMultiplier;
                GenerateFormationInstance(formation);
            }
        }
    }
    
    private void RebuildSlotList()
    {
        _formationSlots.Clear();
        foreach (var formation in _formations)
        {
            _formationSlots.AddRange(formation.slots);
        }
    }
    
    private bool IsFormationInstanceWithinBounds(FormationInstance formation)
    {
        Vector2 boundary = GetFormationBoundary();
        
        foreach (var slot in formation.slots)
        {
            Vector3 worldPos = GetSlotWorldPosition(slot);
            Vector3 localPos = Quaternion.Inverse(_splineRotation) * (worldPos - FormationCenter);
            
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
        int halfCount = _currentFormationSettings.VShapeCount / 2;
        var spacing = GetAdjustedSpacing(formation.spacingMultiplier);

        for (int i = 0; i < _currentFormationSettings.VShapeCount; i++)
        {
            float xPos = (i - halfCount) * spacing.x;
            float yPos = Mathf.Abs(i - halfCount) * spacing.y;
            AddSlot(formation, new Vector3(xPos, yPos, 0));
        }
        
        // Normalize so the lowest point is at y=0
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
        float halfSize = (_currentFormationSettings.SquareSize - 1) * 0.5f;
        var spacing = GetAdjustedSpacing(formation.spacingMultiplier);

        for (int y = 0; y < _currentFormationSettings.SquareSize; y++)
        {
            for (int x = 0; x < _currentFormationSettings.SquareSize; x++)
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

        for (int row = 0; row < _currentFormationSettings.TriangleRows; row++)
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
        float angleStep = 360f / _currentFormationSettings.CircleCount;
        float adjustedRadius = circleRadius * formation.spacingMultiplier;

        for (int i = 0; i < _currentFormationSettings.CircleCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float xPos = Mathf.Cos(angle) * adjustedRadius;
            float yPos = Mathf.Sin(angle) * adjustedRadius;
            AddSlot(formation, new Vector3(xPos, yPos, 0));
        }
    }

    private void GenerateGrid(FormationInstance formation)
    {
        int columns = Mathf.Max(1, _currentFormationSettings.GridSize.x);
        int rows = Mathf.Max(1, _currentFormationSettings.GridSize.y);

        if (_currentFormationSettings.GridFillsBoundary && levelManager)
        {
            Vector2 boundary = GetFormationBoundary();
            
            // FIX: Use more conservative safety margins for grid filling
            float safetyMargin = constrainToBoundary ? 0.85f : 0.95f;
            Vector2 safeBoundary = boundary * safetyMargin;

            // FIX: Ensure minimum spacing between slots
            float minSpacing = 0.5f; // Minimum spacing between grid slots
            
            float maxHorizontalSpace = columns > 1 ? safeBoundary.x / (columns - 1) : 0;
            float maxVerticalSpace = rows > 1 ? safeBoundary.y / (rows - 1) : 0;
            
            // FIX: Clamp spacing to minimum values
            float horizontalSpace = Mathf.Max(minSpacing, maxHorizontalSpace * formation.spacingMultiplier);
            float verticalSpace = Mathf.Max(minSpacing, maxVerticalSpace * formation.spacingMultiplier);
            
            // FIX: Recalculate actual grid size to ensure it fits
            float actualGridWidth = (columns - 1) * horizontalSpace;
            float actualGridHeight = (rows - 1) * verticalSpace;
            
            if (actualGridWidth > safeBoundary.x || actualGridHeight > safeBoundary.y)
            {
                // Fallback to standard spacing if boundary filling fails
                var spacing = GetAdjustedSpacing(formation.spacingMultiplier);
                horizontalSpace = spacing.x;
                verticalSpace = spacing.y;
                
                Debug.LogWarning($"Grid boundary filling failed, using standard spacing. " +
                               $"Required: {actualGridWidth:F1}x{actualGridHeight:F1}, " +
                               $"Available: {safeBoundary.x:F1}x{safeBoundary.y:F1}");
            }

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
            // Standard grid generation
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
        if (!levelManager) return;
        
        Vector2 boundary = GetFormationBoundary();
        
        var currentBounds = CalculateFormationBoundsWithOffset(formation);
        float currentWidth = currentBounds.z - currentBounds.x;
        float currentHeight = currentBounds.w - currentBounds.y;
        
        if (Application.isPlaying)
        {
            //Debug.Log($"[FormationConstraints] Checking {currentFormationSettings.FormationType} - Boundary: {boundary.x:F1}x{boundary.y:F1}, Formation: {currentWidth:F1}x{currentHeight:F1}");
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
                Vector3 localPos = Quaternion.Inverse(_splineRotation) * (worldPos - FormationCenter);
                
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
                //Debug.Log($"[FormationConstraints] Iteration {iteration}: Spacing {(formation.spacingMultiplier * 100):F0}%, Exceed X:{maxExceedX:F1} Y:{maxExceedY:F1}");
            }
            
            GenerateFormationInstance(formation);
            
            if (formation.spacingMultiplier <= minSpacingMultiplier)
            {
                break;
            }
        }
        
        _currentSpacingMultiplier = formation.spacingMultiplier;
        
        if (Application.isPlaying)
        {
            bool fits = IsFormationInstanceWithinBounds(formation);
            //Debug.Log($"[FormationConstraints] Final result: Fits={fits}, Spacing={(currentSpacingMultiplier * 100):F0}%, Iterations={iteration}");
            
            if (!fits)
            {
                //Debug.LogWarning($"Formation '{currentFormationSettings.FormationType}' at position '{currentFormationSettings.FormationPosition}' cannot fit within bounds even at minimum spacing!");
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
        if (!levelManager) return;

        if (_currentFormationSettings.IsGridFillingBoundary)
        {
            formation.positionOffset = Vector2.zero;
            return;
        }
        
        Vector2 boundary = GetFormationBoundary();
        var bounds = formation.bounds;
        float formationHeight;
        
        if (_currentFormationSettings.FormationType == FormationType.VShape)
        {
            // FIX: Ensure V-Shape positioning accounts for formation height
            formationHeight = bounds.size.y;
            float bottomBoundary = -(boundary.y * 0.5f);
            
            // FIX: Make sure the formation fits with padding
            float availableSpace = boundary.y - formationHeight;
            float safeBottomPadding = Mathf.Min(vShapeBottomPadding, availableSpace * 0.3f);
            
            float targetCenterY = bottomBoundary + safeBottomPadding + (formationHeight * 0.5f);
            
            // FIX: Clamp to ensure it doesn't exceed top boundary
            float topLimit = boundary.y * 0.5f - (formationHeight * 0.5f);
            targetCenterY = Mathf.Min(targetCenterY, topLimit);
            
            formation.positionOffset = new Vector2(0, targetCenterY);
            
            return;
        }

        // Standard positioning logic for other formations
        float formationWidth = bounds.size.x;
        formationHeight = bounds.size.y;

        // FIX: Use more conservative safety margin
        float safetyMargin = constrainToBoundary ? 0.9f : 1f;
        
        float maxOffsetX = Mathf.Max(0, ((boundary.x * safetyMargin) - formationWidth) * 0.5f);
        float maxOffsetY = Mathf.Max(0, ((boundary.y * safetyMargin) - formationHeight) * 0.5f);

        float padding = boundaryPadding * Mathf.Min(boundary.x, boundary.y) * 0.5f;
        maxOffsetX = Mathf.Max(0, maxOffsetX - padding);
        maxOffsetY = Mathf.Max(0, maxOffsetY - padding);

        if (maxOffsetX <= 0 && maxOffsetY <= 0)
        {
            formation.positionOffset = Vector2.zero;
            return;
        }

        switch (_currentFormationSettings.FormationPosition)
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
        foreach (var slot in _formationSlots)
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
        if (formationIndex < 0 || formationIndex >= _formations.Count) return null;
        
        foreach (var slot in _formations[formationIndex].slots)
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
    
    public bool OccupySpecificSlot(FormationSlot slot, GameObject occupant)
    {
        if (slot is { isOccupied: false })
        {
            slot.isOccupied = true;
            slot.occupant = occupant;
            return true;
        }
        return false;
    }
    public FormationSlot GetNearestAvailableSlot(Vector3 worldPosition)
    {
        FormationSlot nearestSlot = null;
        float nearestDistance = float.MaxValue;

        foreach (var slot in _formationSlots)
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
        return _formationSlots.FindAll(slot => !slot.isOccupied);
    }
    
    public List<FormationSlot> GetAvailableSlotsInFormation(int formationIndex)
    {
        if (formationIndex < 0 || formationIndex >= _formations.Count) return new List<FormationSlot>();
        return _formations[formationIndex].slots.FindAll(slot => !slot.isOccupied);
    }

    public Vector3 GetSlotWorldPosition(FormationSlot slot)
    {
        FormationInstance formationInstance = null;
        foreach (var formation in _formations)
        {
            if (formation.slots.Contains(slot))
            {
                formationInstance = formation;
                break;
            }
        }
        
        if (formationInstance == null)
        {
            return FormationCenter + slot.localPosition;
        }
        
        Vector3 worldPosition = FormationCenter;
        Vector3 rotatedOffset = _splineRotation * new Vector3(formationInstance.positionOffset.x, formationInstance.positionOffset.y, 0);
        worldPosition += rotatedOffset;
        worldPosition += _splineRotation * slot.localPosition;
        
        return worldPosition;
    }
    

    #endregion

    
    
    #region Helper Methods

    private Vector2 GetFormationBoundary()
    {
        if (!levelManager) return new Vector2(50f, 30f);
        
        Vector2 crosshairBoundary = levelManager.EnemyBoundary * 2f;
        
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
    
    
    private void AddSlot(FormationInstance formation, Vector3 position)
    {
        var slot = new FormationSlot(position, formation.index);
        formation.slots.Add(slot);
    }
    
    public Bounds GetFormationWorldBounds()
    {
        if (_formationSlots.Count == 0) return new Bounds(FormationCenter, Vector3.zero);
        
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        
        foreach (var slot in _formationSlots)
        {
            Vector3 worldPos = GetSlotWorldPosition(slot);
            min = Vector3.Min(min, worldPos);
            max = Vector3.Max(max, worldPos);
        }
        
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        
        return new Bounds(center, size);
    }
    
    
    public bool IsCurrentFormationWithinBounds()
    {
        foreach (var formation in _formations)
        {
            if (!IsFormationInstanceWithinBounds(formation))
            {
                return false;
            }
        }
        return true;
    }
    
    private float GetTotalLayoutWidth(int formationCount, float separationDistance)
    {
        if (formationCount <= 1) return 0f;
    
        switch (formationCount)
        {
            case 2:
                return separationDistance * 2.5f; // -1.25 to +1.25
            case 3:
                return separationDistance * 4f; // -2 to +2
            case 4:
                return separationDistance; // Grid layout: -0.5 to +0.5
            default:
                int cols = Mathf.CeilToInt(Mathf.Sqrt(formationCount));
                return (cols - 1) * separationDistance;
        }
    }

    private float GetTotalLayoutHeight(int formationCount, float separationDistance)
    {
        if (formationCount <= 3) return 0f; // Horizontal layouts
        if (formationCount == 4) return separationDistance; // 2x2 grid
    
        int cols = Mathf.CeilToInt(Mathf.Sqrt(formationCount));
        int rows = Mathf.CeilToInt((float)formationCount / cols);
        return (rows - 1) * separationDistance * 0.8f;
    }
    

    #endregion

    #region Editor Methods

    [Button]
    private void ChangeFormation()
    {
        // Note: This is for editor testing only - in game, formations are controlled by stages
        int nextFormation = ((int)_currentFormationSettings.FormationType + 1) % Enum.GetValues(typeof(FormationType)).Length;
        var newSettings = new FormationSettings(_currentFormationSettings);
        // This would require a way to modify FormationSettings, which might need additional methods
        GenerateFormations(true);
    }

    [Button]
    private void RegenerateFormation()
    {
        GenerateFormations(true);
    }

    [Button]
    private void RandomizePosition()
    {
        if (_currentFormationSettings.FormationType == FormationType.VShape)
        {
            Debug.Log("V-Shape formations always position at bottom - randomization has no effect.");
            GenerateFormations(true);
            return;
        }
        
        if (_formationSlots.Count > 0)
        {
            // For testing only - create a temporary position change
            GenerateFormations(true);
        }
    }

    [Button]
    private void CyclePosition()
    {
        if (_currentFormationSettings.FormationType == FormationType.VShape)
        {
            Debug.Log("V-Shape formations always position at bottom - position cycling has no effect.");
            return;
        }
        
        // For testing only - formations are now controlled by stages
        if (_formationSlots.Count > 0)
        {
            GenerateFormations(true);
        }
    }

    [Button]
    private void RecalculateBoundaryConstraints()
    {
        GenerateFormations(true);
    }
    

    #endregion

    
#if UNITY_EDITOR
    
    #region Gizmo Drawing

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        DrawBoundary();
        DrawFormationSlots();
        DrawFormationInfo();
    }
    private void DrawBoundary()
    {
        if (!levelManager) return;
        
        bool isWithinBounds = true;
        if (_formationSlots.Count > 0)
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
        
        Vector3 boundaryCenter = FormationCenter;
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
            worldCorners[i] = boundaryCenter + (_splineRotation * localCorners[i]);
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
        if (showGizmos && _currentFormationSettings.FormationPosition != FormationPosition.Center)
        {
            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.05f);
            UnityEditor.Handles.DrawSolidRectangleWithOutline(worldCorners, new Color(1f, 1f, 0f, 0.05f), Color.clear);
        }
        #endif
    }

    private void DrawFormationSlots()
    {
        Vector2 boundary = GetFormationBoundary();
        
        if (showFormationBounds && _formations.Count > 0)
        {
            foreach (var formation in _formations)
            {
                var bounds = formation.bounds;
                Vector3 boundsSize = new Vector3(bounds.size.x, bounds.size.y, 0.1f);
                Vector3 formationWorldCenter = FormationCenter;
                
                formationWorldCenter += _splineRotation * new Vector3(formation.positionOffset.x, formation.positionOffset.y, 0);

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
                    worldCorners[i] = formationWorldCenter + (_splineRotation * localCorners[i]);
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

        foreach (var slot in _formationSlots)
        {
            Vector3 worldPos = GetSlotWorldPosition(slot);
            
            Vector3 localPos = Quaternion.Inverse(_splineRotation) * (worldPos - FormationCenter);
            
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

            if (_formations.Count > 1 && slot.formationIndex < _formations.Count)
            {
                var formation = _formations[slot.formationIndex];
                Vector3 formationWorldCenter = FormationCenter;
                
                formationWorldCenter += _splineRotation * new Vector3(formation.positionOffset.x, formation.positionOffset.y, 0);
                
                Gizmos.color = GetFormationColor(formation.index) * 0.3f;
                Gizmos.DrawLine(formationWorldCenter, worldPos);
            }
        }

        Gizmos.color = Color.cyan;
        Vector3 forwardDirection = _splineRotation * Vector3.forward;
        Gizmos.DrawRay(FormationCenter, forwardDirection * 3f);
        
        Vector3 arrowTip = FormationCenter + forwardDirection * 3f;
        Vector3 arrowLeft = arrowTip - (_splineRotation * (Vector3.forward * 0.5f + Vector3.left * 0.3f));
        Vector3 arrowRight = arrowTip - (_splineRotation * (Vector3.forward * 0.5f + Vector3.right * 0.3f));
        
        Gizmos.DrawLine(arrowTip, arrowLeft);
        Gizmos.DrawLine(arrowTip, arrowRight);
    }

    private void DrawFormationInfo()
    {
        Vector2 boundary = GetFormationBoundary();
        Vector3 baseInfoPos = FormationCenter + Vector3.up * (boundary.y * 0.5f + 1f); // Start above boundary
        float lineHeight = 1f;
        int lineIndex = 0;

        if (constrainToBoundary && _currentSpacingMultiplier < 1f)
        {
            Vector3 infoPos = baseInfoPos + Vector3.up * (lineHeight * lineIndex++);
            UnityEditor.Handles.Label(infoPos, $"Spacing: {(_currentSpacingMultiplier * 100f):F0}% (Auto-adjusted)");
        }

        if (_currentFormationSettings.FormationType == FormationType.Grid && _currentFormationSettings.GridFillsBoundary && _formationSlots.Count > 0)
        {
            Vector3 gridInfoPos = baseInfoPos + Vector3.up * (lineHeight * lineIndex++);
            UnityEditor.Handles.Label(gridInfoPos, $"Grid: {_currentFormationSettings.GridSize.x}x{_currentFormationSettings.GridSize.y} = {_currentFormationSettings.GridSize.x * _currentFormationSettings.GridSize.y} slots");
        }
        
        if (_currentFormationSettings.FormationType == FormationType.VShape && _formations.Count > 0)
        {
            Vector3 vInfoPos = baseInfoPos + Vector3.up * (lineHeight * lineIndex++);
            UnityEditor.Handles.Label(vInfoPos, $"V-Shape: {_currentFormationSettings.VShapeCount} slots (Auto-positioned at bottom)");
        }

        if (_formations.Count > 1)
        {
            Vector3 countInfoPos = baseInfoPos + Vector3.up * (lineHeight * lineIndex++);
            UnityEditor.Handles.Label(countInfoPos, $"Active Formations: {_formations.Count}");
        }
        
        Vector3 boundaryInfoPos = baseInfoPos + Vector3.up * (lineHeight * lineIndex++);
        UnityEditor.Handles.Label(boundaryInfoPos, $"Formation Boundary: {boundary.x:F1} x {boundary.y:F1}");
        
        bool allWithinBounds = true;
        int totalSlotsOutside = 0;
        foreach (var formation in _formations)
        {
            foreach (var slot in formation.slots)
            {
                Vector3 worldPos = GetSlotWorldPosition(slot);
                Vector3 localPos = Quaternion.Inverse(_splineRotation) * (worldPos - FormationCenter);
                
                if (Mathf.Abs(localPos.x) > boundary.x * 0.5f || Mathf.Abs(localPos.y) > boundary.y * 0.5f)
                {
                    allWithinBounds = false;
                    totalSlotsOutside++;
                }
            }
        }
        
        if (!allWithinBounds)
        {
            Vector3 warningPos = baseInfoPos + Vector3.up * (lineHeight * lineIndex);
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(warningPos, $"⚠ {totalSlotsOutside} SLOTS OUTSIDE BOUNDS!");
            UnityEditor.Handles.color = Color.white;
        }
        else if (constrainToBoundary && _currentSpacingMultiplier < 0.99f)
        {
            Vector3 successPos = baseInfoPos + Vector3.up * (lineHeight * lineIndex);
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(successPos, "Formations auto-fitted to boundary");
            UnityEditor.Handles.color = Color.white;
        }
        else if (_currentFormationSettings.FormationType == FormationType.VShape)
        {
            Vector3 successPos = baseInfoPos + Vector3.up * (lineHeight * lineIndex);
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(successPos, "V-Shape positioned at bottom boundary");
            UnityEditor.Handles.color = Color.white;
        }
    }
    
    private Color GetFormationColor(int index)
    {
        if (formationColors == null || formationColors.Length == 0)
            return Color.white;
        
        return formationColors[index % formationColors.Length];
    }

    #endregion
    
#endif

}