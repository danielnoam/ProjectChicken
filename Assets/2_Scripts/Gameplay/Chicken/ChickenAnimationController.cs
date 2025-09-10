using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ChickenAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public bool showDebugLogs = false;
    public float attackAnimationDuration = 1f; // How long the attack animation should play
    
    [Header("Animation Parameter Names")]
    public string isIdleParam = "IsIdle";
    public string isMovingParam = "IsMoving";
    public string attackTriggerParam = "Attack";
    
    private Animator animator;
    private ChickenStateController stateController;
    private ChickenMovementBehavior movementBehavior;
    
    // Animation state tracking
    private bool isPlayingAttackAnimation = false;
    private float attackAnimationTimer = 0f;
    private ChickenStateController.ChickenState lastState;
    
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
        
        switch (stateController.CurrentState)
        {
            case ChickenStateController.ChickenState.Idle:
            case ChickenStateController.ChickenState.FollowingSlot:
            case ChickenStateController.ChickenState.Concussed:
                shouldBeIdle = true;
                shouldBeMoving = false;
                break;
                
            case ChickenStateController.ChickenState.MovingToSlot:
                shouldBeIdle = false;
                shouldBeMoving = true;
                break;
        }
        
        // For FollowingSlot, check if actually moving
        if (stateController.CurrentState == ChickenStateController.ChickenState.FollowingSlot 
            && movementBehavior != null && movementBehavior.IsActivelyFollowing)
        {
            shouldBeIdle = false;
            shouldBeMoving = true;
        }
        
        // Set animator parameters
        animator.SetBool(isIdleParam, shouldBeIdle);
        animator.SetBool(isMovingParam, shouldBeMoving);
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Animation state - Idle: {shouldBeIdle}, Moving: {shouldBeMoving}");
        }
    }
    
    // Called by combat behavior when shooting eggs
    public void PlayAttackAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning($"ChickenAnimationController on {gameObject.name}: Cannot play attack animation - no Animator!");
            return;
        }
        
        // Trigger attack animation
        animator.SetTrigger(attackTriggerParam);
        
        // Start attack animation timer
        isPlayingAttackAnimation = true;
        attackAnimationTimer = attackAnimationDuration;
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Playing attack animation (duration: {attackAnimationDuration}s)");
        }
    }
    
    // Manual control methods
    public void ForceIdleAnimation()
    {
        if (animator == null) return;
        
        animator.SetBool(isIdleParam, true);
        animator.SetBool(isMovingParam, false);
        isPlayingAttackAnimation = false;
        
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
        isPlayingAttackAnimation = false;
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Forced to moving animation");
        }
    }
    
    public void StopAttackAnimation()
    {
        isPlayingAttackAnimation = false;
        attackAnimationTimer = 0f;
        UpdateAnimationState();
        
        if (showDebugLogs)
        {
            Debug.Log($"ChickenAnimationController on {gameObject.name}: Stopped attack animation");
        }
    }
    
    // Public properties
    public bool IsPlayingAttackAnimation => isPlayingAttackAnimation;
    public float AttackAnimationTimeRemaining => isPlayingAttackAnimation ? attackAnimationTimer : 0f;
    public Animator Animator => animator;
    
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
        
        // Try to get current animator state info
        if (animator.runtimeAnimatorController != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Current Animator State: {stateInfo.shortNameHash}");
            Debug.Log($"Animation Time: {stateInfo.normalizedTime:F2}");
            
            Debug.Log($"Animator Parameters:");
            Debug.Log($"  {isIdleParam}: {animator.GetBool(isIdleParam)}");
            Debug.Log($"  {isMovingParam}: {animator.GetBool(isMovingParam)}");
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
        }
    }
}