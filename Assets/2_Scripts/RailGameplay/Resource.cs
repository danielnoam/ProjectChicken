using System;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using VInspector;
using PrimeTween;

[System.Serializable]
public class WeaponChance 
{
    public SOWeaponData weaponData;
    [Range(0, 100)] public int chance = 10;
    public bool isLocked;
    public string displayName;
    
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        return weaponData ? weaponData.name : "No Weapon";
    }
}


[SelectionBase]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class Resource : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("Time before the resource destroys itself (0 = unlimited time)"), SerializeField, Min(0)] private float lifetime = 8f;
    [SerializeField, Min(0f)] private Vector2 spawnSpeedRange = new Vector2(50f,60f);
    [SerializeField, Min(0f)] private float splineMovementSpeed = 3f;
    [SerializeField, Min(0.1f)] private float deceleration = 4f;
    [SerializeField, Min(0.1f)] private float acceleration = 12f;
    [SerializeField, Min(0f)] private float magnetizedSpeed = 25f;
    [SerializeField, Min(0)] private float rotationSpeed = 45f;

    
    [Header("Resource Settings")]
    [SerializeField, Min(0)] private int scoreWorth = 50;
    [SerializeField] private ResourceType resourceType;
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.Currency)] private int currencyWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.HealthPack)] private int healthWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.ShieldPack)] private int shieldWorth = 50;[EndIf]
    [SerializeField, ShowIf("resourceType", ResourceType.SpecialWeapon)] private WeaponChance[] weaponChances = Array.Empty<WeaponChance>(); [EndIf] 
    
    [Header("Effects")]
    [SerializeField] private SOAudioEvent spawnSfx;
    [SerializeField] private ParticleSystem spawnEffect;
    [SerializeField] private SOAudioEvent collectionSfx;
    [SerializeField] private ParticleSystem collectionEffect;
    [SerializeField] private float spawnAnimationDuration = 1f;
    [SerializeField] private float despawnAnimationDuration = 2f;
    [SerializeField] private float magnetizedPunchStrength = 1f;
    [SerializeField] private float magnetizedPunchDuration = 0.5f;
    
    [Header("References")]
    [SerializeField] private Transform resourceGfx;
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;
    [SerializeField, Self, HideInInspector] private Rigidbody rigidBody;
    

    private Transform _playerTransform;
    private bool _isMagnetized;
    private float _currentLifetime;
    private Vector3 _rotationAxis;
    private Sequence _scaleAnimation;
    private Vector3 _currentVelocity;
    private Vector3 _targetVelocity;
    private Vector3 _currentOffsetFromSpline;
    private Quaternion _splineRotation = Quaternion.identity;
    
    private float MovementBoundaryX => LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.x : 10f;
    private float MovementBoundaryY => LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.y : 6f;
    
    public ResourceType ResourceType => resourceType;
    public int ScoreWorth => scoreWorth;
    public int HealthWorth => healthWorth;
    public int ShieldWorth => shieldWorth;
    public int CurrencyWorth => currencyWorth;
    public SOWeaponData WeaponData { get; private set;}

    
    
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
        CheckLifetime();
        RotateGfx();
        UpdateSplineRotation();
    }
    
    private void FixedUpdate()
    {
        HandleMovement();
    }





    #region State Management ---------------------------------------------------------------------------------------

    private void CheckLifetime()
    {
        if (lifetime <= 0f || _isMagnetized) return;

        _currentLifetime -= Time.deltaTime;
        
        if (_currentLifetime <= despawnAnimationDuration && !_scaleAnimation.isAlive)
        {
            PlayDespawnEffects();
        }
        
        if (_currentLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void SetMagnetized(Transform playerTransform)
    {
        _isMagnetized = true;
        _playerTransform = playerTransform;
    

        PlayMagnetizedEffects();
    }
    
    
    public void ReleaseFromMagnetization()
    {
        _isMagnetized = false;
        _playerTransform = null;
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
        
        _rotationAxis = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
        
        if (resourceType == ResourceType.SpecialWeapon && weaponChances.Length > 0)
        {
            WeaponData = SelectRandomWeapon();
        }
        
        Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
        float randomSpeed = UnityEngine.Random.Range(spawnSpeedRange.x, spawnSpeedRange.y);
        Vector3 localVelocity = new Vector3(randomDir.x, randomDir.y, 0f) * randomSpeed;
        _currentVelocity = _splineRotation * localVelocity;
        _targetVelocity = Vector3.zero;
        resourceGfx.eulerAngles = new Vector3(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f));
        
        PlaySpawnEffects();
    }
    


    #endregion State Management ---------------------------------------------------------------------------------------
    

    
    #region Movement ---------------------------------------------------------------------------------------

    private void UpdateSplineRotation()
    {
        if (!LevelManager.Instance) 
        {
            _splineRotation = Quaternion.identity;
            return;
        }
        Vector3 splineReferencePosition = LevelManager.Instance.CurrentPositionOnPath.position;
        Vector3 splineForward = LevelManager.Instance.GetSplineTangentAtPosition(splineReferencePosition);
        _splineRotation = splineForward != Vector3.zero ? Quaternion.LookRotation(splineForward, Vector3.up) : Quaternion.identity;
    }
    
    
    private void RotateGfx()
    {
        if (rotationSpeed <= 0f) return;
        
        resourceGfx.Rotate(_rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }


    private void HandleMovement()
    {
        if (!LevelManager.Instance) return;
        
        // Get current spline position and move backwards along it
        Vector3 currentSplinePosition = LevelManager.Instance.CurrentPositionOnPath.position;
        Vector3 backwardDirection = -LevelManager.Instance.GetSplineTangentAtPosition(currentSplinePosition);
        
        // Calculate spline velocity (not movement)
        Vector3 splineVelocity = backwardDirection * splineMovementSpeed;
        
        if (_isMagnetized && _playerTransform)
        {
            Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
            _targetVelocity = directionToPlayer * magnetizedSpeed;
            _currentVelocity = Vector3.Lerp(_currentVelocity, _targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);
            if (_currentVelocity.magnitude < 0.1f)
            {
                _currentVelocity = Vector3.zero;
            }
        }
        
        // Calculate proposed position (current movement + spline movement)
        Vector3 proposedPosition = transform.position + ((_currentVelocity + splineVelocity) * Time.fixedDeltaTime);
        Vector3 proposedWorldOffset = proposedPosition - currentSplinePosition;
        Vector3 proposedLocalOffset = Quaternion.Inverse(_splineRotation) * proposedWorldOffset;
        
        // Clamp to boundaries (but allow Z movement for spline progression)
        proposedLocalOffset.x = Mathf.Clamp(proposedLocalOffset.x, -MovementBoundaryX, MovementBoundaryX);
        proposedLocalOffset.y = Mathf.Clamp(proposedLocalOffset.y, -MovementBoundaryY, MovementBoundaryY);
        // Don't clamp Z - let it move freely along the spline
        
        // Handle boundary collisions
        bool hitBoundaryX = Mathf.Abs(proposedLocalOffset.x) >= MovementBoundaryX * 0.99f;
        bool hitBoundaryY = Mathf.Abs(proposedLocalOffset.y) >= MovementBoundaryY * 0.99f;
        
        if (hitBoundaryX || hitBoundaryY)
        {
            Vector3 localVelocity = Quaternion.Inverse(_splineRotation) * _currentVelocity;
            
            if (hitBoundaryX)
            {
                localVelocity.x *= -0.5f;
            }
            if (hitBoundaryY)
            {
                localVelocity.y *= -0.5f;
            }
            
            _currentVelocity = _splineRotation * localVelocity;
        }
        
        // Calculate final world position and set the rigidbody velocity
        Vector3 constrainedWorldPosition = currentSplinePosition + (_splineRotation * proposedLocalOffset);
        Vector3 positionDifference = constrainedWorldPosition - transform.position;
        
        // Combine the position correction with spline velocity
        Vector3 finalVelocity = (positionDifference / Time.fixedDeltaTime) + splineVelocity;
        rigidBody.linearVelocity = finalVelocity;
        rigidBody.rotation = _splineRotation;
        
        // Update current offset for next frame
        _currentOffsetFromSpline = proposedLocalOffset;
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
        
        if (_scaleAnimation.isAlive) _scaleAnimation.Stop();
        _scaleAnimation = Sequence.Create(Tween.Scale(transform, startValue: Vector3.one * 0.5f, endValue:Vector3.one, duration: spawnAnimationDuration, ease: Ease.InOutBounce));
    }
    
    
    private void PlayDespawnEffects()
    {
        if (_scaleAnimation.isAlive) _scaleAnimation.Stop();
        transform.localScale = Vector3.one;
        _scaleAnimation = Sequence.Create()
            .Group(Tween.PunchScale(transform, strength: Vector3.one * magnetizedPunchStrength/2, frequency: 7, duration: despawnAnimationDuration/2, easeBetweenShakes: Ease.InOutBounce))
            .Chain(Tween.Scale(transform, endValue: Vector3.zero, duration: despawnAnimationDuration/2, ease: Ease.OutSine));
        
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
    
    
    private void PlayMagnetizedEffects()
    {
        if (_scaleAnimation.isAlive) _scaleAnimation.Stop();
        transform.localScale = Vector3.one;
        _scaleAnimation = Sequence.Create(Tween.PunchScale(transform, Vector3.one * magnetizedPunchStrength, duration: magnetizedPunchDuration));
    }

    #endregion Effects ---------------------------------------------------------------------------------------

    
    
    #region Weapon Selection ---------------------------------------------------------------------------------------
    
    
    private SOWeaponData SelectRandomWeapon()
    {
        if (weaponChances.Length == 0) return null;
    
        // Filter out weapons with null references
        var validWeapons = new System.Collections.Generic.List<WeaponChance>();
        foreach (var weaponChance in weaponChances)
        {
            if (weaponChance.weaponData && weaponChance.chance > 0)
            {
                validWeapons.Add(weaponChance);
            }
        }
    
        if (validWeapons.Count == 0) return null;
    
        // Calculate total weight
        int totalWeight = 0;
        foreach (var weaponChance in validWeapons)
        {
            totalWeight += weaponChance.chance;
        }
    
        if (totalWeight <= 0) return validWeapons[0].weaponData;
    
        // Select a random weapon based on weights
        int randomValue = UnityEngine.Random.Range(0, totalWeight + 1);
        int currentWeight = 0;
    
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
        
        // Separate locked and unlocked entries (only those with valid weapons)
        var unlockedEntries = new System.Collections.Generic.List<WeaponChance>();
        int lockedTotal = 0;
        
        foreach (var weaponChance in weaponChances)
        {
            // Only consider entries with valid weapons
            if (weaponChance.weaponData)
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