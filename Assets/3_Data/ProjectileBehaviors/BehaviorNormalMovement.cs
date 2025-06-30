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

    public override void OnSpawn(PlayerProjectile projectile, RailPlayer owner)
    {
        _startTime = Time.time;
        
        if (useAimAssist && projectile.Target)
        {
            _previousDistanceToTarget = Vector3.Distance(projectile.transform.position, projectile.Target.transform.position);
            _hasPassedTarget = false;
        }
    }

    public override void OnMovement(PlayerProjectile projectile, RailPlayer owner)
    {

        Vector3 moveDirection = projectile.StartDirection;
        float currentMoveSpeed = moveSpeed;
        

        if (useSpeedStagger)
        {
            float normalizedStaggerTime = (Time.time - _startTime) / speedStaggerTime;
            currentMoveSpeed = moveSpeed * staggerSpeedCurve.Evaluate(normalizedStaggerTime);
        }
        

        if (useAimAssist && projectile.Target && !_hasPassedTarget)
        {

            float currentDistance = Vector3.Distance(projectile.transform.position, projectile.Target.transform.position);
            if (currentDistance > _previousDistanceToTarget)
            {
                _hasPassedTarget = true;
            }
            else
            {
                Vector3 targetDirection = (projectile.Target.transform.position - projectile.transform.position).normalized;
                moveDirection = Vector3.Lerp(projectile.StartDirection, targetDirection, aimAssistStrength / 100f);
                
                projectile.Rigidbody?.MoveRotation(Quaternion.LookRotation(moveDirection));
            }
            
            _previousDistanceToTarget = currentDistance;
        }
        
        projectile.Rigidbody?.MovePosition(projectile.Rigidbody.position + moveDirection * (currentMoveSpeed * Time.fixedDeltaTime));
    }

    public override void OnCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision) { }
    public override void OnDestroy(PlayerProjectile projectile, RailPlayer owner) { }
    public override void OnDrawGizmos(PlayerProjectile projectile, RailPlayer owner) { }
}