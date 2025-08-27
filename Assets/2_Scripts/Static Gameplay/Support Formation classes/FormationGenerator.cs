using UnityEngine;
using System.Collections.Generic;

public class FormationGenerator : MonoBehaviour
{
    private FormationCreator creator;

    public void Initialize(FormationCreator formationCreator)
    {
        creator = formationCreator;
    }

    public List<Vector3> GenerateFormation(FormationCreator.FormationType formationType)
    {
        List<Vector3> formation = new List<Vector3>();
        
        switch (formationType)
        {
            case FormationCreator.FormationType.Square:
                GenerateSquareFormation(formation);
                break;
            case FormationCreator.FormationType.Circle:
                GenerateCircleFormation(formation);
                break;
            case FormationCreator.FormationType.Triangle:
                GenerateTriangleFormation(formation);
                break;
            case FormationCreator.FormationType.VShape:
                GenerateVShapeFormation(formation);
                break;
        }
        
        return formation;
    }

    void GenerateSquareFormation(List<Vector3> formation)
    {
        float effectiveSpacing = creator.BoundaryManager.EffectiveSpacing;
        int halfSize = creator.slotsPerSide / 2;
        
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
        float effectiveRadius = creator.BoundaryManager.EffectiveRadius;
        int totalSlots = creator.slotsPerSide * 4; // More slots for smoother circle
        
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
        float effectiveSpacing = creator.BoundaryManager.EffectiveSpacing;
        
        // Create an equilateral triangle formation centered at origin
        float triangleHeight = (creator.slotsPerSide - 1) * effectiveSpacing * 0.866f; // Total height of triangle
        float centerOffset = triangleHeight * 0.5f; // Offset to center the triangle
        
        for (int row = 0; row < creator.slotsPerSide; row++)
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
        float effectiveSpacing = creator.BoundaryManager.EffectiveSpacing;
        
        // Calculate offset to center the V shape properly
        float maxY = creator.slotsPerSide * effectiveSpacing * 0.707f; // sin(45°)
        float centerOffsetY = -maxY * 0.5f; // Center the V shape vertically
        
        // Center slot
        formation.Add(new Vector3(0, centerOffsetY, 0));
        
        // Create V shape with two angled lines
        for (int i = 1; i <= creator.slotsPerSide; i++)
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

    // Calculate the theoretical bounds of a single formation
    public Vector2 CalculateFormationBounds(FormationCreator.FormationType formationType)
    {
        switch (formationType)
        {
            case FormationCreator.FormationType.Square:
                int halfSize = creator.slotsPerSide / 2;
                return new Vector2(halfSize * creator.spacing * 2, halfSize * creator.spacing * 2);
                
            case FormationCreator.FormationType.Circle:
                return new Vector2(creator.circleRadius * 2, creator.circleRadius * 2);
                
            case FormationCreator.FormationType.Triangle:
                float triangleWidth = (creator.slotsPerSide - 1) * creator.spacing;
                float triangleHeight = (creator.slotsPerSide - 1) * creator.spacing * 0.866f;
                return new Vector2(triangleWidth, triangleHeight);
                
            case FormationCreator.FormationType.VShape:
                float vWidth = creator.slotsPerSide * creator.spacing * 1.414f; // 2 * slotsPerSide * 0.707
                float vHeight = creator.slotsPerSide * creator.spacing * 0.707f; // sin(45°)
                return new Vector2(vWidth, vHeight);
                
            default:
                return Vector2.zero;
        }
    }

    // Get the actual bounds of a single formation with effective values
    public Vector2 GetSingleFormationBounds(FormationCreator.FormationType formationType)
    {
        switch (formationType)
        {
            case FormationCreator.FormationType.Square:
                int halfSize = creator.slotsPerSide / 2;
                return new Vector2(halfSize * creator.BoundaryManager.EffectiveSpacing * 2, 
                                   halfSize * creator.BoundaryManager.EffectiveSpacing * 2);
                
            case FormationCreator.FormationType.Circle:
                return new Vector2(creator.BoundaryManager.EffectiveRadius * 2, 
                                   creator.BoundaryManager.EffectiveRadius * 2);
                
            case FormationCreator.FormationType.Triangle:
                float triangleWidth = (creator.slotsPerSide - 1) * creator.BoundaryManager.EffectiveSpacing;
                float triangleHeight = (creator.slotsPerSide - 1) * creator.BoundaryManager.EffectiveSpacing * 0.866f;
                return new Vector2(triangleWidth, triangleHeight);
                
            case FormationCreator.FormationType.VShape:
                float vWidth = creator.slotsPerSide * creator.BoundaryManager.EffectiveSpacing * 1.414f;
                float vHeight = creator.slotsPerSide * creator.BoundaryManager.EffectiveSpacing * 0.707f;
                return new Vector2(vWidth, vHeight);
                
            default:
                return Vector2.zero;
        }
    }
}