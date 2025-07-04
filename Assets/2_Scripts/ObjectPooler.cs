using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using VInspector;
using Object = UnityEngine.Object;


[Serializable]
public class ObjectPool
{
    [Header("Pool Settings")]
    public string poolName = "New Pool";
    [Min(1)] public int maxPoolSize = 10;
    public GameObject prefab;
    
    [Header("Pre Warm")]
    public bool preWarmPool = true;
    [Range(0,10)] public int  preWarmPoolSize = 5;
    


    private List<GameObject> _activePool;
    private List<GameObject> _inactivePool;
    private Transform _poolHolder;
    
    public void SetUpPool(Transform  poolHolder = null)
    {
        _activePool = new List<GameObject>();
        _inactivePool = new List<GameObject>();
        _poolHolder = poolHolder;
        if (preWarmPool) WarmPool();
    }
    
    private void  WarmPool()
    {
        for (int i = 0; i < preWarmPoolSize; i++)
        {
            InstantiatePoolObject();
        }
    }
    
    private GameObject InstantiatePoolObject()
    {
        if (!prefab) return null;
        
        prefab.SetActive(false);

        var obj = Object.Instantiate(prefab);
        obj.SetActive(false);
        _inactivePool.Add(obj);
        if (_poolHolder) obj.transform.SetParent(_poolHolder);

        prefab.SetActive(true);
        return obj;
    }
}




public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }
    
    
    [SerializeField] private List<ObjectPool> pools = new List<ObjectPool> { new ObjectPool() };
    
    
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
        
        SetUpPools();
    }
    
    
    private void SetUpPools()
    {
        foreach (var pool in pools)
        {
            var poolHolder = new GameObject() { name = $"{pool.poolName} Holder"};
            poolHolder.transform.SetParent(transform);
            pool.SetUpPool(poolHolder.transform);
        }
    }
    
    
    
    
    

}
