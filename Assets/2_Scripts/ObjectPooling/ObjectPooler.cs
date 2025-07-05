using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;
using Object = UnityEngine.Object;


[Serializable]
public class ObjectPool
{
    [Header("Pool Settings")]
    public string poolName = "New Pool";
    [Min(1)] public int maxPoolSize = 25;
    public GameObject prefab;
    public bool dontDestroyOnLoad;
    
    [Header("Pre Warm")]
    public bool preWarmPool = true;
    public int  preWarmPoolSize = 5;


    [SerializeField] private List<GameObject> _activePool;
    [SerializeField] private List<GameObject> _inactivePool;
    private Transform _poolHolder;
    private bool _isInitialized;
    private readonly HashSet<GameObject> _objectsBeingReturned = new HashSet<GameObject>();



    public GameObject GetObjectFromPool(Vector3 position, Quaternion rotation)
    {
        if (!_isInitialized) return null;
        
        if (_inactivePool.Count > 0)
        {
            var obj = _inactivePool[0];
            _inactivePool.RemoveAt(0);
            
            
            _activePool.Add(obj);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }
        else if ((_activePool.Count + _inactivePool.Count) < maxPoolSize)
        {
            var obj = InstantiatePoolObject();
            
            
            _activePool.Add(obj);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }
        else if (_activePool.Count > 0)
        {

            var obj = _activePool[0];
            _activePool.RemoveAt(0);
            obj.SetActive(false);
            
            
            _activePool.Add(obj);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }
        

        return null;
    }
    
    public void ReturnObjectToPool(GameObject obj)
    {
        if (!_isInitialized || !obj) return;
        
        if (_objectsBeingReturned.Contains(obj))
        {
            Debug.LogWarning($"[{poolName}] Object {obj.name} is already being returned to this pool");
            return;
        }

        if (!_activePool.Contains(obj))
        {
            Debug.LogWarning($"[{poolName}] Object {obj.name} not found in active pool");
            return;
        }

        _objectsBeingReturned.Add(obj);
        
        try
        {
            obj.SetActive(false);
            _activePool.Remove(obj);
            _inactivePool.Add(obj);
        }
        finally
        {
            _objectsBeingReturned.Remove(obj);
        }
    }
    
    public bool IsObjectPartOfPool(GameObject obj)
    {
        return _activePool.Contains(obj) || _inactivePool.Contains(obj);
    }
    
    
    
    public void SetUpPool(Transform  poolHolder = null)
    {
        if (_isInitialized) return;
        
        _activePool = new List<GameObject>();
        _inactivePool = new List<GameObject>();
        _poolHolder = poolHolder;
        if (preWarmPool) WarmPool();
        _isInitialized = true;
    }

    public void ClearPools()
    {
        foreach (var obj in _activePool) {
            Object.Destroy(obj);
        }

        foreach (var obj in _inactivePool) {
            Object.Destroy(obj);
        }

        _activePool.Clear();
        _inactivePool.Clear();
        _objectsBeingReturned.Clear();
        if (_poolHolder) Object.Destroy(_poolHolder.gameObject);
        _isInitialized = false;
    }
    
    public void LimitPreWorm()
    {
        preWarmPoolSize = !preWarmPool ? 0 : Mathf.Clamp(preWarmPoolSize, 1, maxPoolSize);
    }
    
    
    
    private void  WarmPool()
    {
        if (_isInitialized) return;
        
        for (int i = 0; i < preWarmPoolSize; i++)
        {
            var obj = InstantiatePoolObject();
            _inactivePool.Add(obj);
        }
    }
    
    private GameObject InstantiatePoolObject()
    {
        if (!prefab) return null;

        var obj = Object.Instantiate(prefab);
        obj.SetActive(false);
        if (_poolHolder) obj.transform.SetParent(_poolHolder);
        return obj;
    }
}




public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool instantiateAsFallBack = true;
    [SerializeField] private bool destroyAsFallBack = true;
    [SerializeField] private List<ObjectPool> pools = new List<ObjectPool>();

    private string _firstSceneName;
    
    
    private void OnValidate()
    {
        foreach (var pool in pools)
        {
            pool.LimitPreWorm();
        }
    }

    
    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
            _firstSceneName =  SceneManager.GetActiveScene().name;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        
        SetUpPools();
    }
    
    
    private static void OnActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
    {

        if (!Instance || newActiveScene.name == Instance._firstSceneName) return;
        
        foreach (var pool in Instance.pools)
        {
            if (!pool.dontDestroyOnLoad)
            {
                pool.ClearPools();
            }
        }
        
        Instance.SetUpPools();
    }


    private void SetUpPools()
    {
        foreach (var pool in pools)
        {
            var poolHolder = new GameObject() { name = $"{pool.poolName} Holder"};
            if (pool.dontDestroyOnLoad) poolHolder.transform.SetParent(transform);
            pool.SetUpPool(poolHolder.transform);
        }
    }

    public static GameObject GetObjectFromPool(GameObject obj, Vector3 positon, Quaternion rotation)
    {
        if (Instance)
        {
            foreach (var pool in Instance.pools)
            {
                if (pool.prefab == obj)
                {
                    return pool.GetObjectFromPool(positon, rotation);
                }
            } 
            
            if (Instance.instantiateAsFallBack)
            {
                // Debug.Log($"No pool found for {obj} was found, instantiating as fall back");
                var fallbackObject = Instantiate(obj, positon, rotation);
                return fallbackObject;
            }
        }
        
        // Debug.LogError($"Can't get object, No object pooler in scene");
        return Instantiate(obj, positon, rotation);
    }
    
    
    public static void ReturnObjectToPool(GameObject obj)
    {
        if (!obj) return;

        if (Instance)
        {
            foreach (var pool in Instance.pools)
            {
                if (pool.IsObjectPartOfPool(obj))
                {
                    pool.ReturnObjectToPool(obj);
                    return;
                }
            }
            
            if (Instance.destroyAsFallBack)
            {
                // Debug.Log($"No pool found for {obj.name}, destroying as fallback");
                Destroy(obj);
                return;
            }
        }
        

        // Debug.LogError($"Can't return object, No object pooler in scene");
        Destroy(obj);
    }

    
    
    

}
