using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using VInspector;
using PrimeTween;


[SelectionBase]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class Resource : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("Time before the resource destroys itself (0 = unlimited time)"), SerializeField, Min(0)] private float lifetime = 8f;
    [SerializeField, MinMaxRange(30f,60f)] private RangedFloat spawnSpeedRange = new (50f,60f);
    [SerializeField, Min(0f)] private float splineMovementSpeed = 3f;
    [SerializeField, Min(0.1f)] private float deceleration = 4f;
    [SerializeField, Min(0.1f)] private float acceleration = 12f;
    [SerializeField, Min(0f)] private float magnetizedSpeed = 25f;
    [SerializeField, Min(0)] private float rotationSpeed = 45f;
    [SerializeField] private Transform resourceGfx;
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;
    [SerializeField, Self, HideInInspector] private Rigidbody rigidBody;

    
    [Header("Effects")]
    [SerializeField] private SOAudioEvent spawnSfx;
    [SerializeField] private ParticleSystem spawnEffect;
    [SerializeField] private SOAudioEvent collectionSfx;
    [SerializeField] private ParticleSystem collectionEffect;
    [SerializeField] private float spawnAnimationDuration = 1f;
    [SerializeField] private float despawnAnimationDuration = 2f;
    [SerializeField] private float magnetizedPunchStrength = 1f;
    [SerializeField] private float magnetizedPunchDuration = 0.5f;
    
    [Header("Resource Settings")]
    [SerializeField, Min(0)] private int scoreWorth = 50;
    [SerializeField] private ResourceType resourceType;
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.Currency)] private int currencyWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.HealthPack)] private int healthWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.ShieldPack)] private int shieldWorth = 50;[EndIf]
    [SerializeField, ShowIf("resourceType", ResourceType.SpecialWeapon)] private ChanceList<SOWeaponData> weapons = new ChanceList<SOWeaponData>();[EndIf] 
    
    

    private Transform _playerTransform;
    private bool _isMagnetized;
    private float _currentLifetime;
    private float _movementBoundaryX;
    private float _movementBoundaryY;
    private Vector3 _rotationAxis;
    private Sequence _scaleAnimation;
    private Vector3 _currentVelocity;
    private Vector3 _targetVelocity;
    private Quaternion _splineRotation = Quaternion.identity;
    
    public ResourceType ResourceType => resourceType;
    public int ScoreWorth => scoreWorth;
    public int HealthWorth => healthWorth;
    public int ShieldWorth => shieldWorth;
    public int CurrencyWorth => currencyWorth;
    public SOWeaponData WeaponData { get; private set;}

    
    private void OnValidate()
    {
        this.ValidateRefs();
    }
    

    private void Awake()
    {
        Setup();
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
    
    private void Setup()
    {
        _currentLifetime = lifetime;
        _isMagnetized = false;
        _playerTransform = null;
        _movementBoundaryX = LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.x : 10f;
        _movementBoundaryY = LevelManager.Instance ? LevelManager.Instance.PlayerBoundary.y : 6f;
        
        _rotationAxis = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        
        if (resourceType == ResourceType.SpecialWeapon && weapons.Count > 0)
        {
            WeaponData = weapons.GetRandomItem();
        }
        
        float randomSpeed = spawnSpeedRange.RandomValue;
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 localVelocity = new Vector3(randomDir.x, randomDir.y, 0f) * randomSpeed;
        _currentVelocity = _splineRotation * localVelocity;
        _targetVelocity = Vector3.zero;
        resourceGfx.eulerAngles = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
        
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
        proposedLocalOffset.x = Mathf.Clamp(proposedLocalOffset.x, -_movementBoundaryX, _movementBoundaryX);
        proposedLocalOffset.y = Mathf.Clamp(proposedLocalOffset.y, -_movementBoundaryY, _movementBoundaryY);
        // Don't clamp Z - let it move freely along the spline
        
        // Handle boundary collisions
        bool hitBoundaryX = Mathf.Abs(proposedLocalOffset.x) >= _movementBoundaryX * 0.99f;
        bool hitBoundaryY = Mathf.Abs(proposedLocalOffset.y) >= _movementBoundaryY * 0.99f;
        
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
    }

    #endregion Movement ---------------------------------------------------------------------------------------
    
    

    #region Effects ---------------------------------------------------------------------------------------

    
    private void RotateGfx()
    {
        if (rotationSpeed <= 0f) return;
        
        resourceGfx.Rotate(_rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }

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
        _scaleAnimation = Sequence.Create(Tween.Scale(resourceGfx, startValue: Vector3.one * 0.5f, endValue:Vector3.one, duration: spawnAnimationDuration, ease: Ease.InOutBounce));
    }
    
    
    private void PlayDespawnEffects()
    {
        if (_scaleAnimation.isAlive) _scaleAnimation.Stop();
        resourceGfx.localScale = Vector3.one;
        _scaleAnimation = Sequence.Create()
            .Group(Tween.PunchScale(resourceGfx, strength: Vector3.one * magnetizedPunchStrength/2, frequency: 7, duration: despawnAnimationDuration/2, easeBetweenShakes: Ease.InOutBounce))
            .Chain(Tween.Scale(resourceGfx, endValue: Vector3.zero, duration: despawnAnimationDuration/2, ease: Ease.OutSine));
        
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
        resourceGfx.localScale = Vector3.one;
        _scaleAnimation = Sequence.Create(Tween.PunchScale(resourceGfx, Vector3.one * magnetizedPunchStrength, duration: magnetizedPunchDuration));
    }

    #endregion Effects ---------------------------------------------------------------------------------------

    
 
}