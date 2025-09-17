using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DNExtensions;
using Random = UnityEngine.Random;

public class SpaceItemSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public List<GameObject> spaceItemPrefabs = new List<GameObject>();
    public int maxItemsOnScreen = 50;
    public float initialDelay = 0f; // Time to wait before first spawn
    public float spawnInterval = 0.5f; // Base time between spawns in seconds
    
    [Header("World Speed Integration")]
    public float minSpawnRate = 0.25f; // Minimum spawn interval when world speed is very high
    
    [Header("Global Item Settings")]
    [MinMaxRange(0,200f)] public RangedFloat itemMoveSpeedRange = new (60, 100f); // Percentage range for random variation
    public float itemScaleDuration = 2f; // Time in seconds for all items to reach target size
    
    [Header("Double Spawn Settings")]
    [Range(0, 100)]
    public int doubleSpawnChance = 40; // Percentage chance for double spawn
    
    [Header("Spawn Zones")]
    public List<BoxCollider> spawnZones = new List<BoxCollider>();
    
    [Header("Pool Integration")]
    public bool useObjectPool = true; // Toggle to use object pool or traditional instantiation
    public SpaceItemPool targetPool; // Reference to specific pool to use
    public string poolName = "SpaceItemPool"; // Name of pool to find if targetPool is not assigned
    
    private int currentItemCount = 0;
    private SpaceItemPool itemPool;
    
    void Start()
    {
        // Setup spawn zones - either use assigned list or find all BoxColliders on this GameObject
        SetupSpawnZones();
        
        // Validate spawn zones and prefabs
        ValidateSpawnZones();
        ValidateItemPrefabs();
        
        // Setup object pool if enabled
        SetupObjectPool();
        
        // Start spawning items
        StartCoroutine(SpawnItems());
    }

    /// <summary>
    /// Calculates the adjusted spawn interval based on world speed
    /// </summary>
    float GetAdjustedSpawnInterval()
    {
        float worldSpeed = LevelManager.WorldSpeed;
        
        // If world speed is 0 or negative, return a very large interval (effectively pausing spawning)
        if (worldSpeed <= 0f)
            return float.MaxValue;
        
        // Calculate adjusted interval: higher world speed = shorter interval (faster spawning)
        float adjustedInterval = spawnInterval / worldSpeed;
        
        // Clamp to minimum spawn rate to prevent extremely fast spawning
        return Mathf.Max(adjustedInterval, minSpawnRate);
    }

    
    void SetupObjectPool()
    {
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
                // Create object pool if it doesn't exist
                GameObject poolObject = new GameObject(poolName);
                itemPool = poolObject.AddComponent<SpaceItemPool>();
                
                // Configure the pool with our prefabs
                itemPool.poolName = poolName;
                itemPool.itemPrefabs = new List<GameObject>(spaceItemPrefabs);
                itemPool.initialPoolSize = Mathf.Max(10, maxItemsOnScreen / 2); // Start with half max items
                itemPool.maxPoolSize = maxItemsOnScreen * 2; // Allow pool to grow beyond max screen items
                
                Debug.Log($"Created {poolName} with {spaceItemPrefabs.Count} prefab types");
            }
            else
            {
                // Ensure the existing pool has our prefabs
                foreach (GameObject prefab in spaceItemPrefabs)
                {
                    if (!itemPool.itemPrefabs.Contains(prefab))
                    {
                        itemPool.itemPrefabs.Add(prefab);
                        Debug.Log($"Added {prefab.name} to existing pool {itemPool.poolName}");
                    }
                }
            }
        }
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
                //Debug.LogWarning($"SpaceItemSpawner: No spawn zones assigned, automatically found {foundColliders.Length} BoxColliders on this GameObject.");
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
            // Get current world speed and adjusted spawn interval
            float currentWorldSpeed = LevelManager.WorldSpeed;
            float adjustedSpawnInterval = GetAdjustedSpawnInterval();
            
            // If world speed is 0 or negative, pause spawning
            if (currentWorldSpeed <= 0f)
            {
                yield return new WaitForSeconds(0.1f); // Check again soon for speed changes
                continue;
            }
            
            // Get current active count from pool or use our counter
            int activeCount = useObjectPool && itemPool != null ? itemPool.GetTotalActiveCount() : currentItemCount;
            
            if (activeCount < maxItemsOnScreen)
            {
                // Check if we should do a double spawn
                int randomRoll = Random.Range(1, 101); // 1 to 100
                bool shouldDoubleSpawn = randomRoll <= doubleSpawnChance;
                
                if (shouldDoubleSpawn && activeCount + 1 < maxItemsOnScreen)
                {
                    // Double spawn: spawn first item immediately, second after half interval
                    SpawnSingleItem();
                    
                    // Wait half the adjusted spawn interval
                    yield return new WaitForSeconds(adjustedSpawnInterval * 0.5f);
                    
                    // Spawn second item (if still under limit and world speed still > 0)
                    if (LevelManager.WorldSpeed > 0f)
                    {
                        activeCount = useObjectPool && itemPool != null ? itemPool.GetTotalActiveCount() : currentItemCount;
                        if (activeCount < maxItemsOnScreen)
                        {
                            SpawnSingleItem();
                        }
                    }
                    
                    // Wait the remaining half interval to complete the full cycle
                    yield return new WaitForSeconds(adjustedSpawnInterval * 0.5f);
                }
                else
                {
                    // Single spawn: spawn one item and wait full interval
                    SpawnSingleItem();
                    yield return new WaitForSeconds(adjustedSpawnInterval);
                }
            }
            else
            {
                // If at max capacity, wait and check again (using adjusted interval)
                yield return new WaitForSeconds(adjustedSpawnInterval);
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
        
        // Create the space item (using pool or traditional instantiation)
        GameObject newItem = CreateSpaceItem(selectedPrefab, spawnPosition);
        
        if (newItem != null)
        {
            // Apply global item settings
            ApplyGlobalItemSettings(newItem);
            
            // Setup the item
            SetupItemDestruction(newItem);
            
            // Only track count if not using pool (pool handles its own counting)
            if (!useObjectPool)
            {
                currentItemCount++;
                StartCoroutine(TrackItem(newItem));
            }
        }
    }
    
    GameObject CreateSpaceItem(GameObject prefab, Vector3 position)
    {
        GameObject newItem = null;
        
        if (useObjectPool && itemPool != null)
        {
            // Get item from pool
            newItem = itemPool.GetPooledItem(prefab);
            if (newItem != null)
            {
                newItem.transform.position = position;
                newItem.transform.rotation = Quaternion.identity;
            }
        }
        else
        {
            // Traditional instantiation
            newItem = Instantiate(prefab, position, Quaternion.identity);
        }
        
        return newItem;
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
                moveSpeedField.SetValue(behaviorComponent, itemMoveSpeedRange.RandomValue);
                
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
        // Only used when object pooling is disabled
        // Simply wait for the item to be destroyed by collision
        while (spaceItem != null)
        {
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
        
        // Reduce count when item is destroyed
        currentItemCount--;
    }
}