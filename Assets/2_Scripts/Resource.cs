using System;
using UnityEngine;
using VInspector;

[Serializable]
public class WeaponChance 
{
    public SOWeaponData weaponData;
    [Range(0f, 100f)] public float chance = 10f;
}


public class Resource : MonoBehaviour
{


    [Header("General Settings")]
    [Tooltip("Time before the resource destroys itself (0 = unlimited time)"), SerializeField, Min(0)] private float lifetime = 10f;
    
    [Header("Movement Settings")]
    [SerializeField] private bool alignToSplineDirection = true;
    [SerializeField,EnableIf("alignToSplineDirection")] private float pathFollowSpeed = 5f;[EndIf]
    [SerializeField, Min(0)] private float rotationSpeed = 45f;

    
    [Header("Spawn Effects")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private ParticleSystem spawnEffect;
    
    [Header("Collection Effects")]
    [SerializeField] private AudioClip collectionSound;
    [SerializeField] private ParticleSystem collectionEffect;
    
    
    [Header("Resource Settings")]
    [SerializeField] private ResourceType resourceType;
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.Currency)] private int currencyWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.HealthPack)] private int healthWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.ShieldPack)] private int shieldWorth = 50;[EndIf]
    [SerializeField, ShowIf("resourceType", ResourceType.SpecialWeapon)] private WeaponChance[] weaponChances = Array.Empty<WeaponChance>();[EndIf]
    
    
    
    private float _currentLifetime;
    private bool _isMagnetized;
    private Vector3 _splineOffset;
    private Vector3 _rotationAxis = Vector3.up;
    
    public ResourceType ResourceType => resourceType;
    public int HealthWorth => healthWorth;
    public int ShieldWorth => shieldWorth;
    public int CurrencyWorth => currencyWorth;
    public SOWeaponData WeaponData { get; private set;}


    private void Awake()
    {
        _currentLifetime = lifetime;
        _rotationAxis = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
        
        if (resourceType == ResourceType.SpecialWeapon && weaponChances.Length > 0)
        {
            WeaponData = SelectRandomWeapon();
        }
        
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

        if (LevelManager.Instance && LevelManager.Instance.SplineContainer && alignToSplineDirection)
        {
            // Get the spline direction at the current position
            Vector3 splineDirection = GetSplineDirectionAtCurrentPosition();
        
            // Move in the opposite direction of the spline flow
            Vector3 movementDirection = -splineDirection;
        
            // Apply movement
            transform.position += movementDirection * (pathFollowSpeed * Time.deltaTime);
        }
    }
    
    private Vector3 GetSplineDirectionAtCurrentPosition()
    {
        if (!LevelManager.Instance || !LevelManager.Instance.SplineContainer) return Vector3.forward;

        var splineContainer = LevelManager.Instance.SplineContainer;
    
        // Get the current T value on the spline based on our current position
        UnityEngine.Splines.SplineUtility.GetNearestPoint(
            splineContainer.Spline, 
            transform.position, 
            out var nearestPoint, 
            out var t
        );
    
        // Get the tangent (direction) at this point on the spline
        Vector3 tangent = splineContainer.EvaluateTangent(t);
    
        return tangent.normalized;
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

    
    #region WeaponData Selection ---------------------------------------------------------------------------------------
    
    
    private void OnValidate()
    {
        if (resourceType == ResourceType.SpecialWeapon && weaponChances is { Length: > 0 })
        {
            NormalizeWeaponChances();
        }
    }
    
    private SOWeaponData SelectRandomWeapon()
    {
        if (weaponChances.Length == 0) return null;
        
        // Filter out weapons with null references
        var validWeapons = new System.Collections.Generic.List<WeaponChance>();
        foreach (var weaponChance in weaponChances)
        {
            if (weaponChance.weaponData && weaponChance.chance > 0f)
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
        
        if (totalWeight <= 0f) return validWeapons[0].weaponData;
        
        // Select a random weaponData based on weights
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var weaponChance in validWeapons)
        {
            currentWeight += weaponChance.chance;
            if (randomValue <= currentWeight)
            {
                return weaponChance.weaponData;
            }
        }
        
        // Fallback
        return validWeapons[0].weaponData;
    }
    
    private void NormalizeWeaponChances()
    {
        if (weaponChances.Length == 0) return;
        
        // Calculate the total of all valid chances
        float totalChance = 0f;
        int validWeaponCount = 0;
        
        foreach (var weaponChance in weaponChances)
        {
            if (weaponChance.weaponData)
            {
                totalChance += Mathf.Max(0f, weaponChance.chance);
                validWeaponCount++;
            }
        }
        
        if (validWeaponCount == 0) return;
        
        // If the total is 0, set equal chances
        if (totalChance <= 0f)
        {
            float equalChance = 100f / validWeaponCount;
            foreach (var weaponChance in weaponChances)
            {
                if (weaponChance.weaponData)
                {
                    weaponChance.chance = equalChance;
                }
            }
        }
        // If the total is not 100, normalize to 100%
        else if (Mathf.Abs(totalChance - 100f) > 0.01f)
        {
            foreach (var weaponChance in weaponChances)
            {
                if (weaponChance.weaponData)
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
            if (weaponChance.weaponData)
            {
                weaponChance.chance = equalChance;
            }
        }
    }
    
    #endregion WeaponData Selection ---------------------------------------------------------------------------------------

    
}