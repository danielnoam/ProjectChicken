using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using VInspector;
using UnityEngine;
using UnityEngine.Splines;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    
    [Header("Resource Settings")]
    [SerializeField] private Transform resourceHolder;
    [SerializeField] private SerializedDictionary<Resource, float> resources = new SerializedDictionary<Resource, float>();
    
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
    
    
    
    private void SpawnResource(Resource resource, Vector3 position)
    {
        if (!resource) return;
        
        Resource newResource = Instantiate(resource, position, Quaternion.identity, resourceHolder);
    }



    public void SpawnRandomResourceAtPosition(Vector3 position)
    {
        if (resources.Count <= 0) return;
        
        List<Resource> resourceList = new List<Resource>(resources.Keys);
        int randomIndex = Random.Range(1, resourceList.Count);
        Resource randomResource = resourceList[randomIndex];
        
        SpawnResource(randomResource, position);
    }
    


    public void SpawnRandomResourceOnSpline()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.SplineContainer) return;
        
        
        SplineContainer spline = LevelManager.Instance.SplineContainer;
        
    }
}
