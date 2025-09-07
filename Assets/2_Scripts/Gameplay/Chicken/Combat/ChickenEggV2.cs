using System;
using DNExtensions;
using UnityEngine;

public class ChickenEggV2 : MonoBehaviour, IPooledObject
{
    [Header("Egg Settings")]
    public float lifetime = 5f;
    public bool showDebugLogs;

    [Header("References")]
    [SerializeField] private TrailRenderer trailVRenderer;
    
    
    private Vector3 _velocity;
    private float _spawnTime;
    private bool _isInitialized;
    
    

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
        _spawnTime = Time.time;
        _velocity = direction.normalized * speed;
        _isInitialized = true;
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
        _isInitialized = false;
        trailVRenderer.emitting = false;
        ObjectPooler.ReturnObjectToPool(gameObject);
    }
    
    public void OnPoolGet()
    {

        
    }

    public void OnPoolReturn()
    {
        trailVRenderer.emitting = false;
    }

    public void OnPoolRecycle()
    {
        trailVRenderer.emitting = false;
    }
    
    

    #endregion Pool Object -------------------------------------------------------------------------
}