using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FormationVisualizer : MonoBehaviour
{
    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.red;
    public float gizmoSize = 0.5f;
    public bool showColliderBounds = true;
    public Color validationErrorColor = Color.magenta;

    private FormationCreator creator;
    private FormationBoundaryManager boundaryManager;
    private FormationPlacer placer;
    private FormationValidator validator;

    public void Initialize(FormationCreator formationCreator, FormationBoundaryManager boundary, FormationPlacer formationPlacer, FormationValidator formationValidator)
    {
        creator = formationCreator;
        boundaryManager = boundary;
        placer = formationPlacer;
        validator = formationValidator;
    }

    // Draw gizmos to visualize formation slots
    public void OnDrawGizmos()
    {
        if (creator == null)
            return;

        DrawFormationSlots();
        DrawFormationBoundingBoxes();
        DrawColliderBounds();
        DrawFormationNameAndStatus();
    }

    void DrawFormationSlots()
    {
        if (creator.FormationSlots == null || creator.FormationSlots.Count == 0)
            return;

        // Draw formation slots
        Gizmos.color = gizmoColor;
        foreach (Vector3 slot in creator.FormationSlots)
        {
            Vector3 worldPos = transform.position + transform.TransformDirection(slot);
            Gizmos.DrawWireSphere(worldPos, gizmoSize);
        }
    }

    void DrawFormationBoundingBoxes()
    {
        // Draw formation bounding boxes (only for random placement)
        if (creator.useRandomPlacement && placer != null && placer.FormationCenters.Count > 0)
        {
            List<int> invalidIndices = validator != null ? validator.InvalidFormationIndices : new List<int>();
            
            for (int i = 0; i < placer.FormationCenters.Count; i++)
            {
                // Use different color for invalid formations
                if (invalidIndices.Contains(i))
                {
                    Gizmos.color = validationErrorColor;
                }
                else
                {
                    Gizmos.color = Color.yellow;
                }
                
                if (i < placer.FormationBounds.Count)
                {
                    Vector3 center = transform.position + transform.TransformDirection(new Vector3(placer.FormationCenters[i].x, placer.FormationCenters[i].y, 0));
                    Vector3 size = new Vector3(placer.FormationBounds[i].x, placer.FormationBounds[i].y, 0.1f);
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }
    }

    void DrawColliderBounds()
    {
        // Draw collider bounds
        if (showColliderBounds && boundaryManager != null && boundaryManager.BoxCollider2D != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 center = new Vector3(boundaryManager.ColliderOffset.x, boundaryManager.ColliderOffset.y, 0);
            Vector3 size = new Vector3(boundaryManager.ColliderSize.x, boundaryManager.ColliderSize.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = Matrix4x4.identity; // Reset matrix
        }
    }

    void DrawFormationNameAndStatus()
    {
        // Draw formation name and validation status
        #if UNITY_EDITOR
        string statusText = creator.currentFormation.ToString();
        if (validator != null && validator.ValidateFormationChanges && validator.InvalidFormationCount > 0)
        {
            statusText += $" [{validator.InvalidFormationCount} REPOSITIONED]";
        }
        Handles.Label(transform.position + Vector3.up * 2, statusText);
        #endif
    }

    // Additional visualization methods can be added here
    public void DrawCustomGizmo(Vector3 position, Color color, float size, string label = "")
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position + transform.TransformDirection(position), size);
        
        #if UNITY_EDITOR
        if (!string.IsNullOrEmpty(label))
        {
            Handles.Label(transform.position + transform.TransformDirection(position) + Vector3.up, label);
        }
        #endif
    }

    public void SetGizmoSettings(Color color, float size, bool showBounds = true)
    {
        gizmoColor = color;
        gizmoSize = size;
        showColliderBounds = showBounds;
    }

    // Context menu for testing visualization
    [ContextMenu("Toggle Collider Bounds")]
    void ToggleColliderBounds()
    {
        showColliderBounds = !showColliderBounds;
        Debug.Log($"FormationVisualizer: Collider bounds visualization {(showColliderBounds ? "enabled" : "disabled")}");
    }

    [ContextMenu("Change Gizmo Color")]
    void CycleGizmoColor()
    {
        Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        int currentIndex = System.Array.FindIndex(colors, c => c == gizmoColor);
        currentIndex = (currentIndex + 1) % colors.Length;
        gizmoColor = colors[currentIndex];
        Debug.Log($"FormationVisualizer: Changed gizmo color to {gizmoColor}");
    }

    [ContextMenu("Increase Gizmo Size")]
    void IncreaseGizmoSize()
    {
        gizmoSize = Mathf.Min(2f, gizmoSize + 0.1f);
        Debug.Log($"FormationVisualizer: Gizmo size increased to {gizmoSize:F1}");
    }

    [ContextMenu("Decrease Gizmo Size")]
    void DecreaseGizmoSize()
    {
        gizmoSize = Mathf.Max(0.1f, gizmoSize - 0.1f);
        Debug.Log($"FormationVisualizer: Gizmo size decreased to {gizmoSize:F1}");
    }
}