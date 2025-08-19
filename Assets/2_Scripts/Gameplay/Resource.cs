using System;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using VInspector;
using PrimeTween;
using Random = UnityEngine.Random;


[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class Resource : MonoBehaviour
{

    
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float acceleration = 6f;
    [SerializeField, Min(0f)] private float moveToBoundarySpeed = 10f;
    [SerializeField, Min(0f)] private float followBoundarySpeed = 10f;
    [SerializeField, Min(0f)] private float magnetizedSpeed = 20f;
    
    [Header("Effects")]
    [SerializeField] private SOAudioEvent spawnSfx;
    [SerializeField] private ParticleSystem spawnEffect;
    [SerializeField] private SOAudioEvent collectionSfx;
    [SerializeField] private ParticleSystem collectionEffect;
    [SerializeField] private Transform resourceGfx;
    [SerializeField, Min(0)] private float rotationSpeed = 45f;
    [SerializeField] private float spawnAnimationDuration = 1f;
    [SerializeField] private float despawnAnimationDuration = 2f;
    [SerializeField] private float magnetizedPunchStrength = 1f;
    [SerializeField] private float magnetizedPunchDuration = 0.5f;
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;
    
    [Header("Resource")]
    [Tooltip("Time before the resource destroys itself (0 = unlimited time)"), SerializeField, Min(0)] private float lifetime = 7f;
    [SerializeField, Min(0)] private int scoreWorth = 50;
    [SerializeField] private ResourceType resourceType;
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.Currency)] private int currencyWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.HealthPack)] private int healthWorth = 1;[EndIf]
    [SerializeField, Min(1), ShowIf("resourceType", ResourceType.ShieldPack)] private int shieldWorth = 50;[EndIf]
    [SerializeField, ShowIf("resourceType", ResourceType.SpecialWeapon)] private ChanceList<SOWeaponData> weapons = new ChanceList<SOWeaponData>();[EndIf] 
    
    [Header("References")]
    [SerializeField] private SOGameSettings gameSettings;

    
    private ResourceState _currentState;
    private Transform _playerTransform;
    private float _currentLifetime;
    private float _movementBoundaryX;
    private float _movementBoundaryY;
    private Vector3 _rotationAxis;
    private Sequence _scaleAnimation;
    private Vector3 _currentVelocity;
    private Vector3 _targetVelocity;
    private Vector3 _targetOffsetFromSpline;
    private Vector3 _currentBoundaryTargetPosition;
    private enum ResourceState
    {
        MovingToBoundary,
        FollowingBoundary,
        Magnetized,
        Collected,
    }
    
    public ResourceType ResourceType => resourceType;
    public int ScoreWorth => scoreWorth;
    public int HealthWorth => healthWorth;
    public int ShieldWorth => shieldWorth;
    public int CurrencyWorth => currencyWorth;
    public SOWeaponData WeaponData { get; private set;}

    public event Action<Resource> OnDestroyEvent;

    
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
        UpdateBoundaryTargetPosition();
        HandleMovement();
    }

    private void OnDestroy()
    {
        OnDestroyEvent?.Invoke(this);
    }



    
    

    #region State Management ---------------------------------------------------------------------------------------

    private void CheckLifetime()
    {
        if (lifetime <= 0f || _currentState != ResourceState.FollowingBoundary) return;

        _currentLifetime -= Time.deltaTime;
        
        if (_currentLifetime <= despawnAnimationDuration && !_scaleAnimation.isAlive)
        {
            resourceGfx.localScale = Vector3.one;
            _scaleAnimation = Sequence.Create()
                .Group(Tween.PunchScale(resourceGfx, strength: Vector3.one * magnetizedPunchStrength/2, frequency: 2, duration: despawnAnimationDuration/2, easeBetweenShakes: Ease.InOutBounce))
                .Chain(Tween.Scale(resourceGfx, endValue: Vector3.zero, duration: despawnAnimationDuration/2, ease: Ease.OutSine));
        }
        
        if (_currentLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void SetMagnetized(Transform playerTransform)
    {
        if (_currentState == ResourceState.Collected) return;
        
        _currentState = ResourceState.Magnetized;
        _playerTransform = playerTransform;
        
        if (_scaleAnimation.isAlive) _scaleAnimation.Stop();
        resourceGfx.localScale = Vector3.one;
        _scaleAnimation = Sequence.Create(Tween.PunchScale(resourceGfx, Vector3.one * magnetizedPunchStrength, frequency:5, duration: magnetizedPunchDuration));
    }
    
    
    public void ReleaseFromMagnetization()
    {
        _currentState = ResourceState.FollowingBoundary;
        _playerTransform = null;
    }
    
    public void ResourceCollected()
    {
        if (_currentState == ResourceState.Collected) return;
        _currentState = ResourceState.Collected;
        
        if (_scaleAnimation.isAlive) _scaleAnimation.Stop();
        if (collectionSfx)
        {
            collectionSfx.PlayAtPoint(transform.position);
        }
        
        if (collectionEffect)
        {
            Instantiate(collectionEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    public void ForceDespawn()
    {
        if (_currentState is ResourceState.Collected or ResourceState.Magnetized) return;
        if (_scaleAnimation.isAlive) _scaleAnimation.Stop();
        _scaleAnimation = Sequence.Create()
            .Group(Tween.PunchScale(resourceGfx, strength: Vector3.one * magnetizedPunchStrength/2, frequency: 2, duration: despawnAnimationDuration/2, easeBetweenShakes: Ease.InOutBounce))
            .Chain(Tween.Scale(resourceGfx, endValue: Vector3.zero, duration: despawnAnimationDuration/2, ease: Ease.OutSine)) 
            .OnComplete((() => { Destroy(gameObject); }));
    }


    private void Setup()
    {
        _currentState = ResourceState.MovingToBoundary;
        _currentLifetime = lifetime;
        _playerTransform = null;
        if (resourceType == ResourceType.SpecialWeapon && weapons.Count > 0)
        {
            WeaponData = weapons.GetRandomItem();
        }
        _currentVelocity = Vector3.zero;
        
        
        if (gameSettings)
        {
            _movementBoundaryX = gameSettings.PlayerBoundary.x;
            _movementBoundaryY = gameSettings.PlayerBoundary.y;
           float randomX = Random.Range(-_movementBoundaryX, _movementBoundaryX);
           float randomY = Random.Range(-_movementBoundaryY, _movementBoundaryY);
           _targetOffsetFromSpline = new Vector3(randomX, randomY, 0f);
           UpdateBoundaryTargetPosition(); 
        }
        
        _rotationAxis = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        resourceGfx.eulerAngles = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
        
        spawnSfx?.Play(audioSource);
        if (spawnEffect) Instantiate(spawnEffect, transform.position, Quaternion.identity);
        if (_scaleAnimation.isAlive) _scaleAnimation.Stop();
        _scaleAnimation = Sequence.Create(Tween.Scale(resourceGfx, startValue: Vector3.one * 0.5f, endValue:Vector3.one, duration: spawnAnimationDuration, ease: Ease.InOutBounce));

    }
    
    private void UpdateBoundaryTargetPosition()
    {
        if (!LevelManager.Instance) return;
        _currentBoundaryTargetPosition = LevelManager.Instance.PlayerPosition + _targetOffsetFromSpline;
    }


    #endregion State Management ---------------------------------------------------------------------------------------
    

    
    #region Movement ---------------------------------------------------------------------------------------
    

    private void HandleMovement()
    {
        switch (_currentState)
        {
            case ResourceState.MovingToBoundary:
                HandleMovingToBoundary();
                break;
            case ResourceState.FollowingBoundary:
                HandleFollowingBoundary();
                break;
            case ResourceState.Magnetized or ResourceState.Collected:
                HandleMagnetizedMovement();
                break;
        }
        
        transform.position += _currentVelocity * Time.deltaTime;
    }
    
    private void HandleMovingToBoundary()
    {
        Vector3 directionToTarget = (_currentBoundaryTargetPosition - transform.position).normalized;
        
        _targetVelocity = directionToTarget * moveToBoundarySpeed;
        _currentVelocity = Vector3.Lerp(_currentVelocity, _targetVelocity, acceleration * Time.deltaTime);
        
        float distanceToTarget = Vector3.Distance(transform.position, _currentBoundaryTargetPosition);
        if (distanceToTarget < 3f)
        {
            _currentState = ResourceState.FollowingBoundary;
        }
    }

    private void HandleFollowingBoundary()
    {
        Vector3 directionToTarget = (_currentBoundaryTargetPosition - transform.position).normalized;
        
        _targetVelocity = directionToTarget * followBoundarySpeed;
        _currentVelocity = Vector3.Lerp(_currentVelocity, _targetVelocity, acceleration * Time.deltaTime);
        
        float distanceToTarget = Vector3.Distance(transform.position, _currentBoundaryTargetPosition);
        if (distanceToTarget > 3f)
        {
            _currentState = ResourceState.MovingToBoundary;
        }
    }

    private void HandleMagnetizedMovement()
    {
        if (!_playerTransform) return;
        
        Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
        _targetVelocity = directionToPlayer * magnetizedSpeed;
        _currentVelocity = Vector3.Lerp(_currentVelocity, _targetVelocity, acceleration * Time.deltaTime);
    }
    
    
    private void RotateGfx()
    {
        if (rotationSpeed <= 0f) return;
        
        resourceGfx.Rotate(_rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }

    
    #endregion Movement ---------------------------------------------------------------------------------------
    
    

    
    private void OnDrawGizmos()
    {
        if (_currentState == ResourceState.MovingToBoundary)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_currentBoundaryTargetPosition, 1.5f);
        
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _currentBoundaryTargetPosition);
        }
    }
 
}