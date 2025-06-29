using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BehaviorChainOnHit : HitscanBehaviorBase
{
    [Tooltip("Radius to search for chain targets from each hit enemy")]
    [SerializeField] private float targetsRadiusCheck = 7f;
    [Tooltip("Maximum number of targets that can be hit (including initial target)")]
    [SerializeField, Min(1f)] private int maxTargets = 5;
    [Tooltip("Base damage dealt to the first target in the chain")]
    [SerializeField] private float baseDamage = 10;
    [Tooltip("Base force applied to the first target in the chain")]
    [SerializeField] private float baseForce = 10;
    [Tooltip("Delay between chain jumps")]
    [SerializeField, Min(0.01f)] private float chainDelay = 0.1f;
    [Tooltip("Damage reduction per chain (0.8 = 20% reduction)")]
    [SerializeField, Range(0f,1f)] private float chainFalloff = 0.8f;


    private List<ChickenController> targetsToHit;
    
    public override void OnBehaviorStart(SOWeapon weapon, RailPlayer owner, ChickenController target = null)
    {
        targetsToHit = new List<ChickenController>();
    }

    public override void OnBehaviorHit(SOWeapon weapon, RailPlayer owner, ChickenController target)
    {
        // Hit the initial target
        if (target)
        {
            targetsToHit.Add(target);
            target.TakeDamage(baseDamage);
            
            // Apply force away from the hit point
            Vector3 forceDirection = target.transform.position - owner.transform.position;
            forceDirection.Normalize();
            target.ApplyForce(forceDirection, baseForce);
        }
        
        // Choose between instant or delayed chaining
        owner.StartCoroutine(ChainTargets(weapon, owner, target));

    }
    

    public override void OnBehaviorEnd(SOWeapon weapon, RailPlayer owner, ChickenController target = null)
    {
        
    }

    public override void OnBehaviorDrawGizmos(SOWeapon weapon, RailPlayer owner, ChickenController target = null)
    {

    }
    
    private IEnumerator ChainTargets(SOWeapon weapon, RailPlayer owner, ChickenController initialTarget)
    {
        ChickenController currentTarget = initialTarget;
        
        for (int chainCount = 1; chainCount < maxTargets; chainCount++)
        {
            yield return new WaitForSeconds(chainDelay);
            
            // Check if current target is still valid after delay
            if (!currentTarget)
            {
                // Try to find any valid target from our hit list that's still alive
                currentTarget = FindValidTargetFromHitList();
                if (!currentTarget) break;
            }
            
            ChickenController nextTarget = FindClosestTarget(currentTarget.transform.position, targetsToHit);
            
            if (!nextTarget) break;
            
            targetsToHit.Add(nextTarget);
            
            float currentDamage = baseDamage * Mathf.Pow(chainFalloff, chainCount);
            float currentForce = baseForce * Mathf.Pow(chainFalloff, chainCount);
            nextTarget.TakeDamage(currentDamage);
            
            Vector3 forceDirection = (nextTarget.transform.position - currentTarget.transform.position).normalized;
            nextTarget.ApplyForce(forceDirection, currentForce);
            
            
            // Check if current target is still valid before playing effect
            if (currentTarget)
            {
                weapon.PlayImpactEffect(currentTarget.transform.position, Quaternion.identity);
            }
            
            currentTarget = nextTarget;
        }
    }
    
    
    
    private ChickenController FindValidTargetFromHitList()
    {
        // Find the first valid (non-destroyed) target from our hit list
        for (int i = targetsToHit.Count - 1; i >= 0; i--)
        {
            if (targetsToHit[i])
            {
                return targetsToHit[i];
            }
            else
            {
                // Remove destroyed targets from the list
                targetsToHit.RemoveAt(i);
            }
        }
        return null;
    }
    
    private ChickenController FindClosestTarget(Vector3 fromPosition, List<ChickenController> excludeTargets)
    {
        Collider[] hitColliders = Physics.OverlapSphere(fromPosition, targetsRadiusCheck);
        
        ChickenController closestTarget = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider hitCollider in hitColliders)
        {
            // Check if collider is still valid
            if (!hitCollider) continue;
            
            ChickenController chickenEnemy = hitCollider.GetComponent<ChickenController>();
            
            // Skip if not a valid enemy, already hit, or destroyed
            if (!chickenEnemy || excludeTargets.Contains(chickenEnemy)) continue;
            
            float distance = Vector3.Distance(fromPosition, chickenEnemy.transform.position);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = chickenEnemy;
            }
        }
        
        return closestTarget;
    }

}