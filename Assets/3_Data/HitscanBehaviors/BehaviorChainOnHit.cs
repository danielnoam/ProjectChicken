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
    
    [Space(10)]
    [Tooltip("Base damage dealt to the first target in the chain")]
    [SerializeField] private float baseDamage = 10;
    [Tooltip("Base force applied to the first target in the chain")]
    [SerializeField] private float baseForce = 10;
    [SerializeField] private bool stun;
    [Tooltip("Base stun chance for the first target in the chain")]
    [SerializeField,Range(0f,1f)] private float baseStunChance = 0.15f;
    [Tooltip("Base stun duration for the first target in the chain")]
    [SerializeField, Min(0f)] private float baseStunDuration = 1f;
    
    [Space(10)]
    [Tooltip("Delay between chain jumps")]
    [SerializeField, Min(0.01f)] private float chainDelay = 0.1f;
    [Tooltip("Reduction per chain for each base type (0.8 = 20% reduction)")]
    [SerializeField, Range(0f,1f)] private float chainFalloff = 0.8f;


    private List<ITargetable> _targetsToHit;
    
    public override void OnStart(WeaponInstance weaponInstance, RailPlayer owner, ITargetable target = null)
    {
        _targetsToHit = new List<ITargetable>();
    }

    public override void OnHit(WeaponInstance weaponInstance, RailPlayer owner, ITargetable target = null)
    {
        // Hit the initial target
        if (target is IDamageable damageable)
        {
            _targetsToHit.Add(target);
            damageable.TakeDamage(baseDamage);
            
            Vector3 forceDirection = target.Transform.position - owner.transform.position;
            forceDirection.Normalize();
            damageable.ApplyForce(forceDirection, baseForce);

            if (stun && UnityEngine.Random.Range(0f, 100f) > baseStunChance)
            {
                damageable.ApplyStun(baseStunDuration);
            }
        }
        
        // Choose between instant or delayed chaining
        owner.StartCoroutine(ChainTargets(weaponInstance, owner, target));

    }
    

    public override void OnEnd(WeaponInstance weaponInstance, RailPlayer owner, ITargetable target = null)
    {
        
    }
    
    
    private IEnumerator ChainTargets(WeaponInstance weaponInstance, RailPlayer owner, ITargetable initialTarget)
    {
        ITargetable currentTarget = initialTarget;
        
        for (int chainCount = 1; chainCount < maxTargets; chainCount++)
        {
            yield return new WaitForSeconds(chainDelay);
            
            // Check if the current target is still valid after delay
            if (currentTarget == null)
            {
                // Try to find any valid target from our hit list that's still alive
                currentTarget = FindValidTargetFromHitList();
                if (currentTarget == null) break;
            }
            
            ITargetable nextTarget = FindClosestTarget(currentTarget.Transform.position, _targetsToHit);
            
            if (nextTarget == null) break;
            
            _targetsToHit.Add(nextTarget);

            if (nextTarget is IDamageable damageable)
            {
                float currentDamage = baseDamage * Mathf.Pow(chainFalloff, chainCount);
                float currentForce = baseForce * Mathf.Pow(chainFalloff, chainCount);
                float currentStunChance = baseStunChance * Mathf.Pow(chainFalloff, chainCount);
                float currentStunDuration = baseStunDuration * Mathf.Pow(chainFalloff, chainCount);
                damageable.TakeDamage(currentDamage);
            
                Vector3 forceDirection = (nextTarget.Transform.position - currentTarget.Transform.position).normalized;
                damageable.ApplyForce(forceDirection, currentForce);


                if (stun && UnityEngine.Random.Range(0f, 100f) > currentStunChance)
                {
                    damageable.ApplyStun(currentStunDuration);
                }
                
            }
            
            currentTarget = nextTarget;
        }
    }
    
    
    
    private ITargetable FindValidTargetFromHitList()
    {
        // Find the first valid (non-destroyed) target from our hit list
        for (int i = _targetsToHit.Count - 1; i >= 0; i--)
        {
            if (_targetsToHit[i] != null)
            {
                return _targetsToHit[i];
            }
            else
            {
                // Remove destroyed targets from the list
                _targetsToHit.RemoveAt(i);
            }
        }
        return null;
    }
    
    private ITargetable FindClosestTarget(Vector3 fromPosition, List<ITargetable> excludeTargets)
    {
        Collider[] hitColliders = Physics.OverlapSphere(fromPosition, targetsRadiusCheck);
        
        ITargetable closestTarget = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (!hitCollider) continue;
            
            
            if (hitCollider.TryGetComponent(out ITargetable targetable))
            {
                if (targetable == null || excludeTargets.Contains(targetable)) continue;
            
                float distance = Vector3.Distance(fromPosition, targetable.Transform.position);
            
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = targetable;
                }
            }
        }
        
        return closestTarget;
    }

}