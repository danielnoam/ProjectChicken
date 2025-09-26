using UnityEngine;
using System.Collections;
using DNExtensions;
using UnityEngine.UI;

public class UIElementAlphaEffector : MonoBehaviour
{
    [SerializeField] private MaskableGraphic maskableGraphic;
    [SerializeField, MinMaxRange(0,1)] private RangedFloat alphaRange = new RangedFloat(0f, 1f);
    [SerializeField] private float duration = 2f;
    [SerializeField] private int cycleCount = 3;
    [SerializeField, Range(0, 1)] private float startValue;
    [SerializeField, Range(0, 1)] private float endValue;
    [SerializeField] private bool startEffectOnStart = true;

    
    private Coroutine _effectCoroutine;
    
    private void Start()
    {
        if (startEffectOnStart)
        {
            StartEffect();
        }
    }
    
    public void StartEffect()
    {
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
        }
        
        _effectCoroutine = StartCoroutine(FadeEffect());
    }
    
    private IEnumerator FadeEffect()
    {
        yield return StartCoroutine(FadeToAlpha(startValue, 0.1f));
        
        float cycleTime = duration / cycleCount;
        
        for (int i = 0; i < cycleCount; i++)
        {
            yield return StartCoroutine(FadeToAlpha(alphaRange.maxValue, cycleTime * 0.5f));
            yield return StartCoroutine(FadeToAlpha(alphaRange.minValue, cycleTime * 0.5f));
        }
        
        yield return StartCoroutine(FadeToAlpha(endValue, 0.1f));
    }
    
    private IEnumerator FadeToAlpha(float targetAlpha, float fadeTime)
    {
        Color startColor = maskableGraphic.color;
        float startAlpha = startColor.a;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeTime);
            
            Color newColor = startColor;
            newColor.a = currentAlpha;
            maskableGraphic.color = newColor;
            
            yield return null;
        }
        
        Color finalColor = maskableGraphic.color;
        finalColor.a = targetAlpha;
        maskableGraphic.color = finalColor;
    }
}