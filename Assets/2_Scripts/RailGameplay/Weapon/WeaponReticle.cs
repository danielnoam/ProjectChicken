using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using VInspector;

public class WeaponReticle : MonoBehaviour
{
    [Header("Emission")]
    [SerializeField, Range(0,1)] private float emissionStrength = 1;
    [SerializeField] private bool emissionAffectsAlpha = true;
    [SerializeField] private List<Renderer> reticleRenderers = new List<Renderer>();
    
    [Header("Pulse Effect")]
    [SerializeField] private bool pulseEmission;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 3f;
    [SerializeField, Range(0.1f, 0.9f)] private float pulseMin = 0.3f;

    

    private bool _isVisible;
    private bool _isAimLocked;
    private float _maxStrength;
    private float _baseSize;
    private Tween _reticleTween;
    private readonly List<Material> _reticleMaterials = new List<Material>();
    private static readonly int EmissionStrength = Shader.PropertyToID("_EmissionStrength");
    private static readonly int EmissionEnabled = Shader.PropertyToID("_EmissionEnabled");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor"); 
    private float DefaultSize => _isAimLocked ? _baseSize / 2 : _baseSize;


    private void Awake()
    {
        _maxStrength = emissionStrength;
        _baseSize = transform.localScale.x;
        _isVisible = false;
        transform.localScale = Vector3.zero;
        
        GetMaterialsFromRenderers();
        UpdateMaterialsAlpha(0f);
    }
    
    private void Update()
    {
        if (!_isVisible) return;
        
        if (pulseEmission)
        {
            PulseEmission();
        }
        
        UpdateMaterialsEmissionStrength(emissionStrength);
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
    }
    
    
    public void EnableAimLock(float size, float duration)
    {
        if (_isAimLocked) return;
        
        _isAimLocked = true;
        if (_isVisible)
        {
            TweenReticleSize(size, duration);
        }
    }
    
    public void DisableAimLock(float duration)
    {
        if (!_isAimLocked) return;

        _isAimLocked = false;
        if (_isVisible)
        {
            TweenReticleSize(DefaultSize, duration);
        }
    }
    
    public void ForceChangeBaseSize(float size)
    {
        _baseSize = size;
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
        strength = Mathf.Clamp(strength, 0, _maxStrength);
        foreach (Material mat in _reticleMaterials)
        {
            if (mat)
            {
                mat.SetFloat(EmissionStrength, strength);
            }
        }
        
        if (emissionAffectsAlpha)
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
    
    private void PulseEmission()
    {
        float normalizedSine = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        emissionStrength = Mathf.Lerp(pulseMin, 1, normalizedSine);
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
    
    
    #region Tweens -----------------------------------------------------------------------------------
    
    private void TweenReticleSize(float size, float duration) 
    {
        if (Mathf.Approximately(transform.localScale.x, size)) return;
        
        if (size >= _baseSize)
        {
            UpdateMaterialsAlpha(1f);
        }
        
        if (_reticleTween.isAlive) _reticleTween.Stop();
        _reticleTween = Tween.Scale(transform, endValue: Vector3.one * size, duration, Ease.InOutBack)
            .OnComplete(() => 
            {
                if (Mathf.Approximately(size, 0f))
                {
                    UpdateMaterialsAlpha(0f);
                }
            });
    }

    public void PunchReticleSize(float strength, float duration)
    {
        if (!_isVisible) return;
        
        if (_reticleTween.isAlive) _reticleTween.Stop();
        
        transform.localScale = Vector3.one * DefaultSize;
        _reticleTween = Tween.PunchScale(transform,Vector3.one * strength, duration: duration);
        
    }

    #endregion Tweens -----------------------------------------------------------------------------------

    

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