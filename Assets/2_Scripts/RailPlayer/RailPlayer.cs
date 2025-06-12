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
    
    [Header("Shield")]
    [SerializeField, Min(0)] private float maxShieldHealth = 100f;
    [SerializeField, Min(0)] private float shieldRegenRate = 5f;
    [SerializeField, Min(0)] private float shieldRegenDelay = 4f;
    
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
    [SerializeField, Child] private TextMeshProUGUI playerStatusText;

    private readonly List<Resource> _resourcesInRange = new List<Resource>();
    private int _currentHealth;
    private int _currentCurrency;
    private float _currentShieldHealth;
    private float _damagedCooldown;
    private Coroutine _regenShieldCoroutine;
    
    public bool AlignToSplineDirection => alignToSplineDirection;
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
            shieldDepletedSfx?.Play(audioSource);
        }
        else
        {
            shieldDamageSfx?.Play(audioSource);
        }
    }
    
    private void DamageHealth()
    {
        if (!IsAlive() || !IsDodging()) return;

        _currentHealth -= 1;
        if (_currentHealth < 0)
        {
            _currentHealth = 0;
            Die();
        }
        else
        {
            healthDamageSfx?.Play(audioSource);
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
        deathSfx?.Play(audioSource);
        Debug.Log("Player has died!");
    }

    #endregion Damage ----------------------------------------------------------------------


    #region Healing ----------------------------------------------------------------------

    
    [Button]
    private void HealHealth(int amount = 1)
    {
        if (amount <= 0 || !IsAlive()) return;
        
        _currentHealth += amount;
        if (_currentHealth > maxHealth)
        {
            _currentHealth = maxHealth;
        }
        healthHealedSfx?.Play(audioSource);
    }
    
    [Button]
    private void HealShield(float amount = 25f)
    {
        if (HasShield()) return;
        
        _currentShieldHealth += amount;
        if (_currentShieldHealth >= maxShieldHealth)
        {
            _currentShieldHealth = maxShieldHealth;
            shieldRegeneratedSfx?.Play(audioSource);
        }
        
        StartShieldRegen();
    }
    

    #endregion Healing ----------------------------------------------------------------------
    
    
    #region Shield  --------------------------------------------------------------------------------------

    
    private IEnumerator RegenShieldRoutine()
    {
        
        while (_currentShieldHealth < maxShieldHealth)
        {
            _currentShieldHealth += shieldRegenRate * Time.deltaTime;
            if (_currentShieldHealth >= maxShieldHealth)
            {
                _currentShieldHealth = maxShieldHealth;
                shieldRegeneratedSfx?.Play(audioSource);
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
        
        shieldStartRegenSfx?.Play(audioSource);
        
    }
    
    

    #endregion Shield  --------------------------------------------------------------------------------------
    
    
    #region Resource Collection --------------------------------------------------------------------------------------

    private void CheckResourcesInRange()
    {
        
        // Find all resources in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, magnetRadius);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out Resource resource))
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
            bool isWithinRange = Vector3.Distance(transform.position, _resourcesInRange[i].transform.position) <= magnetRadius;
            bool isBehindPlayer = LevelManager.Instance.GetPositionOnSpline(transform.position) > LevelManager.Instance.GetPositionOnSpline(_resourcesInRange[i].transform.position);
            if (!_resourcesInRange[i] || !isWithinRange  || isBehindPlayer)
            {
                if (!_resourcesInRange[i]) continue;
                
                _resourcesInRange[i].SetMagnetized(false);
                _resourcesInRange.RemoveAt(i);
            }
        }

    }
    
    private void UpdateMagnetizedResources()
    {
        if (_resourcesInRange.Count == 0) return;
    
        // Iterate through all resources in range
        for (int i = _resourcesInRange.Count - 1; i >= 0; i--)
        {
            var resource = _resourcesInRange[i];
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
                _currentCurrency += resource.CurrencyWorth;
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
    
    public Vector3 GetAimDirection()
    {
        return playerAiming.GetAimDirection();
    }
    
    public ChickenController GetTarget(float radius = 3)
    {
        return playerAiming.GetEnemyTarget(radius);
    }
    
    public ChickenController[] GetAllTargets(int maxTargets, float radius = 3)
    {
        return playerAiming.GetEnemyTargets(maxTargets, radius);
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
                selectedWeapon = $"{playerWeapon.BaseWeapon.WeaponName}";
            }

            
            playerStatusText.text = $"Health: {_currentHealth}/{maxHealth}\n" +
                                    $"Shield: {_currentShieldHealth:F1} / {maxShieldHealth:F1}, Regen: {_damagedCooldown}\n" +
                                    $"Alive: {IsAlive()}\n" +
                                    $"Shielded: {HasShield()}\n" +
                                    $"Weapon: {selectedWeapon}\n" +
                                    $"Currency: {_currentCurrency}"
                ;
        }
    }
    
    

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
