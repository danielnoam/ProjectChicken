using System;
using UnityEngine;

public class BehaviorAimLockMovement : ProjectileBehaviorBase
{
    [SerializeField, Min(0)] private float moveSpeed = 200f;
    [SerializeField, Min(0)] private float straightPhaseDuration = 0.5f;
    [SerializeField, Min(0)] private float bendPhaseDuration = 0.3f;
    [SerializeField, Min(0)] private float targetPhaseDuration = 2f;
    [SerializeField] private bool recheckTarget = true;
    [SerializeField] private int recheckCount = 3;
    [SerializeField] private float recheckRadius = 10f;

    
    private float _startTime;
    private Vector3 _targetPosition;
    private Vector3 _lastTargetDirection;
    private Vector3 _randomBendDirection;
    private Vector3 _currentDirection;
    private Vector3 _bendPhaseStartPosition;
    private Vector3 _targetPhaseStartPosition;
    private bool _hasTarget;
    private int _recheckedTarget;
    private ChickenController _lastTarget;
    private ChickenController _currentTarget;
    
    private enum MovementPhase
    {
        Straight,
        Bend,
        TowardTarget
    }
    
    public override void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner)
    {
        InitializeMovement(projectile);
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
    
    

    private void InitializeMovement(PlayerProjectile projectile)
    {
        _startTime = Time.time;
        _currentDirection = projectile.StartDirection.normalized;
        _lastTargetDirection = _currentDirection;
        
        if (_currentTarget)
        {
            _hasTarget = true;
            _lastTarget = _currentTarget;
        }
        else
        {
            _hasTarget = projectile.Target;
            _lastTarget = projectile.Target;
            _currentTarget = projectile.Target;
        }
        
        // Reset phase positions
        _bendPhaseStartPosition = Vector3.zero;
        _targetPhaseStartPosition = Vector3.zero;
        
        if (_hasTarget && _currentTarget)
        {
            _targetPosition = _currentTarget.transform.position;
        }
        
        // Generate new random bend direction
        GenerateRandomBendDirection();
    }
    

    private void GenerateRandomBendDirection()
    {
        Vector3 perpendicular = Vector3.Cross(_currentDirection, Vector3.up);
        if (perpendicular.magnitude < 0.1f)
        {
            perpendicular = Vector3.Cross(_currentDirection, Vector3.forward);
        }
        
        float randomAngle = UnityEngine.Random.Range(-90f, 90f);
        _randomBendDirection = Quaternion.AngleAxis(randomAngle, _currentDirection) * perpendicular.normalized;
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
        
        if (_hasTarget && _currentTarget)
        {
            // Update target position in case it moved
            _targetPosition = _currentTarget.transform.position;
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
        else if (recheckTarget && _recheckedTarget < recheckCount)
        {
            // Check for target again if it's not set
            ChickenController newTarget = owner.GetTarget(recheckRadius);
            if (newTarget)
            {
                _currentTarget = newTarget;
                _hasTarget = true;
                _lastTarget = newTarget;
                _recheckedTarget = 0;
                InitializeMovement(projectile);
                return projectile.Rigidbody.position; 
            }
            else
            {
                _recheckedTarget += 1;
                return projectile.Rigidbody.position;
            }
        } 
        else
        {
            // continue with last direction
            Vector3 movement = _lastTargetDirection * (moveSpeed * Time.fixedDeltaTime);
            return projectile.Rigidbody.position + movement;
        }
    }
}