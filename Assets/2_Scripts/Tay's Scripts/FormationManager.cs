using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

// FormationManager: Handles enemy formations for a chicken invaders-style game
// Features:
// - Multiple formation types (V-Shape, Square, Triangle, Circle, Grid)
// - Dynamic slot management with occupation tracking
// - Boundary constraints with automatic spacing adjustment
// - Formation positioning (center, corners, edges, or random)
// - Grid formations can fill entire boundary
// - Follows spline path with customizable offset
// - Spline rotation alignment for formations

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

        public FormationSlot(Vector3 position)
        {
            localPosition = position;
            isOccupied = false;
            occupant = null;
        }
    }

    #endregion

    #region Inspector Fields

    [Header("Formation Settings")]
    [SerializeField] private FormationType currentFormation = FormationType.VShape;
    [SerializeField] private bool autoUpdateFormation = true; 
    [SerializeField] private Vector3 formationOffset = Vector3.zero;
    [SerializeField] private bool useLocalOffset = false;
    [SerializeField] private FormationPosition formationPosition = FormationPosition.Center;
    [SerializeField, Range(0f, 1f)] private float boundaryPadding = 0.1f;

    [Header("Spline Rotation")]
    [SerializeField] private bool alignToSplineDirection = false;
    [SerializeField, EnableIf("alignToSplineDirection"), Min(0)] private float splineRotationSpeed = 5f;

    [Header("Formation Parameters")]
    [SerializeField, Min(3)] private int vShapeCount = 7;
    [SerializeField, Min(2)] private int squareSize = 4;
    [SerializeField, Min(3)] private int triangleRows = 4;
    [SerializeField, Min(8)] private int circleCount = 12;
    [SerializeField] private Vector2Int gridSize = new Vector2Int(5, 3);
    [SerializeField] private bool gridFillsBoundary = true;

    [Header("Spacing")]
    [SerializeField] private float horizontalSpacing = 2f;
    [SerializeField] private float verticalSpacing = 2f;
    [SerializeField] private float circleRadius = 5f;
    [SerializeField] private bool constrainToBoundary = true;
    [SerializeField, Min(0.1f)] private float minSpacingMultiplier = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color openSlotColor = Color.green;
    [SerializeField] private Color occupiedSlotColor = Color.red;
    [SerializeField] private float gizmoSize = 0.5f;

    #endregion

    #region Private Fields

    private List<FormationSlot> formationSlots = new List<FormationSlot>();
    private FormationType lastFormationType;
    private Vector3 formationCenter;
    private float currentSpacingMultiplier = 1f;
    private Vector2 randomPositionOffset = Vector2.zero;
    private Quaternion splineRotation = Quaternion.identity;

    #endregion

    #region Properties

    public List<FormationSlot> FormationSlots => formationSlots;
    public FormationType CurrentFormation => currentFormation;
    public Vector3 FormationCenter => formationCenter;
    public float CurrentSpacingMultiplier => currentSpacingMultiplier;
    public bool IsGridFillingBoundary => currentFormation == FormationType.Grid && gridFillsBoundary;
    public Vector3 FormationWorldCenter => formationCenter + new Vector3(randomPositionOffset.x, randomPositionOffset.y, 0);
    public FormationPosition CurrentPosition => formationPosition;
    public bool AlignToSplineDirection => alignToSplineDirection;
    public Quaternion SplineRotation => splineRotation;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        GenerateFormation();
        lastFormationType = currentFormation;
    }

    private void Update()
    {
        HandleSplineRotation();
        UpdateFormationCenter();

        if (autoUpdateFormation && lastFormationType != currentFormation)
        {
            GenerateFormation();
            lastFormationType = currentFormation;
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
        if (!alignToSplineDirection || !LevelManager.Instance || !LevelManager.Instance.SplineContainer)
        {
            splineRotation = Quaternion.identity;
            return;
        }

        // Get the spline direction at the formation position
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

    public void GenerateFormation()
    {
        currentSpacingMultiplier = 1f;
        randomPositionOffset = Vector2.zero;
        GenerateFormationWithSpacing();

        // Always apply position (will be centered if position is Center)
        ApplyFormationPosition();
    }

    private void GenerateFormationWithSpacing()
    {
        formationSlots.Clear();

        switch (currentFormation)
        {
            case FormationType.VShape:
                GenerateVShape();
                break;
            case FormationType.Square2D:
                GenerateSquare2D();
                break;
            case FormationType.Triangle2D:
                GenerateTriangle2D();
                break;
            case FormationType.Circle:
                GenerateCircle();
                break;
            case FormationType.Grid:
                GenerateGrid();
                break;
        }

        if (constrainToBoundary && currentSpacingMultiplier == 1f && !IsGridFillingBoundary)
        {
            ApplyBoundaryConstraints();
        }
    }

    private void GenerateVShape()
    {
        int halfCount = vShapeCount / 2;
        var spacing = GetAdjustedSpacing();

        for (int i = 0; i < vShapeCount; i++)
        {
            float xPos = (i - halfCount) * spacing.x;
            float yPos = Mathf.Abs(i - halfCount) * spacing.y;
            AddSlot(new Vector3(xPos, yPos, 0));
        }
    }

    private void GenerateSquare2D()
    {
        float halfSize = (squareSize - 1) * 0.5f;
        var spacing = GetAdjustedSpacing();

        for (int y = 0; y < squareSize; y++)
        {
            for (int x = 0; x < squareSize; x++)
            {
                float xPos = (x - halfSize) * spacing.x;
                float yPos = (y - halfSize) * spacing.y;
                AddSlot(new Vector3(xPos, yPos, 0));
            }
        }
    }

    private void GenerateTriangle2D()
    {
        var spacing = GetAdjustedSpacing();

        for (int row = 0; row < triangleRows; row++)
        {
            int slotsInRow = row + 1;
            float rowWidth = (slotsInRow - 1) * spacing.x;
            float startX = -rowWidth * 0.5f;
            float yPos = row * spacing.y;

            for (int i = 0; i < slotsInRow; i++)
            {
                float xPos = startX + (i * spacing.x);
                AddSlot(new Vector3(xPos, yPos, 0));
            }
        }
    }

    private void GenerateCircle()
    {
        float angleStep = 360f / circleCount;
        float adjustedRadius = circleRadius * currentSpacingMultiplier;

        for (int i = 0; i < circleCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float xPos = Mathf.Cos(angle) * adjustedRadius;
            float yPos = Mathf.Sin(angle) * adjustedRadius;
            AddSlot(new Vector3(xPos, yPos, 0));
        }
    }

    private void GenerateGrid()
    {
        int columns = Mathf.Max(1, gridSize.x);
        int rows = Mathf.Max(1, gridSize.y);

        if (gridFillsBoundary && LevelManager.Instance)
        {
            Vector2 boundary = LevelManager.Instance.EnemyBoundary;

            // Calculate spacing to fill boundary
            float horizontalSpace = columns > 1 ? boundary.x / (columns - 1) : 0;
            float verticalSpace = rows > 1 ? boundary.y / (rows - 1) : 0;

            // Apply spacing multiplier
            horizontalSpace *= currentSpacingMultiplier;
            verticalSpace *= currentSpacingMultiplier;

            // Calculate grid dimensions
            float gridWidth = (columns - 1) * horizontalSpace;
            float gridHeight = (rows - 1) * verticalSpace;

            // Generate from top-left
            float startX = -gridWidth * 0.5f;
            float startY = gridHeight * 0.5f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    float xPos = startX + x * horizontalSpace;
                    float yPos = startY - y * verticalSpace;
                    AddSlot(new Vector3(xPos, yPos, 0));
                }
            }
        }
        else
        {
            // Use standard spacing
            var spacing = GetAdjustedSpacing();
            float halfWidth = (columns - 1) * 0.5f;
            float halfHeight = (rows - 1) * 0.5f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    float xPos = (x - halfWidth) * spacing.x;
                    float yPos = (y - halfHeight) * spacing.y;
                    AddSlot(new Vector3(xPos, yPos, 0));
                }
            }
        }
    }

    #endregion

    #region Boundary Management

    private void ApplyBoundaryConstraints()
    {
        if (!LevelManager.Instance) return;

        Vector2 boundary = LevelManager.Instance.EnemyBoundary;
        var bounds = CalculateFormationBounds();

        float formationWidth = bounds.z - bounds.x;
        float formationHeight = bounds.w - bounds.y;

        if (formationWidth > boundary.x || formationHeight > boundary.y)
        {
            float requiredScaleX = boundary.x / formationWidth;
            float requiredScaleY = boundary.y / formationHeight;
            float requiredScale = Mathf.Min(requiredScaleX, requiredScaleY) * 0.9f;

            currentSpacingMultiplier = Mathf.Max(requiredScale, minSpacingMultiplier);
            GenerateFormationWithSpacing();
        }
    }

    private Vector4 CalculateFormationBounds()
    {
        if (formationSlots.Count == 0) return Vector4.zero;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var slot in formationSlots)
        {
            // Apply spline rotation to get rotated bounds
            Vector3 rotatedPosition = alignToSplineDirection ? splineRotation * slot.localPosition : slot.localPosition;
            
            minX = Mathf.Min(minX, rotatedPosition.x);
            maxX = Mathf.Max(maxX, rotatedPosition.x);
            minY = Mathf.Min(minY, rotatedPosition.y);
            maxY = Mathf.Max(maxY, rotatedPosition.y);
        }

        return new Vector4(minX, minY, maxX, maxY);
    }

    #endregion

    #region Formation Positioning

    private void ApplyFormationPosition()
    {
        if (!LevelManager.Instance) return;

        Vector2 boundary = LevelManager.Instance.EnemyBoundary;
        var bounds = CalculateFormationBounds();

        float formationWidth = bounds.z - bounds.x;
        float formationHeight = bounds.w - bounds.y;

        // For grid formations that fill boundary, don't reposition
        if (IsGridFillingBoundary)
        {
            randomPositionOffset = Vector2.zero;
            return;
        }

        // Calculate safe area within boundary
        float padding = boundaryPadding * Mathf.Min(boundary.x, boundary.y);
        float safeMinX = -boundary.x * 0.5f + formationWidth * 0.5f + padding;
        float safeMaxX = boundary.x * 0.5f - formationWidth * 0.5f - padding;
        float safeMinY = -boundary.y * 0.5f + formationHeight * 0.5f + padding;
        float safeMaxY = boundary.y * 0.5f - formationHeight * 0.5f - padding;

        // Ensure valid ranges
        if (safeMaxX < safeMinX || safeMaxY < safeMinY)
        {
            // Formation too large, center it
            randomPositionOffset = Vector2.zero;
            return;
        }

        // Apply position based on selected option
        switch (formationPosition)
        {
            case FormationPosition.Center:
                randomPositionOffset = Vector2.zero;
                break;

            case FormationPosition.Random:
                randomPositionOffset = new Vector2(
                    UnityEngine.Random.Range(safeMinX, safeMaxX),
                    UnityEngine.Random.Range(safeMinY, safeMaxY)
                );
                break;

            case FormationPosition.TopLeft:
                randomPositionOffset = new Vector2(safeMinX, safeMaxY);
                break;

            case FormationPosition.TopCenter:
                randomPositionOffset = new Vector2(0, safeMaxY);
                break;

            case FormationPosition.TopRight:
                randomPositionOffset = new Vector2(safeMaxX, safeMaxY);
                break;

            case FormationPosition.MiddleLeft:
                randomPositionOffset = new Vector2(safeMinX, 0);
                break;

            case FormationPosition.MiddleRight:
                randomPositionOffset = new Vector2(safeMaxX, 0);
                break;

            case FormationPosition.BottomLeft:
                randomPositionOffset = new Vector2(safeMinX, safeMinY);
                break;

            case FormationPosition.BottomCenter:
                randomPositionOffset = new Vector2(0, safeMinY);
                break;

            case FormationPosition.BottomRight:
                randomPositionOffset = new Vector2(safeMaxX, safeMinY);
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

    public Vector3 GetSlotWorldPosition(FormationSlot slot)
    {
        Vector3 basePosition = formationCenter + new Vector3(randomPositionOffset.x, randomPositionOffset.y, 0);
        
        // Apply spline rotation to the slot's local position if alignment is enabled
        Vector3 rotatedLocalPosition = alignToSplineDirection ? 
            splineRotation * slot.localPosition : 
            slot.localPosition;
            
        return basePosition + rotatedLocalPosition;
    }

    #endregion

    #region Helper Methods

    private void UpdateFormationCenter()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.CurrentPositionOnPath) return;

        Transform pathTransform = LevelManager.Instance.CurrentPositionOnPath;

        if (useLocalOffset)
        {
            formationCenter = pathTransform.position +
                pathTransform.right * formationOffset.x +
                pathTransform.up * formationOffset.y +
                pathTransform.forward * formationOffset.z;
        }
        else
        {
            formationCenter = pathTransform.position + formationOffset;
        }
    }

    private Vector2 GetAdjustedSpacing()
    {
        return new Vector2(
            horizontalSpacing * currentSpacingMultiplier,
            verticalSpacing * currentSpacingMultiplier
        );
    }

    private void AddSlot(Vector3 position)
    {
        formationSlots.Add(new FormationSlot(position));
    }

    public Vector2Int GetCurrentGridDimensions()
    {
        return currentFormation == FormationType.Grid ? gridSize : Vector2Int.zero;
    }

    public void SetFormationPosition(FormationPosition position)
    {
        formationPosition = position;
        if (formationSlots.Count > 0)
        {
            ApplyFormationPosition();
        }
    }

    #endregion

    #region Editor Methods

    [Button]
    private void ChangeFormation()
    {
        int nextFormation = ((int)currentFormation + 1) % Enum.GetValues(typeof(FormationType)).Length;
        currentFormation = (FormationType)nextFormation;
        GenerateFormation();
    }

    [Button]
    private void RegenerateFormation()
    {
        GenerateFormation();
    }

    [Button]
    private void RandomizePosition()
    {
        if (formationSlots.Count > 0)
        {
            var previousPosition = formationPosition;
            formationPosition = FormationPosition.Random;
            ApplyFormationPosition();
            formationPosition = previousPosition;
        }
    }

    [Button]
    private void CyclePosition()
    {
        int nextPosition = ((int)formationPosition + 1) % Enum.GetValues(typeof(FormationPosition)).Length;
        formationPosition = (FormationPosition)nextPosition;
        if (formationSlots.Count > 0)
        {
            ApplyFormationPosition();
        }
    }

    [Button]
    private void ToggleSplineAlignment()
    {
        alignToSplineDirection = !alignToSplineDirection;
    }

    #endregion

    #region Gizmo Drawing

    private void DrawBoundary()
    {
        if (!LevelManager.Instance) return;

        Gizmos.color = constrainToBoundary ? Color.yellow : Color.gray;
        Vector3 boundaryCenter = formationCenter + new Vector3(randomPositionOffset.x, randomPositionOffset.y, 0);
        
        if (alignToSplineDirection)
        {
            // Draw rotated boundary
            Vector3 boundarySize = new Vector3(
                LevelManager.Instance.EnemyBoundary.x,
                LevelManager.Instance.EnemyBoundary.y,
                0.1f
            );

            // Draw rotated boundary wireframe
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
        }
        else
        {
            Vector3 boundarySize = new Vector3(
                LevelManager.Instance.EnemyBoundary.x,
                LevelManager.Instance.EnemyBoundary.y,
                0.1f
            );
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
        }
    }

    private void DrawFormationSlots()
    {
        // Draw formation bounds when not centered
        if (formationPosition != FormationPosition.Center && formationSlots.Count > 0)
        {
            var bounds = CalculateFormationBounds();
            Vector3 boundsCenter = formationCenter + new Vector3(randomPositionOffset.x, randomPositionOffset.y, 0);
            Vector3 boundsSize = new Vector3(bounds.z - bounds.x, bounds.w - bounds.y, 0.1f);

            Gizmos.color = Color.blue * 0.5f;
            
            if (alignToSplineDirection)
            {
                // Draw rotated formation bounds
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
                    worldCorners[i] = boundsCenter + (splineRotation * localCorners[i]);
                }

                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;
                    Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
                }
            }
            else
            {
                Gizmos.DrawWireCube(boundsCenter, boundsSize);
            }
        }

        Vector3 formationWorldCenter = formationCenter + new Vector3(randomPositionOffset.x, randomPositionOffset.y, 0);

        foreach (var slot in formationSlots)
        {
            Vector3 worldPos = GetSlotWorldPosition(slot);
            Gizmos.color = slot.isOccupied ? occupiedSlotColor : openSlotColor;
            Gizmos.DrawSphere(worldPos, gizmoSize);

            Gizmos.color = Color.gray * 0.5f;
            Gizmos.DrawLine(formationWorldCenter, worldPos);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(formationWorldCenter, gizmoSize * 1.5f);

        // Draw spline alignment indicator
        if (alignToSplineDirection)
        {
            Gizmos.color = Color.cyan;
            Vector3 forwardDirection = splineRotation * Vector3.forward;
            Gizmos.DrawRay(formationWorldCenter, forwardDirection * 3f);
            
            // Draw small arrow to show direction
            Vector3 arrowTip = formationWorldCenter + forwardDirection * 3f;
            Vector3 arrowLeft = arrowTip - (splineRotation * (Vector3.forward * 0.5f + Vector3.left * 0.3f));
            Vector3 arrowRight = arrowTip - (splineRotation * (Vector3.forward * 0.5f + Vector3.right * 0.3f));
            
            Gizmos.DrawLine(arrowTip, arrowLeft);
            Gizmos.DrawLine(arrowTip, arrowRight);
        }

        if (useLocalOffset && LevelManager.Instance?.CurrentPositionOnPath)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(LevelManager.Instance.CurrentPositionOnPath.position, formationCenter);
        }
    }

    private void DrawFormationInfo()
    {
#if UNITY_EDITOR
        Vector3 baseInfoPos = formationCenter + new Vector3(randomPositionOffset.x, randomPositionOffset.y, 0);

        if (constrainToBoundary && currentSpacingMultiplier < 1f)
        {
            Vector3 infoPos = baseInfoPos + Vector3.up * 5f;
            UnityEditor.Handles.Label(infoPos, $"Spacing: {(currentSpacingMultiplier * 100f):F0}%");
        }

        if (currentFormation == FormationType.Grid && gridFillsBoundary && formationSlots.Count > 0)
        {
            Vector3 gridInfoPos = baseInfoPos + Vector3.up * 6f;
            UnityEditor.Handles.Label(gridInfoPos, $"Grid: {gridSize.x}x{gridSize.y} = {gridSize.x * gridSize.y} slots");
        }

        if (formationPosition != FormationPosition.Center)
        {
            Vector3 positionInfoPos = baseInfoPos + Vector3.up * 7f;
            UnityEditor.Handles.Label(positionInfoPos, $"Position: {formationPosition}");
        }

        if (alignToSplineDirection)
        {
            Vector3 splineInfoPos = baseInfoPos + Vector3.up * 8f;
            UnityEditor.Handles.Label(splineInfoPos, "Spline Aligned");
        }
#endif
    }

    #endregion
}