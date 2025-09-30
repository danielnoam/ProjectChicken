using System;
using DNExtensions;
using PrimeTween;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class InfoDisplay : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Ease animationEase = Ease.OutBack;
    [SerializeField] private SOAudioEvent showInfoSfx;
    [SerializeField] private SOAudioEvent hideInfoSfx;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private CanvasGroup canvasGroup;
    
    
    private Sequence _sequence;
    private Vector3 _canvasGroupStartScale;

    private void Awake()
    {
        _canvasGroupStartScale = canvasGroup.transform.localScale;
        canvasGroup.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
    }

    public void Show()
    {
        
        showInfoSfx?.Play(audioSource);
        
        if (_sequence.isAlive) _sequence.Stop();
        _sequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1f, animationDuration * 0.7f))
            .Group(Tween.Scale(canvasGroup.transform, _canvasGroupStartScale, animationDuration, animationEase)) ;
        
    }

    public void Hide()
    {
        hideInfoSfx?.Play(audioSource);
        
        if (_sequence.isAlive) _sequence.Stop();
        _sequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 0f, animationDuration * 0.5f))
            .Group(Tween.Scale(canvasGroup.transform, Vector3.zero, animationDuration, animationEase));
    }
    
    
}
