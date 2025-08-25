using System.Collections;
using UnityEngine;

public class LaserWeaponGfx : WeaponGfx
{
    [Header("Barrel Rotation Settings")]
    [SerializeField] private Transform barrelTransform;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [SerializeField] private float rotationAmount = 5f; 
    [SerializeField] private float rotationDuration = 0.2f;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    public override void AnimateUsage()
    {
        base.AnimateUsage();
        
        if (barrelTransform)
        {
            StartCoroutine(RotateBarrel());
        }
    }
    
    protected override void StopAnimation()
    {
        base.StopAnimation();
        
        // Note: Individual rotations will complete naturally
        // This allows for stacking rotations when firing rapidly
    }
    
    private IEnumerator RotateBarrel()
    {
        if (!barrelTransform) yield break;
        
        Quaternion startRotation = barrelTransform.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.AngleAxis(rotationAmount, rotationAxis);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / rotationDuration;
            float curveValue = rotationCurve.Evaluate(normalizedTime);
            
            barrelTransform.localRotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);
            
            yield return null;
        }
        
        // Ensure we end exactly at the target rotation
        barrelTransform.localRotation = targetRotation;
    }
}