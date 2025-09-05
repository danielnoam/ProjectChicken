using System;
using DNExtensions;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using VInspector;

public class ShieldEffectManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, MinMaxRange(0,1)] private RangedFloat activeShieldAlphaRange = new RangedFloat(0,1);
    [SerializeField] private float alphaChangeDuration = 0.3f;
    
    [Header("References")]
    [SerializeField, Parent(Flag.Editable)] private RailPlayer player;
    [SerializeField] private SOGameSettings gameSettings;
    [SerializeField] private ShieldHitMovement[] shieldHits;


    private Sequence _alphaSequence;
    private float _currentAlpha;
    
    
    private void OnValidate()
    {
        this.ValidateRefs();
    }

    private void OnEnable()
    {
        if (player)
        {
            player.Health.OnShieldChanged += OnShieldChanged;
        }
    }



    private void OnDisable()
    {
        if (player)
        {
            player.Health.OnShieldChanged -= OnShieldChanged;
        }
    }
    
    private void OnShieldChanged(float shieldHealth)
    {
        var alpha = 0f;
        if (shieldHealth >= 0.1f)
        {
            var normalizedShieldHealth = shieldHealth / gameSettings.BaseShield;
             alpha = Mathf.Lerp(activeShieldAlphaRange.minValue, activeShieldAlphaRange.maxValue, normalizedShieldHealth);
        }

        TweenShieldAlpha(alpha, alphaChangeDuration);
    }
    


    private void TweenShieldAlpha(float targetAlpha, float duration)
    {
        if (_alphaSequence.isAlive) _alphaSequence.Stop();
        
        _alphaSequence = Sequence.Create()
            .Group(Tween.Custom(_currentAlpha, targetAlpha, duration, UpdateMaterialsAlpha));
    }
    
    
    private void UpdateMaterialsAlpha(float alpha)
    {
        _currentAlpha = alpha;

        foreach (var shieldHit in shieldHits)
        {
            shieldHit.SetAlpha(alpha);
        }
    }
    
    
    [Button]
    private void FindAllShieldHits()
    {
        shieldHits = Array.Empty<ShieldHitMovement>();
        shieldHits = GetComponentsInChildren<ShieldHitMovement>();
    }
}