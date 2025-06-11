using UnityEngine;
using System;

// Central state controller that manages the chicken's current state and transitions
public class ChickenStateControllerOLD : MonoBehaviour
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
    
    // Current state
    [SerializeField] private ChickenState currentState = ChickenState.WaitingForFormation;
    
    // Events for state changes
    public event Action<ChickenState, ChickenState> OnStateChanged; // oldState, newState
    
    // State-specific events
    public event Action OnEnterCombat;
    public event Action OnExitCombat;
    public event Action OnConcussed;
    public event Action OnRecovered;
    
    // Properties
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
    
    // Set new state
    public void SetState(ChickenState newState)
    {
        if (currentState == newState) return;
        
        ChickenState oldState = currentState;
        currentState = newState;
        
        // Fire state change event
        OnStateChanged?.Invoke(oldState, newState);
        
        // Fire specific events
        if (newState == ChickenState.InCombat)
            OnEnterCombat?.Invoke();
        else if (oldState == ChickenState.InCombat)
            OnExitCombat?.Invoke();
            
        if (newState == ChickenState.Concussed)
            OnConcussed?.Invoke();
        else if (oldState == ChickenState.Concussed && newState == ChickenState.ReturningToSlot)
            OnRecovered?.Invoke();
    }
    
    // State transition validation
    public bool CanTransitionTo(ChickenState newState)
    {
        // Add state transition rules here if needed
        // For example, can't go directly from Idle to InCombat without MovingToSlot first
        
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
    
    // Helper method to check if chicken has arrived at destination
    public bool HasArrivedAtDestination(Vector3 currentPos, Vector3 targetPos, float threshold)
    {
        return Vector3.Distance(currentPos, targetPos) < threshold;
    }
}