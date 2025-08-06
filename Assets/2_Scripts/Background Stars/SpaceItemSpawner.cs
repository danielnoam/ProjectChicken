using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpaceItemSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public List<GameObject> spaceItemPrefabs = new List<GameObject>();
    public int maxItemsOnScreen = 50;
    public float initialDelay = 0f; // Time to wait before first spawn
    public float spawnInterval = 0.5f; // Time between spawns in seconds
    
    [Header("Global Item Settings")]
    public float itemMoveSpeed = 5f; // Applied to all items
    public float itemScaleDuration = 2f; // Time in seconds for all items to reach target size
    
    [Header("Double Spawn Settings")]
    [Range(0, 100)]
    public int doubleSpawnChance = 40; // Percentage chance for double spawn
    
    [Header("Spawn Zones")]
    public List<BoxCollider> spawnZones = new List<BoxCollider>();
    
    private int currentItemCount = 0;
    private Transform playerTransform;
    
    void Start()
    {
        // Find the player or camera
        playerTransform = Camera.main.transform;
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
        
        // Setup spawn zones - either use assigned list or find all BoxColliders on this GameObject
        SetupSpawnZones();
        
        // Validate spawn zones and prefabs
        ValidateSpawnZones();
        ValidateItemPrefabs();
        
        // Start spawning items
        StartCoroutine(SpawnItems());
    }
    
    void SetupSpawnZones()
    {
        // If no spawn zones assigned, automatically find all BoxColliders on this GameObject
        if (spawnZones.Count == 0)
        {
            BoxCollider[] foundColliders = GetComponents<BoxCollider>();
            if (foundColliders.Length > 0)
            {
                spawnZones.AddRange(foundColliders);
                Debug.LogWarning($"SpaceItemSpawner: No spawn zones assigned, automatically found {foundColliders.Length} BoxColliders on this GameObject.");
            }
            else
            {
                Debug.LogError("SpaceItemSpawner: No spawn zones assigned and no BoxColliders found on this GameObject!");
            }
        }
    }
    
    void ValidateSpawnZones()
    {
        for (int i = 0; i < spawnZones.Count; i++)
        {
            if (spawnZones[i] == null)
            {
                Debug.LogWarning($"Spawn Zone at index {i} is null in SpaceItemSpawner!");
            }
        }
        
        if (spawnZones.Count == 0)
        {
            Debug.LogError("No spawn zones available in SpaceItemSpawner!");
        }
    }
    
    void ValidateItemPrefabs()
    {
        if (spaceItemPrefabs.Count == 0)
        {
            Debug.LogError("No space item prefabs assigned to SpaceItemSpawner!");
            return;
        }
        
        for (int i = 0; i < spaceItemPrefabs.Count; i++)
        {
            if (spaceItemPrefabs[i] == null)
            {
                Debug.LogWarning($"Space Item Prefab at index {i} is null!");
            }
        }
    }
    
    IEnumerator SpawnItems()
    {
        // Wait for initial delay before starting to spawn
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }
        
        while (true)
        {
            if (currentItemCount < maxItemsOnScreen)
            {
                // Check if we should do a double spawn
                int randomRoll = Random.Range(1, 101); // 1 to 100
                bool shouldDoubleSpawn = randomRoll <= doubleSpawnChance;
                
                if (shouldDoubleSpawn && currentItemCount + 1 < maxItemsOnScreen)
                {
                    // Double spawn: spawn first item immediately, second after half interval
                    SpawnSingleItem();
                    
                    // Wait half the spawn interval
                    yield return new WaitForSeconds(spawnInterval * 0.5f);
                    
                    // Spawn second item (if still under limit)
                    if (currentItemCount < maxItemsOnScreen)
                    {
                        SpawnSingleItem();
                    }
                    
                    // Wait the remaining half interval to complete the full cycle
                    yield return new WaitForSeconds(spawnInterval * 0.5f);
                }
                else
                {
                    // Single spawn: spawn one item and wait full interval
                    SpawnSingleItem();
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
            else
            {
                // If at max capacity, just wait and check again
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }
    
    void SpawnSingleItem()
    {
        // Select a random spawn zone
        BoxCollider selectedZone = GetRandomSpawnZone();
        if (selectedZone == null)
            return;
        
        // Select a random space item prefab
        GameObject selectedPrefab = GetRandomItemPrefab();
        if (selectedPrefab == null)
            return;
        
        // Calculate spawn position within the selected zone
        Vector3 spawnPosition = CalculateSpawnPosition(selectedZone);
        
        // Create the space item
        GameObject newItem = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        
        // Apply global item settings
        ApplyGlobalItemSettings(newItem);
        
        // Setup the item
        SetupItemDestruction(newItem);
        currentItemCount++;
        
        // Start coroutine to track this item
        StartCoroutine(TrackItem(newItem));
    }
    
    BoxCollider GetRandomSpawnZone()
    {
        List<BoxCollider> validZones = GetValidSpawnZones();
        
        if (validZones.Count == 0)
        {
            Debug.LogWarning("No valid spawn zones found!");
            return null;
        }
        
        // Return random valid zone
        int randomIndex = Random.Range(0, validZones.Count);
        return validZones[randomIndex];
    }
    
    List<BoxCollider> GetValidSpawnZones()
    {
        List<BoxCollider> validZones = new List<BoxCollider>();
        
        foreach (BoxCollider zone in spawnZones)
        {
            if (zone != null)
                validZones.Add(zone);
        }
        
        return validZones;
    }
    
    GameObject GetRandomItemPrefab()
    {
        // Filter out null prefabs
        List<GameObject> validPrefabs = new List<GameObject>();
        
        foreach (GameObject prefab in spaceItemPrefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }
        
        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("No valid space item prefabs found!");
            return null;
        }
        
        // Return random valid prefab (each has equal chance)
        int randomIndex = Random.Range(0, validPrefabs.Count);
        return validPrefabs[randomIndex];
    }
    
    Vector3 CalculateSpawnPosition(BoxCollider spawnZone)
    {
        // Get the bounds of the box collider
        Bounds bounds = spawnZone.bounds;
        
        // Generate random position within the bounds
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);
        
        return new Vector3(randomX, randomY, randomZ);
    }
    
    void ApplyGlobalItemSettings(GameObject spaceItem)
    {
        // Try to find behavior component (works with SpaceItemBehavior and other compatible components)
        MonoBehaviour behaviorComponent = spaceItem.GetComponent<SpaceItemBehavior>();
        
        if (behaviorComponent != null)
        {
            // Apply global settings if the component has these properties
            var moveSpeedField = behaviorComponent.GetType().GetField("moveSpeed");
            var scaleDurationField = behaviorComponent.GetType().GetField("scaleDuration");
            
            if (moveSpeedField != null)
                moveSpeedField.SetValue(behaviorComponent, itemMoveSpeed);
                
            if (scaleDurationField != null)
                scaleDurationField.SetValue(behaviorComponent, itemScaleDuration);
        }
        else
        {
            Debug.LogWarning($"Space item {spaceItem.name} doesn't have a SpaceItemBehavior component!");
        }
    }
    
    void SetupItemDestruction(GameObject spaceItem)
    {
        // Add a trigger collider if it doesn't have one (needed for ItemDestroyer detection)
        Collider itemCollider = spaceItem.GetComponent<Collider>();
        if (itemCollider == null)
        {
            SphereCollider sphere = spaceItem.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
        }
        else
        {
            itemCollider.isTrigger = true;
        }
        
        // Add Rigidbody for reliable trigger detection with ItemDestroyer
        Rigidbody rb = spaceItem.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = spaceItem.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Kinematic so physics doesn't interfere
            rb.useGravity = false;
        }
    }
    
    IEnumerator TrackItem(GameObject spaceItem)
    {
        // Simply wait for the item to be destroyed by collision
        while (spaceItem != null)
        {
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
        
        // Reduce count when item is destroyed
        currentItemCount--;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw spawn zones in scene view
        if (spawnZones != null && spawnZones.Count > 0)
        {
            for (int i = 0; i < spawnZones.Count; i++)
            {
                if (spawnZones[i] != null)
                {
                    // Set different colors for each zone (cycling through colors if more than 4 zones)
                    Color[] colors = { Color.yellow, Color.green, Color.blue, Color.magenta, Color.cyan, Color.red, Color.white };
                    Gizmos.color = colors[i % colors.Length];
                    
                    Bounds bounds = spawnZones[i].bounds;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }
    }
}