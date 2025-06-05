using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

public class FormationManager : MonoBehaviour
{
    public enum FormationType
    {
        VShape,
        Square2D,
        Triangle2D,
        Circle,
        Grid
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

    [Header("Formation Settings")]
    [SerializeField] private FormationType currentFormation = FormationType.VShape;
    [SerializeField] private bool autoUpdateFormation = true;
    [SerializeField] private Vector3 formationOffset = Vector3.zero;
    [SerializeField] private bool useLocalOffset = false; // If true, offset is relative to path direction

    [Header("Formation Parameters")]
    [SerializeField, Min(3)] private int vShapeCount = 7;
    [SerializeField, Min(2)] private int squareSize = 4;
    [SerializeField, Min(3)] private int triangleRows = 4;
    [SerializeField, Min(8)] private int circleCount = 12;
    [SerializeField] private Vector2Int gridSize = new Vector2Int(5, 3);

    [Header("Spacing")]
    [SerializeField] private float horizontalSpacing = 2f;
    [SerializeField] private float verticalSpacing = 2f;
    [SerializeField] private float circleRadius = 5f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color openSlotColor = Color.green;
    [SerializeField] private Color occupiedSlotColor = Color.red;
    [SerializeField] private float gizmoSize = 0.5f;

    private List<FormationSlot> formationSlots = new List<FormationSlot>();
    private FormationType lastFormationType;
    private Vector3 formationCenter;

    // Public accessors
    public List<FormationSlot> FormationSlots => formationSlots;
    public FormationType CurrentFormation => currentFormation;
    public Vector3 FormationCenter => formationCenter;

    private void Start()
    {
        GenerateFormation();
        lastFormationType = currentFormation;
    }

    private void Update()
    {
        // Update formation center based on level path
        if (LevelManager.Instance && LevelManager.Instance.CurrentPositionOnPath)
        {
            Transform pathTransform = LevelManager.Instance.CurrentPositionOnPath;

            if (useLocalOffset)
            {
                // Apply offset relative to the path's local space
                formationCenter = pathTransform.position +
                    pathTransform.right * formationOffset.x +
                    pathTransform.up * formationOffset.y +
                    pathTransform.forward * formationOffset.z;
            }
            else
            {
                // Apply offset in world space
                formationCenter = pathTransform.position + formationOffset;
            }
        }

        // Check if formation type changed
        if (autoUpdateFormation && lastFormationType != currentFormation)
        {
            GenerateFormation();
            lastFormationType = currentFormation;
        }
    }

    // Generate formation based on current type
    public void GenerateFormation()
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

        // Apply boundary constraints
        ApplyBoundaryConstraints();
    }

    private void GenerateVShape()
    {
        int halfCount = vShapeCount / 2;

        for (int i = 0; i < vShapeCount; i++)
        {
            float xPos = (i - halfCount) * horizontalSpacing;
            float yPos = Mathf.Abs(i - halfCount) * verticalSpacing;

            Vector3 position = new Vector3(xPos, yPos, 0);
            formationSlots.Add(new FormationSlot(position));
        }
    }

    private void GenerateSquare2D()
    {
        float halfSize = (squareSize - 1) * 0.5f;

        for (int y = 0; y < squareSize; y++)
        {
            for (int x = 0; x < squareSize; x++)
            {
                float xPos = (x - halfSize) * horizontalSpacing;
                float yPos = (y - halfSize) * verticalSpacing;

                Vector3 position = new Vector3(xPos, yPos, 0);
                formationSlots.Add(new FormationSlot(position));
            }
        }
    }

    private void GenerateTriangle2D()
    {
        for (int row = 0; row < triangleRows; row++)
        {
            int chickensInRow = row + 1;
            float rowWidth = (chickensInRow - 1) * horizontalSpacing;
            float startX = -rowWidth * 0.5f;
            float yPos = row * verticalSpacing;

            for (int i = 0; i < chickensInRow; i++)
            {
                float xPos = startX + (i * horizontalSpacing);
                Vector3 position = new Vector3(xPos, yPos, 0);
                formationSlots.Add(new FormationSlot(position));
            }
        }
    }

    private void GenerateCircle()
    {
        float angleStep = 360f / circleCount;

        for (int i = 0; i < circleCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float xPos = Mathf.Cos(angle) * circleRadius;
            float yPos = Mathf.Sin(angle) * circleRadius;

            Vector3 position = new Vector3(xPos, yPos, 0);
            formationSlots.Add(new FormationSlot(position));
        }
    }

