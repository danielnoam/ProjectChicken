using System;
using System.Collections;
using System.Collections.Generic;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(RailPlayerInput))]
[RequireComponent(typeof(RailPlayerMovement))]
[RequireComponent(typeof(RailPlayerAiming))]
[RequireComponent(typeof(RailPlayerWeaponSystem))]
public class RailPlayer : MonoBehaviour
{

    [Header("Health")]
    [SerializeField, Min(0)] private int maxHealth = 3;
    [SerializeField] private bool receiveHealthOnBonusThreshold = true;
    
    [Header("Shield")]
    [SerializeField, Min(0)] private float maxShieldHealth = 100f;
    [SerializeField, Min(0)] private float shieldRegenCooldown = 4f;
    [SerializeField, Min(0)] private float shieldRegenRate = 5f;
    
    [Header("Resource Collection")]
    [SerializeField, Min(0)] private float resourceCollectionRadius = 2f;
    [SerializeField ,Min(0)] private float magnetRadius = 5f;
    [SerializeField, Min(0)] private float magnetMoveSpeed = 5f;
    
    [Header("Path Following")]
    [SerializeField] private bool alignToSplineDirection = true;
    [SerializeField, Min(0)] private float splineRotationSpeed = 5f;
    [EndIf]
    
    [Header("SFXs")]
    [SerializeField] private SOAudioEvent healthDamageSfx;
    [SerializeField] private SOAudioEvent healthHealedSfx;
    [SerializeField] private SOAudioEvent shieldDamageSfx;
    [SerializeField] private SOAudioEvent shieldStartRegenSfx;
    [SerializeField] private SOAudioEvent shieldRegeneratedSfx;
    [SerializeField] private SOAudioEvent shieldDepletedSfx;
    [SerializeField] private SOAudioEvent deathSfx;
    
    [Header("References")]
    [SerializeField, Self] private RailPlayerInput playerInput;
    [SerializeField, Self] private RailPlayerAiming playerAiming;
    [SerializeField, Self] private RailPlayerWeaponSystem playerWeapon;
    [SerializeField, Self] private RailPlayerMovement playerMovement;
    [SerializeField, Self] private AudioSource audioSource;
    [SerializeField] private LevelManager levelManager;

    
    // Private fields
    private readonly List<Resource> _resourcesInRange = new List<Resource>();
    private int _currentHealth;
    private int _currentCurrency;
    private float _currentShieldHealth;
    private float _damagedCooldown;
    private Coroutine _regenShieldCoroutine;
    
    // Public properties
    public bool AlignToSplineDirection => alignToSplineDirection;
    public float SplineRotationSpeed => splineRotationSpeed;
    public int MaxHealth => maxHealth;
    public float MaxShieldHealth => maxShieldHealth;
    public int CurrentCurrency => _currentCurrency;
    public event Action OnDeath;
    public event Action<int> OnHealthChanged;
    public event Action<float> OnShieldChanged;
    public event Action<SOWeapon,SOWeapon> OnSpecialWeaponSwitched;
    public event Action<SOWeapon,float> OnBaseWeaponCooldownUpdated;
    public event Action<SOWeapon,float> OnSpecialWeaponCooldownUpdated;
    public event Action<float> OnWeaponHeatUpdated;
    public event Action OnWeaponOverheated;
    public event Action OnWeaponHeatReset;
    public event Action<Resource> OnResourceCollected;
    public event Action<int> OnCurrencyChanged;
    public event Action OnDodge;
    public event Action<float> OnDodgeCooldownUpdated;




    private void OnValidate()
    {
        this.ValidateRefs();
        
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
    }

    private void Awake()
    {
        SetUpPlayer();
    }
    

    private void OnEnable()
    {
        playerWeapon.OnSpecialWeaponSwitched += OnSpecialWeaponSwitched;
        playerWeapon.OnBaseWeaponCooldownUpdated += OnBaseWeaponCooldownUpdated;
        playerWeapon.OnSpecialWeaponCooldownUpdated += OnSpecialWeaponCooldownUpdated;
        playerWeapon.OnWeaponHeatUpdated += OnWeaponHeatUpdated;
        playerWeapon.OnWeaponOverheated += OnWeaponOverheated;
        playerWeapon.OnWeaponHeatReset += OnWeaponHeatReset;
        playerMovement.OnDodge += OnDodge;
        playerMovement.OnDodgeCooldownUpdated += OnDodgeCooldownUpdated;
        levelManager.OnBonusThresholdReached += OnMillionScoreReached;
    }

