using System;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

[Serializable]
public class MaterialSpeedSettings
{
    [Tooltip("The material to update")]
    public Material material;
    
    [Tooltip("Base speed value that will be multiplied by world speed")]
    public Vector2 baseSpeed = Vector2.one;
    
    [Tooltip("Maximum speed value (clamped)")]
    public Vector2 maxSpeed = Vector2.one * 10f;
    
    [HideInInspector] public Vector2 currentSpeed;
}

public class WorldSpeedMaterialController : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField] private List<MaterialSpeedSettings> materialSettings = new List<MaterialSpeedSettings>();
    
    [Header("Speed Up")]
    [SerializeField] private float speedUpTweenDuration = 5f;
    [SerializeField] private Ease speedUpEase = Ease.InOutSine;
    
    [Header("Slow Down")]
    [SerializeField] private float slowDownTweenDuration = 0.5f;
    [SerializeField] private Ease slowDownEase = Ease.OutCubic;
    
    private LevelManager _levelManager;
    private static readonly int Speed = Shader.PropertyToID("_Speed");
    private Tween _speedTween;
    private float _lastWorldSpeed = 1f;
    private float _worldSpeedMultiplier = 1f;

    private void Awake()
    {
        _levelManager = FindFirstObjectByType<LevelManager>();
        
        ResetMaterialSpeeds();
    }

    private void OnEnable()
    {
        if (_levelManager)
        {
            _levelManager.OnStageChanged += OnStageChanged;
        }
    }

    private void OnDisable()
    {
        if (_levelManager)
        {
            _levelManager.OnStageChanged -= OnStageChanged;
        }
        
        ResetMaterialSpeeds();
    }
    
    private void OnStageChanged(SOLevelStage stage)
    {
        TweenMaterialSpeeds(stage);
    }

    private void ResetMaterialSpeeds()
    {
        _speedTween.Stop();
        
        foreach (var settings in materialSettings)
        {
            if (settings.material)
            {
                settings.currentSpeed = settings.baseSpeed;
                settings.material.SetVector(Speed, settings.baseSpeed);
            }
        }
        
        _lastWorldSpeed = 1f;
    }

    private void TweenMaterialSpeeds(SOLevelStage stage)
    {
        _speedTween.Stop();
    
        float targetWorldSpeed = stage.WorldSpeed;
        bool isSlowingDown = targetWorldSpeed < _lastWorldSpeed;
    
        float duration = isSlowingDown ? slowDownTweenDuration : speedUpTweenDuration;
        Ease ease = isSlowingDown ? slowDownEase : speedUpEase;
        float delay = isSlowingDown ? 0f : LevelManager.WorldSpeedChangeDuration;
    
        _speedTween = Tween.Custom(_worldSpeedMultiplier, targetWorldSpeed, duration, 
            ease: ease,
            startDelay: delay,
            onValueChange: value => {
                _worldSpeedMultiplier = value;
                UpdateMaterialSpeeds(value);
            });
    
        _lastWorldSpeed = targetWorldSpeed;
    }

    private void UpdateMaterialSpeeds(float worldSpeedMultiplier)
    {
        foreach (var settings in materialSettings)
        {
            if (settings.material)
            {
                Vector2 targetSpeed = settings.baseSpeed * worldSpeedMultiplier;
            
                // Apply clamping
                targetSpeed.x = settings.baseSpeed.x < 0 
                    ? Mathf.Clamp(targetSpeed.x, settings.maxSpeed.x, 0f)
                    : Mathf.Clamp(targetSpeed.x, 0f, settings.maxSpeed.x);
            
                targetSpeed.y = settings.baseSpeed.y < 0 
                    ? Mathf.Clamp(targetSpeed.y, settings.maxSpeed.y, 0f)
                    : Mathf.Clamp(targetSpeed.y, 0f, settings.maxSpeed.y);
            
                settings.material.SetVector(Speed, targetSpeed);
                settings.currentSpeed = targetSpeed;
            }
        }
    }
}