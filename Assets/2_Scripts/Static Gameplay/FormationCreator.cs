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
    
    [Header("Multiple Formations")]
    [Range(1, 3)]
    public int formationCount = 1; // Number of formations (1-3)
    public float formationSpacing = 5f; // Distance between multiple formations
    
    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.red;
    public float gizmoSize = 0.5f;

    private List<Vector3> formationSlots = new List<Vector3>();

    void Start()
    {
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

    // Generate formation slots based on current formation type
    void GenerateFormation()
    {
        formationSlots.Clear();

        // Check if this formation type supports multiple copies
        bool supportsMultiple = currentFormation != FormationType.Cross && currentFormation != FormationType.Multiply;
        int actualCount = supportsMultiple ? formationCount : 1;

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
                xOffset = (i - (actualCount - 1) * 0.5f) * formationSpacing;
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
            formation.Add(new Vector3(i * spacing, 0, 0));
            formation.Add(new Vector3(-i * spacing, 0, 0));
        }
        
        // Vertical line
        for (int i = 1; i <= slotsPerSide; i++)
        {
            formation.Add(new Vector3(0, i * spacing, 0));
            formation.Add(new Vector3(0, -i * spacing, 0));
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
            formation.Add(new Vector3(i * spacing, i * spacing, 0));
            formation.Add(new Vector3(-i * spacing, -i * spacing, 0));
            
            // Top-left to bottom-right diagonal
            formation.Add(new Vector3(-i * spacing, i * spacing, 0));
            formation.Add(new Vector3(i * spacing, -i * spacing, 0));
        }
    }

    void GenerateSquareFormation(List<Vector3> formation)
    {
        int halfSize = slotsPerSide / 2;
        
        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                formation.Add(new Vector3(x * spacing, y * spacing, 0));
            }
        }
    }

    void GenerateCircleFormation(List<Vector3> formation)
    {
        int totalSlots = slotsPerSide * 4; // More slots for smoother circle
        
        for (int i = 0; i < totalSlots; i++)
        {
            float angle = (i * 2 * Mathf.PI) / totalSlots;
            float x = Mathf.Cos(angle) * circleRadius;
            float y = Mathf.Sin(angle) * circleRadius;
            formation.Add(new Vector3(x, y, 0));
        }
        
        // Add center slot
        formation.Add(Vector3.zero);
    }

    void GenerateTriangleFormation(List<Vector3> formation)
    {
        // Create an equilateral triangle formation centered at origin
        float triangleHeight = (slotsPerSide - 1) * spacing * 0.866f; // Total height of triangle
        float centerOffset = triangleHeight * 0.5f; // Offset to center the triangle
        
        for (int row = 0; row < slotsPerSide; row++)
        {
            int slotsInRow = row + 1;
            float rowOffset = -row * spacing * 0.866f + centerOffset; // Apply center offset
            
            for (int col = 0; col < slotsInRow; col++)
            {
                float x = (col - (slotsInRow - 1) * 0.5f) * spacing;
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
            float x1 = -i * spacing * 0.707f; // cos(45°)
            float y1 = i * spacing * 0.707f;  // sin(45°)
            formation.Add(new Vector3(x1, y1, 0));
            
            // Right side of V (45 degree angle)
            float x2 = i * spacing * 0.707f;  // cos(45°)
            float y2 = i * spacing * 0.707f;  // sin(45°)
            formation.Add(new Vector3(x2, y2, 0));
        }
    }

    // Draw gizmos to visualize formation slots
    void OnDrawGizmos()
    {
        if (formationSlots == null || formationSlots.Count == 0)
            return;

        Gizmos.color = gizmoColor;
        
        foreach (Vector3 slot in formationSlots)
        {
            Vector3 worldPos = transform.position + transform.TransformDirection(slot);
            Gizmos.DrawWireSphere(worldPos, gizmoSize);
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
        
        // Show info about multiple formations
        GUILayout.Space(5);
        if (formationCreator.currentFormation == FormationCreator.FormationType.Cross || 
            formationCreator.currentFormation == FormationCreator.FormationType.Multiply)
        {
            EditorGUILayout.HelpBox("Cross and Multiply formations do not support multiple copies.", MessageType.Info);
        }
        else if (formationCreator.formationCount > 1)
        {
            EditorGUILayout.HelpBox($"Showing {formationCreator.formationCount} {formationCreator.currentFormation} formations side by side.", MessageType.Info);
        }
    }
}
#endif