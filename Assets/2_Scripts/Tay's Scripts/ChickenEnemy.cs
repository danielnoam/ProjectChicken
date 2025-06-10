using UnityEngine;

public class ChickenEnemy : MonoBehaviour
{
    [Header("Enemy Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Events for when enemy takes damage or dies
    public System.Action<float> OnHealthChanged;
    public System.Action OnEnemyDeath;
    
    void Start()
    {
        // Initialize current health to max health
        currentHealth = maxHealth;
    }
    
    
    
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0); // Ensure health doesn't go below 0
        
        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name} took {damage} damage. Current health: {currentHealth}/{maxHealth}");
        }
        
        // Trigger health changed event
        OnHealthChanged?.Invoke(currentHealth);
        
        // Check if enemy should die
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name} has died!");
        }
        
        // Trigger death event
        OnEnemyDeath?.Invoke();
        
        // Add death effects here (particles, sound, etc.)
        
        // Destroy the enemy GameObject
        Destroy(gameObject);
    }
    
    // Public methods for external access
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
    }
    
    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Don't exceed max health
        
        OnHealthChanged?.Invoke(currentHealth);
    }
}