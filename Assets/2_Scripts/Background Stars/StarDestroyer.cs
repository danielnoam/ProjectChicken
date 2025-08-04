using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StarDestroyer : MonoBehaviour
{
    [Header("Destruction Settings")]
    public float destructionDelay = 1f; // Time in seconds before destroying the star after trigger
    
    private HashSet<GameObject> starsBeingDestroyed = new HashSet<GameObject>();
    
    void Start()
    {
        // Ensure this object has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add a box collider by default
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(50f, 30f, 5f); // Large area behind player
        }
        else
        {
            col.isTrigger = true;
        }
        
        // Set the tag for easy identification
        gameObject.tag = "StarDestroyer";
        
        // Add Rigidbody if it doesn't exist (needed for reliable trigger detection)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Kinematic so it doesn't fall due to gravity
            rb.useGravity = false;
        }

    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is a star
        StarBehavior star = other.GetComponent<StarBehavior>();
        if (star != null)
        {
            // Check if we're already processing this star
            if (!starsBeingDestroyed.Contains(other.gameObject))
            {
                // Add to tracking set
                starsBeingDestroyed.Add(other.gameObject);
                
                // Start destruction countdown
                StartCoroutine(DestroyStarAfterDelay(star, other.gameObject));
            }
        }
    }
    
    IEnumerator DestroyStarAfterDelay(StarBehavior star, GameObject starObject)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(destructionDelay);
        
        // Check if the star still exists (might have been destroyed by other means)
        if (starObject != null && star != null)
        {
            // Start the fade out process
            star.StartFadeOut();
        }
        
        // Remove from tracking set
        if (starObject != null)
        {
            starsBeingDestroyed.Remove(starObject);
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw the destruction zone in scene view
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}