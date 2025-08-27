using UnityEngine;

public class ChickenStateController : MonoBehaviour
{
    public enum ChickenState
    {
        Idle,            // When there are no available slots or moving to spawn
        MovingToSlot,    // When the chicken is moving to its assigned slot for the first time
        FollowingSlot,   // When the chicken is at its slot and following/tracking slot changes (can attack in this state)
        Concussed        // When they are hit with the player weapon, the chicken can't attack in this state
    }

    [Header("Current State")]
    [SerializeField] private ChickenState currentState = ChickenState.Idle;
    
    [Header("State Settings")]
    public bool allowStateTransitions = true;
    public bool showDebugLogs = false;

    // Properties
    public ChickenState CurrentState => currentState;
    
    // State queries
    public bool IsIdle => currentState == ChickenState.Idle;
    public bool IsMovingToSlot => currentState == ChickenState.MovingToSlot;
    public bool IsFollowingSlot => currentState == ChickenState.FollowingSlot;
    public bool IsConcussed => currentState == ChickenState.Concussed;
    
    // Combined state queries
    public bool CanAttack => IsFollowingSlot; // Only when following slot
    public bool IsMoving => IsMovingToSlot; // Only MovingToSlot counts as moving now
    public bool IsInFormation => IsFollowingSlot; // Following slot means in formation

    // Main method to change state
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

    // Specific state change methods for cleaner code
    public bool SetIdle() => ChangeState(ChickenState.Idle);
    public bool SetMovingToSlot() => ChangeState(ChickenState.MovingToSlot);
    public bool SetFollowingSlot() => ChangeState(ChickenState.FollowingSlot);
    public bool SetConcussed() => ChangeState(ChickenState.Concussed);

    // Force set state without validation (useful for initialization)
    public void ForceSetState(ChickenState newState)
    {
        currentState = newState;
        if (showDebugLogs)
            Debug.Log($"Chicken {gameObject.name}: State force-set to {newState}");
    }

    // Check if a state transition is valid (can be customized later)
    public bool IsValidTransition(ChickenState fromState, ChickenState toState)
    {
        // For now, allow all transitions
        // Later you can add specific rules like:
        // - Can't go from Concussed directly to FollowingSlot
        // - Must go through MovingToSlot before FollowingSlot, etc.
        return true;
    }

    // Context menu methods for testing
    [ContextMenu("Set Idle")]
    void ContextMenuSetIdle() => SetIdle();
    
    [ContextMenu("Set Moving To Slot")]
    void ContextMenuSetMovingToSlot() => SetMovingToSlot();
    
    [ContextMenu("Set Following Slot")]
    void ContextMenuSetFollowingSlot() => SetFollowingSlot();
    
    [ContextMenu("Set Concussed")]
    void ContextMenuSetConcussed() => SetConcussed();

    [ContextMenu("Print Current State")]
    void ContextMenuPrintState()
    {
        Debug.Log($"Chicken {gameObject.name}: Current State = {currentState}");
        Debug.Log($"  Can Attack: {CanAttack}");
        Debug.Log($"  Is Moving: {IsMoving}");
        Debug.Log($"  Is In Formation: {IsInFormation}");
    }
}