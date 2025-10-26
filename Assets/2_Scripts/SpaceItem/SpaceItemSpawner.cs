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
    public float initialDelay = 0f;
    public float spawnInterval = 0.5f;
    
    [Header("World Speed Integration")]
    public float minSpawnRate = 0.25f;
    
    [Header("Global Item Settings")]
    [MinMaxRange(0,200f)] public RangedFloat itemMoveSpeedRange = new (60, 100f);
    public float itemScaleDuration = 2f;
    
    [Header("Double Spawn Settings")]
    [Range(0, 100)]
    public int doubleSpawnChance = 40;
    
    [Header("Spawn Zones")]
    public List<BoxCollider> spawnZones = new List<BoxCollider>();
    
    [Header("Pool Integration")]
    public bool useObjectPool = true;
    public SpaceItemPool targetPool;
    public string poolName = "SpaceItemPool";
    
    private int _currentItemCount = 0;
    private SpaceItemPool _itemPool;
    
    private void Start()
    {
        SetupSpawnZones();
        ValidateSpawnZones();
        ValidateItemPrefabs();
        SetupObjectPool();
        StartCoroutine(SpawnItems());
    }

    private float GetAdjustedSpawnInterval()
    {
        float worldSpeed = LevelManager.WorldSpeed;
        
        if (worldSpeed <= 0f)
            return float.MaxValue;
        
        float adjustedInterval = spawnInterval / worldSpeed;
        
        return Mathf.Max(adjustedInterval, minSpawnRate);
    }
    
    private void SetupObjectPool()
    {
        if (useObjectPool)
        {
            if (targetPool != null)
            {
                _itemPool = targetPool;
            }
            else
            {
                _itemPool = SpaceItemPool.FindPoolByName(poolName);
            }
            
            if (_itemPool == null)
            {
                GameObject poolObject = new GameObject(poolName);
                _itemPool = poolObject.AddComponent<SpaceItemPool>();
                
                _itemPool.poolName = poolName;
                _itemPool.itemPrefabs = new List<GameObject>(spaceItemPrefabs);
                _itemPool.initialPoolSize = Mathf.Max(10, maxItemsOnScreen / 2);
                _itemPool.maxPoolSize = maxItemsOnScreen * 2;
                
                Debug.Log($"Created {poolName} with {spaceItemPrefabs.Count} prefab types");
            }
            else
            {
                foreach (GameObject prefab in spaceItemPrefabs)
                {
                    if (!_itemPool.itemPrefabs.Contains(prefab))
                    {
                        _itemPool.itemPrefabs.Add(prefab);
                    }
                }
            }
        }
    }
    
    private void SetupSpawnZones()
    {
        if (spawnZones.Count == 0)
        {
            BoxCollider[] foundColliders = GetComponents<BoxCollider>();
            if (foundColliders.Length > 0)
            {
                spawnZones.AddRange(foundColliders);
            }
            else
            {
                Debug.LogError("SpaceItemSpawner: No spawn zones assigned and no BoxColliders found on this GameObject!");
            }
        }
    }
    
    private void ValidateSpawnZones()
    {
        for (int i = 0; i < spawnZones.Count; i++)
        {
            if (spawnZones[i] == null)
            {
            }
        }
        
        if (spawnZones.Count == 0)
        {
        }
    }
    
    private void ValidateItemPrefabs()
    {
        if (spaceItemPrefabs.Count == 0)
        {
            return;
        }
        
        for (int i = 0; i < spaceItemPrefabs.Count; i++)
        {
            if (spaceItemPrefabs[i] == null)
            {
            }
        }
    }
    
    private IEnumerator SpawnItems()
    {
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }
        
        while (true)
        {
            float currentWorldSpeed = LevelManager.WorldSpeed;
            float adjustedSpawnInterval = GetAdjustedSpawnInterval();
            
            if (currentWorldSpeed <= 0f)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            
            int activeCount = useObjectPool && _itemPool != null ? _itemPool.GetTotalActiveCount() : _currentItemCount;
            
            if (activeCount < maxItemsOnScreen)
            {
                int randomRoll = Random.Range(1, 101);
                bool shouldDoubleSpawn = randomRoll <= doubleSpawnChance;
                
                if (shouldDoubleSpawn && activeCount + 1 < maxItemsOnScreen)
                {
                    SpawnSingleItem();
                    
                    yield return new WaitForSeconds(adjustedSpawnInterval * 0.5f);
                    
                    if (LevelManager.WorldSpeed > 0f)
                    {
                        activeCount = useObjectPool && _itemPool != null ? _itemPool.GetTotalActiveCount() : _currentItemCount;
                        if (activeCount < maxItemsOnScreen)
                        {
                            SpawnSingleItem();
                        }
                    }
                    
                    yield return new WaitForSeconds(adjustedSpawnInterval * 0.5f);
                }
                else
                {
                    SpawnSingleItem();
                    yield return new WaitForSeconds(adjustedSpawnInterval);
                }
            }
            else
            {
                yield return new WaitForSeconds(adjustedSpawnInterval);
            }
        } 
    }
    
    private void SpawnSingleItem()
    {
        BoxCollider selectedZone = GetRandomSpawnZone();
        if (selectedZone == null)
            return;
        
        GameObject selectedPrefab = GetRandomItemPrefab();
        if (selectedPrefab == null)
            return;
        
        Vector3 spawnPosition = CalculateSpawnPosition(selectedZone);
        
        GameObject newItem = CreateSpaceItem(selectedPrefab, spawnPosition);
        
        if (newItem != null)
        {
            ApplyGlobalItemSettings(newItem);
            SetupItemDestruction(newItem);
            
            if (!useObjectPool)
            {
                _currentItemCount++;
                StartCoroutine(TrackItem(newItem));
            }
        }
    }
    
    private GameObject CreateSpaceItem(GameObject prefab, Vector3 position)
    {
        GameObject newItem = null;
        
        if (useObjectPool && _itemPool != null)
        {
            newItem = _itemPool.GetPooledItem(prefab);
            if (newItem != null)
            {
                newItem.transform.position = position;
                newItem.transform.rotation = Quaternion.identity;
            }
        }
        else
        {
            newItem = Instantiate(prefab, position, Quaternion.identity);
        }
        
        return newItem;
    }
    
    private BoxCollider GetRandomSpawnZone()
    {
        List<BoxCollider> validZones = GetValidSpawnZones();
        
        if (validZones.Count == 0)
        {
            Debug.LogWarning("No valid spawn zones found!");
            return null;
        }
        
        int randomIndex = Random.Range(0, validZones.Count);
        return validZones[randomIndex];
    }
    
    private List<BoxCollider> GetValidSpawnZones()
    {
        List<BoxCollider> validZones = new List<BoxCollider>();
        
        foreach (BoxCollider zone in spawnZones)
        {
            if (zone != null)
                validZones.Add(zone);
        }
        
        return validZones;
    }
    
    private GameObject GetRandomItemPrefab()
    {
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
        
        int randomIndex = Random.Range(0, validPrefabs.Count);
        return validPrefabs[randomIndex];
    }
    
    private Vector3 CalculateSpawnPosition(BoxCollider spawnZone)
    {
        Bounds bounds = spawnZone.bounds;
        
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);
        
        return new Vector3(randomX, randomY, randomZ);
    }
    
    private void ApplyGlobalItemSettings(GameObject spaceItem)
    {
        MonoBehaviour behaviorComponent = spaceItem.GetComponent<SpaceItemBehavior>();
        
        if (behaviorComponent != null)
        {
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
    
    private void SetupItemDestruction(GameObject spaceItem)
    {
        Collider itemCollider = spaceItem.GetComponent<Collider>();
        if (!itemCollider)
        {
            SphereCollider sphere = spaceItem.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
        }
        else
        {
            itemCollider.isTrigger = true;
        }
        
        Rigidbody rb = spaceItem.GetComponent<Rigidbody>();
        if (!rb)
        {
            rb = spaceItem.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    
    private IEnumerator TrackItem(GameObject spaceItem)
    {
        while (spaceItem)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        _currentItemCount--;
    }
}