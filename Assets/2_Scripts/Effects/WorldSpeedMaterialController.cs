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
        _lastWorldSpeed = targetWorldSpeed;
        
        // Store starting speeds
        foreach (var settings in materialSettings)
        {
            if (settings.material)
            {
                settings.currentSpeed = settings.material.GetVector(Speed);
            }
        }
        
        if (isSlowingDown)
        {
            _speedTween = Tween.Custom(0f, 1f, slowDownTweenDuration, ease: slowDownEase, onValueChange: t => UpdateMaterialSpeedsLerped(t, targetWorldSpeed));
        }
        else
        {
            _speedTween = Tween.Custom(0f, 1f, speedUpTweenDuration, ease: speedUpEase, startDelay: LevelManager.WorldSpeedChangeDuration, onValueChange: t => UpdateMaterialSpeedsLerped(t, targetWorldSpeed));
        }
    }

    private void UpdateMaterialSpeedsLerped(float t, float targetWorldSpeed)
    {
        foreach (var settings in materialSettings)
        {
            if (settings.material)
            {
                Vector2 targetSpeed = settings.baseSpeed * targetWorldSpeed;
            
                // Clamp target X
                if (settings.baseSpeed.x < 0)
                    targetSpeed.x = Mathf.Clamp(targetSpeed.x, settings.maxSpeed.x, 0f);
                else
                    targetSpeed.x = Mathf.Clamp(targetSpeed.x, 0f, settings.maxSpeed.x);
            
                // Clamp target Y
                if (settings.baseSpeed.y < 0)
                    targetSpeed.y = Mathf.Clamp(targetSpeed.y, settings.maxSpeed.y, 0f);
                else
                    targetSpeed.y = Mathf.Clamp(targetSpeed.y, 0f, settings.maxSpeed.y);
                
    
                Vector2 lerpedSpeed = Vector2.Lerp(settings.currentSpeed, targetSpeed, t);
                settings.material.SetVector(Speed, lerpedSpeed);
            }
        }
    }
}