    private void OnDisable()
    {
        playerWeapon.OnSpecialWeaponSwitched -= OnSpecialWeaponSwitched;
        playerWeapon.OnBaseWeaponCooldownUpdated -= OnBaseWeaponCooldownUpdated;
        playerWeapon.OnSpecialWeaponCooldownUpdated -= OnSpecialWeaponCooldownUpdated;
        playerWeapon.OnWeaponHeatUpdated -= OnWeaponHeatUpdated;
        playerWeapon.OnWeaponOverheated -= OnWeaponOverheated;
        playerWeapon.OnWeaponHeatReset -= OnWeaponHeatReset;
        playerMovement.OnDodge -= OnDodge;
        playerMovement.OnDodgeCooldownUpdated -= OnDodgeCooldownUpdated;
        levelManager.OnBonusThresholdReached -= OnMillionScoreReached;
    }
    
    private void Update()
    {
        CheckDamageCooldown();
        CheckResourcesInRange();
        UpdateMagnetizedResources();
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
            shieldDepletedSfx?.Play(audioSource);
        }
        else
        {
            shieldDamageSfx?.Play(audioSource);
        }
        
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }
    
    private void DamageHealth()
    {
        if (!IsAlive() || IsDodging()) return;

        _currentHealth -= 1;
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }
        else
        {
            healthDamageSfx?.Play(audioSource);
        }
        
        OnHealthChanged?.Invoke(_currentHealth);
    }
    
    
    private void CheckDamageCooldown()
    {
        if (!IsAlive()) return;
        
        if (_damagedCooldown > 0)
        {
            _damagedCooldown -= Time.deltaTime;
        }
        
        if (_damagedCooldown <= 0 &&  _regenShieldCoroutine == null && _currentShieldHealth < maxShieldHealth)
        {
            StartShieldRegen();
        }
    }
    
    

    private void Die()
    {
        deathSfx?.Play(audioSource);
        
        OnDeath?.Invoke();
        
        
        Debug.Log("Player has died!");
    }

    #endregion Damage ----------------------------------------------------------------------
    
    
    #region Shield  --------------------------------------------------------------------------------------

    
    private IEnumerator RegenShieldRoutine()
    {
        shieldStartRegenSfx?.Play(audioSource);
        
        while (_currentShieldHealth < maxShieldHealth)
        {
            _currentShieldHealth += shieldRegenRate * Time.deltaTime;
            if (_currentShieldHealth >= maxShieldHealth)
            {
                _currentShieldHealth = maxShieldHealth;
                shieldRegeneratedSfx?.Play(audioSource);
                yield break;
            }
            
            OnShieldChanged?.Invoke(_currentShieldHealth);
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
        
        _damagedCooldown = shieldRegenCooldown;
    }

    private void StartShieldRegen()
    {
        _regenShieldCoroutine ??= StartCoroutine(RegenShieldRoutine());

        _damagedCooldown = 0;
    }
    
    

    #endregion Shield  --------------------------------------------------------------------------------------
    
    
    #region Healing ----------------------------------------------------------------------

    
    [Button]
    private void HealHealth(int amount = 1)
    {
        if (amount <= 0) return;
        
        _currentHealth += amount;
        if (_currentHealth > maxHealth)
        {
            _currentHealth = maxHealth;
        }
        healthHealedSfx?.Play(audioSource);
        
        OnHealthChanged?.Invoke(_currentHealth);
    }
    
    [Button]
    private void HealShield(float amount = 25f)
    {
        if (_currentShieldHealth >= maxShieldHealth) return;
        
        _currentShieldHealth += amount;
        if (_currentShieldHealth >= maxShieldHealth)
        {
            _currentShieldHealth = maxShieldHealth;
            shieldRegeneratedSfx?.Play(audioSource);
        }
        else
        {
            StartShieldRegen();
        }
        
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }
    
    private void OnMillionScoreReached()
    {
        if (!receiveHealthOnBonusThreshold) return;
        
        HealHealth(1);
    }
    

    #endregion Healing ----------------------------------------------------------------------

    
    #region Resource Collection --------------------------------------------------------------------------------------

    private void CheckResourcesInRange()
    {
        if (!IsAlive()) return;
        
        // Find all resources in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, magnetRadius);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out Resource resource))
            {
                if (resource && !_resourcesInRange.Contains(resource))
                {
                    _resourcesInRange.Add(resource);
                    resource.SetMagnetized(magnetMoveSpeed);
                }
            }
        }

        // Remove resources that are no longer in range
        for (int i = _resourcesInRange.Count - 1; i >= 0; i--)
        {
            bool isWithinRange = Vector3.Distance(transform.position, _resourcesInRange[i].transform.position) <= magnetRadius;
            bool isBehindPlayer = LevelManager.Instance.GetPositionOnSpline(transform.position) > LevelManager.Instance.GetPositionOnSpline(_resourcesInRange[i].transform.position);
            if (!_resourcesInRange[i] || !isWithinRange  || isBehindPlayer)
            {
                if (!_resourcesInRange[i]) continue;
                
                _resourcesInRange[i].ReleaseFromMagnetization();
                _resourcesInRange.RemoveAt(i);
            }
        }

    }
    
    private void UpdateMagnetizedResources()
    {
        if (_resourcesInRange.Count == 0 || !IsAlive()) return;
    
        // Iterate through all resources in range
        for (int i = _resourcesInRange.Count - 1; i >= 0; i--)
        {
            var resource = _resourcesInRange[i];
            if (!resource) continue;

            // Move the resource towards the player if within magnet radius
            if (Vector3.Distance(transform.position, resource.transform.position) <= magnetRadius)
            {
                resource.MoveTowardsPlayer(transform.position);
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
                UpdateCurrency(resource.CurrencyWorth);
                break;
            case ResourceType.HealthPack:
                HealHealth(resource.HealthWorth);
                break;
            case ResourceType.ShieldPack:
                HealShield(resource.ShieldWorth);
                break;
            case ResourceType.SpecialWeapon:
                playerWeapon.SelectSpecialWeapon(resource.Weapon);
                break;
            default:
                Debug.LogWarning($"Unknown resource type: {resource.ResourceType}");
                break;
        }
        
        _resourcesInRange.Remove(resource);
        resource.ResourceCollected();
        OnResourceCollected?.Invoke(resource);
    }
    
    [Button]
    private void UpdateCurrency(int amount)
    {
        _currentCurrency += amount;
        OnCurrencyChanged?.Invoke(_currentCurrency);
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
    
    public bool IsDodging()
    {
        return playerMovement.IsDodging;
    }
    
    public float GetMaxWeaponHeat()
    {
        return playerWeapon.MaxWeaponHeat;
    }
    
    public float GetDodgeMaxCooldown()
    {
        return playerMovement.MaxDodgeCooldown;
    }
    
    public SOWeapon GetCurrentBaseWeapon()
    {
        return playerWeapon.BaseWeapon;
    }
    
    public SOWeapon GetCurrentSpecialWeapon()
    {
        return playerWeapon.CurrentSpecialWeapon;
    }
    
    public Vector3 GetAimDirectionFromBarrelPosition(Vector3 barrelPosition, float convergenceMultiplier = 0f)
    {
        
        return playerAiming.GetAimDirectionFromBarrelPosition(barrelPosition, convergenceMultiplier);
    }
    
    public ChickenController GetTarget(float radius)
    {
        return playerAiming.GetEnemyTarget(radius);
    }
    
    public ChickenController[] GetAllTargets(int maxTargets, float radius)
    {
        return playerAiming.GetEnemyTargets(maxTargets, radius);
    }

    #endregion Helper Methods --------------------------------------------------------------------------------------

    
    #region Editor  --------------------------------------------------------------------------------------
    
    
    

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, resourceCollectionRadius);
        UnityEditor.Handles.Label(transform.position + (Vector3.up * resourceCollectionRadius), "Resource Collection Radius");

        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
        UnityEditor.Handles.Label(transform.position + (Vector3.up * magnetRadius), "Magnet Radius");
    }


#endif
    #endregion Editor  --------------------------------------------------------------------------------------

}
