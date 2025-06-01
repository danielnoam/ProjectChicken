using System;
using System.Collections;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(RailPlayerInput))]
[RequireComponent(typeof(RailPlayerMovement))]
[RequireComponent(typeof(RailPlayerAiming))]
[RequireComponent(typeof(RailPlayerWeaponSystem))]
public class RailPlayer : MonoBehaviour
{

    [Header("Health")]
    [SerializeField, Min(0)] private int maxHealth = 3;
    
    [Header("Shield")]
    [SerializeField, Min(0)] private float maxShieldHealth = 100f;
    [SerializeField, Min(0)] private float shieldRegenRate = 5f;
    [SerializeField, Min(0)] private float shieldRegenDelay = 2f;
    
    
    [Header("Resource Collection")]
    
    
    
    [Header("Path Following")]
    [SerializeField] private bool alignToSplineDirection = true;
    [SerializeField] private float pathFollowSpeed = 5f;
    [SerializeField, Min(0)] private float splineRotationSpeed = 5f;
    
    [Header("Debug")]
    [SerializeField,Child] private TextMeshProUGUI playerStatusText;


    private int _currentHealth;
    private float _currentShieldHealth;
    private float _damagedCooldown;
    private Coroutine _regenShieldCoroutine;
    
    public bool AlignToSplineDirection => alignToSplineDirection;
    public float PathFollowSpeed => pathFollowSpeed;
    public float SplineRotationSpeed => splineRotationSpeed;

    private void Awake()
    {
        SetUpPlayer();
    }

    private void Update()
    {
        CheckDamageCooldown();
        
        if (playerStatusText)
        {
            playerStatusText.text = $"Health: {_currentHealth}/ {maxHealth}\n" +
                                    $"Shield: {_currentShieldHealth:F1} / {maxShieldHealth:F1}  Regen: {_damagedCooldown}\n" +
                                    $"Alive: {IsAlive()}\n" +
                                    $"Shielded: {HasShield()}";
        }
    }
    
    

    private void OnTriggerEnter(Collider other)
    {
        
    }
    
    
    private void SetUpPlayer()
    {
        _currentHealth = maxHealth;
        _currentShieldHealth = maxShieldHealth;
    }

    

    #region Damage ---------------------------------------------------------------------- 

    [Button]
    private void TakeDamage(float damage)
    {
        if (damage <= 0 || !IsAlive()) return;
        
        StopShieldRegen();
            
        if (HasShield())
        {
            DamageShield(damage);
            return;
        }
        
        DamageHealth();
    }
    
    private void DamageShield(float damage)
    {
        if (damage <= 0 || !HasShield()) return;
        
        _currentShieldHealth -= damage;
        

        if (_currentShieldHealth < 0)
        {
            _currentShieldHealth = 0;
            DamageHealth();
        }
    }
    
    private void DamageHealth()
    {
        if (!IsAlive()) return;

        _currentHealth -= 1;
        if (_currentHealth < 0)
        {
            _currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
    }

    #endregion Damage ----------------------------------------------------------------------

    
    #region Shield Regen --------------------------------------------------------------------------------------

    
    private IEnumerator RegenShieldRoutine()
    {
        
        while (_currentShieldHealth < maxShieldHealth)
        {
            _currentShieldHealth += shieldRegenRate * Time.deltaTime;
            if (_currentShieldHealth > maxShieldHealth)
            {
                _currentShieldHealth = maxShieldHealth;
                yield break;
            }
            yield return null;
        }
    }
    
    private void StopShieldRegen()
    {
        if (_regenShieldCoroutine != null)
        {
            StopCoroutine(_regenShieldCoroutine);
            _regenShieldCoroutine = null;
        }
        
        _damagedCooldown = shieldRegenDelay;
    }

    private void StartShieldRegen()
    {
        
        if (_regenShieldCoroutine == null)
        {
            _regenShieldCoroutine = StartCoroutine(RegenShieldRoutine());
        }
        
        _damagedCooldown = 0;
        
    }
    
    private void CheckDamageCooldown()
    {
        if (_damagedCooldown > 0)
        {
            _damagedCooldown -= Time.deltaTime;
        }
        
        if (_damagedCooldown <= 0 &&  _regenShieldCoroutine == null)
        {
            StartShieldRegen();
        }
    }
    

    #endregion Shield Regen --------------------------------------------------------------------------------------
    
    

    #region Helper Methods --------------------------------------------------------------------------------------

    public bool HasShield()
    {
        return _currentShieldHealth > 0;
    }
    
    public bool IsAlive()
    {
        return _currentHealth > 0;
    }

    #endregion Helper Methods --------------------------------------------------------------------------------------


}
