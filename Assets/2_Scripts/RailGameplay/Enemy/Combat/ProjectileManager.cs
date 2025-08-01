using System.Collections.Generic;
using UnityEngine;
using VInspector;

// Manages all projectiles in the scene and enforces limits
public class ProjectileManager : MonoBehaviour
{
    // Singleton instance
    public static ProjectileManager Instance { get; private set; }
    
    [Header("Projectile Limits")]
    [SerializeField] private int maxProjectiles = 30; // Maximum number of projectiles allowed at once
    [SerializeField] private bool logProjectileCount = false; // Debug logging
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private int currentProjectileCount = 0;
    [SerializeField, ReadOnly] private List<GameObject> activeProjectiles = new List<GameObject>();
    
    // Events
    public event System.Action<int> OnProjectileCountChanged;
    public event System.Action OnProjectileLimitReached;
    
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
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // Check if a new projectile can be spawned
    public bool CanSpawnProjectile()
    {
        CleanupNullProjectiles();
        bool canSpawn = currentProjectileCount < maxProjectiles;
        
        if (!canSpawn)
        {
            OnProjectileLimitReached?.Invoke();
            
            #if UNITY_EDITOR
            if (logProjectileCount)
            {
                Debug.LogWarning($"Projectile limit reached! ({currentProjectileCount}/{maxProjectiles})");
            }
            #endif
        }
        
        return canSpawn;
    }
    
    // Register a new projectile
    public void RegisterProjectile(GameObject projectile)
    {
        if (projectile == null) return;
        
        if (!activeProjectiles.Contains(projectile))
        {
            activeProjectiles.Add(projectile);
            currentProjectileCount = activeProjectiles.Count;
            OnProjectileCountChanged?.Invoke(currentProjectileCount);
            
            #if UNITY_EDITOR
            if (logProjectileCount)
            {
                // Debug.Log($"Projectile registered. Count: {currentProjectileCount}/{maxProjectiles}");
            }
            #endif
        }
    }
    
    // Unregister a projectile
    public void UnregisterProjectile(GameObject projectile)
    {
        if (projectile == null) return;
        
        if (activeProjectiles.Remove(projectile))
        {
            currentProjectileCount = activeProjectiles.Count;
            OnProjectileCountChanged?.Invoke(currentProjectileCount);
            
            #if UNITY_EDITOR
            if (logProjectileCount)
            {
                // Debug.Log($"Projectile unregistered. Count: {currentProjectileCount}/{maxProjectiles}");
            }
            #endif
        }
    }
    
    // Clean up null references
    private void CleanupNullProjectiles()
    {
        activeProjectiles.RemoveAll(p => p == null);
        currentProjectileCount = activeProjectiles.Count;
    }
    
    // Get current projectile count
    public bool GetProjectileCount()
    {
        if (activeProjectiles.Count < maxProjectiles)
        {
            return false;
        }
        return true;
    }
    
    // Set max projectile limit
    public void SetMaxProjectiles(int max)
    {
        maxProjectiles = Mathf.Max(1, max);
    }
    
    // Clear all projectiles
    [Button]
    public void ClearAllProjectiles()
    {
        foreach (var projectile in activeProjectiles)
        {
            if (projectile != null)
            {
                Destroy(projectile);
            }
        }
        
        activeProjectiles.Clear();
        currentProjectileCount = 0;
        OnProjectileCountChanged?.Invoke(0);
        
        Debug.Log("All projectiles cleared!");
    }
}