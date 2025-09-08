using System;
using DNExtensions;
using UnityEngine;

public class ChickenEggV2 : MonoBehaviour, IPooledObject
{
    [Header("Egg Settings")]
    public float lifetime = 5f; // How long the egg exists before destroying itself

    [Header("Debug")]
    public bool showDebugLogs;

    private Vector3 velocity;
    private float spawnTime;
    private bool isInitialized;
    private bool warningCreated = false; // Track if warning was created for this egg
    
    [Header("Trail Settings")]
    [SerializeField] private TrailRenderer trailVRenderer;
    private void OnEnable()
    {
        spawnTime = Time.time;
        warningCreated = false;
    }

    private void OnDisable()
    {
        OnPoolRecycle();
    }

    private void Update()
    {
        // Move the egg if initialized
        if (isInitialized)
        {
            transform.position += velocity * Time.deltaTime;
        }

        // Destroy after lifetime
        if (Time.time - spawnTime >= lifetime)
        {
            if (showDebugLogs) Debug.Log($"Egg {gameObject.name}: Destroyed after {lifetime} seconds");
            ReturnProjectileToPool();
        }
    }

    public void Initialize(Vector3 direction, float speed)
    {
        velocity = direction.normalized * speed;
        isInitialized = true;

        // Only create warning if it hasn't been created already
        // (Square formation attacks create warnings manually)
        if (!warningCreated)
        {
            CreateWarning(transform.position, direction, speed);
        }
    }
    /// <summary>
    /// Initialize without creating a warning (for formation attacks that handle warnings manually)
    /// </summary>
    /// <param name="direction">Direction of movement</param>
    /// <param name="speed">Speed of the egg</param>
    /// <param name="skipWarning">If true, skips automatic warning creation</param>
    public void Initialize(Vector3 direction, float speed, bool skipWarning)
    {
        spawnTime = Time.time;
        velocity = direction.normalized * speed;
        isInitialized = true;

        if (!skipWarning && !warningCreated)
        {
            CreateWarning(transform.position, direction, speed);
        }
        else if (skipWarning)
        {
            warningCreated = true; // Mark as created to prevent duplicate warnings
            if (showDebugLogs)
                Debug.Log($"Egg {gameObject.name}: Skipped automatic warning creation");
        }
    }
    private void CreateWarning(Vector3 startPosition, Vector3 direction, float speed)
    {
        if (warningCreated) return; // Prevent duplicate warnings
        
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.CreateWarning(this, startPosition, direction, speed);
            warningCreated = true;
            
            if (showDebugLogs) 
                Debug.Log($"Egg {gameObject.name}: Created warning circle");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"Egg {gameObject.name}: EggWarningSystem instance not found!");
        }
    }
    
    private void RemoveWarning()
    {
        if (!warningCreated) return;
        
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.RemoveWarning(this);
            
            if (showDebugLogs) 
                Debug.Log($"Egg {gameObject.name}: Removed warning circle");
        }
        
        warningCreated = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.TryGetComponent(out ShieldHitMovement shieldHitMovement))
        {
            shieldHitMovement.HitShield(transform.position);
        }
        
        if (other.TryGetComponent(out RailPlayer player))
        {
            if (showDebugLogs) Debug.Log($"Egg {gameObject.name}: Hit player {other.gameObject.name}");
            player.Health.TakeDamage(25);
            ReturnProjectileToPool();
        }
    }

    private void OnDrawGizmos()
    {
        if (isInitialized)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + velocity.normalized * 2f);
        }
    }
    
    
    
    #region Pool Object -------------------------------------------------------------------------

    private void ReturnProjectileToPool()
    {
        RemoveWarning(); // Make sure warning is removed
        isInitialized = false;
        trailVRenderer.emitting = false;
        ObjectPooler.ReturnObjectToPool(gameObject);
    }
    
    public void OnPoolGet()
    {
        warningCreated = false;
        trailVRenderer.emitting = true;
    }

    public void OnPoolReturn()
    {
        RemoveWarning(); // Ensure warning is cleaned up
        trailVRenderer.emitting = false;
        isInitialized = false;
    }

    public void OnPoolRecycle()
    {
        RemoveWarning(); // Ensure warning is cleaned up
        trailVRenderer.emitting = false;
        isInitialized = false;
    }
    
    

    #endregion Pool Object -------------------------------------------------------------------------
}