using DNExtensions;
using UnityEngine;
using VInspector;



[CreateAssetMenu(fileName = "New LootTable", menuName = "Scriptable Objects/New Loot Table")]
public class SOLootTable : ScriptableObject
{
    [Header("Loot Table")]
    [SerializeField] private ChanceList<Resource> resources = new ChanceList<Resource>();
    
    
    
    private Resource SpawnResource(Resource resource, Vector3 position, Transform parent)
    {
        if (!resource) return null;

        if (parent)
        {
            Resource newResource = Instantiate(resource, position, Quaternion.identity, parent);
            return newResource;
        }
        else
        {
            Resource newResource = Instantiate(resource, position, Quaternion.identity);
            return newResource;
        }
    }

    public void SpawnRandomResource(Vector3 position, Transform parent = null)
    {
        if (resources.Count <= 0) return ;
        
        SpawnResource(resources.GetRandomItem(), position, parent);
    }
    
}