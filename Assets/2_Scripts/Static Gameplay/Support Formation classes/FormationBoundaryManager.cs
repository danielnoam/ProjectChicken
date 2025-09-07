using System;
using KBCore.Refs;
using UnityEngine;

public class FormationBoundaryManager : MonoBehaviour
{
    [Header("Boundary Settings")]
    public bool fitWithinCollider = true;
    [Range(0.1f, 1f)] public float boundaryPadding = 0.9f; // How much of the collider to use (90% by default)
    
    [Header("References")]
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Self(Flag.EditableAnywhere)] private BoxCollider2D boxCollider2D;

    private FormationCreator _creator;
    private float _effectiveSpacing;
    private float _effectiveRadius;
    private float _effectiveFormationSpacing;
    
    
    public bool FitWithinCollider => fitWithinCollider;
    public float BoundaryPadding => boundaryPadding;
    public BoxCollider2D BoxCollider2D => boxCollider2D;
    public Vector2 ColliderSize => boxCollider2D != null ? boxCollider2D.size * boundaryPadding : Vector2.zero;
    public Vector2 ColliderOffset => boxCollider2D != null ? boxCollider2D.offset : Vector2.zero;
    

    private void OnValidate()
    {
        if (!levelManager) levelManager = FindFirstObjectByType<LevelManager>();
        
        UpdateBoundary();
        
        this.ValidateRefs();
    }

    public void Initialize(FormationCreator formationCreator)
    {
        _creator = formationCreator;
        CalculateEffectiveValues();
    }
    
    public void UpdateBoundary()
    {
        if (!levelManager) return;
        
        boxCollider2D.size = levelManager.EnemyBoundarySize * 2;
        boxCollider2D.offset = levelManager.EnemyPosition;
        CalculateEffectiveValues();
    }

    // Calculate scaling factors to fit within collider bounds
    public void CalculateEffectiveValues()
    {
        if (_creator == null) return;
        
        if (!fitWithinCollider || boxCollider2D == null)
        {
            _effectiveSpacing = _creator.spacing;
            _effectiveRadius = _creator.circleRadius;
            _effectiveFormationSpacing = _creator.formationSpacing;
            return;
        }

        Vector2 colliderSize = boxCollider2D.size * boundaryPadding;
        
        if (_creator.useRandomPlacement)
        {
            // For random placement, scale based on single formation size
            Vector2 singleFormationBounds = _creator.Generator.CalculateFormationBounds(_creator.currentFormation);
            
            // Calculate scaling factors for X and Y
            float scaleX = singleFormationBounds.x > 0 ? (colliderSize.x / singleFormationBounds.x) : 1f;
            float scaleY = singleFormationBounds.y > 0 ? (colliderSize.y / singleFormationBounds.y) : 1f;
            
            // Use the smaller scale to ensure it fits in both dimensions
            float uniformScale = Mathf.Min(scaleX, scaleY, 1f); // Don't scale up, only down
            
            _effectiveSpacing = _creator.spacing * uniformScale;
            _effectiveRadius = _creator.circleRadius * uniformScale;
            _effectiveFormationSpacing = _creator.formationSpacing * uniformScale;
        }
        else
        {
            // For side-by-side placement, use original logic
            int actualCount = _creator.formationCount;

            // Calculate the theoretical size of the formation without scaling
            Vector2 formationBoundsCalc = _creator.Generator.CalculateFormationBounds(_creator.currentFormation);
            
            // For multiple formations, account for spacing between them
            if (actualCount > 1)
            {
                float totalWidth = formationBoundsCalc.x * actualCount + _creator.formationSpacing * (actualCount - 1);
                formationBoundsCalc.x = totalWidth;
            }

            // Calculate scaling factors for X and Y
            float scaleX = formationBoundsCalc.x > 0 ? (colliderSize.x / formationBoundsCalc.x) : 1f;
            float scaleY = formationBoundsCalc.y > 0 ? (colliderSize.y / formationBoundsCalc.y) : 1f;
            
            // Use the smaller scale to ensure it fits in both dimensions
            float uniformScale = Mathf.Min(scaleX, scaleY, 1f); // Don't scale up, only down
            
            _effectiveSpacing = _creator.spacing * uniformScale;
            _effectiveRadius = _creator.circleRadius * uniformScale;
            _effectiveFormationSpacing = _creator.formationSpacing * uniformScale;
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
            return _effectiveSpacing; 
        } 
    }
    
    public float EffectiveRadius 
    { 
        get 
        { 
            CalculateEffectiveValues(); // Recalculate in case values changed
            return _effectiveRadius; 
        } 
    }
    
    public float EffectiveFormationSpacing 
    { 
        get 
        { 
            CalculateEffectiveValues(); // Recalculate in case values changed
            return _effectiveFormationSpacing; 
        } 
    }


}