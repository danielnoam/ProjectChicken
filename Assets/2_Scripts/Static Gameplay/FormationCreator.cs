using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FormationCreator : MonoBehaviour
{
    public enum FormationType
    {
        Cross,
        Multiply,
        Square,
        Circle,
        Triangle,
        VShape
    }

    [Header("Formation Settings")]
    public FormationType currentFormation = FormationType.Cross;
    
    [Header("Formation Spacing")]
    public float crossSpacing = 1f; // Spacing for Cross formation
    public float multiplySpacing = 1f; // Spacing for Multiply formation
    public float spacing = 2f; // For Square, Triangle, VShape formations
    
    [Header("Formation Sizes")]
    public int crossSlotsPerSide = 2; // Size control for Cross formation
    public int multiplySlotsPerSide = 2; // Size control for Multiply formation
    public int slotsPerSide = 3; // For Square, Triangle, VShape formations
    public float circleRadius = 3f;
    
    [Header("Boundary Settings")]
    public bool fitWithinCollider = true;
    [Range(0.1f, 1f)]
    public float boundaryPadding = 0.9f; // How much of the collider to use (90% by default)
    
    [Header("Multiple Formations")]
    [Range(1, 10)]
    public int formationCount = 1; // Number of formations (1-10)
    public float formationSpacing = 5f; // Minimum distance between formation bounding boxes
    
    [Header("Random Placement")]
    public bool useRandomPlacement = true; // If false, uses old side-by-side placement
    [Range(10, 1000)]
    public int maxPlacementAttempts = 100; // Maximum attempts to place formations without overlap
    
    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.red;
    public float gizmoSize = 0.5f;
    public bool showColliderBounds = true;

    private List<Vector3> formationSlots = new List<Vector3>();
    private List<Vector2> formationCenters = new List<Vector2>(); // Centers of placed formations
    private List<Vector2> formationBounds = new List<Vector2>(); // Bounds of placed formations
    private BoxCollider2D boxCollider2D;
    private float effectiveCrossSpacing;
    private float effectiveMultiplySpacing;
    private float effectiveSpacing;
    private float effectiveRadius;
    private float effectiveFormationSpacing;

    void Start()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        if (boxCollider2D == null)
        {
            Debug.LogWarning("FormationCreator: No BoxCollider2D found on " + gameObject.name + ". Formations will not be constrained.");
        }
        GenerateFormation();
    }

    void Update()
    {
        // Cycle through formations with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleFormation();
        }
        
        // Regenerate random placements with R key (only in random placement mode)
        if (Input.GetKeyDown(KeyCode.R) && useRandomPlacement)
        {
            GenerateFormation();
        }
    }

    // Button method to cycle formations (can be called from Inspector button)
    [ContextMenu("Cycle Formation")]
    public void CycleFormation()
    {
        int currentIndex = (int)currentFormation;
        currentIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(FormationType)).Length;
        currentFormation = (FormationType)currentIndex;
        GenerateFormation();
    }

    // Calculate scaling factors to fit within collider bounds
    void CalculateEffectiveValues()
    {
        if (!fitWithinCollider || boxCollider2D == null)
        {
            effectiveCrossSpacing = crossSpacing;
            effectiveMultiplySpacing = multiplySpacing;
            effectiveSpacing = spacing;
            effectiveRadius = circleRadius;
            effectiveFormationSpacing = formationSpacing;
            return;
        }

        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        
        if (useRandomPlacement)
        {
            // For random placement, scale based on single formation size
            Vector2 singleFormationBounds = CalculateFormationBounds();
            
            // Calculate scaling factors for X and Y
            float scaleX = singleFormationBounds.x > 0 ? (colliderSize.x / singleFormationBounds.x) : 1f;
            float scaleY = singleFormationBounds.y > 0 ? (colliderSize.y / singleFormationBounds.y) : 1f;
            
            // Use the smaller scale to ensure it fits in both dimensions
            float uniformScale = Mathf.Min(scaleX, scaleY, 1f); // Don't scale up, only down
            
            effectiveCrossSpacing = crossSpacing * uniformScale;
            effectiveMultiplySpacing = multiplySpacing * uniformScale;
            effectiveSpacing = spacing * uniformScale;
            effectiveRadius = circleRadius * uniformScale;
            effectiveFormationSpacing = formationSpacing * uniformScale;
        }
        else
        {
            // For side-by-side placement, use original logic
            int actualCount = formationCount;

            // Calculate the theoretical size of the formation without scaling
            Vector2 formationBoundsCalc = CalculateFormationBounds();
            
            // For multiple formations, account for spacing between them
            if (actualCount > 1)
            {
                float totalWidth = formationBoundsCalc.x * actualCount + formationSpacing * (actualCount - 1);
                formationBoundsCalc.x = totalWidth;
            }

            // Calculate scaling factors for X and Y
            float scaleX = formationBoundsCalc.x > 0 ? (colliderSize.x / formationBoundsCalc.x) : 1f;
            float scaleY = formationBoundsCalc.y > 0 ? (colliderSize.y / formationBoundsCalc.y) : 1f;
            
            // Use the smaller scale to ensure it fits in both dimensions
            float uniformScale = Mathf.Min(scaleX, scaleY, 1f); // Don't scale up, only down
            
            effectiveCrossSpacing = crossSpacing * uniformScale;
            effectiveMultiplySpacing = multiplySpacing * uniformScale;
            effectiveSpacing = spacing * uniformScale;
            effectiveRadius = circleRadius * uniformScale;
            effectiveFormationSpacing = formationSpacing * uniformScale;
        }
    }

    // Calculate the theoretical bounds of a single formation
    Vector2 CalculateFormationBounds()
    {
        switch (currentFormation)
        {
            case FormationType.Cross:
                return new Vector2(crossSlotsPerSide * crossSpacing * 2, crossSlotsPerSide * crossSpacing * 2);
                
            case FormationType.Multiply:
                float diagonalDistance = multiplySlotsPerSide * multiplySpacing * 1.414f; // sqrt(2)
                return new Vector2(diagonalDistance * 2, diagonalDistance * 2);
                
            case FormationType.Square:
                int halfSize = slotsPerSide / 2;
                return new Vector2(halfSize * spacing * 2, halfSize * spacing * 2);
                
            case FormationType.Circle:
                return new Vector2(circleRadius * 2, circleRadius * 2);
                
            case FormationType.Triangle:
                float triangleWidth = (slotsPerSide - 1) * spacing;
                float triangleHeight = (slotsPerSide - 1) * spacing * 0.866f;
                return new Vector2(triangleWidth, triangleHeight);
                
            case FormationType.VShape:
                float vWidth = slotsPerSide * spacing * 1.414f; // 2 * slotsPerSide * 0.707
                float vHeight = slotsPerSide * spacing * 0.707f; // sin(45°)
                return new Vector2(vWidth, vHeight);
                
            default:
                return Vector2.zero;
        }
    }

    // Generate formation slots based on current formation type
    public void GenerateFormation()
    {
        formationSlots.Clear();
        formationCenters.Clear();
        formationBounds.Clear();
        
        // Calculate effective values based on collider constraints
        CalculateEffectiveValues();

        // Generate the base formation
        List<Vector3> baseFormation = new List<Vector3>();
        
        switch (currentFormation)
        {
            case FormationType.Cross:
                GenerateCrossFormation(baseFormation);
                break;
            case FormationType.Multiply:
                GenerateMultiplyFormation(baseFormation);
                break;
            case FormationType.Square:
                GenerateSquareFormation(baseFormation);
                break;
            case FormationType.Circle:
                GenerateCircleFormation(baseFormation);
                break;
            case FormationType.Triangle:
                GenerateTriangleFormation(baseFormation);
                break;
            case FormationType.VShape:
                GenerateVShapeFormation(baseFormation);
                break;
        }

        if (useRandomPlacement)
        {
            PlaceFormationsRandomly(baseFormation);
        }
        else
        {
            PlaceFormationsSideBySide(baseFormation);
        }
    }

    // Place formations randomly without overlapping
    void PlaceFormationsRandomly(List<Vector3> baseFormation)
    {
        if (boxCollider2D == null)
        {
            // If no collider, just place at origin
            foreach (Vector3 slot in baseFormation)
            {
                formationSlots.Add(slot);
            }
            return;
        }

        Vector2 singleFormationBounds = GetSingleFormationBounds();
        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        Vector2 colliderOffset = boxCollider2D.offset;
        
        // Calculate available area for placement (collider area minus formation size and spacing)
        Vector2 halfFormationSize = singleFormationBounds * 0.5f;
        Vector2 availableMin = colliderOffset - colliderSize * 0.5f + halfFormationSize;
        Vector2 availableMax = colliderOffset + colliderSize * 0.5f - halfFormationSize;
        
        // Check if even one formation can fit
        if (availableMin.x >= availableMax.x || availableMin.y >= availableMax.y)
        {
            Debug.LogWarning("FormationCreator: Formation too large to fit within collider bounds!");
            return;
        }

        int placedCount = 0;
        int attempts = 0;
        
        while (placedCount < formationCount && attempts < maxPlacementAttempts)
        {
            attempts++;
            
            // Generate random position within available area
            Vector2 randomCenter = new Vector2(
                Random.Range(availableMin.x, availableMax.x),
                Random.Range(availableMin.y, availableMax.y)
            );
            
            // Check if this position overlaps with existing formations
            if (IsPositionValid(randomCenter, singleFormationBounds))
            {
                // Place formation at this position
                foreach (Vector3 slot in baseFormation)
                {
                    Vector3 worldSlot = new Vector3(slot.x + randomCenter.x, slot.y + randomCenter.y, slot.z);
                    formationSlots.Add(worldSlot);
                }
                
                // Track this formation's position and bounds
                formationCenters.Add(randomCenter);
                formationBounds.Add(singleFormationBounds);
                placedCount++;
            }
        }
        
        if (placedCount < formationCount)
        {
            Debug.LogWarning($"FormationCreator: Could only place {placedCount} out of {formationCount} formations after {maxPlacementAttempts} attempts. Try reducing formation count or increasing collider size.");
        }
    }

    // Place formations side by side (original behavior)
    void PlaceFormationsSideBySide(List<Vector3> baseFormation)
    {
        int actualCount = formationCount;

        // Position multiple formations side by side
        for (int i = 0; i < actualCount; i++)
        {
            float xOffset = 0f;
            if (actualCount > 1)
            {
                // Center the formations around the origin
                xOffset = (i - (actualCount - 1) * 0.5f) * effectiveFormationSpacing;
            }

            foreach (Vector3 slot in baseFormation)
            {
                formationSlots.Add(new Vector3(slot.x + xOffset, slot.y, slot.z));
            }
        }
    }

    // Check if a position is valid (doesn't overlap with existing formations)
    bool IsPositionValid(Vector2 center, Vector2 bounds)
    {
        for (int i = 0; i < formationCenters.Count; i++)
        {
            if (DoFormationsOverlap(center, bounds, formationCenters[i], formationBounds[i]))
            {
                return false;
            }
        }
        return true;
    }

    // Check if two formations overlap (including spacing buffer)
    bool DoFormationsOverlap(Vector2 center1, Vector2 bounds1, Vector2 center2, Vector2 bounds2)
    {
        // Add formation spacing as buffer
        Vector2 expandedBounds1 = bounds1 + Vector2.one * effectiveFormationSpacing;
        Vector2 expandedBounds2 = bounds2 + Vector2.one * effectiveFormationSpacing;
        
        // Calculate distance between centers
        Vector2 distance = new Vector2(Mathf.Abs(center1.x - center2.x), Mathf.Abs(center1.y - center2.y));
        
        // Check if they overlap
        Vector2 minDistance = (expandedBounds1 + expandedBounds2) * 0.5f;
        
        return distance.x < minDistance.x && distance.y < minDistance.y;
    }

    // Get the actual bounds of a single formation
    Vector2 GetSingleFormationBounds()
    {
        switch (currentFormation)
        {
            case FormationType.Cross:
                return new Vector2(crossSlotsPerSide * effectiveCrossSpacing * 2, crossSlotsPerSide * effectiveCrossSpacing * 2);
                
            case FormationType.Multiply:
                float diagonalDistance = multiplySlotsPerSide * effectiveMultiplySpacing * 1.414f; // sqrt(2)
                return new Vector2(diagonalDistance * 2, diagonalDistance * 2);
                
            case FormationType.Square:
                int halfSize = slotsPerSide / 2;
                return new Vector2(halfSize * effectiveSpacing * 2, halfSize * effectiveSpacing * 2);
                
            case FormationType.Circle:
                return new Vector2(effectiveRadius * 2, effectiveRadius * 2);
                
            case FormationType.Triangle:
                float triangleWidth = (slotsPerSide - 1) * effectiveSpacing;
                float triangleHeight = (slotsPerSide - 1) * effectiveSpacing * 0.866f;
                return new Vector2(triangleWidth, triangleHeight);
                
            case FormationType.VShape:
                float vWidth = slotsPerSide * effectiveSpacing * 1.414f; // 2 * slotsPerSide * 0.707
                float vHeight = slotsPerSide * effectiveSpacing * 0.707f; // sin(45°)
                return new Vector2(vWidth, vHeight);
                
            default:
                return Vector2.zero;
        }
    }

    void GenerateCrossFormation(List<Vector3> formation)
    {
        // Center slot
        formation.Add(Vector3.zero);
        
        // Horizontal line
        for (int i = 1; i <= crossSlotsPerSide; i++)
        {
            formation.Add(new Vector3(i * effectiveCrossSpacing, 0, 0));
            formation.Add(new Vector3(-i * effectiveCrossSpacing, 0, 0));
        }
        
        // Vertical line
        for (int i = 1; i <= crossSlotsPerSide; i++)
        {
            formation.Add(new Vector3(0, i * effectiveCrossSpacing, 0));
            formation.Add(new Vector3(0, -i * effectiveCrossSpacing, 0));
        }
    }

    void GenerateMultiplyFormation(List<Vector3> formation)
    {
        // Center slot
        formation.Add(Vector3.zero);
        
        // Diagonal lines
        for (int i = 1; i <= multiplySlotsPerSide; i++)
        {
            // Top-right to bottom-left diagonal
            formation.Add(new Vector3(i * effectiveMultiplySpacing, i * effectiveMultiplySpacing, 0));
            formation.Add(new Vector3(-i * effectiveMultiplySpacing, -i * effectiveMultiplySpacing, 0));
            
            // Top-left to bottom-right diagonal
            formation.Add(new Vector3(-i * effectiveMultiplySpacing, i * effectiveMultiplySpacing, 0));
            formation.Add(new Vector3(i * effectiveMultiplySpacing, -i * effectiveMultiplySpacing, 0));
        }
    }

    void GenerateSquareFormation(List<Vector3> formation)
    {
        int halfSize = slotsPerSide / 2;
        
        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                formation.Add(new Vector3(x * effectiveSpacing, y * effectiveSpacing, 0));
            }
        }
    }

    void GenerateCircleFormation(List<Vector3> formation)
    {
        int totalSlots = slotsPerSide * 4; // More slots for smoother circle
        
        for (int i = 0; i < totalSlots; i++)
        {
            float angle = (i * 2 * Mathf.PI) / totalSlots;
            float x = Mathf.Cos(angle) * effectiveRadius;
            float y = Mathf.Sin(angle) * effectiveRadius;
            formation.Add(new Vector3(x, y, 0));
        }
        
        // Add center slot
        formation.Add(Vector3.zero);
    }

    void GenerateTriangleFormation(List<Vector3> formation)
    {
        // Create an equilateral triangle formation centered at origin
        float triangleHeight = (slotsPerSide - 1) * effectiveSpacing * 0.866f; // Total height of triangle
        float centerOffset = triangleHeight * 0.5f; // Offset to center the triangle
        
        for (int row = 0; row < slotsPerSide; row++)
        {
            int slotsInRow = row + 1;
            float rowOffset = -row * effectiveSpacing * 0.866f + centerOffset; // Apply center offset
            
            for (int col = 0; col < slotsInRow; col++)
            {
                float x = (col - (slotsInRow - 1) * 0.5f) * effectiveSpacing;
                formation.Add(new Vector3(x, rowOffset, 0));
            }
        }
    }

    void GenerateVShapeFormation(List<Vector3> formation)
    {
        // Calculate offset to center the V shape properly
        float maxY = slotsPerSide * effectiveSpacing * 0.707f; // sin(45°)
        float centerOffsetY = -maxY * 0.5f; // Center the V shape vertically
        
        // Center slot
        formation.Add(new Vector3(0, centerOffsetY, 0));
        
        // Create V shape with two angled lines
        for (int i = 1; i <= slotsPerSide; i++)
        {
            // Left side of V (45 degree angle)
            float x1 = -i * effectiveSpacing * 0.707f; // cos(45°)
            float y1 = i * effectiveSpacing * 0.707f + centerOffsetY;  // sin(45°) + offset
            formation.Add(new Vector3(x1, y1, 0));
            
            // Right side of V (45 degree angle)
            float x2 = i * effectiveSpacing * 0.707f;  // cos(45°)
            float y2 = i * effectiveSpacing * 0.707f + centerOffsetY;  // sin(45°) + offset
            formation.Add(new Vector3(x2, y2, 0));
        }
    }

    // Draw gizmos to visualize formation slots
    void OnDrawGizmos()
    {
        if (formationSlots == null || formationSlots.Count == 0)
            return;

        // Draw formation slots
        Gizmos.color = gizmoColor;
        foreach (Vector3 slot in formationSlots)
        {
            Vector3 worldPos = transform.position + transform.TransformDirection(slot);
            Gizmos.DrawWireSphere(worldPos, gizmoSize);
        }
        
        // Draw formation bounding boxes (only for random placement)
        if (useRandomPlacement && formationCenters.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < formationCenters.Count; i++)
            {
                Vector3 center = transform.position + transform.TransformDirection(new Vector3(formationCenters[i].x, formationCenters[i].y, 0));
                Vector3 size = new Vector3(formationBounds[i].x, formationBounds[i].y, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }
        }
        
        // Draw collider bounds
        if (showColliderBounds && boxCollider2D != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 center = new Vector3(boxCollider2D.offset.x, boxCollider2D.offset.y, 0);
            Vector3 size = new Vector3(boxCollider2D.size.x * boundaryPadding, boxCollider2D.size.y * boundaryPadding, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
        
        // Draw formation name
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, currentFormation.ToString());
        #endif
    }

    // Get all current formation slot positions in world space
    public List<Vector3> GetFormationSlots()
    {
        List<Vector3> worldSlots = new List<Vector3>();
        foreach (Vector3 slot in formationSlots)
        {
            worldSlots.Add(transform.position + transform.TransformDirection(slot));
        }
        return worldSlots;
    }

    // Regenerate formation when values change in inspector
    void OnValidate()
    {
        if (boxCollider2D == null)
            boxCollider2D = GetComponent<BoxCollider2D>();
        GenerateFormation();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FormationCreator))]
