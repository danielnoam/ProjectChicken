using System;
using UnityEngine;

public class PassthroughObstacle : BaseObstacle
{
    [Header("References")]
    [SerializeField] private GameObject centerObject;
    [SerializeField] private PassthroughTrigger passthroughTrigger;
    public Transform CenterObjectTransform => centerObject ? centerObject.transform : transform;

    public event Action<PassthroughObstacle> OnPlayerEnteredPassthrough;
    public event Action<PassthroughObstacle> OnPlayerPassedThrough;


    private void OnEnable()
    {
        if (passthroughTrigger)
        {
            passthroughTrigger.OnPlayerEnteredTrigger += HandlePlayerEnteredTrigger;
            passthroughTrigger.OnPlayerPassedThrough += HandlePlayerPassedThrough;
        }

    }

    private void OnDisable()
    {
        if (passthroughTrigger)
        {
            passthroughTrigger.playerEnteredTrigger = false;
            passthroughTrigger.playerPassedThrough = false;
            passthroughTrigger.OnPlayerEnteredTrigger -= HandlePlayerEnteredTrigger;
            passthroughTrigger.OnPlayerPassedThrough -= HandlePlayerPassedThrough;
        }
    }


    private void HandlePlayerEnteredTrigger()
    {
        PlayFlyByEffects();
        OnPlayerEnteredPassthrough?.Invoke(this);
    }
    
    private void HandlePlayerPassedThrough()
    {
        OnPlayerPassedThrough?.Invoke(this);
    }
    
    protected override void MoveAlongSpline()
    {
        if (!spline || !centerObject) return;
        
        float splineLength = spline.Spline.GetLength();
        float progressIncrement = (moveSpeed * Time.deltaTime) / splineLength;
        
        splineProgress += progressIncrement;
        
        if (splineProgress >= 1f)
        {
            OnSplineComplete();
            return;
        }
        
        spline.Evaluate(splineProgress, out var position, out var tangent, out var up);
        
        // Convert position to Vector3 to avoid ambiguity with float3
        Vector3 splinePosition = position;
        
        // The centerObject defines the "middle" of the obstacle
        // We want this middle point to be at the spline position
        
        // Get centerObject's position in parent's local space
        Vector3 centerLocalPos = centerObject.transform.localPosition;
        
        // Transform this local position to world space offset (accounts for rotation/scale)
        Vector3 centerWorldOffset = transform.TransformVector(centerLocalPos);
        
        // Set parent position so that center ends up at spline position
        transform.position = splinePosition - centerWorldOffset;
        
        transform.Rotate(rotationDirection, rotationSpeed * Time.deltaTime);
    }
    
    protected override void OnCollisionWithPlayer(RailPlayer player)
    {
        player.Health.TakeDamage(100f, 5f);
        Vector3 moveDirection = (player.transform.position - CenterObjectTransform.position).normalized;
        player.Movement.Push(-moveDirection, 3f);
    }
    
    protected override void OnCollisionWithChicken(ChickenStateController chicken)
    {
        chicken.TakeDamage(100);
    }

    protected override void OnCollisionWithPassthroughObstacle(PassthroughObstacle passthroughObstacle)
    {

    }

    protected override void OnCollisionWithObstacle(NormalObstacle normalObstacle)
    {

    }

    public override void TakeDamage(float damage)
    {
   
    }

    public override void ApplyStun(float duration)
    {
      
    }

    public override void ApplyForce(Vector3 direction, float force)
    {
  
    }
}