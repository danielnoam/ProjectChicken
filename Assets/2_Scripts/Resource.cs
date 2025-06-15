using System;
using KBCore.Refs;
using UnityEngine;
using VInspector;

[System.Serializable]
public class WeaponChance 
{
    public SOWeapon weapon;
    [Range(0, 100)] public int chance = 10;
    public bool isLocked;
    public string displayName;
    
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        return weapon ? weapon.name : "No Weapon";
    }
}


[RequireComponent(typeof(AudioSource))]
public class Resource : MonoBehaviour
{


    [Header("General Settings")]
    [Tooltip("Time before the resource destroys itself (0 = unlimited time)"), SerializeField, Min(0)] private float lifetime = 10f;
    
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float rotationSpeed = 45f;
    [SerializeField] private bool conformToSpline = true;
    [EnableIf("conformToSpline")]
    [SerializeField] private float pathFollowSpeed = 3f;
    [EndIf]
    
    [Header("Spawn Effects")]
    [SerializeField] private SOAudioEvent spawnSfx;
    [SerializeField] private ParticleSystem spawnEffect;
    
    [Header("Collection Effects")]
    [SerializeField] private SOAudioEvent collectionSfx;
    [SerializeField] private ParticleSystem collectionEffect;
    
    
    [Header("Resource Settings")]
    [SerializeField] private ResourceType resourceType;
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.Currency)] private int currencyWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.HealthPack)] private int healthWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.ShieldPack)] private int shieldWorth = 50;[EndIf]
    [SerializeField, ShowIf("resourceType", ResourceType.SpecialWeapon)] private WeaponChance[] weaponChances = Array.Empty<WeaponChance>();[EndIf]
    
    
    [Header("References")]
    [SerializeField, Self] private AudioSource audioSource;
    
    private float _currentLifetime;
    private bool _isMagnetized;
    private Vector3 _splineOffset;
    private Vector3 _rotationAxis;
    
    public ResourceType ResourceType => resourceType;
    public int HealthWorth => healthWorth;
    public int ShieldWorth => shieldWorth;
    public int CurrencyWorth => currencyWorth;
    public SOWeapon Weapon { get; private set;}

    
    
    private void OnValidate()
    {
        
        this.ValidateRefs();
        
        if (resourceType == ResourceType.SpecialWeapon && weaponChances is { Length: > 0 })
        {
            NormalizeWeaponChances();
        }
    }
    

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        
        GetSplineOffset();
        PlaySpawnEffects();
    }


    private void Update()
    {
        Rotate();
        MoveAlongSpline();
        CheckLifetime();
    }
    

    private void Initialize()
    {
        
        _currentLifetime = lifetime;
        _isMagnetized = false;
        _splineOffset = Vector3.zero;
        _rotationAxis = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
        
        if (resourceType == ResourceType.SpecialWeapon && weaponChances.Length > 0)
        {
            Weapon = SelectRandomWeapon();
        }
    }

    #region State Management ---------------------------------------------------------------------------------------

    private void CheckLifetime()
    {
        if (lifetime <= 0f || _isMagnetized) return;

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
        if (_isMagnetized) return;

        if (LevelManager.Instance && conformToSpline)
        {
            // Get the spline direction at the current position
            Vector3 splineDirection = GetSplineDirectionAtCurrentPosition();
        
            // Move in the opposite direction of the spline flow
            Vector3 movementDirection = -splineDirection;
            
            // Move the resource along the spline
            transform.position += movementDirection * (pathFollowSpeed * Time.deltaTime);
            
        }
    }
    
    private Vector3 GetSplineDirectionAtCurrentPosition()
    {
        if (!LevelManager.Instance) return Vector3.forward;
        
    
        // Get the current T value on the spline based on our current position
        float currentT = LevelManager.Instance.GetCurrentSplineT(transform.position);
    
        // Get the tangent (direction) at this point on the spline
        Vector3 tangent = LevelManager.Instance.EvaluateTangentOnSpline(currentT);
    
        return tangent;
    }
    
    private void GetSplineOffset()
    {
        if (LevelManager.Instance && LevelManager.Instance.CurrentPositionOnPath)
        {
            _splineOffset = LevelManager.Instance.CurrentPositionOnPath.position - transform.position;
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
        if (spawnSfx)
        {
            spawnSfx.Play(audioSource);
        }
        
        if (spawnEffect)
        {
            Instantiate(spawnEffect, transform.position, Quaternion.identity);
        }
    }
    
    private void PlayCollectionEffects()
    {
        if (collectionSfx)
        {
            collectionSfx.PlayAtPoint(transform.position);
        }
        
        if (collectionEffect)
        {
            Instantiate(collectionEffect, transform.position, Quaternion.identity);
        }
    }

    #endregion Effects ---------------------------------------------------------------------------------------

    
    
    #region Weapon Selection ---------------------------------------------------------------------------------------
    
    
    private SOWeapon SelectRandomWeapon()
    {
        if (weaponChances.Length == 0) return null;
    
        // Filter out weapons with null references
        var validWeapons = new System.Collections.Generic.List<WeaponChance>();
        foreach (var weaponChance in weaponChances)
        {
            if (weaponChance.weapon && weaponChance.chance > 0) // Changed from 0f to 0
            {
                validWeapons.Add(weaponChance);
            }
        }
    
        if (validWeapons.Count == 0) return null;
    
        // Calculate total weight
        int totalWeight = 0; // Changed from float to int
        foreach (var weaponChance in validWeapons)
        {
            totalWeight += weaponChance.chance;
        }
    
        if (totalWeight <= 0) return validWeapons[0].weapon; // Changed from 0f to 0
    
        // Select a random weapon based on weights
        int randomValue = UnityEngine.Random.Range(0, totalWeight + 1); // Changed to int range
        int currentWeight = 0; // Changed from float to int
    
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
        
        // Separate locked and unlocked entries (only those with valid weapons)
        var unlockedEntries = new System.Collections.Generic.List<WeaponChance>();
        int lockedTotal = 0;
        
        foreach (var weaponChance in weaponChances)
        {
            // Only consider entries with valid weapons
            if (weaponChance.weapon)
            {
                if (weaponChance.isLocked)
                {
                    lockedTotal += Mathf.Max(0, weaponChance.chance);
                }
                else
                {
                    unlockedEntries.Add(weaponChance);
                }
            }
        }
        
        // If all valid entries are locked, don't normalize
        if (unlockedEntries.Count == 0) return;
        
        // Calculate remaining percentage for unlocked entries
        int remainingPercentage = Mathf.Max(0, 100 - lockedTotal);
        
        // Calculate the total of unlocked chances
        int unlockedTotal = 0;
        foreach (var weaponChance in unlockedEntries)
        {
            unlockedTotal += Mathf.Max(0, weaponChance.chance);
        }
        
        // If the unlocked total is 0, set equal chances for unlocked entries
        if (unlockedTotal <= 0)
        {
            int equalChance = remainingPercentage / unlockedEntries.Count;
            int remainder = remainingPercentage % unlockedEntries.Count;
            
            for (int i = 0; i < unlockedEntries.Count; i++)
            {
                unlockedEntries[i].chance = equalChance + (i < remainder ? 1 : 0);
            }
        }
        // If the unlocked total doesn't match the remaining percentage, normalize unlocked entries
        else if (unlockedTotal != remainingPercentage)
        {
            int newTotal = 0;
            
            // First pass: calculate normalized values for unlocked entries only
            foreach (var weaponChance in unlockedEntries)
            {
                int normalizedChance = Mathf.RoundToInt((weaponChance.chance / (float)unlockedTotal) * remainingPercentage);
                weaponChance.chance = normalizedChance;
                newTotal += normalizedChance;
            }
            
            // Second pass: adjust for rounding errors to ensure unlocked total = remainingPercentage
            int difference = remainingPercentage - newTotal;
            if (difference != 0 && unlockedEntries.Count > 0)
            {
                // Sort unlocked entries by current chance value (descending) to adjust larger values first
                unlockedEntries.Sort((a, b) => b.chance.CompareTo(a.chance));
                
                // Distribute the difference, ensuring no negative values
                for (int i = 0; i < Mathf.Abs(difference) && i < unlockedEntries.Count; i++)
                {
                    if (difference > 0)
                    {
                        unlockedEntries[i].chance += 1;
                    }
                    else if (unlockedEntries[i].chance > 0) // Only subtract if we won't go negative
                    {
                        unlockedEntries[i].chance -= 1;
                    }
                }
            }
        }
        
        // Final safety check: ensure no negative values in all entries
        foreach (var weaponChance in weaponChances)
        {
            if (weaponChance.chance < 0)
            {
                weaponChance.chance = 0;
            }
        }
    }
    
    
    
    #endregion Weapon Selection ---------------------------------------------------------------------------------------

    
}