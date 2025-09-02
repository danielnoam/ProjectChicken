using UnityEngine;
using System.Collections.Generic;

public class FormationPlacer : MonoBehaviour
{
    private FormationCreator creator;
    private FormationBoundaryManager boundaryManager;
    private FormationValidator validator;

    // Formation placement state
    private List<Vector2> formationCenters = new List<Vector2>();
    private List<Vector2> formationBounds = new List<Vector2>();

    public void Initialize(FormationCreator formationCreator, FormationBoundaryManager boundary, FormationValidator val)
    {
        creator = formationCreator;
        boundaryManager = boundary;
        validator = val;
    }

    public List<Vector3> PlaceFormations(List<Vector3> baseFormation)
    {
        List<Vector3> allFormationSlots = new List<Vector3>();

        if (creator.useRandomPlacement)
        {
            // If we have existing positions, use them; otherwise generate new random positions
            if (formationCenters.Count > 0)
            {
                allFormationSlots = PlaceFormationsAtExistingPositions(baseFormation);
            }
            else
            {
                allFormationSlots = PlaceFormationsRandomly(baseFormation);
            }
        }
        else
        {
            allFormationSlots = PlaceFormationsSideBySide(baseFormation);
        }

        return allFormationSlots;
    }

    // Place formations at existing random positions (preserves random placement)
    List<Vector3> PlaceFormationsAtExistingPositions(List<Vector3> baseFormation)
    {
        List<Vector3> allSlots = new List<Vector3>();
        
        // Update bounds for current formation type
        formationBounds.Clear();
        Vector2 singleFormationBounds = creator.Generator.GetSingleFormationBounds(creator.currentFormation);
        
        int placedCount = 0;
        foreach (Vector2 existingCenter in formationCenters)
        {
            if (placedCount >= creator.formationCount)
                break;
                
            // Place formation at existing position
            foreach (Vector3 slot in baseFormation)
            {
                Vector3 worldSlot = new Vector3(slot.x + existingCenter.x, slot.y + existingCenter.y, slot.z);
                allSlots.Add(worldSlot);
            }
            
            // Update bounds for this formation
            formationBounds.Add(singleFormationBounds);
            placedCount++;
        }
        
        // If we need more formations than we have existing positions, generate new ones
        if (placedCount < creator.formationCount)
        {
            Debug.Log($"FormationPlacer: Need {creator.formationCount} formations but only have {placedCount} existing positions. Generating new random positions for the remaining formations.");
            
            int remainingCount = creator.formationCount - placedCount;
            List<Vector3> additionalSlots = GenerateAdditionalRandomPositions(baseFormation, remainingCount);
            allSlots.AddRange(additionalSlots);
        }
        
        // Remove extra positions if we have more than needed
        while (formationCenters.Count > creator.formationCount)
        {
            formationCenters.RemoveAt(formationCenters.Count - 1);
            if (formationBounds.Count > creator.formationCount)
                formationBounds.RemoveAt(formationBounds.Count - 1);
        }

        return allSlots;
    }

    // Generate additional random positions for remaining formations
    List<Vector3> GenerateAdditionalRandomPositions(List<Vector3> baseFormation, int additionalCount)
    {
        List<Vector3> additionalSlots = new List<Vector3>();
        
        BoxCollider2D boxCollider2D = creator.GetComponent<BoxCollider2D>();
        if (boxCollider2D == null || additionalCount <= 0)
            return additionalSlots;

        Vector2 singleFormationBounds = creator.Generator.GetSingleFormationBounds(creator.currentFormation);
        Vector2 colliderSize = boxCollider2D.size * boundaryManager.BoundaryPadding;
        Vector2 colliderOffset = boxCollider2D.offset;
        
        // Calculate available area for placement
        Vector2 halfFormationSize = singleFormationBounds * 0.5f;
        Vector2 availableMin = colliderOffset - colliderSize * 0.5f + halfFormationSize;
        Vector2 availableMax = colliderOffset + colliderSize * 0.5f - halfFormationSize;
        
        if (availableMin.x >= availableMax.x || availableMin.y >= availableMax.y)
        {
            Debug.LogWarning("FormationPlacer: No space available for additional formations!");
            return additionalSlots;
        }

        int placedCount = 0;
        int attempts = 0;
        
        while (placedCount < additionalCount && attempts < creator.maxPlacementAttempts)
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
                    additionalSlots.Add(worldSlot);
                }
                
