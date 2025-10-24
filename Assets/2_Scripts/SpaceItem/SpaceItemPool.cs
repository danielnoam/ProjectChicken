using UnityEngine;
using System.Collections.Generic;

public class SpaceItemPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public List<GameObject> itemPrefabs = new List<GameObject>();
    public int initialPoolSize = 20; // Pre-instantiated objects per prefab
    public int maxPoolSize = 100; // Maximum objects per prefab type
    public Transform poolParent; // Parent transform for pooled objects (optional)
    
    // Dictionary to store pools for each prefab type
    private Dictionary<GameObject, Queue<GameObject>> itemPools = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, List<GameObject>> activeItems = new Dictionary<GameObject, List<GameObject>>();
    
    [Header("Pool Identity")]
    public string poolName = "DefaultPool"; // Name for this specific pool
    
    void Awake()
    {
        InitializePools();
    }
    
    void InitializePools()
    {
        // Create pool parent if not assigned
        if (poolParent == null)
        {
            GameObject poolParentObject = new GameObject("SpaceItemPool");
            poolParent = poolParentObject.transform;
            poolParent.SetParent(transform);
        }
        
        // Initialize pools for each prefab
        foreach (GameObject prefab in itemPrefabs)
        {
            if (prefab != null)
            {
                CreatePool(prefab, initialPoolSize);
            }
        }
    }
    
    void CreatePool(GameObject prefab, int poolSize)
    {
        Queue<GameObject> pool = new Queue<GameObject>();
        List<GameObject> activeList = new List<GameObject>();
        
        // Pre-instantiate objects
        for (int i = 0; i < poolSize; i++)
        {
            GameObject pooledItem = Instantiate(prefab);
            pooledItem.SetActive(false);
            pooledItem.transform.SetParent(poolParent);
            pool.Enqueue(pooledItem);
        }
        
        itemPools[prefab] = pool;
        activeItems[prefab] = activeList;
    }
    
    public GameObject GetPooledItem(GameObject prefab)
    {
        // Check if pool exists for this prefab
        if (!itemPools.ContainsKey(prefab))
        {
            Debug.LogWarning($"No pool found for prefab {prefab.name}. Creating new pool.");
            CreatePool(prefab, initialPoolSize);
        }
        
        Queue<GameObject> pool = itemPools[prefab];
        List<GameObject> activeList = activeItems[prefab];
        
        GameObject pooledItem;
        
        // Get item from pool or create new one if pool is empty
        if (pool.Count > 0)
        {
            pooledItem = pool.Dequeue();
        }
        else if (activeList.Count < maxPoolSize)
        {
            // Create new item if under max pool size
            pooledItem = Instantiate(prefab);
            Debug.Log($"Pool for {prefab.name} was empty, created new item. Active: {activeList.Count + 1}");
        }
        else
        {
            // Pool is at max capacity
            Debug.LogWarning($"Pool for {prefab.name} is at maximum capacity ({maxPoolSize})");
            return null;
        }
        
        // Reset the item and activate it
        ResetPooledItem(pooledItem);
        pooledItem.SetActive(true);
        activeList.Add(pooledItem);
        
        return pooledItem;
    }
    
    public void ReturnToPool(GameObject item)
    {
        if (item == null) return;
        
        // Find which prefab this item belongs to
        GameObject originalPrefab = FindOriginalPrefab(item);
        
        if (originalPrefab != null && itemPools.ContainsKey(originalPrefab))
        {
            // Remove from active list
            activeItems[originalPrefab].Remove(item);
            
            // Deactivate and return to pool
            item.SetActive(false);
            item.transform.SetParent(poolParent);
            itemPools[originalPrefab].Enqueue(item);
        }
        else
        {
            Debug.LogWarning($"Could not find pool for item {item.name}. Destroying instead.");
            Destroy(item);
        }
    }
    
    GameObject FindOriginalPrefab(GameObject item)
    {
        // This is a simple approach - you might want to store the original prefab reference
        // in a component on the instantiated object for better performance
        string itemName = item.name.Replace("(Clone)", "").Trim();
        
        foreach (GameObject prefab in itemPrefabs)
        {
            if (prefab != null && prefab.name == itemName)
            {
                return prefab;
            }
        }
        
        return null;
    }
    
    void ResetPooledItem(GameObject item)
    {
        // Reset transform
        item.transform.position = Vector3.zero;
        item.transform.rotation = Quaternion.identity;
        item.transform.localScale = Vector3.one;
        
        // Reset SpaceItemBehavior if it exists
        SpaceItemBehavior behavior = item.GetComponent<SpaceItemBehavior>();
        if (behavior != null)
        {
            behavior.ResetForPool();
        }
    }
    
    // Get pool statistics
    public int GetActiveCount(GameObject prefab)
    {
        if (activeItems.ContainsKey(prefab))
            return activeItems[prefab].Count;
        return 0;
    }
    
    public int GetPooledCount(GameObject prefab)
    {
        if (itemPools.ContainsKey(prefab))
            return itemPools[prefab].Count;
        return 0;
    }
    
    public int GetTotalActiveCount()
    {
        int total = 0;
        foreach (var activeList in activeItems.Values)
        {
            total += activeList.Count;
        }
        return total;
    }
    
    
    // Static method to find a pool by name
    public static SpaceItemPool FindPoolByName(string name)
    {
        SpaceItemPool[] allPools = FindObjectsByType<SpaceItemPool>( FindObjectsSortMode.None);
        foreach (SpaceItemPool pool in allPools)
        {
            if (pool.poolName == name)
            {
                return pool;
            }
        }
        return null;
    }
    
    // Static method to find a pool that contains a specific prefab
    public static SpaceItemPool FindPoolWithPrefab(GameObject prefab)
    {
        SpaceItemPool[] allPools = FindObjectsByType<SpaceItemPool>( FindObjectsSortMode.None);
        foreach (SpaceItemPool pool in allPools)
        {
            if (pool.itemPrefabs.Contains(prefab))
            {
                return pool;
            }
        }
        return null;
    }
}