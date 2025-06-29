using UnityEngine;
using VInspector;

// Handles the egg projectile behavior
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EggProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float lifetime = 5f; // Time before auto-destroy
    [SerializeField] private bool rotateInFlight = true; // Spin while flying
    [SerializeField] private float rotationSpeed = 360f; // Degrees per second
    [SerializeField] private LayerMask hitLayers = -1; // What can the egg hit
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem trailVFX; // Trail effect
    [SerializeField] private GameObject impactVFXPrefab; // Impact effect prefab
    [SerializeField] private SOAudioEvent impactSfx; // Impact sound
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private float currentSpeed;
    [SerializeField, ReadOnly] private float currentDamage;
    [SerializeField, ReadOnly] private float aliveTime;
    
    // Components
    private Rigidbody rb;
    private Collider col;
    
    // State
    private Vector3 moveDirection;
    private bool isInitialized = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        // Setup rigidbody
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Make sure it's a trigger
        col.isTrigger = true;
    }
    
    public void Initialize(Vector3 direction, float speed, float damage)
    {
        moveDirection = direction.normalized;
        currentSpeed = speed;
        currentDamage = damage;
        isInitialized = true;
        
        // Set velocity
        rb.linearVelocity = moveDirection * currentSpeed;
        
        // Start lifetime countdown
        Destroy(gameObject, lifetime);
    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        aliveTime += Time.deltaTime;
        
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
        RailPlayer player = other.GetComponentInParent<RailPlayer>();
        if (player != null)
        {
            // Deal damage to player
            DamagePlayer(player);
        }
        
        // Play impact effects
        PlayImpactEffects(other.ClosestPoint(transform.position));
        
        // Destroy projectile
        Destroy(gameObject);
    }
    
    private void DamagePlayer(RailPlayer player)
    {
        // Check if player implements IDamageable
        IDamageable damageable = player.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(currentDamage);
        }
        else
        {
            // Fallback: Use SendMessage (less efficient but works)
            player.SendMessage("TakeDamage", currentDamage, SendMessageOptions.DontRequireReceiver);
        }
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
}