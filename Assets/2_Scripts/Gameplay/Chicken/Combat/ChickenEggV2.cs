using System;
using DNExtensions;
using UnityEngine;

public class ChickenEggV2 : MonoBehaviour, IPooledObject
{
    [Header("Egg Settings")]
    public float lifetime = 5f; // How long the egg exists before destroying itself
    public bool useGravity; // Whether egg should be affected by gravity

    [Header("Debug")]
    public bool showDebugLogs;

    private Vector3 velocity;
    private float spawnTime;
    private bool isInitialized;
    private bool warningCreated = false; // Track if warning was created for this egg
    
    [Header("Trail Settings")]
    [SerializeField] private TrailRenderer trailVRenderer;

    private void Awake()
    {
        spawnTime = Time.time;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = useGravity;
        }
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
        warningCreated = false;
    }

    private void OnDisable()
    {
        // Remove warning when egg is disabled
        RemoveWarning();
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
            DeactivateEgg();
        }
    }

    public void Initialize(Vector3 direction, float speed)
    {
        velocity = direction.normalized * speed;
        isInitialized = true;
        
        // Create warning circle when egg is initialized
        CreateWarning(transform.position, direction, speed);
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
    
    private void DeactivateEgg()
    {
        RemoveWarning();
        gameObject.SetActive(false);
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