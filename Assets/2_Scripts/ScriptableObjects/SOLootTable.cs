using UnityEngine;



[System.Serializable]
public class ResourceChance 
{
    public Resource resource;
    [Range(0, 100)] public int chance = 10;
    public bool isLocked;
    public string displayName;
    
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        return resource ? resource.name : "Nothing";
    }
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
        
        // Include ALL entries (even null resources for "nothing")
        var validResources = new System.Collections.Generic.List<ResourceChance>();
        foreach (var resourceChance in resourceChances)
        {
            if (resourceChance.chance > 0) // Only need chance > 0, resource can be null
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
        
        if (totalWeight <= 0f) return validResources[0].resource; // Could be null
        
        // Select random resource based on weights
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var resourceChance in validResources)
        {
            currentWeight += resourceChance.chance;
            if (randomValue <= currentWeight)
            {
                return resourceChance.resource; // Could return null for "nothing"
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
        
        // Separate locked and unlocked entries
        var unlockedEntries = new System.Collections.Generic.List<ResourceChance>();
        int lockedTotal = 0;
        
        foreach (var chance in resourceChances)
        {
            if (chance.isLocked)
            {
                lockedTotal += Mathf.Max(0, chance.chance);
            }
            else
            {
                unlockedEntries.Add(chance);
            }
        }
        
        // If all entries are locked, don't normalize
        if (unlockedEntries.Count == 0) return;
        
        // Calculate remaining percentage for unlocked entries
        int remainingPercentage = Mathf.Max(0, 100 - lockedTotal);
        
        // Calculate the total of unlocked chances
        int unlockedTotal = 0;
        foreach (var chance in unlockedEntries)
        {
            unlockedTotal += Mathf.Max(0, chance.chance);
        }
        
        // If the unlocked total is 0, set equal chances for unlocked entries
        if (unlockedTotal <= 0)
        {
            int equalChance = remainingPercentage / unlockedEntries.Count;
            int remainder = remainingPercentage % unlockedEntries.Count;
            
            for (int i = 0; i < unlockedEntries.Count; i++)
            {
                unlockedEntries[i].chance = equalChance + (i < remainder ? 1 : 0);
            }
        }
        // If the unlocked total doesn't match the remaining percentage, normalize unlocked entries
        else if (unlockedTotal != remainingPercentage)
        {
            int newTotal = 0;
            
            // First pass: calculate normalized values for unlocked entries only
            foreach (var resourceChance in unlockedEntries)
            {
                int normalizedChance = Mathf.RoundToInt((resourceChance.chance / (float)unlockedTotal) * remainingPercentage);
                resourceChance.chance = normalizedChance;
                newTotal += normalizedChance;
            }
            
            // Second pass: adjust for rounding errors to ensure unlocked total = remainingPercentage
            int difference = remainingPercentage - newTotal;
            if (difference != 0 && unlockedEntries.Count > 0)
            {
                // Sort unlocked entries by current chance value (descending) to adjust larger values first
                unlockedEntries.Sort((a, b) => b.chance.CompareTo(a.chance));
                
                // Distribute the difference, ensuring no negative values
                for (int i = 0; i < Mathf.Abs(difference) && i < unlockedEntries.Count; i++)
                {
                    if (difference > 0)
                    {
                        unlockedEntries[i].chance += 1;
                    }
                    else if (unlockedEntries[i].chance > 0) // Only subtract if we won't go negative
                    {
                        unlockedEntries[i].chance -= 1;
                    }
                }
            }
        }
        
        // Final safety check: ensure no negative values in all entries
        foreach (var chance in resourceChances)
        {
            if (chance.chance < 0)
            {
                chance.chance = 0;
            }
        }
    }
    
    #endregion Resource List Handling ---------------------------------------------------------------------------------------
}