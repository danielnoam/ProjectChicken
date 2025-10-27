using System;
using DNExtensions;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(ControllerVibrationSource))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public abstract class BaseObstacle : MonoBehaviour, IDamageable
{
    [Header("General")]
    [SerializeField, Min(0)] protected int scoreWorth = 100;
    
    [Header("Movement")]
    [SerializeField, Min(1f)] protected float baseMoveSpeed = 75f;
    [SerializeField, MinMaxRange(0f, 100f)] protected RangedFloat moveSpeedRange = new(50f, 100f);
    [SerializeField, Min(0f)] protected float baseRotationSpeed = 50f;
    [SerializeField, MinMaxRange(0f, 360f)] protected RangedFloat rotationSpeedRange = new(30f, 100f);
    
    [Header("Spawn Animation")]
    [SerializeField, MinMaxRange(0.1f, 10f)] protected RangedFloat spawnAnimationDuration = new RangedFloat(2f, 5f);

    [Header("FlyBy")]
    [SerializeField] protected float flyByDistance = 50f;
    [SerializeField] protected SOAudioEvent flyBySfx;
    [SerializeField] protected CameraShakeSettings flyByCameraShakeSettings;
    [SerializeField] protected ControllerVibrationEffectSettings flyByVibrationEffectSettings;
    
    [Header("References")]
    [SerializeField] protected ControllerVibrationSource vibrationSource;
    [SerializeField] protected CinemachineImpulseSource impulseSource;
    [SerializeField] protected AudioSource audioSource;
    

    protected bool initialized;
    protected float rotationSpeed;
    protected float moveSpeed;
    protected bool isMoving;
    protected bool flyByEffectsPlayed;
    protected Vector3 rotationDirection;
    protected SplineContainer spline;
    protected float splineProgress;
    protected bool canCollide;
    protected Sequence scaleSequence;
    
    public int ScoreWorth => scoreWorth;
    
    public event Action<BaseObstacle> OnObstacleDestroyed;


    private void OnDestroy()
    {
        if (spline) Destroy(spline.gameObject);
        if (scaleSequence.isAlive) scaleSequence.Stop();
        OnObstacleDestroyed?.Invoke(this);
    }

    protected virtual void Update()
    {
        if (isMoving)
        {
            MoveAlongSpline();
        }
        
        if (LevelManager.Instance && !flyByEffectsPlayed)
        {
            if (Vector3.Distance(transform.position, LevelManager.Instance.Player.transform.position) < flyByDistance)
            {
                Vector3 directionToPlayer = (LevelManager.Instance.Player.transform.position - transform.position).normalized;
                PlayFlyByEffects(directionToPlayer);
            }
        }
    }
    
    public virtual void Initialize(SplineContainer spline)
    {
        if (initialized) return;
        
        this.spline = spline;
        splineProgress = 0f;
        moveSpeed = baseMoveSpeed + moveSpeedRange.RandomValue;
        rotationSpeed = baseRotationSpeed + rotationSpeedRange.RandomValue;
        rotationDirection = UnityEngine.Random.onUnitSphere;
        isMoving = true;
        initialized = true;
        canCollide = false;
        
        if (scaleSequence.isAlive) scaleSequence.Stop();
        scaleSequence = Sequence.Create();
        scaleSequence.Group(Tween.Scale(transform, startValue: Vector3.zero, endValue: Vector3.one, duration: spawnAnimationDuration.RandomValue, ease: Ease.InOutSine));
        scaleSequence.OnComplete(() => canCollide = true);
    }
    
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!initialized || !canCollide) return;
        
        if (other.TryGetComponent<ChickenStateController>(out var chicken))
        {
            OnCollisionWithChicken(other, chicken);
        }
        
        if (other.TryGetComponent<RailPlayer>(out var player))
        {
            OnCollisionWithPlayer(other, player);
        }
        
        if (other.TryGetComponent(out PassthroughObstacle passthroughObstacle))
        {
            OnCollisionWithPassthroughObstacle(other, passthroughObstacle);
        }
        
        if (other.TryGetComponent(out NormalObstacle obstacle))
        {
            OnCollisionWithObstacle(other, obstacle);
        }
    }
    
    protected virtual void MoveAlongSpline()
    {
        if (!spline) return;
        
        float splineLength = spline.Spline.GetLength();
        
        var moveSpeedToUse = moveSpeed * LevelManager.WorldSpeed;
        moveSpeedRange.Clamp(moveSpeedToUse);
        float progressIncrement = (moveSpeedToUse * Time.deltaTime) / splineLength;
        
        splineProgress += progressIncrement;
        
        if (splineProgress >= 1f)
        {
            OnSplineComplete();
            return;
        }
        
        spline.Evaluate(splineProgress, out var position, out var tangent, out var up);
        transform.position = position;
        
        var rotationSpeedToUse = rotationSpeedRange.Clamp(rotationSpeed * LevelManager.WorldSpeed);
        transform.Rotate(rotationDirection, rotationSpeedToUse * Time.deltaTime);
    }
    
    protected virtual void OnSplineComplete()
    {
        isMoving = false;
        Destroy(gameObject);
    }
    
    protected virtual void DestroyObstacle()
    {
        Destroy(gameObject);
    }
    
    protected virtual void PlayFlyByEffects(Vector3 flyByPosition = default)
    {

        flyByEffectsPlayed = true;
        flyBySfx?.Play(audioSource);
        vibrationSource.Vibrate(flyByVibrationEffectSettings);
        if (flyByPosition == default)
        {
            flyByCameraShakeSettings.GenerateImpulse(impulseSource);
        }
        else
        {
            flyByCameraShakeSettings.GenerateImpulse(impulseSource, flyByPosition);
        }
    }
    
    protected abstract void OnCollisionWithPlayer(Collider other, RailPlayer player);
    protected abstract void OnCollisionWithChicken(Collider other, ChickenStateController chicken);
    protected abstract void OnCollisionWithPassthroughObstacle(Collider other, PassthroughObstacle passthroughObstacle);
    protected abstract void OnCollisionWithObstacle(Collider other, NormalObstacle normalObstacle);
    public abstract void TakeDamage(float damage);

    public abstract void ApplyStun(float duration);

    public abstract void ApplyForce(Vector3 direction, float force);
}