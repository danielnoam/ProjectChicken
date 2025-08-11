using System;
using UnityEngine;
using VInspector;

public class BehaviorNormalMovement : ProjectileBehaviorBase
{
    [SerializeField, Min(0)] private float moveSpeed = 100f;
    
    [Header("Speed Stagger")]
    [SerializeField] private bool useSpeedStagger;
    [SerializeField, Tooltip("For how long the projectile speed be affected by the speed curve")] private float speedStaggerTime = 0.5f;
    [SerializeField] private AnimationCurve staggerSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);


    private const float DistanceToAimPosition = 1.5f;
    private bool _hasPassedAimPosition;
    private float _currentMoveSpeed;
    private Vector3 _moveDirection;
    private Vector3 _lastMoveDirection;
    
    
    public override void OnMovement(PlayerProjectile projectile, RailPlayer owner)
    {
        if (!_hasPassedAimPosition)
        {
            _moveDirection = (projectile.CurrentTargetPosition - projectile.transform.position).normalized;
            float distanceToTarget = Vector3.Distance(projectile.transform.position, projectile.CurrentTargetPosition);
            
            if (distanceToTarget > DistanceToAimPosition)
            {
                _lastMoveDirection = _moveDirection;
            }
        
            if (distanceToTarget <= DistanceToAimPosition)
            {
                _hasPassedAimPosition = true;
                _moveDirection = _lastMoveDirection;
            }
        }

        _currentMoveSpeed = moveSpeed;
    
        if (useSpeedStagger)
        {
            float normalizedStaggerTime = (Time.time - projectile.StartTime) / speedStaggerTime;
            _currentMoveSpeed = moveSpeed * staggerSpeedCurve.Evaluate(normalizedStaggerTime);
        }

        projectile.Rigidbody?.MoveRotation(Quaternion.LookRotation(_moveDirection));
        projectile.Rigidbody?.MovePosition(projectile.Rigidbody.position + _moveDirection * (_currentMoveSpeed * Time.fixedDeltaTime));
    }
    
    
    public override void OnSpawn(PlayerProjectile projectile, RailPlayer owner) { }
    public override void OnCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision) { }
    public override void OnDestroy(PlayerProjectile projectile, RailPlayer owner) { }
}
