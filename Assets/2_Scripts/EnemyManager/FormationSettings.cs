using System;
using UnityEngine;


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



[Serializable]
public class FormationSettings
{
    [Header("Formation Type")]
    [SerializeField] private FormationType formationType;
    [Tooltip("Where the formation center should be positioned within the boundary. V-Shape formations ignore this and always position at bottom.")]
    [SerializeField] private FormationPosition formationPosition;
    [Tooltip("Number of formation instances to spawn. Grid and V-Shape formations always use 1.")]
    [SerializeField][Range(1, 10)] private int formationCount;
    
    [Header("Formation Parameters")]
    [Tooltip("Number of slots in the V-Shape formation. Should be odd for symmetry.")]
    [SerializeField, Min(3)] private int vShapeCount;
    [SerializeField, Min(2)] private int squareSize;
    [SerializeField, Min(3)] private int triangleRows;
    [SerializeField, Min(8)] private int circleCount;
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private bool gridFillsBoundary;
    

    
    // Public properties to access the settings
    public FormationType FormationType => formationType;
    public FormationPosition FormationPosition => formationPosition;
    public int VShapeCount => vShapeCount;
    public int SquareSize => squareSize;
    public int TriangleRows => triangleRows;
    public int CircleCount => circleCount;
    public Vector2Int GridSize => gridSize;
    public bool GridFillsBoundary => gridFillsBoundary;
    public int FormationCount => formationCount;
    public bool IsGridFillingBoundary => FormationType == FormationType.Grid && GridFillsBoundary;
    
    // Constructor with default values
    public FormationSettings()
    {
        formationType = FormationType.VShape;
        formationPosition = FormationPosition.Center;
        vShapeCount = 19;
        squareSize = 4;
        triangleRows = 4;
        circleCount = 12;
        gridSize = new Vector2Int(5, 3);
        gridFillsBoundary = true;
        formationCount = 1;
    }
    
    // Copy constructor
    public FormationSettings(FormationSettings other)
    {
        formationType = other.formationType;
        formationPosition = other.formationPosition;
        vShapeCount = other.vShapeCount;
        squareSize = other.squareSize;
        triangleRows = other.triangleRows;
        circleCount = other.circleCount;
        gridSize = other.gridSize;
        gridFillsBoundary = other.gridFillsBoundary;
        formationCount = other.formationCount;
    }
    
    
    // Helper methods
    public static bool AreFormationSettingsEqual(FormationSettings a, FormationSettings b)
    {
        return a.FormationType == b.FormationType &&
               a.FormationPosition == b.FormationPosition &&
               a.FormationCount == b.FormationCount &&
               a.VShapeCount == b.VShapeCount &&
               a.SquareSize == b.SquareSize &&
               a.TriangleRows == b.TriangleRows &&
               a.CircleCount == b.CircleCount &&
               a.GridSize == b.GridSize &&
               a.GridFillsBoundary == b.GridFillsBoundary;
    }
}