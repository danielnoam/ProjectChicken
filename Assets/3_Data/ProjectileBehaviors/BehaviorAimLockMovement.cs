using System;
using UnityEngine;

public class BehaviorAimLockMovement : ProjectileBehaviorBase
{
    [SerializeField, Min(0)] private float moveSpeed = 200f;
    [SerializeField, Min(0)] private float straightPhaseDuration = 0.5f;
    [SerializeField, Min(0)] private float bendPhaseDuration = 0.3f;
    [SerializeField, Min(0)] private float targetPhaseDuration = 2f;
    [SerializeField] private bool recheckTarget = true;
    [SerializeField] private float recheckRadius = 10f;

    
    private float _startTime;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private Vector3 _lastTargetDirection;
    private Vector3 _randomBendDirection;
    private Vector3 _currentDirection;
    private Vector3 _bendPhaseStartPosition;
    private Vector3 _targetPhaseStartPosition;
    private bool _hasTarget;
    private bool _recheckedTarget;
    
    private enum MovementPhase
    {
        Straight,
        Bend,
        TowardTarget
    }
    
    public override void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner)
    {
        _startTime = Time.time;
        _startPosition = projectile.transform.position;
        _currentDirection = projectile.StartDirection.normalized;
        _lastTargetDirection = _currentDirection;
        _hasTarget = projectile.Target;
        
        if (_hasTarget)
        {
            _targetPosition = projectile.Target.transform.position;
        }
        
        // Generate random bend direction (perpendicular to current direction for more natural curve)
        Vector3 perpendicular = Vector3.Cross(_currentDirection, Vector3.up);
        if (perpendicular.magnitude < 0.1f) // Handle case where direction is parallel to up
        {
            perpendicular = Vector3.Cross(_currentDirection, Vector3.forward);
        }
        
        float randomAngle = UnityEngine.Random.Range(-90f, 90f);
        _randomBendDirection = Quaternion.AngleAxis(randomAngle, _currentDirection) * perpendicular.normalized;
    }

    public override void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner)
    {
        float elapsedTime = Time.time - _startTime;
        MovementPhase currentPhase = GetCurrentPhase(elapsedTime);
        Vector3 newPosition = projectile.transform.position;
        
        switch (currentPhase)
        {
            case MovementPhase.Straight:
                newPosition = MoveStraight(projectile, elapsedTime);
                break;
                
            case MovementPhase.Bend:
                newPosition = MoveBend(projectile, elapsedTime);
                break;
                
            case MovementPhase.TowardTarget:
                newPosition = MoveTowardTarget(projectile, owner, elapsedTime);
                break;
        }
        
        projectile.Rigidbody?.MovePosition(newPosition);
    }
    

    public override void OnBehaviorCollision(PlayerProjectile projectile, RailPlayer owner , ChickenController collision)
    {

    }

    public override void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner )
    {

    }

    public override void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner )
    {

    }
    
    
    
    private MovementPhase GetCurrentPhase(float elapsedTime)
    {
        if (elapsedTime < straightPhaseDuration)
        {
            return MovementPhase.Straight;
        }
        else if (elapsedTime < straightPhaseDuration + bendPhaseDuration)
        {
            return MovementPhase.Bend;
        }
        else
        {
            return MovementPhase.TowardTarget;
        }
    }
    
    private Vector3 MoveStraight(PlayerProjectile projectile, float elapsedTime)
    {
        Vector3 movement = _currentDirection * (moveSpeed * Time.fixedDeltaTime);
        return projectile.Rigidbody.position + movement;
    }
    
    private Vector3 MoveBend(PlayerProjectile projectile, float elapsedTime)
    {
        // Store position at start of bend phase
        if (elapsedTime >= straightPhaseDuration && _bendPhaseStartPosition == Vector3.zero)
        {
            _bendPhaseStartPosition = projectile.transform.position;
        }
        
        float bendProgress = (elapsedTime - straightPhaseDuration) / bendPhaseDuration;
        bendProgress = Mathf.Clamp01(bendProgress);
        
        // Smoothly transition from straight direction to bend direction
        Vector3 blendedDirection = Vector3.Slerp(_currentDirection, _randomBendDirection, bendProgress);
        Vector3 movement = blendedDirection * (moveSpeed * Time.fixedDeltaTime);
        
        // Update current direction for next phase
        _currentDirection = blendedDirection;
        
        return projectile.Rigidbody.position + movement;
    }
    
    private Vector3 MoveTowardTarget(PlayerProjectile projectile, RailPlayer owner, float elapsedTime)
    {
        // Store position at start of target phase
        if (elapsedTime >= straightPhaseDuration + bendPhaseDuration && _targetPhaseStartPosition == Vector3.zero)
        {
            _targetPhaseStartPosition = projectile.transform.position;
        }
        
        if (_hasTarget && projectile.Target)
        {
            // Update target position in case it moved
            _targetPosition = projectile.Target.transform.position;
            _lastTargetDirection = (_targetPosition - projectile.transform.position).normalized;
            
            float targetProgress = (elapsedTime - straightPhaseDuration - bendPhaseDuration) / targetPhaseDuration;
            targetProgress = Mathf.Clamp01(targetProgress);
            
            // Smoothly transition from bend direction to target direction
            Vector3 directionToTarget = (_targetPosition - projectile.transform.position).normalized;
            Vector3 blendedDirection = Vector3.Slerp(_currentDirection, directionToTarget, targetProgress);
            
            _currentDirection = blendedDirection;
            
            Vector3 movement = blendedDirection * (moveSpeed * Time.fixedDeltaTime);
            return projectile.Rigidbody.position + movement;
        }
        else if (recheckTarget && !_recheckedTarget)
        {
            // Check for target again if it's not set
            _hasTarget = owner.GetTarget(recheckRadius);
            _recheckedTarget = true;
            _startTime = Time.time;
            return projectile.Rigidbody.position;
        } 
        else
        {
            // No target - continue with  direction
            Vector3 movement = _lastTargetDirection * (moveSpeed * Time.fixedDeltaTime);
            return projectile.Rigidbody.position + movement;
        }
    }
}