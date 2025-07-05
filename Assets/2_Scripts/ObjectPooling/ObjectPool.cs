using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;


[Serializable]
public class ObjectPool
{
    [Header("Pool Settings")]
    public string poolName = "New Pool";
    [Min(1)] public int maxPoolSize = 50;
    public GameObject prefab;
    [Tooltip("Adds the pool to don't destroy list")]
    public bool dontDestroyOnLoad = true;
    [Tooltip("If max pool size reached and there are no objects in inactive pool, recycle the last active object (this is not recommended, objects are notified that they have been reyceled but it is not performent")]
    public bool recycleActiveObjects;
    
    [Header("Pre Warm")]
    [Tooltip("Pre populate the pool")]
    public bool preWarmPool = true;
    public int  preWarmPoolSize = 5;


    [SerializeField] private List<GameObject> _activePool;
    [SerializeField] private List<GameObject> _inactivePool;
    private Transform _poolHolder;
    private bool _isInitialized;
    private readonly HashSet<GameObject> _objectsBeingReturned = new HashSet<GameObject>();
    
    

    public GameObject GetObjectFromPool(Vector3 position, Quaternion rotation)
    {
        if (!_isInitialized)
        {
            Debug.LogError($"[{poolName}] Pool not initialized!");
            return null;
        }
        
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
        else if (recycleActiveObjects && _activePool.Count > 0)
        {

            var obj = _activePool[0];
            _activePool.RemoveAt(0);
            obj.SendMessage("OnPoolRecycle", SendMessageOptions.DontRequireReceiver);
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
    
    
    
    public void SetUpPool(Transform  poolHolder)
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
        if (_activePool != null)
        {
            foreach (var obj in _activePool.Where(obj => obj))
            {
                Object.Destroy(obj);
            }
            _activePool.Clear();
        }
    
        if (_inactivePool != null)
        {
            foreach (var obj in _inactivePool.Where(obj => obj))
            {
                Object.Destroy(obj);
            }
            _inactivePool.Clear();
        }
    
        _objectsBeingReturned?.Clear();
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

