using System;
using UnityEngine;
using VInspector;

public class BehaviorNormalMovement : ProjectileBehaviorBase
{
    [SerializeField, Min(0)] private float moveSpeed = 100f;
    
    [Header("Aim Assist")]
    [SerializeField] private bool useAimAssist = true;
    [SerializeField, Range(0, 100)] private int aimAssistStrength = 10;
    
    [Header("Speed Stagger")]
    [SerializeField] private bool useSpeedStagger;
    [SerializeField, Tooltip("For how long the projectile speed be affected by the speed curve")] private float speedStaggerTime = 0.5f;
    [SerializeField] private AnimationCurve staggerSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);
    


    private float _startTime;
    private float _previousDistanceToTarget;
    private bool _hasPassedTarget;

    public override void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner)
    {
        // Initialize properties
        _startTime = Time.time;
        
        if (useAimAssist && projectile.Target)
        {
            _previousDistanceToTarget = Vector3.Distance(projectile.transform.position, projectile.Target.transform.position);
            _hasPassedTarget = false;
        }
    }

    public override void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner)
    {

        Vector3 moveDirection = projectile.StartDirection;
        float currentMoveSpeed = moveSpeed;
        
        // Handle speed stagger
        if (useSpeedStagger)
        {
            float normalizedStaggerTime = (Time.time - _startTime) / speedStaggerTime;
            currentMoveSpeed = moveSpeed * staggerSpeedCurve.Evaluate(normalizedStaggerTime);
        }
        
        // Handle aim assist
        if (useAimAssist && projectile.Target && !_hasPassedTarget)
        {
            // Check if we passed the target
            float currentDistance = Vector3.Distance(projectile.transform.position, projectile.Target.transform.position);
            if (currentDistance > _previousDistanceToTarget)
            {
                _hasPassedTarget = true;
            }
            else
            {
                // Calculate aim-assisted direction
                Vector3 targetDirection = (projectile.Target.transform.position - projectile.transform.position).normalized;
                moveDirection = Vector3.Lerp(projectile.StartDirection, targetDirection, aimAssistStrength / 100f);
                
                projectile.Rigidbody?.MoveRotation(Quaternion.LookRotation(moveDirection));
            }
            
            _previousDistanceToTarget = currentDistance;
        }
        
        // Apply movement
        projectile.Rigidbody?.MovePosition(projectile.Rigidbody.position + moveDirection * (currentMoveSpeed * Time.fixedDeltaTime));
    }

    public override void OnBehaviorCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision) { }
    public override void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner) { }
    public override void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner) { }
}