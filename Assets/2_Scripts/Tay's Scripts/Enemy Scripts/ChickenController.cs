using UnityEngine;
using System;
using KBCore.Refs;
using VInspector;

// Main controller that manages state and all chicken behaviors

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ChickenFormationBehavior))]
[RequireComponent(typeof(ChickenCombatBehavior))]
[RequireComponent(typeof(ChickenIdleBehavior))]
[RequireComponent(typeof(ChickenLookAtBehavior))]
public class ChickenController : MonoBehaviour
{
    // State enum
    public enum ChickenState
    {
        WaitingForFormation,
        MovingToSpawnPoint,
        AtSpawnPoint,
        MovingToSlot,
        InCombat,
        Concussed,
        ReturningToSlot,
        Idle
    }
    
    [Header("Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private SOLootTable lootTable;
    
    
    [Header("SFXs")]
    [SerializeField] private SOAudioEvent deathSfx;
    [SerializeField] private SOAudioEvent damageSfx;
    [SerializeField] private SOAudioEvent concussedSfx;
    [SerializeField] private SOAudioEvent attackSfx;
    
    
    [Header("Status")]
    [SerializeField, ReadOnly] private ChickenState currentState = ChickenState.WaitingForFormation;
    [SerializeField, ReadOnly] private bool hasSlot = false;
    [SerializeField, ReadOnly] private bool isInCombat = false;
    
    [Header("References")]
    [SerializeField, Self] private ChickenFormationBehavior formationBehavior;
    [SerializeField, Self] private ChickenCombatBehavior combatBehavior;
    [SerializeField, Self] private ChickenIdleBehavior idleBehavior;
    [SerializeField, Self] private ChickenLookAtBehavior lookAtBehavior;
    [SerializeField, Self] private AudioSource audioSource;
    [SerializeField, Self] private Rigidbody rb;
    
    

    
    // State change events
    public event Action<ChickenState, ChickenState> OnStateChanged; // oldState, newState
    public event Action OnEnterCombat;
    public event Action OnExitCombat;
    public event Action OnConcussed;
    public event Action OnRecovered;
    public event Action OnDeath;
    public event Action<float> OnHealthChanged;
    
    // Public properties
    public ChickenState CurrentState => currentState;
    public bool IsInFormation => currentState == ChickenState.InCombat;
    public bool IsWaitingForSlot => currentState == ChickenState.WaitingForFormation;
    public bool IsIdle => currentState == ChickenState.Idle;
    public bool IsAtSpawnPoint => currentState == ChickenState.AtSpawnPoint;
    public bool IsConcussed => currentState == ChickenState.Concussed;
    public bool IsInCombatMode => currentState == ChickenState.InCombat;
    public bool IsMoving => currentState == ChickenState.MovingToSlot || 
                           currentState == ChickenState.MovingToSpawnPoint || 
                           currentState == ChickenState.ReturningToSlot;
    public float CurrentHealth => _currentHealth;
    
    public float MaxHealth => maxHealth;
    
    public float HealthPercentage => _currentHealth / maxHealth;
    
    
    // Behavior properties
    public bool HasAssignedSlot => formationBehavior != null && formationBehavior.HasAssignedSlot;
    public Transform CurrentPlayerTarget => lookAtBehavior != null ? lookAtBehavior.CurrentPlayerTarget : null;
    
    
    // Private properties
    private float _currentHealth;
    

    private void OnValidate()
    {
        this.ValidateRefs();
    }

    private void Awake()
    {
        // Setup rigidbody for space movement
        rb.useGravity = false;
        rb.linearDamping = 1f;
        rb.angularDamping = 2f;
        rb.freezeRotation = true;
        
        // Initialize health
        _currentHealth = maxHealth;
    }
    
    
    
    private void OnDestroy()
    {
        // Ensure the slot is released
        if (formationBehavior != null)
        {
            formationBehavior.ReleaseSlot();
        }
    }
    
    private void Update()
    {
        // Update debug info
        hasSlot = formationBehavior != null && formationBehavior.HasAssignedSlot;
        isInCombat = currentState == ChickenState.InCombat;
    }


    



    #region State Management -----------------------------------------------------------------------------------------------------

        
    // Set new state with validation
    public void SetState(ChickenState newState)
    {
        if (currentState == newState) return;
        
        // Validate transition
        if (!CanTransitionTo(newState))
        {
            Debug.LogWarning($"{gameObject.name}: Invalid state transition from {currentState} to {newState}");
            return;
        }
        
        ChickenState oldState = currentState;
        currentState = newState;

        
        // Fire state change event
        OnStateChanged?.Invoke(oldState, newState);
        
        // Fire specific state events
        if (newState == ChickenState.InCombat)
        {
            OnEnterCombat?.Invoke();
        }
        else if (oldState == ChickenState.InCombat)
        {
            OnExitCombat?.Invoke();
        }


        
        if (newState == ChickenState.Concussed)
        {
            OnConcussed?.Invoke();
            concussedSfx?.Play(audioSource);
        }
        else if (oldState == ChickenState.Concussed && newState == ChickenState.ReturningToSlot)
        {
            OnRecovered?.Invoke();
        }

    }
    
    // State transition validation
    private bool CanTransitionTo(ChickenState newState)
    {
        switch (currentState)
        {
            case ChickenState.Concussed:
                // Can only transition to ReturningToSlot from Concussed
                return newState == ChickenState.ReturningToSlot;
                
            case ChickenState.InCombat:
                // Can transition to Concussed or back to waiting states
                return newState == ChickenState.Concussed || 
                       newState == ChickenState.WaitingForFormation;
                       
            default:
                return true; // Allow most transitions by default
        }
    }
    
    

    #endregion State Management -----------------------------------------------------------------------------------------------------

    
    
    
    
    
    #region Health Management -----------------------------------------------------------------------------------------------------
    
    
    public void TakeDamage(float damage)
    {
        
        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0); // Ensure health doesn't go below 0
        
        // Play damage sound effect
        damageSfx?.Play(audioSource);
        
        // Trigger health changed event
        OnHealthChanged?.Invoke(_currentHealth);
        
        // Check if the enemy should die
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        
        // Trigger death event
        OnDeath?.Invoke();
        
        
        if (formationBehavior != null)
            formationBehavior.ReleaseSlot();


        // Drop a loot resource if available
        if (lootTable)
        {
            Resource loot = lootTable.GetRandomResource();
            lootTable.SpawnResource(loot, transform.position);
        }
        
        // Play death sound effect
        deathSfx?.PlayAtPoint(transform.position);

        
        // Destroy or pool the chicken
        Destroy(gameObject);
    }
    
    
    
    
    public void Heal(float healAmount)
    {
        _currentHealth += healAmount;
        _currentHealth = Mathf.Min(_currentHealth, maxHealth); // Don't exceed max health
        
        OnHealthChanged?.Invoke(_currentHealth);
    }
    

    #endregion  Health Management ----------------------------------------------------------------------------------------------------- 




    #region Public API methods for external systems -----------------------------------------------------------------------------------------------------

    public void ApplyConcussion(float concussDuration)
    {
        if (combatBehavior != null) combatBehavior.ApplyConcussion(concussDuration);
    }
    
    public void ApplyForce(Vector3 direction, float force)
    {
        if (combatBehavior != null) combatBehavior.ApplyForce(direction, force);
    }
    
    public void ApplyTorque(Vector3 torque, float force)
    {
        if (combatBehavior != null) combatBehavior.ApplyTorque(torque, force);
        // Not working yet, need to fix the rigidbody constraints
    }
    
    // Force chicken to find a new slot
    public void ForceReassignSlot()
    {
        if (formationBehavior != null)
        {
            formationBehavior.ReleaseSlot();
            SetState(ChickenState.WaitingForFormation);
        }
    }
    
    // Notify that a slot is available
    public void NotifySlotAvailable()
    {
        if (formationBehavior != null)
            formationBehavior.NotifySlotAvailable();
    }
    
    // Update player reference
    public void SetPlayerTransform(Transform player)
    {
        if (lookAtBehavior != null)
            lookAtBehavior.SetPlayerTransform(player);
    }
    
    public bool HasArrivedAtDestination(Vector3 currentPos, Vector3 targetPos, float threshold)
    {
        return Vector3.Distance(currentPos, targetPos) < threshold;
    }


    #endregion Public API methods for external systems -----------------------------------------------------------------------------------------------------



    #region Debugging and Utility Methods -----------------------------------------------------------------------------------------------------


    
    [Button]
    private void DebugCurrentStatus()
    {
        Debug.Log($"=== {gameObject.name} Status ===");
        Debug.Log($"Has Slot: {hasSlot}");
        Debug.Log($"In Combat: {isInCombat}");
        Debug.Log($"Is Concussed: {IsConcussed}");
        
        if (lookAtBehavior != null)
            Debug.Log($"Player Target: {(lookAtBehavior.CurrentPlayerTarget != null ? lookAtBehavior.CurrentPlayerTarget.name : "None")}");
    }
    
    [Button]
    private void ForceCheckSlots()
    {
        if (idleBehavior != null)
            idleBehavior.ForceCheckSlots();
    }
    
    // Static helper to notify all idle chickens
    public static void NotifyAllIdleChickens()
    {
        var chickens = FindObjectsByType<ChickenController>(FindObjectsSortMode.None);
        foreach (var chicken in chickens)
        {
            if (chicken.IsIdle || chicken.IsAtSpawnPoint)
            {
                chicken.NotifySlotAvailable();
            }
        }
    }
    

    #endregion Debugging and Utility Methods -----------------------------------------------------------------------------------------------------
    

    

}