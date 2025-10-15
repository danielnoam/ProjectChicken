using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class WarningUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float showHideDuration = 0.3f;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private RectTransform _rectTransform;
    private Sequence _showSequence;
    private Sequence _hideSequence;
    private bool _isVisible;
    

    private Vector3 _originalScale;
    
    public event Action OnWarningHidden;
    public event Action OnWarningCompleted;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        
        if (!canvasGroup)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        _originalScale = _rectTransform.localScale;
        canvasGroup.alpha = 0f;
    }

    public void ShowWarning(SOWarning warning)
    {
        if (!warning) return;
        
        // Stop any active sequences
        if (_showSequence.isAlive) _showSequence.Stop();
        if (_hideSequence.isAlive) _hideSequence.Stop();
        
        // Setup warning content and initial states
        SetupWarningContent(warning);
        
        // Create show animation sequence
        _showSequence = Sequence.Create();
        
        // Build the show animation from warning data
        BuildShowAnimation(warning, ref _showSequence);
        
        // Chain the duration wait and completion callback
        _showSequence.ChainDelay(warning.Duration);
        _showSequence.OnComplete(() => OnWarningCompleted?.Invoke());
        
        _isVisible = true;
    }

    public void HideWarning(SOWarning warning)
    {
        if (!_isVisible) return;
        
        // Stop any active sequences
        if (_showSequence.isAlive) _showSequence.Stop();
        if (_hideSequence.isAlive) _hideSequence.Stop();
        
        // Create hide animation sequence
        _hideSequence = Sequence.Create();
        
        // Build the hide animation from warning data
        BuildHideAnimation(warning, ref _hideSequence);
        
        // Always fade out and move to hide position at the end
        _hideSequence.Group(Tween.Alpha(canvasGroup, 0f, showHideDuration));
        
        // Chain completion callback
        _hideSequence.OnComplete(() =>
        {
            _isVisible = false;
            OnWarningHidden?.Invoke();
        });
    }

    private void SetupWarningContent(SOWarning warning)
    {
        // Set icon
        if (iconImage)
        {
            if (warning.Icon) iconImage.sprite = warning.Icon;
            iconImage.color = warning.IconColor;
        }
        
        // Set background color
        if (backgroundImage)
        {
            backgroundImage.color = warning.BackgroundColor;
        }
        
        // Set message text
        if (warningText && !string.IsNullOrEmpty(warning.Message))
        {
            warningText.text = warning.Message;
        }
        
        // Play audio
        if (audioSource && warning.AudioClip)
        {
            audioSource.PlayOneShot(warning.AudioClip);
        }
        
        // Set initial states for show animation (start from offset)
        if (warning.AnimateShowScale)
        {
            _rectTransform.localScale = _originalScale + warning.ShowScaleOffset;
        }
        
        // Always start with alpha 0 and fade in
        canvasGroup.alpha = 0f;
    }

    private void BuildShowAnimation(SOWarning warning, ref Sequence sequence)
    {
        // Shake Animation
        if (warning.AnimateShowShake)
        {
            sequence.Group(Tween.ShakeLocalPosition(
                transform,
                warning.ShowShakeStrength,
                warning.ShowShakeDuration,
                warning.ShowShakeFrequency,
                easeBetweenShakes: warning.ShowShakeEase
            ));
        }
        
        // Scale Animation - animate FROM offset TO original
        if (warning.AnimateShowScale)
        {
            sequence.Group(Tween.Scale(
                transform,
                _originalScale,
                warning.ShowScaleDuration,
                warning.ShowScaleEase
            ));
        }
        
        // Alpha Animation - always fade in to 1
        sequence.Group(Tween.Alpha(
            canvasGroup,
            1f,
            showHideDuration
        ));
    }

    private void BuildHideAnimation(SOWarning warning, ref Sequence sequence)
    {
        // Shake Animation
        if (warning.AnimateHideShake)
        {
            sequence.Group(Tween.ShakeLocalPosition(
                transform,
                warning.HideShakeStrength,
                warning.HideShakeDuration,
                warning.HideShakeFrequency,
                easeBetweenShakes: warning.HideShakeEase
            ));
        }
        
        // Scale Animation - animate FROM original TO offset
        if (warning.AnimateHideScale)
        {
            sequence.Group(Tween.Scale(
                transform,
                _originalScale + warning.HideScaleOffset,
                warning.HideScaleDuration,
                warning.HideScaleEase
            ));
        }
    }
}