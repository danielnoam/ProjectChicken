using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using VInspector;





[SelectionBase]
[RequireComponent(typeof(RailPlayerInput))]
[RequireComponent(typeof(RailPlayerMovement))]
[RequireComponent(typeof(RailPlayerAiming))]
[RequireComponent(typeof(RailPlayerWeaponSystem))]
[RequireComponent(typeof(RumbleSource))]
public class RailPlayer : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(0)] private int baseHealth = 3;
    [SerializeField] private bool dodgingGivesInvincibility = true;
    [SerializeField] private bool receiveHealthOnBonusThreshold = true;
    [SerializeField] private SOHealthUpgrade[] healthUpgrades = Array.Empty<SOHealthUpgrade>();
    
    [Header("Shield")]
    [SerializeField, Min(0)] private float baseShieldHealth = 100f;
    [SerializeField, Min(0)] private float shieldRegenCooldown = 3f;
    [SerializeField, Min(0)] private float shieldRegenRate = 15f;
    [SerializeField] private SOShieldUpgrade[] shieldUpgrades = Array.Empty<SOShieldUpgrade>();
    
    [Header("Resource Collection")]
    [SerializeField ,Min(0)] private float magnetRadius = 14f;
    [SerializeField] private SOResourceMagnetUpgrade[] resourceMagnetUpgrades = Array.Empty<SOResourceMagnetUpgrade>();
    
    [Header("Path Following")]
    [SerializeField] private bool alignToSplineDirection = true;
    [SerializeField, Min(0)] private float splineRotationSpeed = 5f;
    [EndIf]
    
    [Header("References")]
    [SerializeField, Child(Flag.Editable)] private AudioSource audioSource;
    [SerializeField] private Transform cameraPositions;
    [SerializeField] private Transform followCameraTarget;
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
    [SerializeField, Self, HideInInspector] private RumbleSource rumbleSource;

    


    private int _currentHealth;
    private int _maxHealth;
    private int _currentCurrency;
    private float _currentShieldHealth;
    private float _maxShieldHealth;
    private float _currentMagnetRadius;
    private float _damagedCooldown;
    private Coroutine _regenShieldCoroutine;
    private Quaternion _splineRotation = Quaternion.identity;
    private readonly List<Resource> _resourcesInRange = new List<Resource>();
    private readonly Dictionary<ResourceType, Action<Resource>> _collectionActions = new Dictionary<ResourceType, Action<Resource>>();

    public RailPlayerAiming PlayerAiming => playerAiming;
    public RailPlayerWeaponSystem PlayerWeapon => playerWeapon;
    public RailPlayerMovement PlayerMovement => playerMovement;
    public LevelManager LevelManager => levelManager;
    public Quaternion SplineRotation => _splineRotation;
    public bool AlignToSplineDirection => alignToSplineDirection;
    public int MaxHealth => _maxHealth;
    public float MaxShieldHealth => _maxShieldHealth;
    public int CurrentHealth => _currentHealth;
    public float CurrentShieldHealth => _currentShieldHealth;
    public int CurrentCurrency => _currentCurrency;
    public event Action OnDeath;
    public event Action<int> OnHealthChanged;
    public event Action<float> OnShieldChanged;
    public event Action<Resource> OnResourceCollected;
    public event Action<int> OnCurrencyChanged;




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
        _collectionActions.Add(ResourceType.Currency, (resource) => UpdateCurrency(resource.CurrencyWorth) );
        _collectionActions.Add(ResourceType.HealthPack, (resource) => HealHealth(resource.HealthWorth));
        _collectionActions.Add(ResourceType.ShieldPack, (resource) => HealShield(resource.ShieldWorth));
        _collectionActions.Add(ResourceType.SpecialWeapon, (resource) => playerWeapon.SetSpecialWeapon(resource.WeaponData));
        
        _maxHealth = baseHealth + TotalHealthUpgrades();
        _maxShieldHealth = baseShieldHealth + TotalShieldUpgrades();
        _currentMagnetRadius = magnetRadius + TotalResourceMagnetUpgrades();
        
        _currentCurrency = SaveManager.GetCurrency();
        _currentHealth = _maxHealth;
        _currentShieldHealth = _maxShieldHealth;
    }


    private void Start()
    {
        OnCurrencyChanged?.Invoke(_currentCurrency);
        OnHealthChanged?.Invoke(_currentHealth);
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }


    private void OnEnable()
    {
        levelManager.OnBonusThresholdReached += OnMillionScoreReached;
        levelManager.OnStageChanged += OnStageChanged;
        levelManager.OnRestartFromSavePoint += OnRestartFromSavePoint;
    }

    private void OnDisable()
    {
        levelManager.OnBonusThresholdReached -= OnMillionScoreReached;
        levelManager.OnStageChanged -= OnStageChanged;
        levelManager.OnRestartFromSavePoint -= OnRestartFromSavePoint;
    }
    

    private void Update()
    {
        GetSplineRotations();
        CheckDamageCooldown();
        CheckResourcesInRange();
    }
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsAlive()) return;

        if (other.TryGetComponent(out Resource resource))
        {
            CollectResource(resource);
        }
    }
    
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        if (stage.StageType == StageType.Outro)
        {
            SaveManager.UpdatePlayerCurrency(_currentCurrency);
        }
    }

    private void OnRestartFromSavePoint(SavePointInformation savePoint)
    {
        if (savePoint == null) return;
        
        _currentCurrency = savePoint.PlayerCurrency;
        _currentHealth = savePoint.PlayerHealth;
        _currentShieldHealth = savePoint.PlayerShield;
        
        
        OnCurrencyChanged?.Invoke(_currentCurrency);
        OnHealthChanged?.Invoke(_currentHealth);
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }
    

    #region Damage ---------------------------------------------------------------------- 

    [Button]
    public void TakeDamage(float damage)
    {
        if (damage <= 0 || !IsAlive() || (dodgingGivesInvincibility && IsDodging())) return;
        
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
        
        rumbleSource.Rumble(0.1f,0, 0.1f);
        OnShieldChanged?.Invoke(_currentShieldHealth);
    }
    
    private void DamageHealth()
    {
        if (!IsAlive()) return;

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
        
        rumbleSource.Rumble(0,0.5f, 0.3f);
        OnHealthChanged?.Invoke(_currentHealth);
    }
    
    
    private void CheckDamageCooldown()
    {
        if (!IsAlive()) return;
        
        if (_damagedCooldown > 0)
        {
            _damagedCooldown -= Time.deltaTime;
        }
        
        if (_damagedCooldown <= 0 &&  _regenShieldCoroutine == null && _currentShieldHealth < _maxShieldHealth)
        {
            StartShieldRegen();
        }
    }
    
    

    private void Die()
    {
        deathSfx?.Play(audioSource);
        rumbleSource.Rumble(0.2f,0.2f, 1f);
        OnDeath?.Invoke();
    }

    #endregion Damage ----------------------------------------------------------------------
    
    
    #region Shield  --------------------------------------------------------------------------------------

    
    private IEnumerator RegenShieldRoutine()
    {
        shieldStartRegenSfx?.Play(audioSource);
        
        while (_currentShieldHealth < _maxShieldHealth)
        {
            _currentShieldHealth += shieldRegenRate * Time.deltaTime;
            if (_currentShieldHealth >= _maxShieldHealth)
            {
                _currentShieldHealth = _maxShieldHealth;
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
        if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }
        healthHealedSfx?.Play(audioSource);
        
        OnHealthChanged?.Invoke(_currentHealth);
    }
    
    [Button]
    private void HealShield(float amount = 25f)
    {
        if (_currentShieldHealth >= _maxShieldHealth) return;
        
        _currentShieldHealth += amount;
        if (_currentShieldHealth >= _maxShieldHealth)
        {
            _currentShieldHealth = _maxShieldHealth;
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

    
    #region Curreny --------------------------------------------------------------------------------

    [Button]
    private void UpdateCurrency(int amount)
    {
        _currentCurrency += amount;
        OnCurrencyChanged?.Invoke(_currentCurrency);
    }

    #endregion Curreny --------------------------------------------------------------------------------
    
    
    #region Resource Collection --------------------------------------------------------------------------------------

    private void CheckResourcesInRange()
    {
        if (!IsAlive()) return;
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, magnetRadius);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out Resource resource))
            {
                if (!resource || _resourcesInRange.Contains(resource)) continue;
                _resourcesInRange.Add(resource);
                resource.SetMagnetized(transform);
            }
        }
        
        for (int i = _resourcesInRange.Count - 1; i >= 0; i--)
        {
            var resource = _resourcesInRange[i];
            
            if (!resource)
            {
                _resourcesInRange.RemoveAt(i);
                continue;
            }
    

            var distance = Vector3.Distance(transform.position, resource.transform.position);
            if (distance > magnetRadius)
            {
                resource.ReleaseFromMagnetization();
                _resourcesInRange.RemoveAt(i);
            }
        }
    }
    
    
    private void CollectResource(Resource resource)
    {
        if (!resource) return;
        
        if (_collectionActions.TryGetValue(resource.ResourceType, out var action))
        {
            action(resource);
        }
        
        _resourcesInRange.Remove(resource);
        resource.ResourceCollected();
        OnResourceCollected?.Invoke(resource);
    }
    

    #endregion Resource Collection --------------------------------------------------------------------------------------

    #region Upgrades -----------------------------------------------------------------------------------------


    private int TotalHealthUpgrades()
    {
        var health = 0;
        
        foreach (var upgrade in healthUpgrades)
        {
            if (SaveManager.HasStoreItem(upgrade.ItemID))
            {
                health += upgrade.HealthUpgradeAmount;
            }
        }
        
        return health;
    }
    
    private float TotalShieldUpgrades()
    {
        var shield = 0f;

        foreach (var upgrade in shieldUpgrades)
        {
            if (SaveManager.HasStoreItem(upgrade.ItemID))
            {
                shield += upgrade.ShieldUpgradeAmount;
            }
        }

        return shield;
    }
    
    private float TotalResourceMagnetUpgrades()
    {
        var  magnet = 0f;
        
        foreach (var upgrade in resourceMagnetUpgrades)
        {
            if (SaveManager.HasStoreItem(upgrade.ItemID))
            {
                magnet += upgrade.MagnetUpgradeAmount;
            }
        }

        return magnet;
    }

    

    #endregion Upgrades -----------------------------------------------------------------------------------------

    
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
    
    
    public ChickenController GetTarget(float radius)
    {
        return playerAiming.CurrentAimLockTarget ? playerAiming.CurrentAimLockTarget : playerAiming.GetEnemyTarget(radius);
    }
    
    public ChickenController[] GetAllTargets(int maxTargets, float radius)
    {
        return playerAiming.GetEnemyTargets(maxTargets, radius);
    }
    
    public Transform GetFollowCameraTarget()
    {
        return followCameraTarget;
    }
    
    public Transform GetRandomCameraPosition()
    {
        switch (cameraPositions.childCount)
        {
            case 0:
                return null;
            case 1:
                cameraPositions.GetChild(1);
                break;
        }

        int randomIndex = UnityEngine.Random.Range(0, cameraPositions.childCount);
        return cameraPositions.GetChild(randomIndex);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
        UnityEditor.Handles.Label(transform.position + (Vector3.up * magnetRadius), "Magnet Radius");
    }


#endif
    #endregion Editor  --------------------------------------------------------------------------------------

}
