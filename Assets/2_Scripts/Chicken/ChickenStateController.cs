using System;
using DNExtensions;
using UnityEngine;

[RequireComponent(typeof(ChickenHealth))]
[RequireComponent(typeof(EnemyChickenRegistration))]
public class ChickenStateController : MonoBehaviour, IPooledObject, ITargetable, IDamageable
{
    [Header("Current State")]
    [SerializeField] private ChickenState currentState = ChickenState.Idle;
    
    [Header("State Settings")]
    public bool allowStateTransitions = true;
    public bool showDebugLogs = false;
    
    [Header("Loot Settings")]
    [SerializeField] private SOLootTable lootTable;
    
    [Header("Score Settings")]
    [SerializeField] private int scoreWorth = 50;
    
    private bool _hasBeenReturnedToPool;
    private ChickenHealth _chickenHealth;
    private EnemyChickenRegistration _registration;

    public Transform Transform => transform;
    public bool IsValidTarget => _chickenHealth.IsAlive();
    
    public ChickenState CurrentState => currentState;
    public SOLootTable LootTable => lootTable;
    public int ScoreWorth => scoreWorth;
    public event Action<ChickenStateController> OnDeathEvent;
    public bool IsIdle => currentState == ChickenState.Idle;
    public bool IsMovingToSlot => currentState == ChickenState.MovingToSlot;
    public bool IsFollowingSlot => currentState == ChickenState.FollowingSlot;
    public bool IsConcussed => currentState == ChickenState.Concussed;
    public bool IsFailsafeMovement => currentState == ChickenState.FailsafeMovement; // NEW
    public bool CanAttack => IsFollowingSlot; // Only when following slot
    public bool IsMoving => IsMovingToSlot || IsFailsafeMovement; // Both states count as moving
    public bool IsInFormation => IsFollowingSlot; // Following slot means in formation
    
    public enum ChickenState
    {
        Idle,               // When there are no available slots or moving to spawn
        MovingToSlot,       // When the chicken is moving to its assigned slot for the first time
        FollowingSlot,      // When the chicken is at its slot and following/tracking slot changes (can attack in this state)
        Concussed,          // When they are hit with the player weapon, the chicken can't attack in this state
        FailsafeMovement    // NEW: When failsafe triggers, chicken moves directly to slot before entering FollowingSlot
    }
    
    private void Awake()
    {
        _chickenHealth = GetComponent<ChickenHealth>();
        _chickenHealth.OnDeath += OnDeath;
        
        _registration = GetComponent<EnemyChickenRegistration>();
    }
    
    void OnEnable() 
    {
        _hasBeenReturnedToPool = false;
    }
    
    public bool ChangeState(ChickenState newState)
    {
        if (!allowStateTransitions)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: State transitions are disabled!");
            return false;
        }

        if (currentState == newState)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Chicken {gameObject.name}: Already in state {newState}");
            return false;
        }

        ChickenState previousState = currentState;
        currentState = newState;

        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: State changed from {previousState} to {newState}");

        return true;
    }

    public bool SetIdle() => ChangeState(ChickenState.Idle);
    public bool SetMovingToSlot() => ChangeState(ChickenState.MovingToSlot);
    public bool SetFollowingSlot() => ChangeState(ChickenState.FollowingSlot);
    public bool SetConcussed() => ChangeState(ChickenState.Concussed);
    public bool SetFailsafeMovement() => ChangeState(ChickenState.FailsafeMovement); // NEW
    
    public void ForceSetState(ChickenState newState)
    {
        currentState = newState;
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: State force-set to {newState}");
    }

    public bool IsValidTransition(ChickenState fromState, ChickenState toState)
    {
        // For now, allow all transitions
        // Later you can add specific rules like:
        // - Can't go from Concussed directly to FollowingSlot
        // - Must go through MovingToSlot before FollowingSlot, etc.
        return true;
    }

    private void OnDeath()
    {
        OnDeathEvent?.Invoke(this);
        ReleaseFromSlot();
        ReturnToPool();
    }
    
    public void TakeDamage(float amount)
    {
        _chickenHealth.TakeDamage(amount);
    }

    public void ApplyForce(Vector3 pushDirection, float pushForce)
    {
        // Implementation here
    }

    public void ApplyStun(float finalStunTime)
    {
        // Implementation here
    }

    private void ReleaseFromSlot()
    {
        if (!_registration.IsRegistered) return;
        
        _registration.UnregisterFromManager();
        if (showDebugLogs) Debug.Log($"ChickenHealth on {gameObject.name}: Released from slot assignment");
    }

    #region Pool Object -------------------------------------------------------------------------

    public void ReturnToPool()
    {
        // Prevent multiple returns to pool
        if (_hasBeenReturnedToPool)
        {
            Debug.LogWarning($"ChickenStateController {gameObject.name}: Already returned to pool, skipping");
            return;
        }
        _hasBeenReturnedToPool = true;
        ObjectPooler.ReturnObjectToPool(gameObject);
    }

    public void OnPoolGet()
    {
        _chickenHealth.Revive();
        _registration.RegisterWithManager();
        
        // Reset animation state when getting from pool
        ChickenAnimationController animController = GetComponent<ChickenAnimationController>();
        if (animController != null)
        {
            animController.OnPoolGet();
        }
    }

    public void OnPoolReturn()
    {
        ReleaseFromSlot();
        
        // Clean up animation state when returning to pool
        ChickenAnimationController animController = GetComponent<ChickenAnimationController>();
        if (animController != null)
        {
            animController.OnPoolReturn();
        }
    }

    public void OnPoolRecycle()
    {
        ReleaseFromSlot();
    }

    #endregion Pool Object -------------------------------------------------------------------------
}