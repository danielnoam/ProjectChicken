using System;
using UnityEngine;

public class BehaviorAssistedMovement : ProjectileBehaviorBase
{
    [Header("Assisted Movement Settings")]
    [SerializeField, Min(0)] private float duration = 2f;
    [SerializeField] private AnimationCurve positionOverTime = AnimationCurve.Linear(0, 0, 1, 1);
    
    private float _startTime;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    
    public override void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {
        _startTime = Time.time;
        _startPosition = projectile.transform.position;
        _targetPosition = target.transform.position;
    }

    public override void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {
        // Calculate elapsed time and progress
        float elapsedTime = Time.time - _startTime;
        float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
        
        // Evaluate the curve to get the interpolation factor
        float curveValue = positionOverTime.Evaluate(normalizedTime);
        
        // Update target position in case the target has moved
        _targetPosition = target.transform.position;
        
        // Interpolate between start and target positions using the curve
        Vector3 newPosition = Vector3.Lerp(_startPosition, _targetPosition, curveValue);
        projectile.transform.position = newPosition;
        
        // Optional: Destroy projectile when duration is complete
        if (normalizedTime >= 1f)
        {
            // Movement complete - you might want to trigger collision or destroy
            // This depends on your game's logic
        }
    }

    public override void OnBehaviorCollision(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target, ChickenEnemy collision)
    {

    }

    public override void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {

    }

    public override void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner, ChickenEnemy target)
    {
        // Draw gizmos for debugging
        if (Application.isPlaying)
        {
            // Draw line from current position to target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(projectile.transform.position, target.transform.position);
            
            // Draw start position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_startPosition, 0.1f);
            
            // Draw target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_targetPosition, 0.1f);
        }
    }
}