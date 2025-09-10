using System;
using DNExtensions;
using UnityEngine;

public class ChickenEggV2 : MonoBehaviour, IPooledObject
{
    [Header("Egg Settings")]
    public float lifetime = 5f; 
    [SerializeField] private TrailRenderer trailVRenderer;
    public bool showDebugLogs;

    private Vector3 _velocity;
    private float _spawnTime;
    private bool _isInitialized;
    private bool _warningCreated;
    
    private void Update()
    {
        // Move the egg if initialized
        if (!_isInitialized) return;
            
        
        transform.position += _velocity * Time.deltaTime;

        // Destroy after lifetime
        if (Time.time - _spawnTime >= lifetime)
        {
            if (showDebugLogs) Debug.Log($"Egg {gameObject.name}: Destroyed after {lifetime} seconds");
            ReturnProjectileToPool();
        }
    }

    public void Initialize(Vector3 direction, float speed)
    {
        _velocity = direction.normalized * speed;
        _isInitialized = true;
    }

    public void Initialize(Vector3 direction, float speed, bool skipWarning)
    {
        _spawnTime = Time.time;
        _velocity = direction.normalized * speed;
        _isInitialized = true;

        if (!skipWarning && !_warningCreated)
        {
            CreateWarning(transform.position, direction, speed);
        }
        else if (skipWarning)
        {
            _warningCreated = true; // Mark as created to prevent duplicate warnings
            if (showDebugLogs)
                Debug.Log($"Egg {gameObject.name}: Skipped automatic warning creation");
        }
    }
    
    private void CreateWarning(Vector3 startPosition, Vector3 direction, float speed)
    {
        if (_warningCreated) return; // Prevent duplicate warnings
        
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.CreateWarning(this, startPosition, direction, speed);
            _warningCreated = true;
            
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
        if (!_warningCreated) return;
        
        if (EggWarningSystem.Instance != null)
        {
            EggWarningSystem.Instance.RemoveWarning(this);
            
            if (showDebugLogs) 
                Debug.Log($"Egg {gameObject.name}: Removed warning circle");
        }
        
        _warningCreated = false;
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
        if (_isInitialized)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + _velocity.normalized * 2f);
        }
    }
    
    
    
    #region Pool Object -------------------------------------------------------------------------

    private void ReturnProjectileToPool()
    {
        RemoveWarning(); // Make sure warning is removed
        _isInitialized = false;
        trailVRenderer.emitting = false;
        ObjectPooler.ReturnObjectToPool(gameObject);
    }
    
    public void OnPoolGet()
    {
        _spawnTime = Time.time;
        _warningCreated = false;
        trailVRenderer.emitting = true;
    }

    public void OnPoolReturn()
    {
        RemoveWarning(); // Ensure warning is cleaned up
        trailVRenderer.emitting = false;
        _isInitialized = false;
    }

    public void OnPoolRecycle()
    {
        RemoveWarning(); // Ensure warning is cleaned up
        trailVRenderer.emitting = false;
        _isInitialized = false;
    }
    
    

    #endregion Pool Object -------------------------------------------------------------------------
}