using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions;
using PrimeTween;
using UnityEngine;
using VInspector;

public class ReticleVisualsController : MonoBehaviour
{
    [Header("Emission")]
    [SerializeField] private bool emissionEffectsAlpha = true;
    [SerializeField, Range(0,1)] private float emissionStrength = 1;
    [SerializeField, MinMaxRange(0f,1f)] private RangedFloat emissionRange = new(0.1f, 1f);

    [Header("Size")]
    [SerializeField] private float aimLockSize = 0.5f;
    
    [Header("References")]
    [SerializeField] private Transform punchTransform;
    [SerializeField] private Transform aimLockTransform;
    [SerializeField] private List<Renderer> reticleRenderers = new List<Renderer>();

    private bool _isVisible;
    private bool _isAimLocked;
    private float _baseSize;
    private float _currentHeat;
    private Tween _sizeTween;
    private Tween _punchSizeTween;
    private Tween _aimLockSizeTween;
    private Tween _punchPositionTween;
    private readonly List<Material> _reticleMaterials = new List<Material>();
    private static readonly int EmissionStrength = Shader.PropertyToID("_EmissionStrength");
    private static readonly int EmissionEnabled = Shader.PropertyToID("_EmissionEnabled");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor"); 



    private void Awake()
    {
        _baseSize = transform.localScale.x;
        aimLockTransform.localScale = Vector3.one;
        punchTransform.localScale = Vector3.one;
        transform.localScale = Vector3.zero;
        _isVisible = false;
        _isAimLocked = false;
        
        GetMaterialsFromRenderers();
        UpdateMaterialsAlpha(0f);
        SetEmissionStrength(0);
    }
    
    
    public void Show()
    {
        _isVisible = true;
        TweenReticleSize(_baseSize, 0.5f);
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
            TweenAimLockSize(aimLockSize, duration);
        }
    }
    
    public void DisableAimLockSize(float duration)
    {
        if (!_isAimLocked) return;

        _isAimLocked = false;
        if (_isVisible)
        {
            TweenAimLockSize(1, duration);
        }
    }
    
    public void ForceChangeAimLockSize(float size)
    {
        aimLockSize = size;
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
    
    private void TweenAimLockSize(float size, float duration, float delay = 0f)
    {
        if (!_isVisible || !aimLockTransform) return;
        
        if (Mathf.Approximately(aimLockTransform.localScale.x, size)) return;
        
        if (_aimLockSizeTween.isAlive) _aimLockSizeTween.Stop();
        _aimLockSizeTween = Tween.Scale(aimLockTransform, endValue: Vector3.one * size, duration, Ease.InOutBack, startDelay: delay);
    }
    
    public void PunchReticleSize(float strength, float duration, float delay = 0f)
    {
        if (!_isVisible) return;
        
        if (_punchSizeTween.isAlive) _punchSizeTween.Stop();
        punchTransform.localScale = Vector3.one;
        _punchSizeTween = Tween.PunchScale(punchTransform,Vector3.one * strength, startDelay: delay, duration: duration);
    }

    #endregion Size -----------------------------------------------------------------------------------


    #region Position  -----------------------------------------------------------------------------------------------

    
    public void PunchReticlePosition(Vector3 strength, float duration, float delay = 0f)
    {
        if (!_isVisible || strength == Vector3.zero) return;
        

        if (_punchPositionTween.isAlive) _punchPositionTween.Stop();
        _punchPositionTween = Tween.PunchLocalPosition(punchTransform, strength, frequency: 1, startDelay: delay, duration: duration);

        _punchPositionTween.OnComplete((() =>
        {
            _punchPositionTween = Tween.LocalPosition(punchTransform, Vector3.zero, startDelay: delay, duration: 0.5f, ease: Ease.InOutSine);
        }));
    }

    #endregion Position  -----------------------------------------------------------------------------------------------
    
    

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