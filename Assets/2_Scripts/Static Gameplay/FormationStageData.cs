using System;
using UnityEngine;
using VInspector;

[Serializable]
public class FormationStageData
{
    [Tooltip("Type of formation to create")]
    [SerializeField] private FormationCreator.FormationType formationType;
    
    [Tooltip("Number of formation instances to spawn")]
    [SerializeField, Range(1, 10)] private int formationCount;
    
    [Tooltip("Total number of slots desired (will be rounded up to fit formation shape)")]
    [SerializeField, Min(1)] private int numberOfSlots;
    
    [Tooltip("Spacing between slots in Square, Triangle, and VShape formations")]
    [SerializeField, Min(0.1f), HideIf("formationType", FormationCreator.FormationType.Circle)] private float spacing;
    
    [Tooltip("Radius of the circle formation")]
    [SerializeField, Min(0.1f), ShowIf("formationType", FormationCreator.FormationType.Circle)] private float circleRadius;
    
    [Header("Placement Settings")]
    [Tooltip("Use random placement instead of side-by-side positioning")]
    [SerializeField] private bool useRandomPlacement;
    
    [Tooltip("Spacing between multiple formations")]
    [SerializeField, Min(0.1f)] private float formationSpacing;
    
    [Tooltip("Maximum attempts to place formations without overlap")]
    [SerializeField, Range(10, 1000), ShowIf("useRandomPlacement")] private int maxPlacementAttempts;
    
    // Public properties to access the settings
    public FormationCreator.FormationType FormationType => formationType;
    public int FormationCount => formationCount;
    public int NumberOfSlots => numberOfSlots;
    public float Spacing => spacing;
    public float CircleRadius => circleRadius;
    public bool UseRandomPlacement => useRandomPlacement;
    public float FormationSpacing => formationSpacing;
    public int MaxPlacementAttempts => maxPlacementAttempts;
    
    // Constructor with default values
    public FormationStageData()
    {
        formationType = FormationCreator.FormationType.Square;
        formationCount = 1;
        numberOfSlots = 9;
        spacing = 2f;
        circleRadius = 3f;
        useRandomPlacement = true;
        formationSpacing = 8f;
        maxPlacementAttempts = 100;
    }
    
    // Copy constructor
    public FormationStageData(FormationStageData other)
    {
        formationType = other.formationType;
        formationCount = other.formationCount;
        numberOfSlots = other.numberOfSlots;
        spacing = other.spacing;
        circleRadius = other.circleRadius;
        useRandomPlacement = other.useRandomPlacement;
        formationSpacing = other.formationSpacing;
        maxPlacementAttempts = other.maxPlacementAttempts;
    }
    
    // Apply these settings to a FormationCreator
    public void ApplyToFormationCreator(FormationCreator formationCreator)
    {
        if (formationCreator == null)
        {
            Debug.LogError("FormationStageData: Cannot apply settings to null FormationCreator");
            return;
        }
        
        formationCreator.currentFormation = formationType;
        formationCreator.formationCount = formationCount;
        formationCreator.numberOfSlots = numberOfSlots;
        formationCreator.spacing = spacing;
        formationCreator.circleRadius = circleRadius;
        formationCreator.useRandomPlacement = useRandomPlacement;
        formationCreator.formationSpacing = formationSpacing;
        formationCreator.maxPlacementAttempts = maxPlacementAttempts;
        
        // Trigger formation regeneration
        formationCreator.GenerateFormation();
    }
    
    // Load settings from a FormationCreator
    public void LoadFromFormationCreator(FormationCreator formationCreator)
    {
        if (formationCreator == null)
        {
            Debug.LogError("FormationStageData: Cannot load settings from null FormationCreator");
            return;
        }
        
        formationType = formationCreator.currentFormation;
        formationCount = formationCreator.formationCount;
        numberOfSlots = formationCreator.numberOfSlots;
        spacing = formationCreator.spacing;
        circleRadius = formationCreator.circleRadius;
        useRandomPlacement = formationCreator.useRandomPlacement;
        formationSpacing = formationCreator.formationSpacing;
        maxPlacementAttempts = formationCreator.maxPlacementAttempts;
    }
    
