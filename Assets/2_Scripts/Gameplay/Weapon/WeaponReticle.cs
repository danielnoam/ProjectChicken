using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions;
using PrimeTween;
using UnityEngine;
using VInspector;

public class WeaponReticle : MonoBehaviour
{
    [Header("Emission")]
    [SerializeField, Range(0,1)] private float emissionStrength = 1;
    [SerializeField, MinMaxRange(0f,1f)] private RangedFloat emissionRange = new(0.1f, 1f);
    [SerializeField] private bool emissionEffectsAlpha = true;
    [SerializeField] private List<Renderer> reticleRenderers = new List<Renderer>();

    
    [Header("References")]
    [SerializeField] private Transform punchTransform;

    private bool _isVisible;
    private bool _isAimLocked;
    private float _baseSize;
    private float _aimLockSize;
    private float _currentHeat;
    private Tween _sizeTween;
    private Tween _punchTween;
    private readonly List<Material> _reticleMaterials = new List<Material>();
    private static readonly int EmissionStrength = Shader.PropertyToID("_EmissionStrength");
    private static readonly int EmissionEnabled = Shader.PropertyToID("_EmissionEnabled");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor"); 
    private float DefaultSize => _isAimLocked ? _aimLockSize : _baseSize;



    private void Awake()
    {
        _baseSize = transform.localScale.x;
        _aimLockSize = _baseSize / 2;
        _isVisible = false;
        transform.localScale = Vector3.zero;
        
        GetMaterialsFromRenderers();
        UpdateMaterialsAlpha(0f);
        SetEmissionStrength(0);
    }
    
    
    public void Show()
    {
        _isVisible = true;
        TweenReticleSize(DefaultSize, 0.5f);
    }
    
    public void Hide()
    {
        _isVisible = false;
        TweenReticleSize(0f, 0.5f);
        SetEmissionStrength(0);
    }
    
    
    public void EnableAimLockSize(float duration)
    {
        if (_isAimLocked) return;
        
        _isAimLocked = true;
        if (_isVisible)
        {
            TweenReticleSize(_aimLockSize,duration);
        }
    }
    
    public void DisableAimLockSize(float duration)
    {
        if (!_isAimLocked) return;

        _isAimLocked = false;
        if (_isVisible)
        {
            TweenReticleSize(_baseSize, duration);
        }
    }
    
    public void ForceChangeBaseSize(float size)
    {
        _baseSize = size;
    }
    
    public void ForceChangeAimLockSize(float size)
    {
        _aimLockSize = size;
    }
    
    public void SetEmissionStrength(float strength)
    {
        emissionStrength = strength;
        UpdateMaterialsEmissionStrength(emissionStrength);
    }
    
    

    #region Material -----------------------------------------------------------------------------------------------
    
    
    private void UpdateMaterialsEmissionState(bool state)
    {
        float value = state ? 1.0f : 0.0f;
        foreach (Material mat in _reticleMaterials)
        {
            if (mat)
                mat.SetFloat(EmissionEnabled, value);
        }
    }
    

    private void UpdateMaterialsEmissionStrength(float strength)
    {
        strength = Mathf.Clamp(strength, emissionRange.minValue, emissionRange.maxValue);
        foreach (Material mat in _reticleMaterials)
        {
            if (mat)
            {
                mat.SetFloat(EmissionStrength, strength);
            }
        }
        
        if (emissionEffectsAlpha)
        {
            float alpha = Mathf.Pow(strength, 15f);
            UpdateMaterialsAlpha(alpha);
        }
    }

    private void UpdateMaterialsAlpha(float alpha)
    {
        foreach (Material mat in _reticleMaterials)
        {
            if (mat)
            {
                Color baseColor = mat.GetColor(BaseColor);
                baseColor.a = alpha;
                mat.SetColor(BaseColor, baseColor);
            }
        }
    }
    
    
    private void GetMaterialsFromRenderers()
    {
        _reticleMaterials.Clear();
        
        foreach (Renderer rend in reticleRenderers)
        {
            if (!rend) continue;
            var materials = rend.materials;
            foreach (Material mat in materials)
            {
                if (mat)
                {
                    _reticleMaterials.Add(mat);
                }
            }
        }
    }
    
    

    #endregion Material -----------------------------------------------------------------------------------------------
    
    
    #region Size -----------------------------------------------------------------------------------
    
    private void TweenReticleSize(float size, float duration) 
    {
        if (Mathf.Approximately(transform.localScale.x, size)) return;
        
        if (size >= _baseSize)
        {
            UpdateMaterialsAlpha(1f);
        }
        
        if (_sizeTween.isAlive) _sizeTween.Stop();
        _sizeTween = Tween.Scale(transform, endValue: Vector3.one * size, duration, Ease.InOutBack)
            .OnComplete(() => 
            {
                if (Mathf.Approximately(size, 0f))
                {
                    UpdateMaterialsAlpha(0f);
                }
            });
    }

    public void PunchReticleSize(float strength, float duration, float delay = 0f)
    {
        if (!_isVisible) return;
        
        if (_punchTween.isAlive) _punchTween.Stop();
        punchTransform.localScale = Vector3.one;
        _punchTween = Tween.PunchScale(punchTransform,Vector3.one * strength, startDelay: delay, duration: duration);
        
    }

    #endregion Size -----------------------------------------------------------------------------------

    

#if UNITY_EDITOR
    #region Editor -----------------------------------------------------------------------------------------------

    [Button]
    private void RebuildRendererList()
    {
        reticleRenderers.Clear();
        reticleRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
    }
    
    #endregion Editor -----------------------------------------------------------------------------------------------
    
#endif

}