    private void GenerateGrid()
    {
        float halfWidth = (gridSize.x - 1) * 0.5f;
        float halfHeight = (gridSize.y - 1) * 0.5f;

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                float xPos = (x - halfWidth) * horizontalSpacing;
                float yPos = (y - halfHeight) * verticalSpacing;

                Vector3 position = new Vector3(xPos, yPos, 0);
                formationSlots.Add(new FormationSlot(position));
            }
        }
    }

    private void ApplyBoundaryConstraints()
    {
        if (!LevelManager.Instance) return;

        Vector2 boundary = LevelManager.Instance.EnemyBoundary;

        // Find the formation bounds
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var slot in formationSlots)
        {
            minX = Mathf.Min(minX, slot.localPosition.x);
            maxX = Mathf.Max(maxX, slot.localPosition.x);
            minY = Mathf.Min(minY, slot.localPosition.y);
            maxY = Mathf.Max(maxY, slot.localPosition.y);
        }

        float formationWidth = maxX - minX;
        float formationHeight = maxY - minY;

        // Scale down if formation exceeds boundary
        float scaleX = formationWidth > boundary.x ? boundary.x / formationWidth : 1f;
        float scaleY = formationHeight > boundary.y ? boundary.y / formationHeight : 1f;
        float scale = Mathf.Min(scaleX, scaleY) * 0.9f; // 0.9f for margin

        // Apply scaling
        if (scale < 1f)
        {
            foreach (var slot in formationSlots)
            {
                slot.localPosition *= scale;
            }
        }
    }

    // Get world position of a slot
    public Vector3 GetSlotWorldPosition(FormationSlot slot)
    {
        return formationCenter + slot.localPosition;
    }

    // Try to occupy a slot
    public FormationSlot TryOccupySlot(GameObject chicken)
    {
        foreach (var slot in formationSlots)
        {
            if (!slot.isOccupied)
            {
                slot.isOccupied = true;
                slot.occupant = chicken;
                return slot;
            }
        }
        return null;
    }

    // Release a slot
    public void ReleaseSlot(FormationSlot slot)
    {
        if (slot != null)
        {
            slot.isOccupied = false;
            slot.occupant = null;
        }
    }

    // Get nearest available slot to a position
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

    // Get all available slots
    public List<FormationSlot> GetAvailableSlots()
    {
        List<FormationSlot> availableSlots = new List<FormationSlot>();
        foreach (var slot in formationSlots)
        {
            if (!slot.isOccupied)
            {
                availableSlots.Add(slot);
            }
        }
        return availableSlots;
    }

    [Button]
    private void ChangeFormation()
    {
        int nextFormation = ((int)currentFormation + 1) % Enum.GetValues(typeof(FormationType)).Length;
        currentFormation = (FormationType)nextFormation;
        GenerateFormation();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Update formation center for gizmo drawing
        if (LevelManager.Instance && LevelManager.Instance.CurrentPositionOnPath)
        {
            Transform pathTransform = LevelManager.Instance.CurrentPositionOnPath;

            if (useLocalOffset)
            {
                // Apply offset relative to the path's local space
                formationCenter = pathTransform.position +
                    pathTransform.right * formationOffset.x +
                    pathTransform.up * formationOffset.y +
                    pathTransform.forward * formationOffset.z;
            }
            else
            {
                // Apply offset in world space
                formationCenter = pathTransform.position + formationOffset;
            }
        }

        // Draw boundary
        if (LevelManager.Instance)
        {
            Gizmos.color = Color.yellow;
            Vector3 boundarySize = new Vector3(LevelManager.Instance.EnemyBoundary.x, LevelManager.Instance.EnemyBoundary.y, 0.1f);
            Gizmos.DrawWireCube(formationCenter, boundarySize);
        }

        // Draw formation slots
        foreach (var slot in formationSlots)
        {
            Vector3 worldPos = GetSlotWorldPosition(slot);
            Gizmos.color = slot.isOccupied ? occupiedSlotColor : openSlotColor;
            Gizmos.DrawSphere(worldPos, gizmoSize);

            // Draw line from center to slot
            Gizmos.color = Color.gray * 0.5f;
            Gizmos.DrawLine(formationCenter, worldPos);
        }

        // Draw formation center
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(formationCenter, gizmoSize * 1.5f);

        // Draw offset direction arrow if using local offset
        if (useLocalOffset && LevelManager.Instance && LevelManager.Instance.CurrentPositionOnPath)
        {
            Gizmos.color = Color.cyan;
            Vector3 pathPos = LevelManager.Instance.CurrentPositionOnPath.position;
            Gizmos.DrawLine(pathPos, formationCenter);
        }
    }
}