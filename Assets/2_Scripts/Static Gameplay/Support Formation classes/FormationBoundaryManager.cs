using UnityEngine;

public class FormationBoundaryManager : MonoBehaviour
{
    [Header("Boundary Settings")]
    public bool fitWithinCollider = true;
    [Range(0.1f, 1f)]
    public float boundaryPadding = 0.9f; // How much of the collider to use (90% by default)

    private FormationCreator creator;
    private BoxCollider2D boxCollider2D;
    
    // Calculated effective values
    private float effectiveSpacing;
    private float effectiveRadius;
    private float effectiveFormationSpacing;

    public void Initialize(FormationCreator formationCreator)
    {
        creator = formationCreator;
        boxCollider2D = GetComponent<BoxCollider2D>();
        
        if (boxCollider2D == null)
        {
            Debug.LogWarning("FormationBoundaryManager: No BoxCollider2D found on " + gameObject.name + ". Formations will not be constrained.");
        }
        
        CalculateEffectiveValues();
    }

    // Calculate scaling factors to fit within collider bounds
    public void CalculateEffectiveValues()
    {
        if (!fitWithinCollider || boxCollider2D == null)
        {
            effectiveSpacing = creator.spacing;
            effectiveRadius = creator.circleRadius;
            effectiveFormationSpacing = creator.formationSpacing;
            return;
        }

        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        
        if (creator.useRandomPlacement)
        {
            // For random placement, scale based on single formation size
            Vector2 singleFormationBounds = creator.Generator.CalculateFormationBounds(creator.currentFormation);
            
            // Calculate scaling factors for X and Y
            float scaleX = singleFormationBounds.x > 0 ? (colliderSize.x / singleFormationBounds.x) : 1f;
            float scaleY = singleFormationBounds.y > 0 ? (colliderSize.y / singleFormationBounds.y) : 1f;
            
            // Use the smaller scale to ensure it fits in both dimensions
            float uniformScale = Mathf.Min(scaleX, scaleY, 1f); // Don't scale up, only down
            
            effectiveSpacing = creator.spacing * uniformScale;
            effectiveRadius = creator.circleRadius * uniformScale;
            effectiveFormationSpacing = creator.formationSpacing * uniformScale;
        }
        else
        {
            // For side-by-side placement, use original logic
            int actualCount = creator.formationCount;

            // Calculate the theoretical size of the formation without scaling
            Vector2 formationBoundsCalc = creator.Generator.CalculateFormationBounds(creator.currentFormation);
            
            // For multiple formations, account for spacing between them
            if (actualCount > 1)
            {
                float totalWidth = formationBoundsCalc.x * actualCount + creator.formationSpacing * (actualCount - 1);
                formationBoundsCalc.x = totalWidth;
            }

            // Calculate scaling factors for X and Y
            float scaleX = formationBoundsCalc.x > 0 ? (colliderSize.x / formationBoundsCalc.x) : 1f;
            float scaleY = formationBoundsCalc.y > 0 ? (colliderSize.y / formationBoundsCalc.y) : 1f;
            
            // Use the smaller scale to ensure it fits in both dimensions
            float uniformScale = Mathf.Min(scaleX, scaleY, 1f); // Don't scale up, only down
            
            effectiveSpacing = creator.spacing * uniformScale;
            effectiveRadius = creator.circleRadius * uniformScale;
            effectiveFormationSpacing = creator.formationSpacing * uniformScale;
        }
    }

    // Get available area for placement (used by validator and placer)
    public void GetAvailableArea(Vector2 formationBounds, out Vector2 availableMin, out Vector2 availableMax)
    {
        if (boxCollider2D == null)
        {
            availableMin = Vector2.zero;
            availableMax = Vector2.zero;
            return;
        }

        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        Vector2 colliderOffset = boxCollider2D.offset;
        
        // Calculate available area for placement
        Vector2 halfFormationSize = formationBounds * 0.5f;
        availableMin = colliderOffset - colliderSize * 0.5f + halfFormationSize;
        availableMax = colliderOffset + colliderSize * 0.5f - halfFormationSize;
    }

    // Check if formation fits within collider bounds
    public bool DoesFormationFitInBounds(Vector2 center, Vector2 bounds)
    {
        if (boxCollider2D == null)
            return true;

        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        Vector2 colliderOffset = boxCollider2D.offset;
        
        Vector2 halfFormationSize = bounds * 0.5f;
        Vector2 minPos = center - halfFormationSize;
        Vector2 maxPos = center + halfFormationSize;
        Vector2 colliderMin = colliderOffset - colliderSize * 0.5f;
        Vector2 colliderMax = colliderOffset + colliderSize * 0.5f;
        
        return !(minPos.x < colliderMin.x || minPos.y < colliderMin.y || 
                 maxPos.x > colliderMax.x || maxPos.y > colliderMax.y);
    }

    // Properties for external access
    public float EffectiveSpacing 
    { 
        get 
        { 
            CalculateEffectiveValues(); // Recalculate in case values changed
            return effectiveSpacing; 
        } 
    }
    
    public float EffectiveRadius 
    { 
        get 
        { 
            CalculateEffectiveValues(); // Recalculate in case values changed
            return effectiveRadius; 
        } 
    }
    
    public float EffectiveFormationSpacing 
    { 
        get 
        { 
            CalculateEffectiveValues(); // Recalculate in case values changed
            return effectiveFormationSpacing; 
        } 
    }

    public bool FitWithinCollider => fitWithinCollider;
    public float BoundaryPadding => boundaryPadding;
    public BoxCollider2D BoxCollider2D => boxCollider2D;
    public Vector2 ColliderSize => boxCollider2D != null ? boxCollider2D.size * boundaryPadding : Vector2.zero;
    public Vector2 ColliderOffset => boxCollider2D != null ? boxCollider2D.offset : Vector2.zero;
}