    // Helper method to check if two FormationStageData are equal
    public static bool AreFormationStageDataEqual(FormationStageData a, FormationStageData b)
    {
        return a.FormationType == b.FormationType &&
               a.FormationCount == b.FormationCount &&
               a.NumberOfSlots == b.NumberOfSlots &&
               Mathf.Approximately(a.Spacing, b.Spacing) &&
               Mathf.Approximately(a.CircleRadius, b.CircleRadius) &&
               a.UseRandomPlacement == b.UseRandomPlacement &&
               Mathf.Approximately(a.FormationSpacing, b.FormationSpacing) &&
               a.MaxPlacementAttempts == b.MaxPlacementAttempts;
    }
    
    // Method to cycle through formation types
    public void SetNextFormationType()
    {
        int nextFormation = ((int)formationType + 1) % Enum.GetValues(typeof(FormationCreator.FormationType)).Length;
        formationType = (FormationCreator.FormationType)nextFormation;
    }
    
    // Method to set formation type by index (useful for dropdown)
    public void SetFormationType(int index)
    {
        if (index >= 0 && index < Enum.GetValues(typeof(FormationCreator.FormationType)).Length)
        {
            formationType = (FormationCreator.FormationType)index;
        }
    }
    
    // Get all available formation types as strings (useful for UI dropdowns)
    public static string[] GetFormationTypeNames()
    {
        return Enum.GetNames(typeof(FormationCreator.FormationType));
    }
    
    // Get the current formation type as index (useful for dropdowns)
    public int GetFormationTypeIndex()
    {
        return (int)formationType;
    }
    
    // Validation methods
    public bool IsValidConfiguration()
    {
        if (formationCount < 1 || formationCount > 10)
            return false;
            
        if (numberOfSlots < 1)
            return false;
            
        if (spacing <= 0)
            return false;
            
        if (circleRadius <= 0)
            return false;
            
        if (formationSpacing < 0)
            return false;
            
        if (maxPlacementAttempts < 10)
            return false;
            
        return true;
    }
    
    // Calculate what the actual slot count would be for the current settings
    public int CalculateActualSlotCount()
    {
        switch (formationType)
        {
            case FormationCreator.FormationType.Square:
                int sideLength = Mathf.CeilToInt(Mathf.Sqrt(numberOfSlots));
                return sideLength * sideLength;
                
            case FormationCreator.FormationType.Triangle:
                // Find smallest triangular number >= numberOfSlots
                int rows = Mathf.CeilToInt((-1f + Mathf.Sqrt(1f + 8f * numberOfSlots)) / 2f);
                return rows * (rows + 1) / 2;
                
            case FormationCreator.FormationType.Circle:
                return numberOfSlots; // Circle can accommodate exact number
                
            case FormationCreator.FormationType.VShape:
                // V-shape has 1 center + 2*slotsPerSide
                int slotsPerSide = Mathf.CeilToInt((numberOfSlots - 1) / 2f);
                return 1 + 2 * slotsPerSide;
                
            default:
                return numberOfSlots;
        }
    }
    
    // Get a description of the current formation settings
    public string GetFormationDescription()
    {
        int actualSlots = CalculateActualSlotCount();
        
        switch (formationType)
        {
            case FormationCreator.FormationType.Square:
                int sideLength = Mathf.CeilToInt(Mathf.Sqrt(numberOfSlots));
                return $"Square formation: {actualSlots} slots ({sideLength}x{sideLength}), spacing {spacing:F1}";
                
            case FormationCreator.FormationType.Circle:
                return $"Circle formation: {actualSlots} slots (1.5x requested {numberOfSlots}), radius {circleRadius:F1}";
                
            case FormationCreator.FormationType.Triangle:
                int rows = Mathf.CeilToInt((-1f + Mathf.Sqrt(1f + 8f * numberOfSlots)) / 2f);
                return $"Triangle formation: {actualSlots} slots ({rows} rows), spacing {spacing:F1}";
                
            case FormationCreator.FormationType.VShape:
                int slotsPerSide = Mathf.CeilToInt((numberOfSlots - 1) / 2f);
                return $"V-Shape formation: {actualSlots} slots ({slotsPerSide} per side), spacing {spacing:F1}";
                
            default:
                return formationType.ToString();
        }
    }
    
    // Get formation efficiency (how close actual slots are to requested)
    public float GetFormationEfficiency()
    {
        int actualSlots = CalculateActualSlotCount();
        return (float)numberOfSlots / actualSlots;
    }
    
    // Reset to default values
    public void ResetToDefaults()
    {
        formationType = FormationCreator.FormationType.Square;
        formationCount = 1;
        numberOfSlots = 9;
        spacing = 8f;
        circleRadius = 22.5f;
        useRandomPlacement = true;
        formationSpacing = 8;
        maxPlacementAttempts = 100;
    }
}