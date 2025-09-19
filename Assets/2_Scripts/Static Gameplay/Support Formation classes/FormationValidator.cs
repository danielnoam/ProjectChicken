using UnityEngine;
using System.Collections.Generic;

public class FormationValidator : MonoBehaviour
{
    [Header("Formation Change Validation")]
    public bool validateFormationChanges = true; // Validate that formation changes don't cause boundary/collision issues
    public bool autoAdjustOnValidationFail = true; // Try to adjust formation size if validation fails

    private FormationCreator creator;
    private FormationBoundaryManager boundaryManager;
    private FormationPlacer placer;
    
    // Validation tracking
    private List<int> invalidFormationIndices = new List<int>();
    private bool lastValidationPassed = true;

    public void Initialize(FormationCreator formationCreator, FormationBoundaryManager boundary, FormationPlacer formationPlacer)
    {
        creator = formationCreator;
        boundaryManager = boundary;
        placer = formationPlacer;
    }

    public void HandleFormationTypeChange()
    {
        if (validateFormationChanges && creator.useRandomPlacement)
        {
            // Validate the new formation type at existing positions
            bool validationPassed = ValidateFormationChangeAtExistingPositions();
            
            if (validationPassed)
            {
                // Debug.Log("FormationValidator: Validation passed. Updating formation shapes at existing positions.");
                creator.GenerateFormation(); // Update formation shapes at existing positions
                lastValidationPassed = true;
                invalidFormationIndices.Clear();
            }
            else
            {
                // Debug.LogWarning($"FormationValidator: {invalidFormationIndices.Count} formations have conflicts with new formation type. Attempting resolution...");
                
                // Try auto-adjustment first if enabled
                if (autoAdjustOnValidationFail && TryAutoAdjustFormation())
                {
                    Debug.Log("FormationValidator: Auto-adjustment successful. Applying new formation.");
                    creator.GenerateFormation();
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
            creator.GenerateFormation();
            lastValidationPassed = true;
            invalidFormationIndices.Clear();
        }
    }

    // Validate if the new formation type would work at existing positions
    bool ValidateFormationChangeAtExistingPositions()
    {
        if (!creator.useRandomPlacement || placer.FormationCenters.Count == 0 || boundaryManager.BoxCollider2D == null)
        {
            return true; // No validation needed
        }

        // Calculate what the new formation bounds would be
        Vector2 newFormationBounds = creator.Generator.GetSingleFormationBounds(creator.currentFormation);
        
        invalidFormationIndices.Clear();
        bool allValid = true;
        
        for (int i = 0; i < placer.FormationCenters.Count && i < creator.formationCount; i++)
        {
            Vector2 center = placer.FormationCenters[i];
            bool isValid = true;
            
            // Check boundary constraints
            if (!boundaryManager.DoesFormationFitInBounds(center, newFormationBounds))
            {
                isValid = false;
                Debug.LogWarning($"FormationValidator: Formation {i} would exceed boundaries with new formation type");
            }
            
            // Check collision with other formations
            if (isValid)
            {
                for (int j = 0; j < placer.FormationCenters.Count && j < creator.formationCount; j++)
                {
                    if (i != j)
                    {
                        if (DoFormationsOverlap(center, newFormationBounds, placer.FormationCenters[j], newFormationBounds))
                        {
                            isValid = false;
                            Debug.LogWarning($"FormationValidator: Formation {i} would overlap with formation {j} with new formation type");
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

    // Check if two formations overlap (including spacing buffer)
    bool DoFormationsOverlap(Vector2 center1, Vector2 bounds1, Vector2 center2, Vector2 bounds2)
    {
        // Add formation spacing as buffer
        Vector2 expandedBounds1 = bounds1 + Vector2.one * boundaryManager.EffectiveFormationSpacing;
        Vector2 expandedBounds2 = bounds2 + Vector2.one * boundaryManager.EffectiveFormationSpacing;
        
        // Calculate distance between centers
        Vector2 distance = new Vector2(Mathf.Abs(center1.x - center2.x), Mathf.Abs(center1.y - center2.y));
        
        // Check if they overlap
        Vector2 minDistance = (expandedBounds1 + expandedBounds2) * 0.5f;
        
        return distance.x < minDistance.x && distance.y < minDistance.y;
    }

    // Randomize only the formations that have validation conflicts
    void RandomizeConflictingFormations()
    {
        if (invalidFormationIndices.Count == 0 || boundaryManager.BoxCollider2D == null)
        {
            Debug.Log("FormationValidator: No conflicting formations to randomize.");
            creator.GenerateFormation(); // Just update shapes
            return;
        }

        Debug.Log($"FormationValidator: Randomizing {invalidFormationIndices.Count} conflicting formations while keeping {placer.FormationCenters.Count - invalidFormationIndices.Count} valid ones in place.");

        Vector2 singleFormationBounds = creator.Generator.GetSingleFormationBounds(creator.currentFormation);
        boundaryManager.GetAvailableArea(singleFormationBounds, out Vector2 availableMin, out Vector2 availableMax);
        
        if (availableMin.x >= availableMax.x || availableMin.y >= availableMax.y)
        {
            Debug.LogWarning("FormationValidator: No space available for repositioning conflicting formations!");
            creator.GenerateFormation(); // Generate with existing positions (will show validation errors)
            return;
        }

        // Create a list of valid formation positions (for collision checking)
        List<Vector2> validPositions = new List<Vector2>();
        List<Vector2> validBounds = new List<Vector2>();
        
        for (int i = 0; i < placer.FormationCenters.Count; i++)
        {
            if (!invalidFormationIndices.Contains(i))
            {
                validPositions.Add(placer.FormationCenters[i]);
                validBounds.Add(singleFormationBounds); // All formations now have the same bounds (new formation type)
            }
        }

        // Try to find new positions for each conflicting formation
        int successfulRepositions = 0;
        foreach (int invalidIndex in invalidFormationIndices)
        {
            if (invalidIndex >= placer.FormationCenters.Count)
                continue;

            bool foundPosition = false;
            int attempts = 0;
            
            while (!foundPosition && attempts < creator.maxPlacementAttempts)
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
                    placer.UpdateFormationCenter(invalidIndex, randomCenter);
                    placer.UpdateFormationBounds(invalidIndex, singleFormationBounds);
                    
                    // Add this new position to valid positions for checking against remaining formations
                    validPositions.Add(randomCenter);
                    validBounds.Add(singleFormationBounds);
                    
                    foundPosition = true;
                    successfulRepositions++;
                    Debug.Log($"FormationValidator: Successfully repositioned formation {invalidIndex} after {attempts} attempts");
                }
            }
            
            if (!foundPosition)
            {
                Debug.LogWarning($"FormationValidator: Failed to find new position for formation {invalidIndex} after {creator.maxPlacementAttempts} attempts. Keeping original position (may still have conflicts).");
            }
        }
        
        Debug.Log($"FormationValidator: Successfully repositioned {successfulRepositions} out of {invalidFormationIndices.Count} conflicting formations.");
        
        // Now generate the formation with the updated positions
        creator.GenerateFormation();
        
        // Re-validate to update the invalid indices list
        ValidateFormationChangeAtExistingPositions();
        
        if (invalidFormationIndices.Count == 0)
        {
            Debug.Log("FormationValidator: All conflicts resolved!");
            lastValidationPassed = true;
        }
        else
        {
            Debug.LogWarning($"FormationValidator: {invalidFormationIndices.Count} formations still have conflicts after repositioning.");
            lastValidationPassed = false;
        }
    }

    bool TryAutoAdjustFormation()
    {
        if (!creator.useRandomPlacement || boundaryManager.BoxCollider2D == null)
            return false;
            
        // Store original formation parameters
        float originalSpacing = creator.spacing;
        float originalRadius = creator.circleRadius;
        int originalNumberOfSlots = creator.numberOfSlots;
        
        // Try reducing formation size parameters by 10% steps up to 50%
        for (float reduction = 0.1f; reduction <= 0.5f; reduction += 0.1f)
        {
            creator.spacing = originalSpacing * (1f - reduction);
            creator.circleRadius = originalRadius * (1f - reduction);
            // Reduce number of slots more gradually
            creator.numberOfSlots = Mathf.Max(1, Mathf.RoundToInt(originalNumberOfSlots * (1f - reduction * 0.5f)));
            
            // Recalculate formation parameters and boundary values
            boundaryManager.CalculateEffectiveValues();
            
            if (ValidateFormationChangeAtExistingPositions())
            {
                Debug.Log($"FormationValidator: Auto-adjustment successful with {reduction * 100}% size reduction (slots: {originalNumberOfSlots} → {creator.numberOfSlots})");
                return true;
            }
        }
        
        // Restore original values if adjustment failed
        creator.spacing = originalSpacing;
        creator.circleRadius = originalRadius;
        creator.numberOfSlots = originalNumberOfSlots;
        boundaryManager.CalculateEffectiveValues();
        
        return false;
    }

    // Properties for external access
    public bool ValidateFormationChanges => validateFormationChanges;
    public bool LastValidationPassed => lastValidationPassed;
    public int InvalidFormationCount => invalidFormationIndices.Count;
    public List<int> InvalidFormationIndices => new List<int>(invalidFormationIndices); // Return copy
}