public class FormationCreatorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        FormationCreator formationCreator = (FormationCreator)target;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Cycle to Next Formation", GUILayout.Height(30)))
        {
            formationCreator.CycleFormation();
        }
        
        // Add button to regenerate random placements
        if (formationCreator.useRandomPlacement)
        {
            if (GUILayout.Button("Regenerate Random Placements", GUILayout.Height(25)))
            {
                formationCreator.GenerateFormation();
            }
        }
        
        // Show info about collider constraint
        GUILayout.Space(5);
        if (formationCreator.GetComponent<BoxCollider2D>() == null)
        {
            EditorGUILayout.HelpBox("No BoxCollider2D found! Add a BoxCollider2D to constrain formations within bounds.", MessageType.Warning);
        }
        else if (formationCreator.fitWithinCollider)
        {
            EditorGUILayout.HelpBox("Formation will be automatically scaled to fit within the BoxCollider2D bounds.", MessageType.Info);
        }
        
        // Show info about placement mode
        if (formationCreator.useRandomPlacement)
        {
            string sizeInfo = "";
            switch (formationCreator.currentFormation)
            {
                case FormationCreator.FormationType.Cross:
                    sizeInfo = $"(Size: {formationCreator.crossSlotsPerSide} slots, Spacing: {formationCreator.crossSpacing})";
                    break;
                case FormationCreator.FormationType.Multiply:
                    sizeInfo = $"(Size: {formationCreator.multiplySlotsPerSide} slots, Spacing: {formationCreator.multiplySpacing})";
                    break;
                case FormationCreator.FormationType.Square:
                case FormationCreator.FormationType.Triangle:
                case FormationCreator.FormationType.VShape:
                    sizeInfo = $"(Size: {formationCreator.slotsPerSide} slots, Spacing: {formationCreator.spacing})";
                    break;
                case FormationCreator.FormationType.Circle:
                    sizeInfo = $"(Radius: {formationCreator.circleRadius})";
                    break;
            }
            
            EditorGUILayout.HelpBox($"Random placement: {formationCreator.formationCount} {formationCreator.currentFormation} formations {sizeInfo} with {formationCreator.formationSpacing} unit spacing between bounding boxes.\n\nControls:\n• Tab: Cycle formations\n• R: Regenerate random placements\n\nGizmos:\n• Red spheres: Formation slots\n• Yellow boxes: Formation bounding boxes\n• Blue box: Collider bounds", MessageType.Info);
        }
        else if (formationCreator.formationCount > 1)
        {
            EditorGUILayout.HelpBox($"Side-by-side placement: {formationCreator.formationCount} {formationCreator.currentFormation} formations.\n\nControls:\n• Tab: Cycle formations", MessageType.Info);
        }
    }
}
#endif