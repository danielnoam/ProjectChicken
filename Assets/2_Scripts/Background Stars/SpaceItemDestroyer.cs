using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpaceItemDestroyer : MonoBehaviour
{
    [Header("Destruction Settings")]
    public float destructionDelay = 1f; // Time in seconds before destroying the item after trigger
    
    [Header("Pool Integration")]
    public bool useObjectPool = true; // Toggle to use object pool or traditional destruction
    public SpaceItemPool targetPool; // Reference to specific pool to use
    public string poolName = "SpaceItemPool"; // Name of pool to find if targetPool is not assigned
    
    private HashSet<GameObject> itemsBeingDestroyed = new HashSet<GameObject>();
    private SpaceItemPool itemPool;
    
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
        gameObject.tag = "ItemDestroyer";
        
        // Add Rigidbody if it doesn't exist (needed for reliable trigger detection)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Kinematic so it doesn't fall due to gravity
            rb.useGravity = false;
        }
        
        // Get reference to object pool if using it
        if (useObjectPool)
        {
            // Use assigned pool or find one by name
            if (targetPool != null)
            {
                itemPool = targetPool;
            }
            else
            {
                itemPool = SpaceItemPool.FindPoolByName(poolName);
            }
            
            if (itemPool == null)
            {
                //Debug.LogWarning($"SpaceItemDestroyer: Object pool '{poolName}' not found, falling back to traditional destruction");
                useObjectPool = false;
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is a space item
        SpaceItemBehavior spaceItem = other.GetComponent<SpaceItemBehavior>();
        if (spaceItem != null)
        {
            // Check if we're already processing this item
            if (!itemsBeingDestroyed.Contains(other.gameObject))
            {
                // Add to tracking set
                itemsBeingDestroyed.Add(other.gameObject);
                
                // Start destruction countdown
                StartCoroutine(ProcessItemAfterDelay(spaceItem, other.gameObject));
            }
        }
    }
    
    IEnumerator ProcessItemAfterDelay(SpaceItemBehavior spaceItem, GameObject itemObject)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(destructionDelay);
        
        // Check if the item still exists (might have been destroyed by other means)
        if (itemObject != null && spaceItem != null)
        {
            if (useObjectPool && itemPool != null)
            {
                // Return to pool instead of destroying
                ReturnToPool(spaceItem, itemObject);
            }
            else
            {
                // Traditional destruction with fade out
                spaceItem.StartFadeOut();
            }
        }
        
        // Remove from tracking set
        if (itemObject != null)
        {
            itemsBeingDestroyed.Remove(itemObject);
        }
    }
    
    void ReturnToPool(SpaceItemBehavior spaceItem, GameObject itemObject)
    {
        // Find the appropriate pool for this item
        SpaceItemPool poolToUse = itemPool; // Use assigned pool first
        
        if (poolToUse == null)
        {
            // Try to find a pool that contains this prefab type
            poolToUse = SpaceItemPool.FindPoolWithPrefab(FindOriginalPrefab(itemObject));
        }
        
        if (poolToUse != null)
        {
            // Check if the item should fade out or return immediately
            if (spaceItem.fadeOutDuration > 0f)
            {
                // Start fade out, which will automatically return to pool when complete
                spaceItem.StartFadeOut();
            }
            else
            {
                // Return to pool immediately
                poolToUse.ReturnToPool(itemObject);
            }
        }
        else
        {
            // No suitable pool found, use traditional destruction
            spaceItem.StartFadeOut();
        }
    }
    
    GameObject FindOriginalPrefab(GameObject item)
    {
        // Simple approach to find original prefab based on name
        string itemName = item.name.Replace("(Clone)", "").Trim();
        
        // Search through all pools to find matching prefab
        SpaceItemPool[] allPools = FindObjectsOfType<SpaceItemPool>();
        foreach (SpaceItemPool pool in allPools)
        {
            foreach (GameObject prefab in pool.itemPrefabs)
            {
                if (prefab != null && prefab.name == itemName)
                {
                    return prefab;
                }
            }
        }
        
        return null;
    }
    
    // Public method to immediately process an item (useful for external calls)
    public void ProcessItemImmediately(GameObject itemObject)
    {
        SpaceItemBehavior spaceItem = itemObject.GetComponent<SpaceItemBehavior>();
        if (spaceItem != null && !itemsBeingDestroyed.Contains(itemObject))
        {
            itemsBeingDestroyed.Add(itemObject);
            
            if (useObjectPool && itemPool != null)
            {
                ReturnToPool(spaceItem, itemObject);
            }
            else
            {
                spaceItem.StartFadeOut();
            }
            
            // Remove from tracking set after a short delay
            StartCoroutine(RemoveFromTrackingAfterDelay(itemObject, 0.1f));
        }
    }
    
    IEnumerator RemoveFromTrackingAfterDelay(GameObject itemObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (itemObject != null)
        {
            itemsBeingDestroyed.Remove(itemObject);
        }
    }
    
    // Method to force return an item to pool without fade (emergency cleanup)
    public void ForceReturnToPool(GameObject itemObject)
    {
        if (useObjectPool && itemPool != null && itemObject != null)
        {
            // Remove from tracking
            itemsBeingDestroyed.Remove(itemObject);
            
            // Stop any fade coroutines
            SpaceItemBehavior spaceItem = itemObject.GetComponent<SpaceItemBehavior>();
            if (spaceItem != null)
            {
                spaceItem.ReturnToPoolOrDestroyImmediately();
            }
            else
            {
                // Return directly to pool
                itemPool.ReturnToPool(itemObject);
            }
        }
    }
    
    // Clean up tracking set of any null references (useful for debugging)
    public void CleanupTrackingSet()
    {
        itemsBeingDestroyed.RemoveWhere(item => item == null);
    }
    
    // Get count of items currently being processed
    public int GetProcessingCount()
    {
        CleanupTrackingSet();
        return itemsBeingDestroyed.Count;
    }
    


}