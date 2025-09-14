using UnityEngine;
using System.Collections;

public class BoxColliderCenterer : MonoBehaviour
{
    private BoxCollider boxCollider;
    private bool hasBeenCentered = false;
    private float targetZ;
    
    [Header("Scaling Compensation")]
    public float scalingDuration = 2f; // Should match itemScaleDuration from spawner
    public bool autoDetectScaleDuration = true; // Try to get duration from SpaceItemBehavior
    
    void Start()
    {
        // Small delay to ensure spawner has set position first
        Invoke(nameof(InitialCenter), 0.02f);
    }
    
    void OnEnable()
    {
        // Reset flags when object is re-enabled from pool
        hasBeenCentered = false;
        
        // Use a small delay to ensure the spawner has set the position first
        if (boxCollider != null)
        {
            Invoke(nameof(InitialCenter), 0.02f);
        }
    }
    
    private void InitialCenter()
    {
        // Prevent multiple centering calls
        if (hasBeenCentered) return;
        
        // Get the BoxCollider component
        boxCollider = GetComponent<BoxCollider>();
        
        if (boxCollider == null)
        {
            Debug.LogWarning("No BoxCollider found on " + gameObject.name);
            return;
        }
        
        // Get the Z position from current transform (set by spawner)
        targetZ = transform.position.z;
        
        // Try to auto-detect scale duration from SpaceItemBehavior
        if (autoDetectScaleDuration)
        {
            MonoBehaviour spaceItemBehavior = GetComponent<MonoBehaviour>();
            if (spaceItemBehavior != null)
            {
                var scaleDurationField = spaceItemBehavior.GetType().GetField("scaleDuration");
                if (scaleDurationField != null)
                {
                    scalingDuration = (float)scaleDurationField.GetValue(spaceItemBehavior);
                }
            }
        }
        
        // Initial centering
        CenterBoxColliderAtTarget();
        
        // Start monitoring position during scaling period
        StartCoroutine(MonitorPositionDuringScaling());
        
        hasBeenCentered = true;
    }
    
    private void CenterBoxColliderAtTarget()
    {
        if (boxCollider == null) return;
        
        // Calculate the world position of the box collider's center
        Vector3 colliderWorldCenter = transform.TransformPoint(boxCollider.center);
        
        // We want the collider center to be at (0, 0, targetZ)
        Vector3 targetPosition = new Vector3(0f, 0f, targetZ);
        
        // Calculate the offset needed to move the collider center to the target
        Vector3 offset = targetPosition - colliderWorldCenter;
        
        // Apply the offset to the transform position
        transform.position += offset;
    }
    
    private IEnumerator MonitorPositionDuringScaling()
    {
        float elapsedTime = 0f;
        
        // Monitor and adjust position during the entire scaling duration
        while (elapsedTime < scalingDuration)
        {
            // Continuously center the box collider during scaling
            CenterBoxColliderAtTarget();
            
            // Wait a frame before checking again
            yield return null;
            elapsedTime += Time.deltaTime;
        }
        
        // Final centering after scaling is complete
        CenterBoxColliderAtTarget();
        
        Debug.Log($"Finished monitoring {gameObject.name} - final position: {transform.position}");
    }
    
    void OnDisable()
    {
        // Cancel any pending operations when object is disabled
        CancelInvoke(nameof(InitialCenter));
        StopAllCoroutines();
        hasBeenCentered = false;
    }
    
    // Optional: Manual re-centering method you can call if needed
    public void RecenterBoxCollider()
    {
        if (boxCollider != null)
        {
            CenterBoxColliderAtTarget();
        }
    }
}