using System;
using KBCore.Refs;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class TestEnemyBase : MonoBehaviour
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
    private Transform _followPosition;
    
    private void Awake()
    {
        SetUp();
    }


    private void FixedUpdate()
    {
        FollowPosition();
        LookAtPosition();
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
        if (followTarget) _followPosition = followTarget;
    }   
    
    
    public abstract void Attack();

    
    
    #region Base Methods ------------------------------------------------------------

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0) Die();

    }
    
    private void Die()
    {
        Destroy(gameObject);
    }

    private void FollowPosition()
    {
        if (!_followPosition) return;

        float distanceFromTarget = Vector3.Distance(transform.position, _followPosition.position);
        float speedModifier = Mathf.Clamp01(distanceFromTarget / maxDistanceFromFollowTarget);
        
        Vector3 direction = (_followPosition.position - transform.position).normalized;
        rigidBody.linearVelocity = (direction * (maxMoveSpeed * speedModifier * Time.fixedDeltaTime));
    }

    private void LookAtPosition()
    {
        if (!LevelManager.Instance) return;
    
        Vector3 direction = (LevelManager.Instance.PlayerPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-direction);
            rigidBody.rotation = targetRotation;
        }
    }
    
    

    #endregion Base Methods ------------------------------------------------------------
    
    
    
    
    #region Editor ------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        // Draw the enemy's follow target
        if (_followPosition)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_followPosition.position , 0.5f);
            Gizmos.DrawLine(transform.position, _followPosition.position);
        }
        
    }

    #endregion Editor ------------------------------------------------------------------
}
