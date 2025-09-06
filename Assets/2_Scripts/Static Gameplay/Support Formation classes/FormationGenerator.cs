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
        int sideLength = creator.CalculatedSideLength;
        int halfSize = (sideLength - 1) / 2;
        
        // Generate grid centered at origin
        for (int x = 0; x < sideLength; x++)
        {
            for (int y = 0; y < sideLength; y++)
            {
                float xPos = (x - halfSize) * effectiveSpacing;
                float yPos = (y - halfSize) * effectiveSpacing;
                formation.Add(new Vector3(xPos, yPos, 0));
            }
        }
    }

    void GenerateCircleFormation(List<Vector3> formation)
    {
        float effectiveRadius = creator.BoundaryManager.EffectiveRadius;
        int totalSlots = creator.ActualSlotCount;
        
        if (totalSlots == 1)
        {
            // Single slot at center
            formation.Add(Vector3.zero);
            return;
        }
        
        // Distribute slots evenly around the circle
        for (int i = 0; i < totalSlots; i++)
        {
            float angle = (i * 2 * Mathf.PI) / totalSlots;
            float x = Mathf.Cos(angle) * effectiveRadius;
            float y = Mathf.Sin(angle) * effectiveRadius;
            formation.Add(new Vector3(x, y, 0));
        }
    }

    void GenerateTriangleFormation(List<Vector3> formation)
    {
        float effectiveSpacing = creator.BoundaryManager.EffectiveSpacing;
        int rows = creator.CalculatedRows;
        
        // Calculate triangle height and center offset
        float triangleHeight = (rows - 1) * effectiveSpacing * 0.866f; // sin(60°) for equilateral triangle
        float centerOffsetY = triangleHeight * 0.5f; // Offset to center the triangle
        
        for (int row = 0; row < rows; row++)
        {
            int slotsInRow = row + 1;
            float rowY = -row * effectiveSpacing * 0.866f + centerOffsetY; // Apply center offset
            
            for (int col = 0; col < slotsInRow; col++)
            {
                float x = (col - (slotsInRow - 1) * 0.5f) * effectiveSpacing;
                formation.Add(new Vector3(x, rowY, 0));
            }
        }
    }

    void GenerateVShapeFormation(List<Vector3> formation)
    {
        float effectiveSpacing = creator.BoundaryManager.EffectiveSpacing;
        int slotsPerSide = creator.CalculatedSlotsPerSide;
        
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

    // Calculate the theoretical bounds of a single formation based on original parameters
    public Vector2 CalculateFormationBounds(FormationCreator.FormationType formationType)
    {
        switch (formationType)
        {
            case FormationCreator.FormationType.Square:
                int sideLength = creator.CalculatedSideLength;
                float squareSize = (sideLength - 1) * creator.spacing;
                return new Vector2(squareSize, squareSize);
                
            case FormationCreator.FormationType.Circle:
                return new Vector2(creator.circleRadius * 2, creator.circleRadius * 2);
                
            case FormationCreator.FormationType.Triangle:
                int rows = creator.CalculatedRows;
                float triangleWidth = (rows - 1) * creator.spacing;
                float triangleHeight = (rows - 1) * creator.spacing * 0.866f;
                return new Vector2(triangleWidth, triangleHeight);
                
            case FormationCreator.FormationType.VShape:
                int slotsPerSide = creator.CalculatedSlotsPerSide;
                float vWidth = slotsPerSide * creator.spacing * 1.414f; // 2 * slotsPerSide * 0.707
                float vHeight = slotsPerSide * creator.spacing * 0.707f; // sin(45°)
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
                int sideLength = creator.CalculatedSideLength;
                float squareSize = (sideLength - 1) * creator.BoundaryManager.EffectiveSpacing;
                return new Vector2(squareSize, squareSize);
                
            case FormationCreator.FormationType.Circle:
                return new Vector2(creator.BoundaryManager.EffectiveRadius * 2, 
                                   creator.BoundaryManager.EffectiveRadius * 2);
                
            case FormationCreator.FormationType.Triangle:
                int rows = creator.CalculatedRows;
                float triangleWidth = (rows - 1) * creator.BoundaryManager.EffectiveSpacing;
                float triangleHeight = (rows - 1) * creator.BoundaryManager.EffectiveSpacing * 0.866f;
                return new Vector2(triangleWidth, triangleHeight);
                
            case FormationCreator.FormationType.VShape:
                int slotsPerSide = creator.CalculatedSlotsPerSide;
                float vWidth = slotsPerSide * creator.BoundaryManager.EffectiveSpacing * 1.414f;
                float vHeight = slotsPerSide * creator.BoundaryManager.EffectiveSpacing * 0.707f;
                return new Vector2(vWidth, vHeight);
                
            default:
                return Vector2.zero;
        }
    }
    
    // Helper method to get formation info for debugging
    public string GetFormationInfo(FormationCreator.FormationType formationType)
    {
        switch (formationType)
        {
            case FormationCreator.FormationType.Square:
                return $"Square: {creator.CalculatedSideLength}x{creator.CalculatedSideLength} = {creator.ActualSlotCount} slots";
                
            case FormationCreator.FormationType.Triangle:
                return $"Triangle: {creator.CalculatedRows} rows = {creator.ActualSlotCount} slots";
                
            case FormationCreator.FormationType.Circle:
                return $"Circle: {creator.ActualSlotCount} slots";
                
            case FormationCreator.FormationType.VShape:
                return $"V-Shape: 1 center + {creator.CalculatedSlotsPerSide}×2 sides = {creator.ActualSlotCount} slots";
                
            default:
                return formationType.ToString();
        }
    }
}