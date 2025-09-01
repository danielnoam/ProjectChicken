using System;
using System.Collections;
using UnityEngine;
using VInspector;

[Serializable]
public class HitFXSettings
{
    [Header("Visual Settings")]
    [ColorUsage(true, true)] public Color color = Color.red;
    
    [Header("Warp & Scale")]
    [Range(-1f, 0f)] public float warpIntensity;
    public Vector2 scale = Vector2.one;
    
    [Header("Vignette")]
    [Range(0f, 3f)] public float fallOff;
    public float intensity = 1f;
    
    [Header("Effect Properties")]
    public float speed;
    public float density = 1f;
    public float softness = 1f;
    public float cutout = 1f;
}

[Serializable]
public class PunchSettings
{
    public float punchDuration = 0.3f;
    public float returnDuration = 0.5f;
    public HitFXSettings punchSettings = new HitFXSettings();
}



public class FullScreenHitFXController : MonoBehaviour
{
    public static FullScreenHitFXController Instance { get; private set; }
    
    [Header("Material Reference")]
    [SerializeField] private Material hitFXMaterial;
    [SerializeField] private HitFXSettings offSettings = new HitFXSettings();
    
    private Coroutine _currentTransition;
    
    private static readonly int Color = Shader.PropertyToID("_Color");
    private static readonly int WarpIntensity = Shader.PropertyToID("_Warp_Intensity");
    private static readonly int Scale = Shader.PropertyToID("_Scale");
    private static readonly int FallOff = Shader.PropertyToID("_FallOff");
    private static readonly int Intensity = Shader.PropertyToID("_intensity");
    private static readonly int Speed = Shader.PropertyToID("_Speed");
    private static readonly int Density = Shader.PropertyToID("_Density");
    private static readonly int Softness = Shader.PropertyToID("_Softness");
    private static readonly int Cutout = Shader.PropertyToID("_Cutout");
    

    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        ToggleOff();
    }

    private void OnApplicationQuit()
    {
        ToggleOff();
    }


    #region  Public methods

    public void ToggleOff()
    {
        ApplySettings(offSettings);
    }
    
    
    [Button]
    public void TransitionToOff(float duration = 1f)
    {
        if (_currentTransition != null) StopCoroutine(_currentTransition);
        _currentTransition = StartCoroutine(TransitionCoroutine(GetCurrentSettingsFromMaterial(), offSettings, duration));
    }
    
    public void TransitionTo(HitFXSettings customSettings, float duration = 1f)
    {
        if (_currentTransition != null) StopCoroutine(_currentTransition);
        _currentTransition = StartCoroutine(TransitionCoroutine(GetCurrentSettingsFromMaterial(), customSettings, duration));
    }

    public void TransitionFrom(HitFXSettings customSettings, float duration = 1f)
    {
        if (_currentTransition != null) StopCoroutine(_currentTransition);
        _currentTransition = StartCoroutine(TransitionCoroutine(customSettings, offSettings, duration));
    }
    
    public void Punch(PunchSettings punchConfig, bool resetCurrentEffect = false)
    {
        if (_currentTransition != null) StopCoroutine(_currentTransition);
        if (resetCurrentEffect) ApplySettings(offSettings);
        _currentTransition = StartCoroutine(PunchCoroutine(punchConfig.punchSettings, punchConfig.punchDuration, punchConfig.returnDuration));
    }
    

    

    #endregion Public methods


    #region Private methods

        private IEnumerator PunchCoroutine(HitFXSettings punchSettings, float punchDuration, float returnDuration)
    {
        HitFXSettings startSettings = GetCurrentSettingsFromMaterial();
        
        yield return StartCoroutine(TransitionCoroutine(startSettings, punchSettings, punchDuration, false));
        yield return StartCoroutine(TransitionCoroutine(punchSettings, offSettings, returnDuration, false));
        
        _currentTransition = null;
    }
    

    
    private IEnumerator TransitionCoroutine(HitFXSettings from, HitFXSettings to, float duration, bool clearCurrentTransition = true)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            HitFXSettings current = LerpSettings(from, to, t);
            ApplySettings(current);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplySettings(to);
        
        if (clearCurrentTransition)
            _currentTransition = null;
    }
    
    private HitFXSettings LerpSettings(HitFXSettings a, HitFXSettings b, float t)
    {
        HitFXSettings result = new HitFXSettings
        {
            color = UnityEngine.Color.Lerp(a.color, b.color, t),
            warpIntensity = Mathf.Lerp(a.warpIntensity, b.warpIntensity, t),
            scale = Vector2.Lerp(a.scale, b.scale, t),
            fallOff = Mathf.Lerp(a.fallOff, b.fallOff, t),
            intensity = Mathf.Lerp(a.intensity, b.intensity, t),
            speed = Mathf.Lerp(a.speed, b.speed, t),
            density = Mathf.Lerp(a.density, b.density, t),
            softness = Mathf.Lerp(a.softness, b.softness, t),
            cutout = Mathf.Lerp(a.cutout, b.cutout, t)
        };

        return result;
    }
    
    private void ApplySettings(HitFXSettings settings)
    {
        if (!hitFXMaterial) return;
        
        hitFXMaterial.SetColor(Color, settings.color);
        hitFXMaterial.SetFloat(WarpIntensity, settings.warpIntensity);
        hitFXMaterial.SetVector(Scale, settings.scale);
        hitFXMaterial.SetFloat(FallOff, settings.fallOff);
        hitFXMaterial.SetFloat(Intensity, settings.intensity);
        hitFXMaterial.SetFloat(Speed, settings.speed);
        hitFXMaterial.SetFloat(Density, settings.density);
        hitFXMaterial.SetFloat(Softness, settings.softness);
        hitFXMaterial.SetFloat(Cutout, settings.cutout);
    }
    
    private HitFXSettings GetCurrentSettingsFromMaterial()
    {
        if (!hitFXMaterial) return new HitFXSettings();
        
        HitFXSettings current = new HitFXSettings
        {
            color = hitFXMaterial.GetColor(Color),
            warpIntensity = hitFXMaterial.GetFloat(WarpIntensity),
            scale = hitFXMaterial.GetVector(Scale),
            fallOff = hitFXMaterial.GetFloat(FallOff),
            intensity = hitFXMaterial.GetFloat(Intensity),
            speed = hitFXMaterial.GetFloat(Speed),
            density = hitFXMaterial.GetFloat(Density),
            softness = hitFXMaterial.GetFloat(Softness),
            cutout = hitFXMaterial.GetFloat(Cutout)
        };

        return current;
    }

    #endregion Private methods
    

    

}