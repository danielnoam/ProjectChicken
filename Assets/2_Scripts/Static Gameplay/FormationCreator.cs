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
    
    [Header("Formation Change Validation")]
    public bool validateFormationChanges = true; // Validate that formation changes don't cause boundary/collision issues
    public bool autoAdjustOnValidationFail = true; // Try to adjust formation size if validation fails
    
    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.red;
    public float gizmoSize = 0.5f;
    public bool showColliderBounds = true;
    public Color validationErrorColor = Color.magenta;

    private List<Vector3> formationSlots = new List<Vector3>();
    private List<Vector2> formationCenters = new List<Vector2>(); // Centers of placed formations
    private List<Vector2> formationBounds = new List<Vector2>(); // Bounds of placed formations
    private BoxCollider2D boxCollider2D;
    private float effectiveSpacing;
    private float effectiveRadius;
    private float effectiveFormationSpacing;
    
    // Formation change tracking
    private FormationType previousFormationType;
    private bool hasBeenInitialized = false;
    
    // Validation tracking
    private List<int> invalidFormationIndices = new List<int>(); // Track which formations have validation issues
    private bool lastValidationPassed = true;

    void Start()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        if (boxCollider2D == null)
        {
            Debug.LogWarning("FormationCreator: No BoxCollider2D found on " + gameObject.name + ". Formations will not be constrained.");
        }
        
        // Store initial formation type
        previousFormationType = currentFormation;
        
        // Initial generation with random placement
        GenerateFormation();
        RandomizeAllPositions(); // Always randomize on initial creation
        hasBeenInitialized = true;
    }

    void Update()
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
        
        // Check for formation type changes (without auto-repositioning)
        if (hasBeenInitialized && currentFormation != previousFormationType)
        {
            // Formation type changed - validate and update shapes only
            HandleFormationTypeChange();
            previousFormationType = currentFormation;
        }
    }

    // Handle formation type changes with validation
    void HandleFormationTypeChange()
    {
        Debug.Log($"FormationCreator: Formation type changed from {previousFormationType} to {currentFormation}. Validating new formation...");
        
        if (validateFormationChanges && useRandomPlacement)
        {
            // Validate the new formation type at existing positions
            bool validationPassed = ValidateFormationChangeAtExistingPositions();
            
            if (validationPassed)
            {
                Debug.Log("FormationCreator: Validation passed. Updating formation shapes at existing positions.");
                GenerateFormation(); // Update formation shapes at existing positions
                lastValidationPassed = true;
                invalidFormationIndices.Clear();
            }
            else
            {
                Debug.LogWarning($"FormationCreator: {invalidFormationIndices.Count} formations have conflicts with new formation type. Randomizing conflicting formations...");
                
                // Try auto-adjustment first if enabled
                if (autoAdjustOnValidationFail && TryAutoAdjustFormation())
                {
                    Debug.Log("FormationCreator: Auto-adjustment successful. Applying new formation.");
                    GenerateFormation();
                    lastValidationPassed = true;
                    invalidFormationIndices.Clear();
                }
                else
                {
                    // Randomize only the conflicting formations
                    RandomizeConflictingFormations();
                }
            }
        }
        else
        {
            // No validation or not using random placement - just update shapes
            GenerateFormation();
            lastValidationPassed = true;
            invalidFormationIndices.Clear();
        }
    }

    // Validate if the new formation type would work at existing positions
    bool ValidateFormationChangeAtExistingPositions()
    {
        if (!useRandomPlacement || formationCenters.Count == 0 || boxCollider2D == null)
        {
            return true; // No validation needed
        }

        // Calculate what the new formation bounds would be
        Vector2 newFormationBounds = GetSingleFormationBounds();
        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        Vector2 colliderOffset = boxCollider2D.offset;
        
        invalidFormationIndices.Clear();
        bool allValid = true;
        
        for (int i = 0; i < formationCenters.Count && i < formationCount; i++)
        {
            Vector2 center = formationCenters[i];
            bool isValid = true;
            
            // Check boundary constraints
            Vector2 halfFormationSize = newFormationBounds * 0.5f;
            Vector2 minPos = center - halfFormationSize;
            Vector2 maxPos = center + halfFormationSize;
            Vector2 colliderMin = colliderOffset - colliderSize * 0.5f;
            Vector2 colliderMax = colliderOffset + colliderSize * 0.5f;
            
            if (minPos.x < colliderMin.x || minPos.y < colliderMin.y || 
                maxPos.x > colliderMax.x || maxPos.y > colliderMax.y)
            {
                isValid = false;
                Debug.LogWarning($"FormationCreator: Formation {i} would exceed boundaries with new formation type");
            }
            
            // Check collision with other formations
            if (isValid)
            {
                for (int j = 0; j < formationCenters.Count && j < formationCount; j++)
                {
                    if (i != j)
                    {
                        if (DoFormationsOverlap(center, newFormationBounds, formationCenters[j], newFormationBounds))
                        {
                            isValid = false;
                            Debug.LogWarning($"FormationCreator: Formation {i} would overlap with formation {j} with new formation type");
                            break;
                        }
                    }
                }
            }
            
            if (!isValid)
            {
                invalidFormationIndices.Add(i);
                allValid = false;
            }
        }
        
        lastValidationPassed = allValid;
        return allValid;
    }

    // Randomize only the formations that have validation conflicts
    void RandomizeConflictingFormations()
    {
        if (invalidFormationIndices.Count == 0 || boxCollider2D == null)
        {
            Debug.Log("FormationCreator: No conflicting formations to randomize.");
            GenerateFormation(); // Just update shapes
            return;
        }

        Debug.Log($"FormationCreator: Randomizing {invalidFormationIndices.Count} conflicting formations while keeping {formationCenters.Count - invalidFormationIndices.Count} valid ones in place.");

        Vector2 singleFormationBounds = GetSingleFormationBounds();
        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        Vector2 colliderOffset = boxCollider2D.offset;
        
        // Calculate available area for placement
        Vector2 halfFormationSize = singleFormationBounds * 0.5f;
        Vector2 availableMin = colliderOffset - colliderSize * 0.5f + halfFormationSize;
        Vector2 availableMax = colliderOffset + colliderSize * 0.5f - halfFormationSize;
        
        if (availableMin.x >= availableMax.x || availableMin.y >= availableMax.y)
        {
            Debug.LogWarning("FormationCreator: No space available for repositioning conflicting formations!");
            GenerateFormation(); // Generate with existing positions (will show validation errors)
            return;
        }

        // Create a list of valid formation positions (for collision checking)
        List<Vector2> validPositions = new List<Vector2>();
        List<Vector2> validBounds = new List<Vector2>();
        
        for (int i = 0; i < formationCenters.Count; i++)
        {
            if (!invalidFormationIndices.Contains(i))
            {
                validPositions.Add(formationCenters[i]);
                validBounds.Add(singleFormationBounds); // All formations now have the same bounds (new formation type)
            }
        }

        // Try to find new positions for each conflicting formation
        int successfulRepositions = 0;
        foreach (int invalidIndex in invalidFormationIndices)
        {
            if (invalidIndex >= formationCenters.Count)
                continue;

            bool foundPosition = false;
            int attempts = 0;
            
            while (!foundPosition && attempts < maxPlacementAttempts)
            {
                attempts++;
                
                Vector2 randomCenter = new Vector2(
                    Random.Range(availableMin.x, availableMax.x),
                    Random.Range(availableMin.y, availableMax.y)
                );
                
                // Check if this position is valid (doesn't overlap with valid formations or other repositioned formations)
                bool isValidPosition = true;
                
                // Check against valid formations
                for (int i = 0; i < validPositions.Count; i++)
                {
                    if (DoFormationsOverlap(randomCenter, singleFormationBounds, validPositions[i], validBounds[i]))
                    {
                        isValidPosition = false;
                        break;
                    }
                }
                
                if (isValidPosition)
                {
                    // Update the formation center to the new position
                    formationCenters[invalidIndex] = randomCenter;
                    formationBounds[invalidIndex] = singleFormationBounds;
                    
                    // Add this new position to valid positions for checking against remaining formations
                    validPositions.Add(randomCenter);
                    validBounds.Add(singleFormationBounds);
                    
                    foundPosition = true;
                    successfulRepositions++;
                    Debug.Log($"FormationCreator: Successfully repositioned formation {invalidIndex} after {attempts} attempts");
                }
            }
            
            if (!foundPosition)
            {
                Debug.LogWarning($"FormationCreator: Failed to find new position for formation {invalidIndex} after {maxPlacementAttempts} attempts. Keeping original position (may still have conflicts).");
            }
        }
        
        Debug.Log($"FormationCreator: Successfully repositioned {successfulRepositions} out of {invalidFormationIndices.Count} conflicting formations.");
        
        // Now generate the formation with the updated positions
        GenerateFormation();
        
        // Re-validate to update the invalid indices list
        ValidateFormationChangeAtExistingPositions();
        
        if (invalidFormationIndices.Count == 0)
        {
            Debug.Log("FormationCreator: All conflicts resolved!");
            lastValidationPassed = true;
        }
        else
        {
            Debug.LogWarning($"FormationCreator: {invalidFormationIndices.Count} formations still have conflicts after repositioning.");
            lastValidationPassed = false;
        }
    }
    private bool TryAutoAdjustFormation()
    {
        if (!useRandomPlacement || boxCollider2D == null)
            return false;
            
        // Try reducing formation size parameters
        float originalSpacing = spacing;
        float originalRadius = circleRadius;
        int originalSlotsPerSide = slotsPerSide;
        
        // Try reducing by 10% steps up to 50%
        for (float reduction = 0.1f; reduction <= 0.5f; reduction += 0.1f)
        {
            spacing = originalSpacing * (1f - reduction);
            circleRadius = originalRadius * (1f - reduction);
            slotsPerSide = Mathf.Max(2, Mathf.RoundToInt(originalSlotsPerSide * (1f - reduction * 0.5f))); // Reduce slots more gradually
            
            if (ValidateFormationChangeAtExistingPositions())
            {
                Debug.Log($"FormationCreator: Auto-adjustment successful with {reduction * 100}% size reduction");
                return true;
            }
        }
        
        // Restoration if adjustment failed
        spacing = originalSpacing;
        circleRadius = originalRadius;
        slotsPerSide = originalSlotsPerSide;
        
        return false;
    }

    // Button method to cycle formations (can be called from Inspector button)
    [ContextMenu("Cycle Formation")]
    public void CycleFormation()
    {
        int currentIndex = (int)currentFormation;
        currentIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(FormationType)).Length;
        currentFormation = (FormationType)currentIndex;
        // Let Update() handle the validation and formation change
    }

    // NEW: Public method to randomize all formation positions
    [ContextMenu("Randomize All Positions")]
    public void RandomizeAllPositions()
    {
        if (!useRandomPlacement)
        {
            Debug.Log("FormationCreator: Random placement is disabled, cannot randomize positions");
            return;
        }
        
        Debug.Log("FormationCreator: Randomizing all formation positions...");
        
        // Clear existing positions and regenerate
        formationCenters.Clear();
        formationBounds.Clear();
        invalidFormationIndices.Clear();
        
        GenerateFormation(); // This will call PlaceFormationsRandomly since centers are cleared
        lastValidationPassed = true;
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
        
        if (useRandomPlacement)
        {
            // For random placement, scale based on single formation size
            Vector2 singleFormationBounds = CalculateFormationBounds();
            
            // Calculate scaling factors for X and Y
            float scaleX = singleFormationBounds.x > 0 ? (colliderSize.x / singleFormationBounds.x) : 1f;
            float scaleY = singleFormationBounds.y > 0 ? (colliderSize.y / singleFormationBounds.y) : 1f;
            
            // Use the smaller scale to ensure it fits in both dimensions
            float uniformScale = Mathf.Min(scaleX, scaleY, 1f); // Don't scale up, only down
            
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
        // Always clear and regenerate the basic formation structure
        formationSlots.Clear();
        
        // Calculate effective values based on collider constraints
        CalculateEffectiveValues();

        // Generate the base formation
        List<Vector3> baseFormation = new List<Vector3>();
        
        switch (currentFormation)
        {
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
            // If we have existing positions, use them; otherwise generate new random positions
            if (formationCenters.Count > 0)
            {
                PlaceFormationsAtExistingPositions(baseFormation);
            }
            else
            {
                PlaceFormationsRandomly(baseFormation);
            }
        }
        else
        {
            PlaceFormationsSideBySide(baseFormation);
        }
    }

    // Place formations at existing random positions (preserves random placement)
    void PlaceFormationsAtExistingPositions(List<Vector3> baseFormation)
    {
        // Update bounds for current formation type
        formationBounds.Clear();
        Vector2 singleFormationBounds = GetSingleFormationBounds();
        
        int placedCount = 0;
        foreach (Vector2 existingCenter in formationCenters)
        {
            if (placedCount >= formationCount)
                break;
                
            // Place formation at existing position
            foreach (Vector3 slot in baseFormation)
            {
                Vector3 worldSlot = new Vector3(slot.x + existingCenter.x, slot.y + existingCenter.y, slot.z);
                formationSlots.Add(worldSlot);
            }
            
            // Update bounds for this formation
            formationBounds.Add(singleFormationBounds);
            placedCount++;
        }
        
        // If we need more formations than we have existing positions, generate new ones
        if (placedCount < formationCount)
        {
            Debug.Log($"FormationCreator: Need {formationCount} formations but only have {placedCount} existing positions. Generating new random positions for the remaining formations.");
            
            // Generate additional random positions for remaining formations
            List<Vector3> remainingFormations = new List<Vector3>(baseFormation);
            int remainingCount = formationCount - placedCount;
            
            // Add new random positions for remaining formations
            GenerateAdditionalRandomPositions(remainingFormations, remainingCount);
        }
        
        // Remove extra positions if we have more than needed
        while (formationCenters.Count > formationCount)
        {
            formationCenters.RemoveAt(formationCenters.Count - 1);
            if (formationBounds.Count > formationCount)
                formationBounds.RemoveAt(formationBounds.Count - 1);
        }
    }

    // Generate additional random positions for remaining formations
    void GenerateAdditionalRandomPositions(List<Vector3> baseFormation, int additionalCount)
    {
        if (boxCollider2D == null || additionalCount <= 0)
            return;

        Vector2 singleFormationBounds = GetSingleFormationBounds();
        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        Vector2 colliderOffset = boxCollider2D.offset;
        
        // Calculate available area for placement
        Vector2 halfFormationSize = singleFormationBounds * 0.5f;
        Vector2 availableMin = colliderOffset - colliderSize * 0.5f + halfFormationSize;
        Vector2 availableMax = colliderOffset + colliderSize * 0.5f - halfFormationSize;
        
        if (availableMin.x >= availableMax.x || availableMin.y >= availableMax.y)
        {
            Debug.LogWarning("FormationCreator: No space available for additional formations!");
            return;
        }

        int placedCount = 0;
        int attempts = 0;
        
        while (placedCount < additionalCount && attempts < maxPlacementAttempts)
        {
            attempts++;
            
            Vector2 randomCenter = new Vector2(
                Random.Range(availableMin.x, availableMax.x),
                Random.Range(availableMin.y, availableMax.y)
            );
            
            if (IsPositionValid(randomCenter, singleFormationBounds))
            {
                // Place formation at this position
                foreach (Vector3 slot in baseFormation)
                {
                    Vector3 worldSlot = new Vector3(slot.x + randomCenter.x, slot.y + randomCenter.y, slot.z);
                    formationSlots.Add(worldSlot);
                }
                
                formationCenters.Add(randomCenter);
                formationBounds.Add(singleFormationBounds);
                placedCount++;
            }
        }
        
        if (placedCount < additionalCount)
        {
            Debug.LogWarning($"FormationCreator: Could only place {placedCount} out of {additionalCount} additional formations after {maxPlacementAttempts} attempts.");
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
            formationCenters.Add(Vector2.zero);
            formationBounds.Add(GetSingleFormationBounds());
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
            for (int i = 0; i < formationCenters.Count; i++)
            {
                // Use different color for invalid formations
                if (invalidFormationIndices.Contains(i))
                {
                    Gizmos.color = validationErrorColor;
                }
                else
                {
                    Gizmos.color = Color.yellow;
                }
                
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
        
        // Draw formation name and validation status
        #if UNITY_EDITOR
        string statusText = currentFormation.ToString();
        if (validateFormationChanges && invalidFormationIndices.Count > 0)
        {
            statusText += $" [{invalidFormationIndices.Count} REPOSITIONED]";
        }
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, statusText);
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
            
        // Only regenerate if we're in play mode and initialized
        if (Application.isPlaying && hasBeenInitialized)
        {
            // For parameter changes, don't regenerate random positions
            GenerateFormation();
        }
        else if (!Application.isPlaying)
        {
            // In edit mode, always regenerate for preview
            GenerateFormation();
        }
    }

    // Public properties for external access
    public bool LastValidationPassed => lastValidationPassed;
    public int InvalidFormationCount => invalidFormationIndices.Count;
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
        
        // Add button to randomize positions
        if (formationCreator.useRandomPlacement)
        {
            if (GUILayout.Button("Randomize All Positions", GUILayout.Height(25)))
            {
                formationCreator.RandomizeAllPositions();
            }
        }
        
        // Show validation status
        if (formationCreator.validateFormationChanges && formationCreator.useRandomPlacement)
        {
            GUILayout.Space(5);
            if (formationCreator.LastValidationPassed)
            {
                EditorGUILayout.HelpBox("✓ Formation validation: PASSED - All formations fit properly", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"⚠ Formation validation: {formationCreator.InvalidFormationCount} formations repositioned due to conflicts", MessageType.Warning);
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
        
        // Show info about the new behavior
        if (formationCreator.useRandomPlacement)
        {
            string sizeInfo = "";
            switch (formationCreator.currentFormation)
            {
                case FormationCreator.FormationType.Square:
                case FormationCreator.FormationType.Triangle:
                case FormationCreator.FormationType.VShape:
                    sizeInfo = $"(Size: {formationCreator.slotsPerSide} slots, Spacing: {formationCreator.spacing})";
                    break;
                case FormationCreator.FormationType.Circle:
                    sizeInfo = $"(Radius: {formationCreator.circleRadius})";
                    break;
            }
            
            EditorGUILayout.HelpBox($"Random placement: {formationCreator.formationCount} {formationCreator.currentFormation} formations {sizeInfo}.\n\n• Tab: Change formation shape (validates & repositions conflicts)\n• R: Randomize all positions\n• Randomize button: Randomize all positions\n\nValidation: {(formationCreator.validateFormationChanges ? "Enabled - conflicts auto-repositioned" : "Disabled")}\nAuto-adjust: {(formationCreator.autoAdjustOnValidationFail ? "Enabled" : "Disabled")}", MessageType.None);
        }
        else if (formationCreator.formationCount > 1)
        {
            EditorGUILayout.HelpBox($"Side-by-side placement: {formationCreator.formationCount} {formationCreator.currentFormation} formations.\n\nControls:\n• Tab: Cycle formations", MessageType.Info);
        }
    }
}
#endif