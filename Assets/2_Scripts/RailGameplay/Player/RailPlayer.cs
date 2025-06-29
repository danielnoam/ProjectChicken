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
    
    [Header("Path Following")]
    [SerializeField] private bool alignToSplineDirection = true;
    [SerializeField, Min(0)] private float splineRotationSpeed = 5f;
    [EndIf]
    
    [Header("References")]
    [SerializeField] private Transform followCameraTarget;
    [SerializeField] private Transform introCameraTarget;
    [SerializeField] private SOAudioEvent healthDamageSfx;
    [SerializeField] private SOAudioEvent healthHealedSfx;
    [SerializeField] private SOAudioEvent shieldDamageSfx;
    [SerializeField] private SOAudioEvent shieldStartRegenSfx;
    [SerializeField] private SOAudioEvent shieldRegeneratedSfx;
    [SerializeField] private SOAudioEvent shieldDepletedSfx;
    [SerializeField] private SOAudioEvent deathSfx;
    [SerializeField] private LevelManager levelManager;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming playerAiming;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem playerWeapon;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement playerMovement;
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;

    
    // Private fields
    private readonly List<Resource> _resourcesInRange = new List<Resource>();
    private int _currentHealth;
    private int _currentCurrency;
    private float _currentShieldHealth;
    private float _damagedCooldown;
    private Coroutine _regenShieldCoroutine;
    private Quaternion _splineRotation = Quaternion.identity;
    
    // Public properties
    public LevelManager LevelManager => levelManager;
    public Quaternion SplineRotation => _splineRotation;
    public bool AlignToSplineDirection => alignToSplineDirection;
    public int MaxHealth => maxHealth;
    public float MaxShieldHealth => maxShieldHealth;
    public int CurrentCurrency => _currentCurrency;
    public event Action OnDeath;
    public event Action<int> OnHealthChanged;
    public event Action<float> OnShieldChanged;
    public event Action<WeaponInstance> OnWeaponFired;
    public event Action<WeaponInstance,WeaponInstance> OnSpecialWeaponSwitched;
    public event Action<WeaponInstance,float> OnBaseWeaponCooldownUpdated;
    public event Action<WeaponInstance,float> OnSpecialWeaponCooldownUpdated;
    public event Action<WeaponInstance> OnSpecialWeaponDisabled;
    public event Action<WeaponInstance> OnBaseWeaponSwitched;
    public event Action<float> OnWeaponHeatUpdated;
    public event Action OnWeaponOverheated;
    public event Action OnWeaponHeatReset;
    public event Action<float,float, float> OnWeaponHeatMiniGameWindowCreated;
    public event Action OnWeaponHeatMiniGameSucceeded;
    public event Action OnWeaponHeatMiniGameFailed;
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
        SetupPlayer();
    }
    

    private void OnEnable()
    {
        playerWeapon.OnWeaponFired += OnWeaponFired;
        playerWeapon.OnSpecialWeaponSwitched += OnSpecialWeaponSwitched;
        playerWeapon.OnBaseWeaponCooldownUpdated += OnBaseWeaponCooldownUpdated;
        playerWeapon.OnSpecialWeaponCooldownUpdated += OnSpecialWeaponCooldownUpdated;
        playerWeapon.OnWeaponHeatUpdated += OnWeaponHeatUpdated;
        playerWeapon.OnWeaponOverheated += OnWeaponOverheated;
        playerWeapon.OnWeaponHeatReset += OnWeaponHeatReset;
        playerWeapon.OnWeaponHeatMiniGameWindowCreated += OnWeaponHeatMiniGameWindowCreated;
        playerWeapon.OnWeaponHeatMiniGameSucceeded +=  OnWeaponHeatMiniGameSucceeded;
        playerWeapon.OnWeaponHeatMiniGameFailed += OnWeaponHeatMiniGameFailed;
        playerWeapon.OnSpecialWeaponDisabled += OnSpecialWeaponDisabled;
        playerWeapon.OnBaseWeaponSwitched += OnBaseWeaponSwitched;
        playerMovement.OnDodge += OnDodge;
        playerMovement.OnDodgeCooldownUpdated += OnDodgeCooldownUpdated;
        levelManager.OnBonusThresholdReached += OnMillionScoreReached;
        levelManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        playerWeapon.OnWeaponFired -= OnWeaponFired;
        playerWeapon.OnSpecialWeaponSwitched -= OnSpecialWeaponSwitched;
        playerWeapon.OnBaseWeaponCooldownUpdated -= OnBaseWeaponCooldownUpdated;
        playerWeapon.OnSpecialWeaponCooldownUpdated -= OnSpecialWeaponCooldownUpdated;
        playerWeapon.OnWeaponHeatUpdated -= OnWeaponHeatUpdated;
        playerWeapon.OnWeaponOverheated -= OnWeaponOverheated;
        playerWeapon.OnWeaponHeatReset -= OnWeaponHeatReset;
        playerWeapon.OnWeaponHeatMiniGameWindowCreated -= OnWeaponHeatMiniGameWindowCreated;
        playerMovement.OnDodge -= OnDodge;
        playerMovement.OnDodgeCooldownUpdated -= OnDodgeCooldownUpdated;
        levelManager.OnBonusThresholdReached -= OnMillionScoreReached;
        levelManager.OnStageChanged -= OnStageChanged;
    }
    

    private void Update()
    {
        GetSplineRotations();
        CheckDamageCooldown();
        CheckResourcesInRange();
        UpdateMagnetizedResources();
    }

    private void SetupPlayer()
    {
        _currentCurrency = SaveManager.GetCurrency();
        _currentHealth = maxHealth;
        _currentShieldHealth = maxShieldHealth;
        
        OnCurrencyChanged?.Invoke(_currentCurrency);
        OnHealthChanged?.Invoke(_currentHealth);
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        if (stage.StageType == StageType.Outro)
        {
            SaveManager.UpdatePlayerProgress(_currentCurrency);
        }
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
                    resource.SetMagnetized(transform, magnetRadius);
                }
            }
        }

        // Remove resources that are no longer in range
        for (int i = _resourcesInRange.Count - 1; i >= 0; i--)
        {
            bool isWithinRange = Vector3.Distance(transform.position, _resourcesInRange[i].transform.position) <= magnetRadius;
            if (!_resourcesInRange[i] || !isWithinRange)
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
                playerWeapon.SetSpecialWeapon(resource.Weapon);
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
    
    public WeaponInstance GetCurrentBaseWeapon()
    {
        return playerWeapon.BaseWeaponInstance;
    }
    
    public WeaponInstance GetCurrentSpecialWeapon()
    {
        return playerWeapon.CurrentSpecialWeaponInstance;
    }
    
    public Vector3 GetAimDirectionFromBarrelPosition(Vector3 barrelPosition, float convergenceMultiplier = 0f)
    {
        
        return playerAiming.GetAimDirectionFromBarrelPosition(barrelPosition, convergenceMultiplier);
    }
    
    public ChickenController GetTarget(float radius)
    {
        return playerAiming.CurrentAimLockTarget ? playerAiming.CurrentAimLockTarget : playerAiming.GetEnemyTarget(radius);
    }
    
    public ChickenController[] GetAllTargets(int maxTargets, float radius)
    {
        return playerAiming.GetEnemyTargets(maxTargets, radius);
    }

    public Vector2 GetNormalizedReticlePosition()
    {
        return playerAiming.NormalizedReticlePosition;
    }
    public Transform GetFollowCameraTarget()
    {
        return followCameraTarget;
    }
    
    public Transform GetIntroCameraTarget()
    {
        return introCameraTarget;
    }

    public Transform GetReticleTarget()
    {
        return playerAiming.AimWorldPosition;
    }

    public bool HasSpecialWeapon()
    {
        return playerWeapon.CurrentSpecialWeaponInstance != null;
    }
    
    private void GetSplineRotations()
    {
        if (!alignToSplineDirection || !levelManager)
        {
            _splineRotation = Quaternion.identity;
            return;
        }
        

        Vector3 splineForward = levelManager.GetSplineTangentAtPosition(levelManager.CurrentPositionOnPath.position);
        
        if (splineForward != Vector3.zero)
        {
            Quaternion targetSplineRotation = Quaternion.LookRotation(splineForward, Vector3.up);
            _splineRotation = Quaternion.Slerp(_splineRotation, targetSplineRotation, splineRotationSpeed * Time.deltaTime);
        }
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
