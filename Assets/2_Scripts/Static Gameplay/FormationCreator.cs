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
    public float spacing = 2f;
    public int slotsPerSide = 3; // For formations that need size control
    public float circleRadius = 3f;
    
    [Header("Boundary Settings")]
    public bool fitWithinCollider = true;
    [Range(0.1f, 1f)]
    public float boundaryPadding = 0.9f; // How much of the collider to use (90% by default)
    
    [Header("Multiple Formations")]
    [Range(1, 3)]
    public int formationCount = 1; // Number of formations (1-3)
    public float formationSpacing = 5f; // Distance between multiple formations
    
    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.red;
    public float gizmoSize = 0.5f;
    public bool showColliderBounds = true;

    private List<Vector3> formationSlots = new List<Vector3>();
    private BoxCollider2D boxCollider2D;
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
            effectiveSpacing = spacing;
            effectiveRadius = circleRadius;
            effectiveFormationSpacing = formationSpacing;
            return;
        }

        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        
        // All formation types now support multiple copies
        int actualCount = formationCount;

        // Calculate the theoretical size of the formation without scaling
        Vector2 formationBounds = CalculateFormationBounds();
        
        // For multiple formations, account for spacing between them
        if (actualCount > 1)
        {
            float totalWidth = formationBounds.x * actualCount + formationSpacing * (actualCount - 1);
            formationBounds.x = totalWidth;
        }

        // Calculate scaling factors for X and Y
        float scaleX = formationBounds.x > 0 ? (colliderSize.x / formationBounds.x) : 1f;
        float scaleY = formationBounds.y > 0 ? (colliderSize.y / formationBounds.y) : 1f;
        
        // Use the smaller scale to ensure it fits in both dimensions
        float uniformScale = Mathf.Min(scaleX, scaleY, 1f); // Don't scale up, only down
        
        effectiveSpacing = spacing * uniformScale;
        effectiveRadius = circleRadius * uniformScale;
        effectiveFormationSpacing = formationSpacing * uniformScale;
    }

    // Calculate the theoretical bounds of a single formation
    Vector2 CalculateFormationBounds()
    {
        switch (currentFormation)
        {
            case FormationType.Cross:
                return new Vector2(slotsPerSide * spacing * 2, slotsPerSide * spacing * 2);
                
            case FormationType.Multiply:
                float diagonalDistance = slotsPerSide * spacing * 1.414f; // sqrt(2)
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
                float vWidth = slotsPerSide * spacing * 1.414f; // sqrt(2)
                float vHeight = slotsPerSide * spacing * 0.707f; // sin(45°)
                return new Vector2(vWidth, vHeight);
                
            default:
                return Vector2.zero;
        }
    }

    // Generate formation slots based on current formation type
    void GenerateFormation()
    {
        formationSlots.Clear();
        
        // Calculate effective values based on collider constraints
        CalculateEffectiveValues();

        // All formation types now support multiple copies
        int actualCount = formationCount;

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

    void GenerateCrossFormation(List<Vector3> formation)
    {
        // Center slot
        formation.Add(Vector3.zero);
        
        // Horizontal line
        for (int i = 1; i <= slotsPerSide; i++)
        {
            formation.Add(new Vector3(i * effectiveSpacing, 0, 0));
            formation.Add(new Vector3(-i * effectiveSpacing, 0, 0));
        }
        
        // Vertical line
        for (int i = 1; i <= slotsPerSide; i++)
        {
            formation.Add(new Vector3(0, i * effectiveSpacing, 0));
            formation.Add(new Vector3(0, -i * effectiveSpacing, 0));
        }
    }

    void GenerateMultiplyFormation(List<Vector3> formation)
    {
        // Center slot
        formation.Add(Vector3.zero);
        
        // Diagonal lines
        for (int i = 1; i <= slotsPerSide; i++)
        {
            // Top-right to bottom-left diagonal
            formation.Add(new Vector3(i * effectiveSpacing, i * effectiveSpacing, 0));
            formation.Add(new Vector3(-i * effectiveSpacing, -i * effectiveSpacing, 0));
            
            // Top-left to bottom-right diagonal
            formation.Add(new Vector3(-i * effectiveSpacing, i * effectiveSpacing, 0));
            formation.Add(new Vector3(i * effectiveSpacing, -i * effectiveSpacing, 0));
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
        // Center slot
        formation.Add(Vector3.zero);
        
        // Create V shape with two angled lines
        for (int i = 1; i <= slotsPerSide; i++)
        {
            // Left side of V (45 degree angle)
            float x1 = -i * effectiveSpacing * 0.707f; // cos(45°)
            float y1 = i * effectiveSpacing * 0.707f;  // sin(45°)
            formation.Add(new Vector3(x1, y1, 0));
            
            // Right side of V (45 degree angle)
            float x2 = i * effectiveSpacing * 0.707f;  // cos(45°)
            float y2 = i * effectiveSpacing * 0.707f;  // sin(45°)
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
        
        // Show info about multiple formations
        if (formationCreator.formationCount > 1)
        {
            EditorGUILayout.HelpBox($"Showing {formationCreator.formationCount} {formationCreator.currentFormation} formations side by side.", MessageType.Info);
        }
    }
}
#endif