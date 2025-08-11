using DNExtensions;
using UnityEngine;

// Handles the egg projectile behavior
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EggProjectile : MonoBehaviour, IPooledObject
{
    [Header("Projectile Settings")]
    [SerializeField] private float lifetime = 5f; // Time before auto-destroy
    [SerializeField] private bool rotateInFlight = true; // Spin while flying
    [SerializeField] private float rotationSpeed = 360f; // Degrees per second
    [SerializeField] private LayerMask hitLayers = -1; // What can the egg hit
    
    [Header("Visual Effects")]
    [SerializeField] private TrailRenderer trailVRenderer; // Trail effect
    [SerializeField] private GameObject impactVFXPrefab; // Impact effect prefab
    [SerializeField] private SOAudioEvent impactSfx; // Impact sound
    
    [Header("Debug")]
    [SerializeField, VInspector.ReadOnly] private float currentSpeed;
    [SerializeField, VInspector.ReadOnly] private float currentDamage;
    [SerializeField, VInspector.ReadOnly] private float aliveTime;
    
    // Components
    private Rigidbody rb;
    private Collider col;
    
    // State
    private Vector3 moveDirection;
    private bool isInitialized = false;
    private float _currentLifeTime;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        col.isTrigger = true;
        trailVRenderer.emitting = false;
    }
    
    public void Initialize(Vector3 direction, float speed, float damage)
    {
        if (ProjectileManager.Instance)
        {
            ProjectileManager.Instance.RegisterProjectile(gameObject);
        }
        
        moveDirection = direction.normalized;
        currentSpeed = speed;
        currentDamage = damage;
        _currentLifeTime = lifetime;
        rb.linearVelocity = moveDirection * currentSpeed;
        trailVRenderer.emitting = true;
        
        isInitialized = true;

    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        _currentLifeTime -= Time.deltaTime;
        if (_currentLifeTime <= 0f)
        {
            ReturnProjectileToPool();
        }
        
        // Handle rotation
        if (rotateInFlight)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void FixedUpdate()
    {
        if (!isInitialized) return;
        
        // Ensure constant velocity (in case something slows it down)
        rb.linearVelocity = moveDirection * currentSpeed;
    }
    
    private void OnDestroy()
    {
        // Unregister from ProjectileManager
        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.UnregisterProjectile(gameObject);
        }
    }
    

    
    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit something on the hit layers
        if (((1 << other.gameObject.layer) & hitLayers) == 0) return;
        
        // Check if we hit the player
        if (other.TryGetComponent(out RailPlayer player))
        {
            // Deal damage to player
            player.TakeDamage(currentDamage);
        }
        
        // Play impact effects
        PlayImpactEffects(other.ClosestPoint(transform.position));
        

        ReturnProjectileToPool();
    }
    
    
    private void PlayImpactEffects(Vector3 impactPoint)
    {
        // Spawn impact VFX
        if (impactVFXPrefab != null)
        {
            GameObject impact = Instantiate(impactVFXPrefab, impactPoint, Quaternion.identity);
            Destroy(impact, 2f); // Clean up after 2 seconds
        }
        
        // Play impact sound
        if (impactSfx != null)
        {
            impactSfx.PlayAtPoint(impactPoint);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!isInitialized) return;
        
        // Draw velocity direction
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, moveDirection * 2f);
    }
    
    
    
    #region Pool Object -------------------------------------------------------------------------

    private void ReturnProjectileToPool()
    {
        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.UnregisterProjectile(gameObject);
        }
        isInitialized = false;
        trailVRenderer.emitting = false;
        ObjectPooler.ReturnObjectToPool(gameObject);
    }
    
    public void OnPoolGet()
    {

        
    }

    public void OnPoolReturn()
    {

    }

    public void OnPoolRecycle()
    {
        isInitialized = false;
        trailVRenderer.emitting = false;
    }
    
    
    
    

    #endregion Pool Object -------------------------------------------------------------------------


}