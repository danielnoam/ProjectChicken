using UnityEngine;
using VInspector;


[System.Serializable]
public class ResourceChance 
{
    public Resource resource;
    [Range(0f, 100f)] public float chance = 10f;
}

[CreateAssetMenu(fileName = "New LootTable", menuName = "Scriptable Objects/New LootTable")]
public class SOLootTable : ScriptableObject
{
    
    [Header("Loot Table")]
    [SerializeField] private ResourceChance[] resourceChances = System.Array.Empty<ResourceChance>();


    
    private void OnValidate()
    {
        if (resourceChances is { Length: > 0 })
        {
            NormalizeResourceChances();
        }
    }


    #region Resource Spawning ---------------------------------------------------------------------------------------

    
    public Resource SpawnResource(Resource resource, Vector3 position, Transform parent)
    {
        if (!resource) return null;
        
        Resource newResource = Instantiate(resource, position, Quaternion.identity, parent);

        return newResource;
    }
    
    public Resource SpawnResource(Resource resource, Vector3 position)
    {
        if (!resource) return null;
        
        Resource newResource = Instantiate(resource, position, Quaternion.identity);

        return newResource;
    }

    #endregion Resource Spawning ---------------------------------------------------------------------------------------
    
    
    #region Resource Selection ---------------------------------------------------------------------------------------
    
    public Resource GetRandomResource()
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
        float randomValue = Random.Range(0f, totalWeight);
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
    
    
    #endregion Resource Selection ---------------------------------------------------------------------------------------
    

    #region Resource List Handling ---------------------------------------------------------------------------------------

    
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
    
    
    [ContextMenu("Equalize Resource Chances")]
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

    #endregion Resource List Handling ---------------------------------------------------------------------------------------

    
    
}
