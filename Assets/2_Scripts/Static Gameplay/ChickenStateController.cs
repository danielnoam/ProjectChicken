using UnityEngine;

public class ChickenStateController : MonoBehaviour
{
    public enum ChickenState
    {
        Idle,                    // When there are no available slots
        MovingToSlotOnce,        // The first time the chicken moves to the slot
        InCombat,                // When the chicken can start being inside combat mode
        MovingInsideFormation,   // When the chicken moves while already in formation, it can attack in that state
        Concussed,               // When they are hit with the player weapon, the chicken cant attack in that state
        ReturningToSlot          // After the chicken has been concussed it returns to the slot, the chicken cant attack in that time
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
    public bool IsMovingToSlotOnce => currentState == ChickenState.MovingToSlotOnce;
    public bool IsInCombat => currentState == ChickenState.InCombat;
    public bool IsMovingInsideFormation => currentState == ChickenState.MovingInsideFormation;
    public bool IsConcussed => currentState == ChickenState.Concussed;
    public bool IsReturningToSlot => currentState == ChickenState.ReturningToSlot;
    
    // Combined state queries
    public bool CanAttack => IsInCombat || IsMovingInsideFormation;
    public bool IsMoving => IsMovingToSlotOnce || IsMovingInsideFormation || IsReturningToSlot;
    public bool IsInFormation => IsInCombat || IsMovingInsideFormation;

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
    public bool SetMovingToSlotOnce() => ChangeState(ChickenState.MovingToSlotOnce);
    public bool SetInCombat() => ChangeState(ChickenState.InCombat);
    public bool SetMovingInsideFormation() => ChangeState(ChickenState.MovingInsideFormation);
    public bool SetConcussed() => ChangeState(ChickenState.Concussed);
    public bool SetReturningToSlot() => ChangeState(ChickenState.ReturningToSlot);

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
        // - Can't go from Concussed directly to InCombat
        // - Must go through MovingToSlotOnce before InCombat, etc.
        return true;
    }

    // Context menu methods for testing
    [ContextMenu("Set Idle")]
    void ContextMenuSetIdle() => SetIdle();
    
    [ContextMenu("Set Moving To Slot Once")]
    void ContextMenuSetMovingToSlotOnce() => SetMovingToSlotOnce();
    
    [ContextMenu("Set In Combat")]
    void ContextMenuSetInCombat() => SetInCombat();
    
    [ContextMenu("Set Moving Inside Formation")]
    void ContextMenuSetMovingInsideFormation() => SetMovingInsideFormation();
    
    [ContextMenu("Set Concussed")]
    void ContextMenuSetConcussed() => SetConcussed();
    
    [ContextMenu("Set Returning To Slot")]
    void ContextMenuSetReturningToSlot() => SetReturningToSlot();

    [ContextMenu("Print Current State")]
    void ContextMenuPrintState()
    {
        Debug.Log($"Chicken {gameObject.name}: Current State = {currentState}");
        Debug.Log($"  Can Attack: {CanAttack}");
        Debug.Log($"  Is Moving: {IsMoving}");
        Debug.Log($"  Is In Formation: {IsInFormation}");
    }
}