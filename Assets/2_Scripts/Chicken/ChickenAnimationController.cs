using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Animator))]
public class ChickenAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public bool showDebugLogs = false;
    public float attackAnimationDuration = 1f; // How long the attack animation should play
    
    [Header("Attack Animation Speed")]
    [Range(1f, 2f)]
    public float minAttackSpeedMultiplier = 1f; // Minimum speed multiplier for attacks
    [Range(1f, 2f)]
    public float maxAttackSpeedMultiplier = 1.5f; // Maximum speed multiplier for attacks
    
    [Header("Animation Parameter Names")]
    public string isIdleParam = "IsIdle";
    public string isMovingParam = "IsMoving";
    public string attackTriggerParam = "Attack";
    public string attackSpeedParam = "AttackSpeed"; // NEW: Parameter to control attack animation speed
    public string isFailsafeParam = "IsFailsafe"; // NEW: Optional parameter for failsafe-specific animations
    
    private Animator animator;
    private ChickenStateController stateController;
    private ChickenMovementBehavior movementBehavior;
    
    // Animation state tracking
    private bool isPlayingAttackAnimation = false;
    private float attackAnimationTimer = 0f;
    private ChickenStateController.ChickenState lastState;
    
    // Speed tracking for attack animations
    private float baseAnimatorSpeed = 1f;
    private float currentAttackSpeedMultiplier = 1f;
    
    
    
    
    void Start()
    {
        // Get required components
        animator = GetComponent<Animator>();
        stateController = GetComponent<ChickenStateController>();
        movementBehavior = GetComponent<ChickenMovementBehavior>();
        
        // Validate components
        if (animator == null)
        {
            Debug.LogError($"ChickenAnimationController on {gameObject.name}: No Animator component found!");
            enabled = false;
            return;
        }
        
        if (stateController == null)
        {
            Debug.LogError($"ChickenAnimationController on {gameObject.name}: No ChickenStateController found!");
            enabled = false;
            return;
        }
        
        // Initialize animation state
        lastState = stateController.CurrentState;
        animator.speed = Random.Range(0.9f, 1.1f); // ±10% speed difference for variety
        
        // Set default attack speed if parameter exists
        if (HasAnimatorParameter(attackSpeedParam))
        {
            animator.SetFloat(attackSpeedParam, 1f);
        }
  
        UpdateAnimationState();
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Initialized with state {lastState}");
        }
    }
    
    void Update()
    {
        // Handle attack animation timer
        if (isPlayingAttackAnimation)
        {
            attackAnimationTimer -= Time.deltaTime;
            if (attackAnimationTimer <= 0f)
            {
                isPlayingAttackAnimation = false;
                
                UpdateAnimationState(); // Return to normal state animation
                
                if (showDebugLogs)
                {
                    Debug.Log($"ChickenAnimationController on {gameObject.name}: Attack animation finished, returning to normal state");
                }
            }
        }
        
        // Check for state changes
        if (stateController.CurrentState != lastState)
        {
            if (showDebugLogs)
            {
                Debug.Log($"ChickenAnimationController on {gameObject.name}: State changed from {lastState} to {stateController.CurrentState}");
            }
            
            lastState = stateController.CurrentState;
            
            // Only update animation if not playing attack animation
            if (!isPlayingAttackAnimation)
            {
                UpdateAnimationState();
            }
        }
    }
    
    void UpdateAnimationState()
    {
        if (animator == null || stateController == null)
            return;
        
        // Determine which animation should play based on state
        bool shouldBeIdle = false;
        bool shouldBeMoving = false;
        bool shouldBeFailsafe = false; // NEW
        
        switch (stateController.CurrentState)
        {
            case ChickenStateController.ChickenState.Idle:
            case ChickenStateController.ChickenState.Concussed:
                shouldBeIdle = true;
                shouldBeMoving = false;
                shouldBeFailsafe = false;
                break;
                
            case ChickenStateController.ChickenState.MovingToSlot:
                shouldBeIdle = false;
                shouldBeMoving = true;
                shouldBeFailsafe = false;
                break;
                
            case ChickenStateController.ChickenState.FailsafeMovement: // NEW
                shouldBeIdle = false;
                shouldBeMoving = true;
                shouldBeFailsafe = true;
                break;
                
            case ChickenStateController.ChickenState.FollowingSlot:
                // For FollowingSlot, check if actually moving
                if (movementBehavior != null && movementBehavior.IsActivelyFollowing)
                {
                    shouldBeIdle = false;
                    shouldBeMoving = true;
                    shouldBeFailsafe = false;
                }
                else
                {
                    shouldBeIdle = true;
                    shouldBeMoving = false;
                    shouldBeFailsafe = false;
                }
                break;
        }
        
        // Set animator parameters
        animator.SetBool(isIdleParam, shouldBeIdle);
        animator.SetBool(isMovingParam, shouldBeMoving);
        
        // Set failsafe parameter if it exists in the animator - NEW
        if (HasAnimatorParameter(isFailsafeParam))
        {
            animator.SetBool(isFailsafeParam, shouldBeFailsafe);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Animation state - Idle: {shouldBeIdle}, Moving: {shouldBeMoving}, Failsafe: {shouldBeFailsafe}");
        }
    }
    
    // NEW: Helper method to check if animator has a parameter
    bool HasAnimatorParameter(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;
            
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
    
    // Called by combat behavior when shooting eggs
    public void PlayAttackAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning($"ChickenAnimationController on {gameObject.name}: Cannot play attack animation - no Animator!");
            return;
        }
        
        // Generate random speed multiplier for this attack
        currentAttackSpeedMultiplier = Random.Range(minAttackSpeedMultiplier, maxAttackSpeedMultiplier);
        
        // Set the attack speed parameter in the animator
        if (HasAnimatorParameter(attackSpeedParam))
        {
            animator.SetFloat(attackSpeedParam, currentAttackSpeedMultiplier);
        }
        else
        {
            Debug.LogWarning($"ChickenAnimationController on {gameObject.name}: Attack speed parameter '{attackSpeedParam}' not found in Animator!");
        }
        
        // Trigger attack animation
        animator.SetTrigger(attackTriggerParam);
        
        // Start attack animation timer
        isPlayingAttackAnimation = true;
        attackAnimationTimer = attackAnimationDuration / currentAttackSpeedMultiplier; // Adjust timer for animation speed
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Playing attack animation (duration: {attackAnimationTimer:F2}s, speed multiplier: {currentAttackSpeedMultiplier:F2})");
        }
    }

    private void OnEnable()
    {
        // Reset animator completely when enabled from pool
        ResetAnimatorCompletely();
    }

    private void OnDisable()
    {
        // Clean up animation state when disabled
        // ResetAnimatorCompletely();
    }

    // NEW: Complete animator reset method
    void ResetAnimatorCompletely()
    {
        if (animator == null) return;
        
        // Reset all triggers
        animator.ResetTrigger(attackTriggerParam);
        
        // Force rebind to reset the animator state machine
        animator.Rebind();
        
        // Update animator immediately to process the rebind
        animator.Update(0f);
        
        // Now force idle animation
        ForceIdleAnimation();
        
        // Reset all tracking variables
        isPlayingAttackAnimation = false;
        attackAnimationTimer = 0f;
        currentAttackSpeedMultiplier = 1f;
        
        // Restore base animator speed
        animator.speed = baseAnimatorSpeed;
        
        if (showDebugLogs)
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Animator completely reset");
    }

    // Manual control methods
    public void ForceIdleAnimation()
    {
        if (animator == null) return;
        
        // Force play the idle state by name instead of current state
        // This ensures we're actually switching to idle, not replaying stuck state
        animator.Play("Idle", 0, 0f);  // Assuming your idle state is named "Idle"
        
        // Alternatively, if you don't know the state name, force a cross fade
        // animator.CrossFadeInFixedTime("Idle", 0.1f);
        
        // Set all parameters
        animator.SetBool(isIdleParam, true);
        animator.SetBool(isMovingParam, false);
        
        // Reset failsafe parameter if it exists
        if (HasAnimatorParameter(isFailsafeParam))
        {
            animator.SetBool(isFailsafeParam, false);
        }
        
        // Reset any triggers that might be stuck
        animator.ResetTrigger(attackTriggerParam);
        
        isPlayingAttackAnimation = false;
        attackAnimationTimer = 0f;
        
        // Restore base speed
        animator.speed = baseAnimatorSpeed;
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Forced to idle animation");
        }
    }
    
    public void ForceMovingAnimation()
    {
        if (animator == null) return;
        
        animator.SetBool(isIdleParam, false);
        animator.SetBool(isMovingParam, true);
        
        // Reset failsafe parameter if it exists - NEW
        if (HasAnimatorParameter(isFailsafeParam))
        {
            animator.SetBool(isFailsafeParam, false);
        }
        
        isPlayingAttackAnimation = false;
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Forced to moving animation");
        }
    }
    
    // NEW: Force failsafe animation
    public void ForceFailsafeAnimation()
    {
        if (animator == null) return;
        
        animator.SetBool(isIdleParam, false);
        animator.SetBool(isMovingParam, true);
        
        // Set failsafe parameter if it exists
        if (HasAnimatorParameter(isFailsafeParam))
        {
            animator.SetBool(isFailsafeParam, true);
        }
        
        isPlayingAttackAnimation = false;
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Forced to failsafe animation");
        }
    }
    
    public void StopAttackAnimation()
    {
        isPlayingAttackAnimation = false;
        attackAnimationTimer = 0f;
        
        // Restore base animator speed
        animator.speed = baseAnimatorSpeed;
        
        UpdateAnimationState();
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Stopped attack animation, restored base speed {baseAnimatorSpeed:F2}");
        }
    }
    

    
    // Public properties
    public bool IsPlayingAttackAnimation => isPlayingAttackAnimation;
    public float AttackAnimationTimeRemaining => isPlayingAttackAnimation ? attackAnimationTimer : 0f;
    public Animator Animator => animator;
    public bool IsInFailsafeAnimation => stateController != null && stateController.IsFailsafeMovement; // NEW
    public float CurrentAttackSpeedMultiplier => currentAttackSpeedMultiplier; // NEW: Expose current attack speed
    
    // Context menu methods for testing
    [ContextMenu("Test Attack Animation")]
    void ContextMenuTestAttackAnimation()
    {
        PlayAttackAnimation();
        Debug.Log($"ChickenAnimationController on {gameObject.name}: Testing attack animation");
    }
    
    [ContextMenu("Force Idle Animation")]
    void ContextMenuForceIdle() => ForceIdleAnimation();
    
    [ContextMenu("Force Moving Animation")]
    void ContextMenuForceMoving() => ForceMovingAnimation();
    
    [ContextMenu("Force Failsafe Animation")] // NEW
    void ContextMenuForceFailsafe() => ForceFailsafeAnimation();
    
    [ContextMenu("Stop Attack Animation")]
    void ContextMenuStopAttack() => StopAttackAnimation();
    
    [ContextMenu("Toggle Debug Logs")]
    void ContextMenuToggleDebugLogs()
    {
        showDebugLogs = !showDebugLogs;
        Debug.Log($"ChickenAnimationController on {gameObject.name}: Debug logs {(showDebugLogs ? "enabled" : "disabled")}");
    }
    
    [ContextMenu("Print Animation State")]
    void ContextMenuPrintAnimationState()
    {
        if (animator == null)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: No Animator component!");
            return;
        }
        
        Debug.Log($"=== ANIMATION STATE INFO ===");
        Debug.Log($"Chicken: {gameObject.name}");
        Debug.Log($"Current Game State: {(stateController != null ? stateController.CurrentState.ToString() : "null")}");
        Debug.Log($"Is Playing Attack: {isPlayingAttackAnimation}");
        Debug.Log($"Attack Time Remaining: {attackAnimationTimer:F2}s");
        Debug.Log($"Is In Failsafe: {IsInFailsafeAnimation}"); // NEW
        Debug.Log($"Base Animator Speed: {baseAnimatorSpeed:F2}");
        Debug.Log($"Current Animator Speed: {animator.speed:F2}");
        Debug.Log($"Current Attack Speed Multiplier: {currentAttackSpeedMultiplier:F2}");
        
        // Try to get current animator state info
        if (animator.runtimeAnimatorController != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Current Animator State: {stateInfo.shortNameHash}");
            Debug.Log($"Animation Time: {stateInfo.normalizedTime:F2}");
            
            Debug.Log($"Animator Parameters:");
            Debug.Log($"  {isIdleParam}: {animator.GetBool(isIdleParam)}");
            Debug.Log($"  {isMovingParam}: {animator.GetBool(isMovingParam)}");
            
            // Check failsafe parameter if it exists - NEW
            if (HasAnimatorParameter(isFailsafeParam))
            {
                Debug.Log($"  {isFailsafeParam}: {animator.GetBool(isFailsafeParam)}");
            }
        }
        else
        {
            Debug.Log("No Animator Controller assigned!");
        }
        
        if (movementBehavior != null)
        {
            Debug.Log($"Movement Info:");
            Debug.Log($"  Is Currently Moving: {movementBehavior.IsCurrentlyMoving}");
            Debug.Log($"  Is Actively Following: {movementBehavior.IsActivelyFollowing}");
            Debug.Log($"  Is In Failsafe Movement: {movementBehavior.IsInFailsafeMovement}"); // NEW
        }
    }
}