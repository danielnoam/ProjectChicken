using KBCore.Refs;
using UnityEngine;
using VInspector;

// Handles chicken shooting mechanics with accurate and radius-based attacks
[RequireComponent(typeof(ChickenController))]
[RequireComponent(typeof(AudioSource))]
public class ChickenAttackBehavior : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private bool enableAttacking = true;
    [SerializeField] private float attackRange = 50f; // Max range to attack player
    [SerializeField] private float minAttackCooldown = 3f; // Minimum time between attacks
    [SerializeField] private float currentAttackCooldown = 3f; // Minimum time between attacks
    [SerializeField] private float maxAttackCooldown = 6f; // Maximum time between attacks
    [SerializeField] private float firstAttackDelay = 5f; // Delay before first attack after entering combat
    [SerializeField] private float firstAttackDelayVariance = 1f; // Random variance for first attack (0 = no variance)
    
    [Header("Accuracy Settings")]
    [SerializeField, Range(0f, 100f)] private float accurateShotChance = 15f; // 15% chance for perfect shot
    [SerializeField] private float inaccuracyRadius = 5f; // Radius around player for inaccurate shots
    
    [Header("Projectile Settings")]
    [SerializeField] private GameObject eggProjectilePrefab; // Egg projectile prefab
    [SerializeField] private float projectileSpeed = 20f; // Speed of the egg
    [SerializeField] private Transform firePoint; // Where to spawn the projectile
    [SerializeField] private float projectileDamage = 10f; // Damage dealt by projectile
    
    [Header("Visual/Audio")]
    [SerializeField] private SOAudioEvent attackSfx; // Sound effect when attacking
    [SerializeField] private ParticleSystem muzzleFlashVFX; // Optional muzzle flash
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private float nextAttackTime = 0f;
    [SerializeField, ReadOnly] private float currentCooldown = 0f;
    [SerializeField, ReadOnly] private bool canAttack = false;
    [SerializeField, ReadOnly] private float distanceToPlayer = 0f;
    [SerializeField, ReadOnly] private bool isFirstAttack = true;
    
    // References
    [SerializeField, Self] private ChickenController chickenController;
    [SerializeField, Self] private AudioSource audioSource;
    [SerializeField] private ProjectileManager projectileManager;
    private Transform playerTransform;
    private RailPlayer player;
    
    // Events
    public event System.Action<Vector3> OnAttack; // Fired position
    
    private void OnValidate()
    {
        this.ValidateRefs();
        
        // Ensure min/max cooldowns are valid
        minAttackCooldown = Mathf.Max(0.1f, minAttackCooldown);
        maxAttackCooldown = Mathf.Max(minAttackCooldown, maxAttackCooldown);
        firstAttackDelayVariance = Mathf.Max(0f, firstAttackDelayVariance);
        
        // Ensure we have a fire point
        if (firePoint == null)
        {
            // Try to find a child named "FirePoint" or use the transform
            Transform foundFirePoint = transform.Find("FirePoint");
            if (foundFirePoint != null)
            {
                firePoint = foundFirePoint;
            }
        }
    }
    
    private void Awake()
    {
        // Find player
        player = FindFirstObjectByType<RailPlayer>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError($"{gameObject.name}: RailPlayer not found!");
        }
        
        if (projectileManager == null)
        {
            projectileManager = FindObjectOfType<ProjectileManager>();
        }
  
        // If no fire point assigned, use this transform
        if (firePoint == null)
        {
            firePoint = transform;
        }
        
        // Initialize attack time to prevent immediate attacks
        float initialDelay = firstAttackDelay + Random.Range(-firstAttackDelayVariance, firstAttackDelayVariance);
        initialDelay = Mathf.Max(0.1f, initialDelay);
        nextAttackTime = Time.time + initialDelay;
        currentCooldown = initialDelay;
    }
    
    private void OnEnable()
    {
        if (chickenController != null)
        {
            chickenController.OnStateChanged += OnStateChanged;
        }
    }
    
    private void OnDisable()
    {
        if (chickenController != null)
        {
            chickenController.OnStateChanged -= OnStateChanged;
        }
    }
    
    private void OnStateChanged(ChickenController.ChickenState oldState, ChickenController.ChickenState newState)
    {
        // Check if we can attack based on state
        canAttack = enableAttacking && newState == ChickenController.ChickenState.InCombat;
        
        // Reset attack timer when entering combat
        if (newState == ChickenController.ChickenState.InCombat && oldState != ChickenController.ChickenState.InCombat)
        {
            isFirstAttack = true;
            // Set the next attack time - first attack uses the firstAttackDelay with variance
            float actualFirstDelay = firstAttackDelay + Random.Range(-firstAttackDelayVariance, firstAttackDelayVariance);
            actualFirstDelay = Mathf.Max(0.1f, actualFirstDelay); // Ensure it's not negative
            nextAttackTime = Time.time + actualFirstDelay;
            currentCooldown = actualFirstDelay;
            
            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name}: Entered combat, will attack in {actualFirstDelay:F1} seconds");
            #endif
        }
    }
    
    private void Update()
    {
        if (!enableAttacking || playerTransform == null || !canAttack) return;
        
        // Update distance to player
        distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Update current cooldown for debug display
        currentCooldown = Mathf.Max(0f, nextAttackTime - Time.time);
        
        // Check if we're in range and ready to attack
        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime && player != null && player.IsAlive() && eggProjectilePrefab != null)
        {
            PerformAttack();
        }
        
        /*// Debug: Show why we're not attacking (only in editor)
        #if UNITY_EDITOR
        if (canAttack && Time.frameCount % 60 == 0) // Log every second
        {
            if (eggProjectilePrefab == null)
            {
                Debug.LogError($"{gameObject.name}: Not attacking - egg projectile prefab not assigned!");
            }
            else if (ProjectileManager.Instance != null && !ProjectileManager.Instance.CanSpawnProjectile())
            {
                Debug.Log($"{gameObject.name}: Not attacking - projectile limit reached!");
            }
            else if (distanceToPlayer > attackRange)
            {
                Debug.Log($"{gameObject.name}: Not attacking - out of range ({distanceToPlayer:F1} > {attackRange})");
            }
            else if (Time.time < nextAttackTime)
            {
                float timeLeft = nextAttackTime - Time.time;
                Debug.Log($"{gameObject.name}: Not attacking - on cooldown ({timeLeft:F1}s left)");
            }
        }
        #endif*/
    }
    
    private void PerformAttack()
    {
        if (!player.IsAlive() || eggProjectilePrefab == null || projectileManager.GetProjectileCount()) return;
        
        // Calculate target position
        Vector3 targetPosition = CalculateTargetPosition();
        
        #if UNITY_EDITOR
        bool wasAccurate = Vector3.Distance(targetPosition, playerTransform.position) < 0.1f;
        Debug.Log($"{gameObject.name}: Attacking! Accurate shot: {wasAccurate}");
        #endif
        
        // Spawn projectile
        SpawnProjectile(targetPosition);
        
        // Play effects
        PlayAttackEffects();
        
        // Fire event
        OnAttack?.Invoke(targetPosition);

        currentCooldown = GetRandomAttackCooldown();
            
        // Reset timer
        isFirstAttack = false;
        nextAttackTime = Time.time + currentAttackCooldown;
    }
    
    private Vector3 CalculateTargetPosition()
    {
        Vector3 playerPosition = playerTransform.position;
        
        // Decide if this is an accurate shot (convert percentage to 0-1 range)
        bool isAccurateShot = Random.Range(0f, 100f) < accurateShotChance;
        
        if (isAccurateShot)
        {
            // Perfect shot - aim directly at player
            return playerPosition;
        }
        else
        {
            // Inaccurate shot - aim within radius around player
            Vector2 randomCircle = Random.insideUnitCircle * inaccuracyRadius;
            Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Apply offset in world space
            return playerPosition + offset;
        }
    }
    
    private void SpawnProjectile(Vector3 targetPosition)
    {
        // Instantiate projectile
        GameObject projectileObj = Instantiate(eggProjectilePrefab, firePoint.position, Quaternion.identity);
        
        // Register with ProjectileManager
        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.RegisterProjectile(projectileObj);
        }
        
        // Calculate direction to target
        Vector3 direction = (targetPosition - firePoint.position).normalized;
        
        // Setup projectile
        EggProjectile projectile = projectileObj.GetComponent<EggProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, projectileSpeed, projectileDamage);
        }
        else
        {
            // Fallback if no EggProjectile script - just add velocity
            Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * projectileSpeed;
            }
        }
        
        // Make projectile face the direction it's moving
        projectileObj.transform.rotation = Quaternion.LookRotation(direction);
    }
    
    private void PlayAttackEffects()
    {
        // Play sound
        if (attackSfx != null && audioSource != null)
        {
            attackSfx.Play(audioSource);
        }
        
        // Play particle effect
        if (muzzleFlashVFX != null)
        {
            muzzleFlashVFX.Play();
        }
    }
    
    // Public methods for external control
    public void SetAttackEnabled(bool enabled)
    {
        enableAttacking = enabled;
    }
    
    public void SetAccuracyChance(float chance)
    {
        accurateShotChance = Mathf.Clamp(chance, 0f, 100f);
    }
    
    public void SetInaccuracyRadius(float radius)
    {
        inaccuracyRadius = Mathf.Max(0f, radius);
    }
    
    public void SetAttackCooldownRange(float min, float max)
    {
        minAttackCooldown = Mathf.Max(0.1f, min);
        maxAttackCooldown = Mathf.Max(minAttackCooldown, max);
    }
    private float GetRandomAttackCooldown()
    {
        return Random.Range(minAttackCooldown, maxAttackCooldown);
    }
    // Force an attack (bypasses cooldown)
    [Button]
    public void ForceAttack()
    {
        if (canAttack && playerTransform != null)
        {
            PerformAttack();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Cannot force attack - not in combat mode or no player found!");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw fire point
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(firePoint.position, 0.2f);
        }
        
        // Draw inaccuracy radius around player (if in play mode)
        if (Application.isPlaying && playerTransform != null && distanceToPlayer <= attackRange)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, inaccuracyRadius);
            
            #if UNITY_EDITOR
            // Show attack info
            if (canAttack)
            {
                Vector3 labelPos = transform.position + Vector3.up * 3f;
                float timeUntilAttack = Mathf.Max(0f, nextAttackTime - Time.time);
                UnityEditor.Handles.Label(labelPos, $"Next Attack: {timeUntilAttack:F1}s");
            }
            #endif
        }
    }
}