using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FormationCreator : MonoBehaviour
{
    public enum FormationType
    {
        Square,
        Circle,
        Triangle,
        VShape
    }

    [Header("Formation Settings")]
    public FormationType currentFormation = FormationType.Square;
    
    [Header("Formation Spacing")]
    public float spacing = 2f; // For Square, Triangle, VShape formations
    
    [Header("Formation Sizes")]
    public int slotsPerSide = 3; // For Square, Triangle, VShape formations
    public float circleRadius = 3f;
    
    [Header("Multiple Formations")]
    [Range(1, 10)]
    public int formationCount = 1; // Number of formations (1-10)
    public float formationSpacing = 5f; // Minimum distance between formation bounding boxes
    
    [Header("Random Placement")]
    public bool useRandomPlacement = true; // If false, uses old side-by-side placement
    [Range(10, 1000)]
    public int maxPlacementAttempts = 100; // Maximum attempts to place formations without overlap

    // Components
    private FormationGenerator generator;
    private FormationPlacer placer;
    private FormationValidator validator;
    private FormationBoundaryManager boundaryManager;
    private FormationVisualizer visualizer;
    
    // Formation state
    private List<Vector3> formationSlots = new List<Vector3>();
    private FormationType previousFormationType;
    private bool hasBeenInitialized = false;

    void Awake()
    {
        // Initialize components
        InitializeComponents();
    }

    void Start()
    {
        // Store initial formation type
        previousFormationType = currentFormation;
        
        // Initial generation
        GenerateFormation();
        
        if (useRandomPlacement)
        {
            placer.RandomizeAllPositions();
        }
        
        hasBeenInitialized = true;
    }

    void Update()
    {
        // Handle input
        HandleInput();
        
        // Check for formation type changes
        if (hasBeenInitialized && currentFormation != previousFormationType)
        {
            HandleFormationTypeChange();
            previousFormationType = currentFormation;
        }
    }

    void InitializeComponents()
    {
        // Get or add required components
        generator = GetComponent<FormationGenerator>() ?? gameObject.AddComponent<FormationGenerator>();
        placer = GetComponent<FormationPlacer>() ?? gameObject.AddComponent<FormationPlacer>();
        validator = GetComponent<FormationValidator>() ?? gameObject.AddComponent<FormationValidator>();
        boundaryManager = GetComponent<FormationBoundaryManager>() ?? gameObject.AddComponent<FormationBoundaryManager>();
        visualizer = GetComponent<FormationVisualizer>() ?? gameObject.AddComponent<FormationVisualizer>();
        
        // Initialize components with references
        generator.Initialize(this);
        placer.Initialize(this, boundaryManager, validator);
        validator.Initialize(this, boundaryManager, placer);
        boundaryManager.Initialize(this);
        visualizer.Initialize(this, boundaryManager, placer, validator);
    }

    void HandleInput()
    {
        // Cycle through formations with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleFormation();
        }
        
        // Randomize positions with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            RandomizeAllPositions();
        }
    }

    void HandleFormationTypeChange()
    {
        Debug.Log($"FormationCreator: Formation type changed from {previousFormationType} to {currentFormation}. Processing change...");
        
        if (validator.ValidateFormationChanges && useRandomPlacement)
        {
            validator.HandleFormationTypeChange();
        }
        else
        {
            // No validation - just update shapes
            GenerateFormation();
        }
    }

    // Main formation generation method
    public void GenerateFormation()
    {
        // Clear existing slots
        formationSlots.Clear();
        
        // Generate base formation using generator
        List<Vector3> baseFormation = generator.GenerateFormation(currentFormation);
        
        // Place formations using placer
        formationSlots = placer.PlaceFormations(baseFormation);
        
        Debug.Log($"FormationCreator: Generated {currentFormation} formation with {formationSlots.Count} total slots");
    }

    // Public methods for external control
    [ContextMenu("Cycle Formation")]
    public void CycleFormation()
    {
        int currentIndex = (int)currentFormation;
        currentIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(FormationType)).Length;
        currentFormation = (FormationType)currentIndex;
    }

    [ContextMenu("Randomize All Positions")]
    public void RandomizeAllPositions()
    {
        if (!useRandomPlacement)
        {
            Debug.Log("FormationCreator: Random placement is disabled, cannot randomize positions");
            return;
        }
        
        placer.RandomizeAllPositions();
        GenerateFormation();
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

    // Properties for component access
    public FormationGenerator Generator => generator;
    public FormationPlacer Placer => placer;
    public FormationValidator Validator => validator;
    public FormationBoundaryManager BoundaryManager => boundaryManager;
    public FormationVisualizer Visualizer => visualizer;
    
    // Properties for settings access
    public List<Vector3> FormationSlots => formationSlots;
    public bool HasBeenInitialized => hasBeenInitialized;

    // Gizmo drawing (delegated to visualizer)
    void OnDrawGizmos()
    {
        if (visualizer != null)
            visualizer.OnDrawGizmos();
    }

    // Regenerate formation when values change in inspector
    void OnValidate()
    {
        if (Application.isPlaying && hasBeenInitialized)
        {
            GenerateFormation();
        }
        else if (!Application.isPlaying)
        {
            // In edit mode, generate for preview
            if (generator == null) InitializeComponents();
            GenerateFormation();
        }
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
        
        if (formationCreator.useRandomPlacement)
        {
            if (GUILayout.Button("Randomize All Positions", GUILayout.Height(25)))
            {
                formationCreator.RandomizeAllPositions();
            }
        }
        
        // Show validation status if validator exists
        if (formationCreator.Validator != null && formationCreator.Validator.ValidateFormationChanges && formationCreator.useRandomPlacement)
        {
            GUILayout.Space(5);
            if (formationCreator.Validator.LastValidationPassed)
            {
                EditorGUILayout.HelpBox("✓ Formation validation: PASSED - All formations fit properly", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"⚠ Formation validation: {formationCreator.Validator.InvalidFormationCount} formations repositioned due to conflicts", MessageType.Warning);
            }
        }
        
        // Show collider info
        GUILayout.Space(5);
        if (formationCreator.GetComponent<BoxCollider2D>() == null)
        {
            EditorGUILayout.HelpBox("No BoxCollider2D found! Add a BoxCollider2D to constrain formations within bounds.", MessageType.Warning);
        }
        else if (formationCreator.BoundaryManager != null && formationCreator.BoundaryManager.FitWithinCollider)
        {
            EditorGUILayout.HelpBox("Formation will be automatically scaled to fit within the BoxCollider2D bounds.", MessageType.Info);
        }
        
        // Show usage info
        if (formationCreator.useRandomPlacement)
        {
            string sizeInfo = GetFormationSizeInfo(formationCreator);
            EditorGUILayout.HelpBox($"Random placement: {formationCreator.formationCount} {formationCreator.currentFormation} formations {sizeInfo}.\n\n• Tab: Change formation shape\n• R: Randomize all positions", MessageType.None);
        }
    }

    private string GetFormationSizeInfo(FormationCreator fc)
    {
        switch (fc.currentFormation)
        {
            case FormationCreator.FormationType.Square:
            case FormationCreator.FormationType.Triangle:
            case FormationCreator.FormationType.VShape:
                return $"(Size: {fc.slotsPerSide} slots, Spacing: {fc.spacing})";
            case FormationCreator.FormationType.Circle:
                return $"(Radius: {fc.circleRadius})";
            default:
                return "";
        }
    }
}
#endif