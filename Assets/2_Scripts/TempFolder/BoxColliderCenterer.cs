using System;
using UnityEngine;
using System.Collections;

public class GameObjectCenterer : MonoBehaviour
{
    [Header("Center Reference")]
    public GameObject centerObject; // The GameObject that represents the center point
    
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
        if (centerObject != null)
        {
            Invoke(nameof(InitialCenter), 0.02f);
        }
    }



    private void InitialCenter()
    {
        // Prevent multiple centering calls
        if (hasBeenCentered) return;
        
        // Check if we have a center object assigned
        if (centerObject == null)
        {
            Debug.LogWarning("No center object assigned on " + gameObject.name);
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
        CenterObjectAtTarget();
        
        // Start monitoring position during scaling period
        StartCoroutine(MonitorPositionDuringScaling());
        
        hasBeenCentered = true;
    }
    
    private void CenterObjectAtTarget()
    {
        if (centerObject == null) return;
        
        // Get the world position of the center object
        Vector3 centerWorldPosition = centerObject.transform.position;
        
        // We want the center object to be at (0, 0, targetZ)
        Vector3 targetPosition = new Vector3(0f, 0f, targetZ);
        
        // Calculate the offset needed to move the center object to the target
        Vector3 offset = targetPosition - centerWorldPosition;
        
        // Apply the offset to this transform (the parent object)
        transform.position += offset;
    }
    
    private IEnumerator MonitorPositionDuringScaling()
    {
        float elapsedTime = 0f;
        
        // Monitor and adjust position during the entire scaling duration
        while (elapsedTime < scalingDuration)
        {
            // Continuously center the object during scaling
            CenterObjectAtTarget();
            
            // Wait a frame before checking again
            yield return null;
            elapsedTime += Time.deltaTime;
        }
        
        // Final centering after scaling is complete
        CenterObjectAtTarget();
        
        Debug.Log($"Finished monitoring {gameObject.name} - final position: {transform.position}");
    }
    
    void OnDisable()
    {
        // Cancel any pending operations when object is disabled
        CancelInvoke(nameof(InitialCenter));
        StopAllCoroutines();
        hasBeenCentered = false;
    }
    

    public void RecenterObject()
    {
        if (centerObject != null)
        {
            CenterObjectAtTarget();
        }
    }
}