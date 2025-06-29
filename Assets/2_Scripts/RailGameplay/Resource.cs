using System;
using KBCore.Refs;
using UnityEngine;
using VInspector;
using PrimeTween;

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


[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class Resource : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("Time before the resource destroys itself (0 = unlimited time)"), SerializeField, Min(0)] private float lifetime = 20f;
    [SerializeField, Min(0)] private float rotationSpeed = 45f;
    [SerializeField, Min(0.1f)] private float acceleration = 12f;
    [SerializeField, Min(0.1f)] private float deceleration = 4f;
    [SerializeField] private float magnetizedSpeed = 20f;
    
    [Header("Resource Settings")]
    [SerializeField] private int scoreWorth = 50;
    [SerializeField] private ResourceType resourceType;
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.Currency)] private int currencyWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.HealthPack)] private int healthWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.ShieldPack)] private int shieldWorth = 50;[EndIf]
    [SerializeField, ShowIf("resourceType", ResourceType.SpecialWeapon)] private WeaponChance[] weaponChances = Array.Empty<WeaponChance>();[EndIf]
    
    [Header("Effects")]
    [SerializeField] private SOAudioEvent spawnSfx;
    [SerializeField] private ParticleSystem spawnEffect;
    [SerializeField] private SOAudioEvent collectionSfx;
    [SerializeField] private ParticleSystem collectionEffect;
    [SerializeField] private float spawnGrowDuration = 1f;
    [SerializeField] private float magnetizedPunchStrength = 1f;
    [SerializeField] private float magnetizedPunchDuration = 0.5f;
    
    [Header("References")]
    [SerializeField, Self] private AudioSource audioSource;
    

    private Transform _playerTransform;
    private bool _isMagnetized;
    private float _currentLifetime;
    private float _currentMagnetizedSpeed;
    private float _targetMagnetizedSpeed;
    private Vector3 _rotationAxis;
    private Tween _scaleTween;
    private float MovementBoundaryX => LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.x : 10f;
    private float MovementBoundaryY => LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.y : 6f;
    
    public ResourceType ResourceType => resourceType;
    public int ScoreWorth => scoreWorth;
    public int HealthWorth => healthWorth;
    public int ShieldWorth => shieldWorth;
    public int CurrencyWorth => currencyWorth;
    public SOWeapon Weapon { get; private set;}

    
    
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
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
    

    private void Update()
    {
        Rotate();
        HandleMovement();
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

    public void SetMagnetized(Transform playerTransform, float magnetDistanceRange)
    {
        _isMagnetized = true;
        _playerTransform = playerTransform;
        _targetMagnetizedSpeed = magnetizedSpeed;
    
        // Play magnetized punch effect
        if (_scaleTween.isAlive) _scaleTween.Stop();
        transform.localScale = Vector3.one;
        _scaleTween = Tween.PunchScale(transform, Vector3.one * magnetizedPunchStrength, duration: magnetizedPunchDuration);
    }
    
    
    public void ReleaseFromMagnetization()
    {
        _isMagnetized = false;
        _playerTransform = null;
        _targetMagnetizedSpeed = 0f;
    }
    
    public void ResourceCollected()
    {
        PlayCollectionEffects();
        Destroy(gameObject);
    }
    
    private void Initialize()
    {
        _currentLifetime = lifetime;
        _isMagnetized = false;
        _playerTransform = null;
        _currentMagnetizedSpeed = 0f;
        _targetMagnetizedSpeed = 0f;
        
        _rotationAxis = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
        
        if (resourceType == ResourceType.SpecialWeapon && weaponChances.Length > 0)
        {
            Weapon = SelectRandomWeapon();
        }
        
        // Check if spawn position is outside boundary and move inside if needed
        CheckAndMoveToBoundary();
        
        PlaySpawnEffects();
    }

    #endregion State Management ---------------------------------------------------------------------------------------
    

    
    #region Movement ---------------------------------------------------------------------------------------

    private void Rotate()
    {
        if (rotationSpeed <= 0f) return;
        
        transform.Rotate(_rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
    


    private void CheckAndMoveToBoundary()
    {
        if (!LevelManager.Instance) return;
        
        Vector3 constrainedPosition = ConstrainToBoundary(transform.position);
        
        // Only move if the position was actually constrained
        if (Vector3.Distance(transform.position, constrainedPosition) > 0.01f)
        {
            transform.position = constrainedPosition;
            Debug.Log($"Resource moved from boundary violation: {gameObject.name}");
        }
    }

    private Vector3 ConstrainToBoundary(Vector3 proposedPosition)
    {
        if (!LevelManager.Instance) return proposedPosition;

        // Use the same reference point as the player: CurrentPositionOnPath.position
        Vector3 splineReferencePosition = LevelManager.Instance.CurrentPositionOnPath.position;
        
        // Calculate the spline rotation the same way the player does
        Vector3 splineForward = LevelManager.Instance.GetSplineTangentAtPosition(splineReferencePosition);
        Quaternion splineRotation = splineForward != Vector3.zero ? 
            Quaternion.LookRotation(splineForward, Vector3.up) : Quaternion.identity;

        // Calculate the current offset from the player's spline position in local space
        // Use PlayerPosition as reference (same as player movement code)
        Vector3 playerSplinePosition = LevelManager.Instance.PlayerPosition;
        Vector3 worldOffset = proposedPosition - playerSplinePosition;
        Vector3 localOffset = Quaternion.Inverse(splineRotation) * worldOffset;

        // Store original offset for debugging
        Vector3 originalLocalOffset = localOffset;
        
        // Only clamp the X and Y components (lateral movement), preserve Z
        float originalZ = localOffset.z;
        localOffset.x = Mathf.Clamp(localOffset.x, -MovementBoundaryX, MovementBoundaryX);
        localOffset.y = Mathf.Clamp(localOffset.y, -MovementBoundaryY, MovementBoundaryY);
        localOffset.z = originalZ; // Preserve the forward/backward offset

        // Convert the clamped offset back to world space
        Vector3 constrainedWorldPosition = playerSplinePosition + (splineRotation * localOffset);

        // Debug info when clamping occurs
        if (Mathf.Abs(originalLocalOffset.x - localOffset.x) > 0.01f || Mathf.Abs(originalLocalOffset.y - localOffset.y) > 0.01f)
        {
            Debug.Log($"Resource boundary clamping: {gameObject.name} - Original local offset: {originalLocalOffset}, Clamped: {localOffset}");
        }

        return constrainedWorldPosition;
    }

    private void HandleMovement()
    {
        // Update magnetized speed
        if (_isMagnetized)
        {
            _currentMagnetizedSpeed = Mathf.Lerp(_currentMagnetizedSpeed, _targetMagnetizedSpeed, acceleration * Time.deltaTime);
        }
        else if (_currentMagnetizedSpeed > 0f)
        {
            _currentMagnetizedSpeed = Mathf.Lerp(_currentMagnetizedSpeed, 0f, deceleration * Time.deltaTime);
            if (_currentMagnetizedSpeed <= 0.1f)
            {
                _currentMagnetizedSpeed = 0f;
            }
        }

        // Move towards player if magnetized
        if (_currentMagnetizedSpeed > 0f && _playerTransform)
        {
            Vector3 playerDirection = (_playerTransform.position - transform.position).normalized;
            Vector3 proposedPosition = transform.position + playerDirection * (_currentMagnetizedSpeed * Time.deltaTime);
            
            // Constrain the proposed position to boundary
            Vector3 constrainedPosition = ConstrainToBoundary(proposedPosition);
            transform.position = constrainedPosition;
        }
        else
        {
            // Even when not magnetized, make sure we stay within bounds
            // This handles cases where the resource might drift due to physics or other factors
            Vector3 constrainedPosition = ConstrainToBoundary(transform.position);
            if (Vector3.Distance(transform.position, constrainedPosition) > 0.01f)
            {
                transform.position = constrainedPosition;
            }
        }
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
        
        if (_scaleTween.isAlive) _scaleTween.Stop();
        _scaleTween = Tween.Scale(transform, startValue: Vector3.zero, endValue:Vector3.one, duration: spawnGrowDuration, ease: Ease.InOutBounce);
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