                formationCenters.Add(randomCenter);
                formationBounds.Add(singleFormationBounds);
                placedCount++;
            }
        }
        
        if (placedCount < additionalCount)
        {
            Debug.LogWarning($"FormationPlacer: Could only place {placedCount} out of {additionalCount} additional formations after {creator.maxPlacementAttempts} attempts.");
        }

        return additionalSlots;
    }

    // Place formations randomly without overlapping
    List<Vector3> PlaceFormationsRandomly(List<Vector3> baseFormation)
    {
        List<Vector3> allSlots = new List<Vector3>();
        
        BoxCollider2D boxCollider2D = creator.GetComponent<BoxCollider2D>();
        if (boxCollider2D == null)
        {
            // If no collider, just place at origin
            foreach (Vector3 slot in baseFormation)
            {
                allSlots.Add(slot);
            }
            formationCenters.Add(Vector2.zero);
            formationBounds.Add(creator.Generator.GetSingleFormationBounds(creator.currentFormation));
            return allSlots;
        }

        Vector2 singleFormationBounds = creator.Generator.GetSingleFormationBounds(creator.currentFormation);
        Vector2 colliderSize = boxCollider2D.size * boundaryManager.BoundaryPadding;
        Vector2 colliderOffset = boxCollider2D.offset;
        
        // Calculate available area for placement
        Vector2 halfFormationSize = singleFormationBounds * 0.5f;
        Vector2 availableMin = colliderOffset - colliderSize * 0.5f + halfFormationSize;
        Vector2 availableMax = colliderOffset + colliderSize * 0.5f - halfFormationSize;
        
        // Check if even one formation can fit
        if (availableMin.x >= availableMax.x || availableMin.y >= availableMax.y)
        {
            Debug.LogWarning("FormationPlacer: Formation too large to fit within collider bounds!");
            return allSlots;
        }

        int placedCount = 0;
        int attempts = 0;
        
        while (placedCount < creator.formationCount && attempts < creator.maxPlacementAttempts)
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
                    allSlots.Add(worldSlot);
                }
                
                // Track this formation's position and bounds
                formationCenters.Add(randomCenter);
                formationBounds.Add(singleFormationBounds);
                placedCount++;
            }
        }
        
        if (placedCount < creator.formationCount)
        {
            Debug.LogWarning($"FormationPlacer: Could only place {placedCount} out of {creator.formationCount} formations after {creator.maxPlacementAttempts} attempts. Try reducing formation count or increasing collider size.");
        }

        return allSlots;
    }

    // Place formations side by side (original behavior)
    List<Vector3> PlaceFormationsSideBySide(List<Vector3> baseFormation)
    {
        List<Vector3> allSlots = new List<Vector3>();
        int actualCount = creator.formationCount;

        // Position multiple formations side by side
        for (int i = 0; i < actualCount; i++)
        {
            float xOffset = 0f;
            if (actualCount > 1)
            {
                // Center the formations around the origin
                xOffset = (i - (actualCount - 1) * 0.5f) * boundaryManager.EffectiveFormationSpacing;
            }

            foreach (Vector3 slot in baseFormation)
            {
                allSlots.Add(new Vector3(slot.x + xOffset, slot.y, slot.z));
            }
        }

        return allSlots;
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
        Vector2 expandedBounds1 = bounds1 + Vector2.one * boundaryManager.EffectiveFormationSpacing;
        Vector2 expandedBounds2 = bounds2 + Vector2.one * boundaryManager.EffectiveFormationSpacing;
        
        // Calculate distance between centers
        Vector2 distance = new Vector2(Mathf.Abs(center1.x - center2.x), Mathf.Abs(center1.y - center2.y));
        
        // Check if they overlap
        Vector2 minDistance = (expandedBounds1 + expandedBounds2) * 0.5f;
        
        return distance.x < minDistance.x && distance.y < minDistance.y;
    }

    // Public method to randomize all formation positions
    public void RandomizeAllPositions()
    {        
        // Clear existing positions
        formationCenters.Clear();
        formationBounds.Clear();
    }

    // Properties for external access
    public List<Vector2> FormationCenters => formationCenters;
    public List<Vector2> FormationBounds => formationBounds;
    
    // Methods for validator access
    public void UpdateFormationCenter(int index, Vector2 newCenter)
    {
        if (index >= 0 && index < formationCenters.Count)
        {
            formationCenters[index] = newCenter;
        }
    }

    public void UpdateFormationBounds(int index, Vector2 newBounds)
    {
        if (index >= 0 && index < formationBounds.Count)
        {
            formationBounds[index] = newBounds;
        }
    }
}