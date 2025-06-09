using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using VInspector;
using UnityEngine;
using UnityEngine.Splines;

[System.Serializable]
public class ResourceChance 
{
    public Resource resource;
    [Range(0f, 100f)] public float chance = 10f;
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    
    [Header("Resource Settings")]
    [SerializeField] private ResourceChance[] resourceChances = System.Array.Empty<ResourceChance>();
    [SerializeField] private Transform resourceHolder;

    
    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    
    

    #region Resource Selection ---------------------------------------------------------------------------------------
    
    private Resource SelectRandomResource()
    {
        if (resourceChances.Length == 0) return null;
        
        // Filter out resources with null references
        var validResources = new System.Collections.Generic.List<ResourceChance>();
        foreach (var resourceChance in resourceChances)
        {
            if (resourceChance.resource && resourceChance.chance > 0f)
            {
                validResources.Add(resourceChance);
            }
        }
        
        if (validResources.Count == 0) return null;
        
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var resourceChance in validResources)
        {
            totalWeight += resourceChance.chance;
        }
        
        if (totalWeight <= 0f) return validResources[0].resource;
        
        // Select random resource based on weights
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var resourceChance in validResources)
        {
            currentWeight += resourceChance.chance;
            if (randomValue <= currentWeight)
            {
                return resourceChance.resource;
            }
        }
        
        // Fallback
        return validResources[0].resource;
    }
    
    private void NormalizeResourceChances()
    {
        if (resourceChances.Length == 0) return;
        
        // Calculate total of all valid chances
        float totalChance = 0f;
        int validResourceCount = 0;
        
        for (int i = 0; i < resourceChances.Length; i++)
        {
            if (resourceChances[i].resource != null)
            {
                totalChance += Mathf.Max(0f, resourceChances[i].chance);
                validResourceCount++;
            }
        }
        
        if (validResourceCount == 0) return;
        
        // If total is 0, set equal chances
        if (totalChance <= 0f)
        {
            float equalChance = 100f / validResourceCount;
            for (int i = 0; i < resourceChances.Length; i++)
            {
                if (resourceChances[i].resource != null)
                {
                    resourceChances[i].chance = equalChance;
                }
            }
        }
        // If total is not 100, normalize to 100%
        else if (Mathf.Abs(totalChance - 100f) > 0.01f)
        {
            foreach (var resourceChance in resourceChances)
            {
                if (resourceChance.resource)
                {
                    resourceChance.chance = (resourceChance.chance / totalChance) * 100f;
                }
            }
        }
    }
    
    #endregion Resource Selection ---------------------------------------------------------------------------------------



    #region Spwaing Resources ---------------------------------------------------------------------------------------

    private void SpawnResource(Resource resource, Vector3 position)
    {
        if (!resource) return;
        
        Resource newResource = Instantiate(resource, position, Quaternion.identity, resourceHolder);
    }
    
    public void SpawnResourceAtPosition(Resource resource, Vector3 position)
    {
        if (!resource) return;
        
        SpawnResource(resource, position);
    }

    public void SpawnRandomResourceAtPosition(Vector3 position)
    {
        Resource randomResource = SelectRandomResource();
        if (randomResource)
        {
            SpawnResource(randomResource, position);
        }
    }


    

    #endregion Spwaing Resources ---------------------------------------------------------------------------------------



    #region Editor Methods ---------------------------------------------------------------------------------------

    
    private void OnValidate()
    {
        if (resourceChances is { Length: > 0 })
        {
            NormalizeResourceChances();
        }
    }
    
    [Button]
    private void EqualizeResourceChances()
    {
        if (resourceChances.Length == 0) return;
        
        float equalChance = 100f / resourceChances.Length;
        foreach (var resourceChance in resourceChances)
        {
            if (resourceChance.resource)
            {
                resourceChance.chance = equalChance;
            }
        }
    }

    [Button]
    private void SpawnRandomResourceOnSpline()
    {
        if (!Application.isPlaying) return;
        if (!LevelManager.Instance || !LevelManager.Instance.SplineContainer) return;

        Resource randomResource = SelectRandomResource();
        if (randomResource)
        {
            // Get a random point on the spline
            float randomT = Random.Range(0f, 1f);
            Vector3 positionOnSpline = LevelManager.Instance.SplineContainer.EvaluatePosition(randomT);
            
            // Add a small offset to the position to avoid overlapping with the spline
            Vector3 randomOffset = new Vector3(Random.Range(-3f,3f), Random.Range(-3f,3f), Random.Range(-3f,3f)); // Adjust Y offset as needed

            // Spawn the resource at that position
            SpawnResource(randomResource, positionOnSpline + randomOffset);
        }

    }

    #endregion Editor Methods ---------------------------------------------------------------------------------------

}