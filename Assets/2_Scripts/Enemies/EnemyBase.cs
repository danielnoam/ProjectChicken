using System;
using KBCore.Refs;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField,Min(0)] private float maxHealth = 100f;
    [SerializeField] private Vector2 randomSizeRange = new Vector2(0.5f, 1.5f);
    
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float maxMoveSpeed = 500f;
    [SerializeField, Min(0)] private float maxDistanceFromFollowTarget = 5;
    
    [Header("References")]
    [SerializeField, Self] private Rigidbody rigidBody;
    private float _currentHealth;
    private Transform _followTarget;
    
    private void Awake()
    {
        SetUp();
    }


    private void FixedUpdate()
    {
        FollowTarget();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerProjectile projectile))
        {
            TakeDamage(projectile.Damage);
        }
    }

    private void OnValidate()
    {
        if (randomSizeRange.x < 0.1f) randomSizeRange.x = 0.1f;
        if (randomSizeRange.y < 0.1f) randomSizeRange.y = 0.1f;
    }

    public void SetUp(Transform followTarget = null)
    {
        
        transform.localScale *= Random.Range(randomSizeRange.x, randomSizeRange.y);
        _currentHealth = maxHealth;
        if (followTarget) _followTarget = followTarget;
    }   
    
    
    public abstract void Attack();

    
    
    #region Base Methods ------------------------------------------------------------

    private void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0) Die();

    }
    
    private void Die()
    {
        Destroy(gameObject);
    }

    private void FollowTarget()
    {
        if (!_followTarget) return;

        float distanceFromTarget = Vector3.Distance(transform.position, _followTarget.position);
        float speedModifier = Mathf.Clamp01(distanceFromTarget / maxDistanceFromFollowTarget);
        
        Vector3 direction = (_followTarget.position - transform.position).normalized;
        rigidBody.linearVelocity = (direction * (maxMoveSpeed * speedModifier * Time.fixedDeltaTime));

        

    }

    #endregion Base Methods ------------------------------------------------------------
    
    
    
    
    #region Editor ------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        // Draw the enemy's follow target
        if (_followTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_followTarget.position , 0.5f);
            Gizmos.DrawLine(transform.position, _followTarget.position);
        }
        
    }

    #endregion Editor ------------------------------------------------------------------
}
