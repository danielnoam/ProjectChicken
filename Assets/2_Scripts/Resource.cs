using System;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;

[Serializable]
public class WeaponChance 
{
    public SOWeapon weapon;
    [Range(0f, 100f)] public float chance = 10f;
}


public class Resource : MonoBehaviour
{

    [Header("Resource Settings")]
    [Tooltip("Time before the resource destroys itself (0 = unlimited time)"), SerializeField, Min(0)] private float lifetime = 10f;
    [SerializeField] private ResourceType resourceType;
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.Currency)] private int currencyWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.HealthPack)] private int healthWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.ShieldPack)] private int shieldWorth = 50;[EndIf]
    [SerializeField, ShowIf("resourceType", ResourceType.SpecialWeapon)] private WeaponChance[] weaponChances = Array.Empty<WeaponChance>();[EndIf]
    
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float moveSpeed = 5f;
    [SerializeField, Min(0)] private float rotationSpeed = 45f;
    
    [Header("Spawn Effects")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private ParticleSystem spawnEffect;
    
    [Header("Collection Effects")]
    [SerializeField] private AudioClip collectionSound;
    [SerializeField] private ParticleSystem collectionEffect;

    
    
    
    private float _currentLifetime;
    private bool _isMagnetized;
    private Vector3 _rotationAxis = Vector3.up;
    
    public ResourceType ResourceType => resourceType;
    public int HealthWorth => healthWorth;
    public int ShieldWorth => shieldWorth;
    public int CurrencyWorth => currencyWorth;
    public SOWeapon Weapon { get; private set;}


    private void Awake()
    {
        _currentLifetime = lifetime;
        _rotationAxis = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
        
        if (resourceType == ResourceType.SpecialWeapon && weaponChances.Length > 0)
        {
            Weapon = SelectRandomWeapon();
        }
        
    }

    private void Start()
    {
        PlaySpawnEffects();
    }


    private void Update()
    {
        Rotate();
        MoveAlongSpline();
        CheckLifetime();
    }
    


    #region State Management ---------------------------------------------------------------------------------------

    private void CheckLifetime()
    {
        if (lifetime <= 0f) return;

        _currentLifetime -= Time.deltaTime;
        if (_currentLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void SetMagnetized(bool magnetized)
    {
        _isMagnetized = magnetized;
    }
    
    public void ResourceCollected()
    {
        
        PlayCollectionEffects();

        Destroy(gameObject);
    }

    #endregion State Management ---------------------------------------------------------------------------------------
     
    

    #region Movement ---------------------------------------------------------------------------------------

    private void Rotate()
    {
        if (rotationSpeed <= 0f) return;
        
        transform.Rotate(_rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        
    }
    
    private void MoveAlongSpline()
    {
        if (!_isMagnetized) return;


        if (LevelManager.Instance && LevelManager.Instance.SplineContainer)
        {
            // Move along the spline
        }

    }
    
    
    public void MoveTowardsPlayer(Vector3 playerPosition, float speed)
    {
        Vector3 direction = (playerPosition - transform.position).normalized;
        transform.position += direction * (speed * Time.deltaTime);
    }
    
    

    #endregion Movement ---------------------------------------------------------------------------------------
    

    #region Effects ---------------------------------------------------------------------------------------


    private void PlaySpawnEffects()
    {
        if (spawnSound)
        {
            AudioSource.PlayClipAtPoint(spawnSound, transform.position);
        }
        
        if (spawnEffect)
        {
            Instantiate(spawnEffect, transform.position, Quaternion.identity);
        }
    }
    
    private void PlayCollectionEffects()
    {
        if (collectionSound)
        {
            AudioSource.PlayClipAtPoint(collectionSound, transform.position);
        }
        
        if (collectionEffect)
        {
            Instantiate(collectionEffect, transform.position, Quaternion.identity);
        }
    }

    #endregion Effects ---------------------------------------------------------------------------------------

    #region Weapon Selection ---------------------------------------------------------------------------------------
    
    
    private void OnValidate()
    {
        if (resourceType == ResourceType.SpecialWeapon && weaponChances is { Length: > 0 })
        {
            NormalizeWeaponChances();
        }
    }
    
    private SOWeapon SelectRandomWeapon()
    {
        if (weaponChances.Length == 0) return null;
        
        // Filter out weapons with null references
        var validWeapons = new System.Collections.Generic.List<WeaponChance>();
        foreach (var weaponChance in weaponChances)
        {
            if (weaponChance.weapon && weaponChance.chance > 0f)
            {
                validWeapons.Add(weaponChance);
            }
        }
        
        if (validWeapons.Count == 0) return null;
        
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var weaponChance in validWeapons)
        {
            totalWeight += weaponChance.chance;
        }
        
        if (totalWeight <= 0f) return validWeapons[0].weapon;
        
        // Select random weapon based on weights
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var weaponChance in validWeapons)
        {
            currentWeight += weaponChance.chance;
            if (randomValue <= currentWeight)
            {
                return weaponChance.weapon;
            }
        }
        
        // Fallback
        return validWeapons[0].weapon;
    }
    
    private void NormalizeWeaponChances()
    {
        if (weaponChances.Length == 0) return;
        
        // Calculate total of all valid chances
        float totalChance = 0f;
        int validWeaponCount = 0;
        
        for (int i = 0; i < weaponChances.Length; i++)
        {
            if (weaponChances[i].weapon)
            {
                totalChance += Mathf.Max(0f, weaponChances[i].chance);
                validWeaponCount++;
            }
        }
        
        if (validWeaponCount == 0) return;
        
        // If total is 0, set equal chances
        if (totalChance <= 0f)
        {
            float equalChance = 100f / validWeaponCount;
            for (int i = 0; i < weaponChances.Length; i++)
            {
                if (weaponChances[i].weapon)
                {
                    weaponChances[i].chance = equalChance;
                }
            }
        }
        // If total is not 100, normalize to 100%
        else if (Mathf.Abs(totalChance - 100f) > 0.01f)
        {
            foreach (var weaponChance in weaponChances)
            {
                if (weaponChance.weapon)
                {
                    weaponChance.chance = (weaponChance.chance / totalChance) * 100f;
                }
            }
        }
    }
    
    [Button]
    private void EqualizeWeaponsChances()
    {
        if (weaponChances.Length == 0) return;
        
        float equalChance = 100f / weaponChances.Length;
        foreach (var weaponChance in weaponChances)
        {
            if (weaponChance.weapon)
            {
                weaponChance.chance = equalChance;
            }
        }
    }
    
    #endregion Weapon Selection ---------------------------------------------------------------------------------------

    
}