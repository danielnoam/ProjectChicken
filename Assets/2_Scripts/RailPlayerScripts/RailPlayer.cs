using System;
using System.Collections;
using System.Collections.Generic;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using VInspector;

[SelectionBase]
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
    [SerializeField, Min(0)] private float shieldRegenDelay = 4f;
    
    
    [Header("Resource Collection")]
    [SerializeField ,Min(0)] private float magnetRadius = 5f;
    [SerializeField, Min(0)] private float magnetMoveSpeed = 5f;
    [SerializeField, Min(0)] private float resourceCollectionRadius = 2f;
    
    
    [Header("Path Following")]
    [SerializeField] private bool alignToSplineDirection = true;
    [EnableIf("alignToSplineDirection")]
    [SerializeField] private float pathFollowSpeed = 5f;
    [SerializeField, Min(0)] private float splineRotationSpeed = 5f;
    [EndIf]
    


    [Header("References")]
    [SerializeField,Child] private TextMeshProUGUI playerStatusText;
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    [SerializeField, Self] private RailPlayerWeaponSystem playerWeapon;
    [SerializeField, Self] private RailPlayerMovement playerMovement;

    private List<Resource> _resourcesInRange = new List<Resource>();
    private int _currentHealth;
    private float _currentShieldHealth;
    private float _damagedCooldown;
    private Coroutine _regenShieldCoroutine;
    
    public bool AlignToSplineDirection => alignToSplineDirection;
    public float PathFollowSpeed => pathFollowSpeed;
    public float SplineRotationSpeed => splineRotationSpeed;


    private void OnValidate() { this.ValidateRefs(); }

    private void Awake()
    {
        SetUpPlayer();
    }

    private void Update()
    {
        CheckDamageCooldown();
        CheckResourcesInRange();
        UpdateMagnetizedResources();
        UpdateDebugText();

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

    private void Die()
    {
        Debug.Log("Player has died!");
    }

    #endregion Damage ----------------------------------------------------------------------


    #region Health  --------------------------------------------------------------------------------------

    private void HealHealth(float amount)
    {
        if (amount <= 0 || !IsAlive()) return;
        
        _currentHealth += (int)amount;
        if (_currentHealth > maxHealth)
        {
            _currentHealth = maxHealth;
        }
    }

    #endregion Health  --------------------------------------------------------------------------------------
    
    
    #region Shield  --------------------------------------------------------------------------------------

    
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
    
    private void HealShield(float amount)
    {
        if (HasShield()) return;
        
        _currentShieldHealth += amount;
        if (_currentShieldHealth > maxShieldHealth)
        {
            _currentShieldHealth = maxShieldHealth;
        }
        
        StartShieldRegen();
    }

    

    #endregion Shield  --------------------------------------------------------------------------------------


    
    #region Resource Collection --------------------------------------------------------------------------------------

    private void CheckResourcesInRange()
    {
        
        // Find all resources in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, magnetRadius);
        foreach (var col in colliders)
        {

            if (TryGetComponent(out Resource resource))
            {
                if (resource && !_resourcesInRange.Contains(resource))
                {
                    _resourcesInRange.Add(resource);
                    resource.SetMagnetized(true);
                }
            }
        }

        // Remove resources that are no longer in range
        for (int i = _resourcesInRange.Count - 1; i >= 0; i--)
        {
            if (!_resourcesInRange[i] || Vector3.Distance(transform.position, _resourcesInRange[i].transform.position) > magnetRadius)
            {
                _resourcesInRange[i].SetMagnetized(false);
                _resourcesInRange.RemoveAt(i);
            }
        }

    }
    
    private void UpdateMagnetizedResources()
    {
        if (_resourcesInRange.Count == 0) return;
        
        foreach (var resource in _resourcesInRange)
        {
            if (!resource) continue;

            // Move the resource towards the player if within magnet radius
            if (Vector3.Distance(transform.position, resource.transform.position) <= magnetRadius)
            {
                resource.MoveTowardsPlayer(transform.position, magnetMoveSpeed);
            }
            
            
            // Check if the resource is within the collection radius
            if (Vector3.Distance(transform.position, resource.transform.position) <= resourceCollectionRadius)
            {
                CollectResource(resource);
            }
        }
    }
    
    
    private void CollectResource(Resource resource)
    {
        if (!resource) return;
        
        switch (resource.ResourceType)
        {
            case ResourceType.Currency:
                Debug.Log("Collected Currency! " + resource.CurrencyWorth);
                break;
            case ResourceType.HealthPack:
                HealHealth(resource.HealthWorth);
                break;
            case ResourceType.ShieldPack:
                HealShield(resource.ShieldWorth);
                break;
            case ResourceType.RandomWeapon:
                playerWeapon.SelectRandomSpecialWeapon();
                break;
            default:
                Debug.LogWarning($"Unknown resource type: {resource.ResourceType}");
                break;
        }
        
        _resourcesInRange.Remove(resource);
        resource.ResourceCollected();
    }

    #endregion Resource Collection --------------------------------------------------------------------------------------
    
    

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

    
    #region Editor  --------------------------------------------------------------------------------------
    
    
    private void UpdateDebugText()
    {
        if (playerStatusText)
        {
            string selectedWeapon = "";


            if (playerWeapon.CurrentSpecialWeapon)
            {
                if (playerWeapon.CurrentSpecialWeapon.WeaponDurationType == WeaponDurationType.AmmoBased)
                {
                    selectedWeapon = $"{playerWeapon.CurrentSpecialWeapon.WeaponName} ({playerWeapon.SpecialWeaponAmmo}/{playerWeapon.CurrentSpecialWeapon.AmmoLimit})";
                } 
                else if (playerWeapon.CurrentSpecialWeapon.WeaponDurationType == WeaponDurationType.TimeBased)
                {
                    selectedWeapon = $"{playerWeapon.CurrentSpecialWeapon.WeaponName} ({playerWeapon.SpecialWeaponTime:F1}/{playerWeapon.CurrentSpecialWeapon.TimeLimit})";
                }
            }
            else
            {
                selectedWeapon = $"Base Weapon";
            }

            
            playerStatusText.text = $"Health: {_currentHealth}/{maxHealth}\n" +
                                    $"Shield: {_currentShieldHealth:F1} / {maxShieldHealth:F1}, Regen: {_damagedCooldown}\n" +
                                    $"Alive: {IsAlive()}\n" +
                                    $"Shielded: {HasShield()}\n" +
                                    $"Weapon: {selectedWeapon}\n"
                ;
        }
    }
    
    

#if UNITY_EDITOR


    private void OnDrawGizmos()
    {

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, resourceCollectionRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }


#endif

    #endregion Editor  --------------------------------------------------------------------------------------

}
