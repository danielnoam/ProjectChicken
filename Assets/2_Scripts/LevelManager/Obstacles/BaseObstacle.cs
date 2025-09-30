using System;
using DNExtensions;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(ControllerVibrationSource))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public abstract class BaseObstacle : MonoBehaviour
{
    
    [Header("Movement")]
    [SerializeField, Min(1f)] private float baseMoveSpeed = 75f;
    [SerializeField, MinMaxRange(0f, 100f)] private RangedFloat moveSpeedRange = new(50f, 100f);
    [SerializeField, Min(0f)] protected float baseRotationSpeed = 50f;
    [SerializeField, MinMaxRange(0f, 360f)] private RangedFloat rotationSpeedRange = new(30f, 100f);
    
    [Header("Spawn Animation")]
    [SerializeField, MinMaxRange(0.1f, 10f)] private RangedFloat spawnAnimationDuration = new RangedFloat(2f, 5f);

    [Header("FlyBy")]
    [SerializeField] private float flyByDistance = 50f;
    [SerializeField] private SOAudioEvent flyBySfx;
    [SerializeField] private CameraShakeSettings flyByCameraShakeSettings;
    [SerializeField] private ControllerVibrationEffectSettings flyByVibrationEffectSettings;
    
    [Header("References")]
    [SerializeField] private ControllerVibrationSource vibrationSource;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private AudioSource audioSource;
    

    protected bool initialized;
    protected float rotationSpeed;
    protected float moveSpeed;
    protected bool isMoving;
    protected bool flyByEffectsPlayed;
    protected Vector3 rotationDirection;
    protected SplineContainer spline;
    protected float splineProgress;
    
    public event Action<BaseObstacle> OnObstacleDestroyed;
    
    protected virtual void Update()
    {
        if (isMoving)
        {
            MoveAlongSpline();
        }
        
        if (LevelManager.Instance && !flyByEffectsPlayed)
        {
            if (Vector3.Distance(transform.position, LevelManager.Instance.PlayerPosition) < flyByDistance)
            {
                PlayFlyByEffects();
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
        
        Tween.Scale(transform, startValue: Vector3.zero, endValue: Vector3.one, duration: spawnAnimationDuration.RandomValue, ease: Ease.InOutSine);
    }
    
    protected virtual void MoveAlongSpline()
    {
        if (!spline) return;
        
        float splineLength = spline.Spline.GetLength();
        float progressIncrement = (moveSpeed * Time.deltaTime) / splineLength;
        
        splineProgress += progressIncrement;
        
        if (splineProgress >= 1f)
        {
            OnSplineComplete();
            return;
        }
        
        spline.Evaluate(splineProgress, out var position, out var tangent, out var up);
        
        transform.position = position;
        transform.Rotate(rotationDirection, rotationSpeed * Time.deltaTime);
    }
    
    protected virtual void OnSplineComplete()
    {
        isMoving = false;
        DestroyObstacle();
    }
    
    protected virtual void DestroyObstacle()
    {
        if (spline) Destroy(spline.gameObject);
        
        OnObstacleDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
    
    protected virtual void PlayFlyByEffects()
    {
        flyByEffectsPlayed = true;
        flyBySfx?.Play(audioSource);
        flyByCameraShakeSettings.GenerateImpulse(impulseSource);
        vibrationSource.Vibrate(flyByVibrationEffectSettings);
    }
    
    protected abstract void OnCollisionWithPlayer(RailPlayer player);
    protected abstract void OnCollisionWithChicken(ChickenStateController chicken);
}