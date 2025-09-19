using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using KBCore.Refs;
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
    public int numberOfSlots = 9; // Total number of slots desired
    public float circleRadius = 3f;

    [Header("Multiple Formations")]
    [Range(1, 10)]
    public int formationCount = 1; // Number of formations (1-10)
    public float formationSpacing = 5f; // Minimum distance between formation bounding boxes

    [Header("Random Placement")]
    public bool useRandomPlacement = true; // If false, uses old side-by-side placement
    [Range(10, 1000)]
    public int maxPlacementAttempts = 100; // Maximum attempts to place formations without overlap

    [Header("References")]
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    
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

    // Calculated values (read-only, calculated from numberOfSlots)
    public int ActualSlotCount { get; private set; }
    public int CalculatedSideLength { get; private set; } // For square formations
    public int CalculatedRows { get; private set; } // For triangle formations
    public int CalculatedSlotsPerSide { get; private set; } // For V-shape formations
    
    void Awake()
    {
        previousFormationType = currentFormation;
        CalculateFormationParameters();
        InitializeComponents();
        StartCoroutine(InitializeFormationWithDelay());
    }

    void OnEnable()
    {
        // Subscribe to LevelManager events when this object becomes active
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from LevelManager events when this object becomes inactive
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }
    }

    // Event handler for when the level stage changes
    private void OnStageChanged(SOLevelStage newStage)
    {
        if (!newStage || newStage.StageType != StageType.EnemyWave) return;
        
        var formation = newStage.FormationStageData;
        if (formation != null)
        {
            currentFormation = formation.FormationType;
            formationCount = formation.FormationCount;
            numberOfSlots = formation.NumberOfSlots;
            spacing = formation.Spacing;
            circleRadius = formation.CircleRadius;
            useRandomPlacement = formation.UseRandomPlacement;
            formationSpacing = formation.FormationSpacing;
            maxPlacementAttempts = formation.MaxPlacementAttempts;
            CalculateFormationParameters();
            StartCoroutine(InitializeFormationWithDelay());
        }
    }

    // Calculate the actual formation parameters based on numberOfSlots
    void CalculateFormationParameters()
    {
        switch (currentFormation)
        {
            case FormationType.Square:
                CalculatedSideLength = Mathf.CeilToInt(Mathf.Sqrt(numberOfSlots));
                ActualSlotCount = CalculatedSideLength * CalculatedSideLength;
                break;
                
            case FormationType.Triangle:
                // Find smallest triangular number >= numberOfSlots
                // Triangular number formula: n(n+1)/2
                // Solve for n: n >= (-1 + sqrt(1 + 8*numberOfSlots))/2
                CalculatedRows = Mathf.CeilToInt((-1f + Mathf.Sqrt(1f + 8f * numberOfSlots)) / 2f);
                ActualSlotCount = CalculatedRows * (CalculatedRows + 1) / 2;
                break;
                
            case FormationType.Circle:
                // Circle can accommodate exact number of slots
                ActualSlotCount = numberOfSlots;
                break;
                
            case FormationType.VShape:
                // V-shape has 1 center + 2*slotsPerSide
                // So slotsPerSide = (numberOfSlots - 1) / 2, rounded up
                CalculatedSlotsPerSide = Mathf.CeilToInt((numberOfSlots - 1) / 2f);
                ActualSlotCount = 1 + 2 * CalculatedSlotsPerSide;
                break;
        }
    }

    // New coroutine to handle proper initialization timing
    IEnumerator InitializeFormationWithDelay()
    {
        // Wait one frame to ensure all components are fully initialized
        yield return null;

        // Generate initial formation
        GenerateFormation();

        // Apply random placement if enabled
        if (useRandomPlacement)
        {
            placer.RandomizeAllPositions();
            // Regenerate after randomization
            GenerateFormation();
        }

        // Force update all visualization components
        ForceUpdateVisualization();

        // Mark as initialized
        hasBeenInitialized = true;

        // Debug.Log($"FormationCreator: Successfully initialized with {currentFormation} formation ({ActualSlotCount} slots from requested {numberOfSlots})");
    }

    void Update()
    {
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

    void HandleFormationTypeChange()
    {
        // Debug.Log($"FormationCreator: Formation type changed from {previousFormationType} to {currentFormation}. Processing change...");

        // Recalculate parameters for new formation type
        CalculateFormationParameters();

        if (validator.ValidateFormationChanges && useRandomPlacement)
        {
            validator.HandleFormationTypeChange();
        }
        else
        {
            // No validation - just update shapes
            GenerateFormation();
        }

        // Force update visualization after formation type change
        ForceUpdateVisualization();
    }

    // New method to force update all visualization components
    void ForceUpdateVisualization()
    {
        // Force boundary manager to update
        if (boundaryManager != null)
        {
            // Try to call a refresh method if it exists
            var refreshMethod = boundaryManager.GetType().GetMethod("RefreshBoundaries");
            refreshMethod?.Invoke(boundaryManager, null);

            // Or call Update method if it exists
            var updateMethod = boundaryManager.GetType().GetMethod("UpdateBoundaries");
            updateMethod?.Invoke(boundaryManager, null);
        }

        // Force visualizer to update
        if (visualizer != null)
        {
            // Try to call a refresh method if it exists
            var refreshMethod = visualizer.GetType().GetMethod("RefreshVisualization");
            refreshMethod?.Invoke(visualizer, null);

            // Or call Update method if it exists
            var updateMethod = visualizer.GetType().GetMethod("UpdateVisualization");
            updateMethod?.Invoke(visualizer, null);
        }

        // Force a repaint in editor
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            SceneView.RepaintAll();
        }
#endif
    }

    // Main formation generation method
    public void GenerateFormation()
    {
        // Recalculate parameters in case numberOfSlots changed
        CalculateFormationParameters();
        
        // Clear existing slots
        formationSlots.Clear();

        // Generate base formation using generator
        List<Vector3> baseFormation = generator.GenerateFormation(currentFormation);

        // Place formations using placer
        formationSlots = placer.PlaceFormations(baseFormation);

        // After generation, ensure visualization is updated
        if (hasBeenInitialized)
        {
            ForceUpdateVisualization();
        }
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

    [ContextMenu("Force Update Visualization")]
    public void ForceUpdateVisualizationMenu()
    {
        ForceUpdateVisualization();
        Debug.Log("FormationCreator: Forced visualization update");
    }

    [ContextMenu("Reinitialize Formation")]
    public void ReinitializeFormation()
    {
        hasBeenInitialized = false;
        StartCoroutine(InitializeFormationWithDelay());
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
        // Ensure numberOfSlots is at least 1
        numberOfSlots = Mathf.Max(1, numberOfSlots);
        
        // Recalculate parameters when values change
        CalculateFormationParameters();
        
        if (Application.isPlaying && hasBeenInitialized)
        {
            GenerateFormation();
        }

        if (!levelManager) levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        
        this.ValidateRefs();
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

        // Show calculated values
        if (formationCreator.numberOfSlots > 0)
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Calculated Values", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Requested Slots: {formationCreator.numberOfSlots}");
            EditorGUILayout.LabelField($"Actual Slots: {formationCreator.ActualSlotCount}");
            
            switch (formationCreator.currentFormation)
            {
                case FormationCreator.FormationType.Square:
                    EditorGUILayout.LabelField($"Grid Size: {formationCreator.CalculatedSideLength}x{formationCreator.CalculatedSideLength}");
                    break;
                case FormationCreator.FormationType.Triangle:
                    EditorGUILayout.LabelField($"Rows: {formationCreator.CalculatedRows}");
                    break;
                case FormationCreator.FormationType.VShape:
                    EditorGUILayout.LabelField($"Slots Per Side: {formationCreator.CalculatedSlotsPerSide}");
                    break;
            }
        }

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

        // New button to force visualization update
        if (GUILayout.Button("Force Update Visualization", GUILayout.Height(25)))
        {
            formationCreator.ForceUpdateVisualizationMenu();
        }

        // Reinitialize button for debugging
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Reinitialize Formation", GUILayout.Height(25)))
            {
                formationCreator.ReinitializeFormation();
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

        // Show initialization status
        GUILayout.Space(5);
        if (formationCreator.HasBeenInitialized)
        {
            EditorGUILayout.HelpBox("✓ Formation properly initialized with boundary visualization", MessageType.Info);
        }
        else if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("⏳ Formation initializing...", MessageType.Warning);
        }

        // Show collider info
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
                return $"({fc.ActualSlotCount} slots in {fc.CalculatedSideLength}x{fc.CalculatedSideLength} grid, Spacing: {fc.spacing})";
            case FormationCreator.FormationType.Triangle:
                return $"({fc.ActualSlotCount} slots in {fc.CalculatedRows} rows, Spacing: {fc.spacing})";
            case FormationCreator.FormationType.VShape:
                return $"({fc.ActualSlotCount} slots with {fc.CalculatedSlotsPerSide} per side, Spacing: {fc.spacing})";
            case FormationCreator.FormationType.Circle:
                return $"({fc.ActualSlotCount} slots, Radius: {fc.circleRadius})";
            default:
                return "";
        }
    }
}
#endif