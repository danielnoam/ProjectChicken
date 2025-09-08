using System;
using DNExtensions;
using UnityEngine;
using VInspector;

public class ChickenHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    
    [Header("Death Effects")]
    public GameObject deathVFXPrefab; // Particle system prefab to spawn on death
    public SOAudioEvent deathSfx;
    public float deathVFXLifetime = 3f; // How long the death VFX should last
    
    [Header("Damage Settings")]
    public bool canTakeDamage = true;
    public bool showDamageNumbers = true; // For debug - could be used for damage number UI later
    public SOAudioEvent damageSfx;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Component references
    private ChickenStateController stateController;
    private ChickenMovementBehavior movementBehavior;
    private AudioSource audioSource;
    
    // Internal state
    private bool isDead = false;
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;
    
    // Events - other scripts can subscribe to these
    public Action<float> OnHealthChanged; // Called when health changes
    public Action<float> OnDamageTaken; // Called when damage is taken
    public Action OnDeath; // Called when chicken dies
    
    void Start()
    {
        // Get component references
        stateController = GetComponent<ChickenStateController>();
        movementBehavior = GetComponent<ChickenMovementBehavior>();
        audioSource = GetComponent<AudioSource>();
        
        // Initialize health
        currentHealth = maxHealth;
        
        // Validate components
        if (stateController == null)
        {
            Debug.LogWarning($"ChickenHealth on {gameObject.name}: No ChickenStateController found!");
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenHealth on {gameObject.name}: Initialized with {currentHealth}/{maxHealth} HP");
        }
    }
    
    void Update()
    {
        // Handle invulnerability timer
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f)
            {
                isInvulnerable = false;
                if (showDebugLogs)
                    Debug.Log($"ChickenHealth on {gameObject.name}: Invulnerability ended");
            }
        }
    }
    

    
    [Button]
    public void TakeDamage(float damage)
    {
        // Check if we can take damage
        if (!canTakeDamage || isDead || isInvulnerable)
        {
            if (showDebugLogs)
            {
                string reason = !canTakeDamage ? "damage disabled" : 
                               isDead ? "already dead" : "invulnerable";
                Debug.Log($"ChickenHealth on {gameObject.name}: Damage blocked - {reason}");
            }
            return;
        }
        
        // Apply damage
        float actualDamage = Mathf.Max(0f, damage);
        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(0f, currentHealth); // Clamp to 0

        if (showDebugLogs || showDamageNumbers)
        {
            Debug.Log($"ChickenHealth on {gameObject.name}: Took {actualDamage} damage! HP: {currentHealth}/{maxHealth}");
        }

        // Trigger events
        damageSfx?.PlayAtPoint(transform.position);
        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(currentHealth);
        
        // Check if dead
        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    
    public void Die()
    {
        if (isDead)
        {
            if (showDebugLogs)
                Debug.Log($"ChickenHealth on {gameObject.name}: Already dead, ignoring Die() call");
            return;
        }
        
        isDead = true;
        
        if (showDebugLogs)
            Debug.Log($"ChickenHealth on {gameObject.name}: DYING!");
        
        
        // Play death effects
        PlayDeathEffects();
        
        // Trigger death event
        OnDeath?.Invoke();
        
        // Disable components that shouldn't work when dead
        DisableChickenComponents();
        
    }

    
    private void PlayDeathEffects()
    {
        // Play death SFX
        deathSfx?.PlayAtPoint(transform.position);
        
        // Spawn death VFX
        if (deathVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            
            // Destroy VFX after specified lifetime
            if (deathVFXLifetime > 0f)
            {
                Destroy(vfxInstance, deathVFXLifetime);
            }
            
            if (showDebugLogs)
                Debug.Log($"ChickenHealth on {gameObject.name}: Spawned death VFX at {transform.position}");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"ChickenHealth on {gameObject.name}: No death VFX prefab assigned!");
        }
    }
    
    private void DisableChickenComponents()
    {
        // Disable movement
        if (movementBehavior != null)
        {
            movementBehavior.enabled = false;
        }
        
        // Disable state controller transitions
        if (stateController != null)
        {
            stateController.allowStateTransitions = false;
        }
        
        // Disable combat behavior
        ChickenCombatBehaviorV2 combatBehavior = GetComponent<ChickenCombatBehaviorV2>();
        if (combatBehavior != null)
        {
            combatBehavior.enabled = false;
        }
        
        // Disable look at player
        EnemyLookAtPlayer lookAtPlayer = GetComponent<EnemyLookAtPlayer>();
        if (lookAtPlayer != null)
        {
            lookAtPlayer.enabled = false;
        }
        
        // Disable colliders (except triggers that might be needed for death effects)
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger) // Keep trigger colliders for potential death effects
            {
                col.enabled = false;
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"ChickenHealth on {gameObject.name}: Disabled chicken components");
    }
    
    // Public utility methods
    public void Heal(float healAmount)
    {
        if (isDead) return;
        
        float actualHeal = Mathf.Max(0f, healAmount);
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + actualHeal);
        
        float actualHealing = currentHealth - oldHealth;
        
        if (showDebugLogs && actualHealing > 0f)
            Debug.Log($"ChickenHealth on {gameObject.name}: Healed {actualHealing} HP! HP: {currentHealth}/{maxHealth}");
        
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public void SetHealth(float newHealth)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }
    
    public void Revive()
    {
        if (!isDead) return;
        
        isDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        
        // Re-enable components
        if (movementBehavior != null)
        {
            movementBehavior.enabled = true;
        }
        
        if (stateController != null)
        {
            stateController.allowStateTransitions = true;
            stateController.ForceSetState(ChickenStateController.ChickenState.Idle);
        }
        
        ChickenCombatBehaviorV2 combatBehavior = GetComponent<ChickenCombatBehaviorV2>();
        if (combatBehavior != null)
        {
            combatBehavior.enabled = true;
        }
        
        EnemyLookAtPlayer lookAtPlayer = GetComponent<EnemyLookAtPlayer>();
        if (lookAtPlayer != null)
        {
            lookAtPlayer.enabled = true;
        }
        
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
        
        if (showDebugLogs)
            Debug.Log($"ChickenHealth on {gameObject.name}: Revived with full health");
    }
    
    public void SetMaxHealth(float newMaxHealth, bool adjustCurrentHealth = true)
    {
        float oldMaxHealth = maxHealth;
        maxHealth = Mathf.Max(1f, newMaxHealth);
        
        if (adjustCurrentHealth)
        {
            // Scale current health proportionally
            float healthRatio = oldMaxHealth > 0f ? currentHealth / oldMaxHealth : 1f;
            currentHealth = maxHealth * healthRatio;
        }
        else
        {
            // Just clamp current health to new max
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
        
        OnHealthChanged?.Invoke(currentHealth);
        
        if (showDebugLogs)
            Debug.Log($"ChickenHealth on {gameObject.name}: Max health set to {maxHealth}, current health: {currentHealth}");
    }
    
    public void SetInvulnerable(bool invulnerable, float duration = 0f)
    {
        isInvulnerable = invulnerable;
        if (invulnerable && duration > 0f)
        {
            invulnerabilityTimer = duration;
        }
        
        if (showDebugLogs)
            Debug.Log($"ChickenHealth on {gameObject.name}: Invulnerability set to {invulnerable}" + 
                     (duration > 0f ? $" for {duration}s" : ""));
    }
    
    // Public properties
    public float HealthPercentage => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;
    public bool IsFullHealth => currentHealth >= maxHealth;
    public bool IsLowHealth => HealthPercentage <= 0.25f; // Consider 25% or less as "low health"
    
    // Context menu methods for testing
    [ContextMenu("Take 25 Damage")]
    void ContextMenuTakeDamage25() => TakeDamage(25f);
    
    [ContextMenu("Take 50 Damage")]
    void ContextMenuTakeDamage50() => TakeDamage(50f);
    
    [ContextMenu("Take Fatal Damage")]
    void ContextMenuTakeFatalDamage() => TakeDamage(currentHealth + 10f);
    
    [ContextMenu("Heal 25 HP")]
    void ContextMenuHeal25() => Heal(25f);
    
    [ContextMenu("Heal to Full")]
    void ContextMenuHealFull() => SetHealth(maxHealth);
    
    [ContextMenu("Set Invulnerable (5s)")]
    void ContextMenuSetInvulnerable() => SetInvulnerable(true, 5f);
    
    [ContextMenu("Remove Invulnerability")]
    void ContextMenuRemoveInvulnerable() => SetInvulnerable(false);
    
    [ContextMenu("Force Die")]
    void ContextMenuForceDie() => Die();
    


}