using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StarSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public List<GameObject> starPrefabs = new List<GameObject>();
    public int maxStarsOnScreen = 50;
    public float initialDelay = 0f; // Time to wait before first spawn
    public float spawnInterval = 0.5f; // Time between spawns in seconds
    
    [Header("Global Star Settings")]
    public float starMoveSpeed = 5f; // Applied to all stars
    public float starScaleDuration = 2f; // Time in seconds for all stars to reach target size
    
    [Header("Double Spawn Settings")]
    [Range(0, 100)]
    public int doubleSpawnChance = 40; // Percentage chance for double spawn
    
    [Header("Spawn Zones")]
    public BoxCollider spawnZone1;
    public BoxCollider spawnZone2;
    public BoxCollider spawnZone3;
    public BoxCollider spawnZone4;
    
    private int currentStarCount = 0;
    private Transform playerTransform;
    private BoxCollider[] spawnZones;
    
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
        
        // Setup spawn zones array
        spawnZones = new BoxCollider[] { spawnZone1, spawnZone2, spawnZone3, spawnZone4 };
        
        // Validate spawn zones and prefabs
        ValidateSpawnZones();
        ValidateStarPrefabs();
        
        // Start spawning stars
        StartCoroutine(SpawnStars());
    }
    
    void ValidateSpawnZones()
    {
        for (int i = 0; i < spawnZones.Length; i++)
        {
            if (spawnZones[i] == null)
            {
                Debug.LogWarning($"Spawn Zone {i + 1} is not assigned in StarSpawner!");
            }
        }
    }
    
    void ValidateStarPrefabs()
    {
        if (starPrefabs.Count == 0)
        {
            Debug.LogError("No star prefabs assigned to StarSpawner!");
            return;
        }
        
        for (int i = 0; i < starPrefabs.Count; i++)
        {
            if (starPrefabs[i] == null)
            {
                Debug.LogWarning($"Star Prefab at index {i} is null!");
            }
        }
    }
    
    IEnumerator SpawnStars()
    {
        // Wait for initial delay before starting to spawn
        if (initialDelay > 0f)
        {
            Debug.Log($"StarSpawner: Waiting {initialDelay} seconds before starting spawns...");
            yield return new WaitForSeconds(initialDelay);
            Debug.Log("StarSpawner: Initial delay complete, starting spawns!");
        }
        
        while (true)
        {
            if (currentStarCount < maxStarsOnScreen)
            {
                // Check if we should do a double spawn
                int randomRoll = Random.Range(1, 101); // 1 to 100
                bool shouldDoubleSpawn = randomRoll <= doubleSpawnChance;
                
                if (shouldDoubleSpawn && currentStarCount + 1 < maxStarsOnScreen)
                {
                    // Double spawn: spawn first star immediately, second after half interval
                    SpawnSingleStar();
                    
                    // Wait half the spawn interval
                    yield return new WaitForSeconds(spawnInterval * 0.5f);
                    
                    // Spawn second star (if still under limit)
                    if (currentStarCount < maxStarsOnScreen)
                    {
                        SpawnSingleStar();
                    }
                    
                    // Wait the remaining half interval to complete the full cycle
                    yield return new WaitForSeconds(spawnInterval * 0.5f);
                }
                else
                {
                    // Single spawn: spawn one star and wait full interval
                    SpawnSingleStar();
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
    
    void SpawnSingleStar()
    {
        // Select a random spawn zone
        BoxCollider selectedZone = GetRandomSpawnZone();
        if (selectedZone == null)
            return;
        
        // Select a random star prefab
        GameObject selectedPrefab = GetRandomStarPrefab();
        if (selectedPrefab == null)
            return;
        
        // Calculate spawn position within the selected zone
        Vector3 spawnPosition = CalculateSpawnPosition(selectedZone);
        
        // Create the star
        GameObject newStar = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        
        // Apply global star settings
        ApplyGlobalStarSettings(newStar);
        
        // Setup the star
        SetupStarDestruction(newStar);
        currentStarCount++;
        
        // Start coroutine to track this star
        StartCoroutine(TrackStar(newStar));
        
        Debug.Log($"Spawned star: {newStar.name} at {spawnPosition}. Current count: {currentStarCount}");
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
    
    GameObject GetRandomStarPrefab()
    {
        // Filter out null prefabs
        List<GameObject> validPrefabs = new List<GameObject>();
        
        foreach (GameObject prefab in starPrefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }
        
        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("No valid star prefabs found!");
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
    
    void ApplyGlobalStarSettings(GameObject star)
    {
        StarBehavior starComponent = star.GetComponent<StarBehavior>();
        if (starComponent != null)
        {
            // Apply global settings to all stars
            starComponent.moveSpeed = starMoveSpeed;
            starComponent.scaleDuration = starScaleDuration;
            
            Debug.Log($"Applied global settings to {star.name}: Speed={starMoveSpeed}, ScaleDuration={starScaleDuration}");
        }
        else
        {
            Debug.LogWarning($"Star {star.name} doesn't have StarBehavior component!");
        }
    }
    
    void SetupStarDestruction(GameObject star)
    {
        // Add a trigger collider if it doesn't have one (needed for StarDestroyer detection)
        Collider starCollider = star.GetComponent<Collider>();
        if (starCollider == null)
        {
            SphereCollider sphere = star.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
        }
        else
        {
            starCollider.isTrigger = true;
        }
        
        // Add Rigidbody for reliable trigger detection with StarDestroyer
        Rigidbody rb = star.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = star.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Kinematic so physics doesn't interfere
            rb.useGravity = false;
        }
        
        Debug.Log("Star setup for destruction detection: " + star.name + " - Trigger: " + starCollider.isTrigger + ", Has Rigidbody: " + (rb != null));
    }
    
    IEnumerator TrackStar(GameObject star)
    {
        StarBehavior starComponent = star.GetComponent<StarBehavior>();
        
        // Simply wait for the star to be destroyed by collision
        while (star != null)
        {
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
        
        // Reduce count when star is destroyed
        currentStarCount--;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw spawn zones in scene view
        if (spawnZones != null)
        {
            for (int i = 0; i < spawnZones.Length; i++)
            {
                if (spawnZones[i] != null)
                {
                    // Set different colors for each zone
                    switch (i)
                    {
                        case 0: Gizmos.color = Color.yellow; break;
                        case 1: Gizmos.color = Color.green; break;
                        case 2: Gizmos.color = Color.blue; break;
                        case 3: Gizmos.color = Color.magenta; break;
                    }
                    
                    Bounds bounds = spawnZones[i].bounds;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }
    }
}