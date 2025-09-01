using System;
using System.Collections;
using UnityEngine;
using VInspector;

[Serializable]
public class ChromaticAberrationSettings
{
    [Header("Surface Inputs")]
    public bool on = false;
    
    [Header("Offset Settings")]
    public Vector3 redOffset = new Vector3(0.01f, -0.01f, 0.02f);
    public Vector3 greenOffset = new Vector3(-0.01f, 0.01f, -0.02f);
    public Vector3 blueOffset = new Vector3(-0.02f, 0.01f, 0.02f);
    
    [Header("Noise Properties")]
    public float size = 500f;
    public Vector2 speed = new Vector2(0.05f, 0f);
    public float exposure = 5f;
    public float contrast = 10000f;
    public float cutOut = 10f;
}

[Serializable]
public class ChromaticAberrationPunchSettings
{
    public float punchDuration = 0.3f;
    public float returnDuration = 0.5f;
    public ChromaticAberrationSettings punchSettings = new ChromaticAberrationSettings();
}

public class FullScreenCAController : MonoBehaviour
{
    public static FullScreenCAController Instance { get; private set; }
    
    [Header("Material Reference")]
    [SerializeField] private Material chromaticAberrationMaterial;
    [SerializeField] private ChromaticAberrationSettings offSettings = new ChromaticAberrationSettings
    {
        on = false,
        redOffset = new Vector3(0.01f, -0.01f, 0.02f),
        greenOffset = new Vector3(-0.01f, 0.01f, -0.02f),
        blueOffset = new Vector3(-0.02f, 0.01f, 0.02f),
        size = 500f,
        speed = new Vector2(0.05f, 0f),
        exposure = 5f,
        contrast = 10000f,
        cutOut = 10f
    };
    
    private Coroutine _currentTransition;
    

    private static readonly int On = Shader.PropertyToID("_On");
    private static readonly int RedOffset = Shader.PropertyToID("_Red_Offset");
    private static readonly int GreenOffset = Shader.PropertyToID("_Green_Offset");
    private static readonly int BlueOffset = Shader.PropertyToID("_Blue_Offset");
    private static readonly int Size = Shader.PropertyToID("_Size");
    private static readonly int Speed = Shader.PropertyToID("_Speed");
    private static readonly int Exposure = Shader.PropertyToID("_Exposure");
    private static readonly int Contrast = Shader.PropertyToID("_Contrast");
    private static readonly int CutOut = Shader.PropertyToID("_CutOut");

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

    #region Public methods

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
    
    public void TransitionTo(ChromaticAberrationSettings customSettings, float duration = 1f)
    {
        if (_currentTransition != null) StopCoroutine(_currentTransition);
        _currentTransition = StartCoroutine(TransitionCoroutine(GetCurrentSettingsFromMaterial(), customSettings, duration));
    }

    public void TransitionFrom(ChromaticAberrationSettings customSettings, float duration = 1f)
    {
        if (_currentTransition != null) StopCoroutine(_currentTransition);
        _currentTransition = StartCoroutine(TransitionCoroutine(customSettings, offSettings, duration));
    }
    
    public void Punch(ChromaticAberrationPunchSettings punchConfig, bool resetCurrentEffect = false)
    {
        if (_currentTransition != null) StopCoroutine(_currentTransition);
        if (resetCurrentEffect) ApplySettings(offSettings);
        _currentTransition = StartCoroutine(PunchCoroutine(punchConfig.punchSettings, punchConfig.punchDuration, punchConfig.returnDuration));
    }

    #endregion Public methods

    #region Private methods

    private IEnumerator PunchCoroutine(ChromaticAberrationSettings punchSettings, float punchDuration, float returnDuration)
    {
        ChromaticAberrationSettings startSettings = GetCurrentSettingsFromMaterial();
        
        yield return StartCoroutine(TransitionCoroutine(startSettings, punchSettings, punchDuration, false));
        yield return StartCoroutine(TransitionCoroutine(punchSettings, offSettings, returnDuration, false));
        
        _currentTransition = null;
    }
    
    private IEnumerator TransitionCoroutine(ChromaticAberrationSettings from, ChromaticAberrationSettings to, float duration, bool clearCurrentTransition = true)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            ChromaticAberrationSettings current = LerpSettings(from, to, t);
            ApplySettings(current);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplySettings(to);
        
        if (clearCurrentTransition)
            _currentTransition = null;
    }
    
    private ChromaticAberrationSettings LerpSettings(ChromaticAberrationSettings a, ChromaticAberrationSettings b, float t)
    {
        ChromaticAberrationSettings result = new ChromaticAberrationSettings
        {
            on = t > 0.5f ? b.on : a.on, // Boolean lerp - switches at halfway point
            redOffset = Vector3.Lerp(a.redOffset, b.redOffset, t),
            greenOffset = Vector3.Lerp(a.greenOffset, b.greenOffset, t),
            blueOffset = Vector3.Lerp(a.blueOffset, b.blueOffset, t),
            size = Mathf.Lerp(a.size, b.size, t),
            speed = Vector2.Lerp(a.speed, b.speed, t),
            exposure = Mathf.Lerp(a.exposure, b.exposure, t),
            contrast = Mathf.Lerp(a.contrast, b.contrast, t),
            cutOut = Mathf.Lerp(a.cutOut, b.cutOut, t)
        };

        return result;
    }
    
    private void ApplySettings(ChromaticAberrationSettings settings)
    {
        if (!chromaticAberrationMaterial) return;
        
        chromaticAberrationMaterial.SetFloat(On, settings.on ? 1f : 0f);
        chromaticAberrationMaterial.SetVector(RedOffset, settings.redOffset);
        chromaticAberrationMaterial.SetVector(GreenOffset, settings.greenOffset);
        chromaticAberrationMaterial.SetVector(BlueOffset, settings.blueOffset);
        chromaticAberrationMaterial.SetFloat(Size, settings.size);
        chromaticAberrationMaterial.SetVector(Speed, settings.speed);
        chromaticAberrationMaterial.SetFloat(Exposure, settings.exposure);
        chromaticAberrationMaterial.SetFloat(Contrast, settings.contrast);
        chromaticAberrationMaterial.SetFloat(CutOut, settings.cutOut);
    }
    
    private ChromaticAberrationSettings GetCurrentSettingsFromMaterial()
    {
        if (!chromaticAberrationMaterial) return new ChromaticAberrationSettings();
        
        ChromaticAberrationSettings current = new ChromaticAberrationSettings
        {
            on = chromaticAberrationMaterial.GetFloat(On) > 0.5f,
            redOffset = chromaticAberrationMaterial.GetVector(RedOffset),
            greenOffset = chromaticAberrationMaterial.GetVector(GreenOffset),
            blueOffset = chromaticAberrationMaterial.GetVector(BlueOffset),
            size = chromaticAberrationMaterial.GetFloat(Size),
            speed = chromaticAberrationMaterial.GetVector(Speed),
            exposure = chromaticAberrationMaterial.GetFloat(Exposure),
            contrast = chromaticAberrationMaterial.GetFloat(Contrast),
            cutOut = chromaticAberrationMaterial.GetFloat(CutOut)
        };

        return current;
    }

    #endregion Private methods
}