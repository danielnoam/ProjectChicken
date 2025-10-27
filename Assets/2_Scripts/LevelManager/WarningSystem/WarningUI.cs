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
    [SerializeField] private Image warningIconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private RectTransform _rectTransform;
    private Sequence _showSequence;
    private Sequence _hideSequence;
    private bool _isVisible;
    private bool _wasPlayingBeforePause;
    private LevelManager _levelManager;
    

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

    private void Start()
    {
        if (!_levelManager)
        {
            _levelManager = LevelManager.Instance;
            _levelManager.OnPause += OnPause;
        }

    }

    private void OnEnable()
    {
        if (_levelManager)
        {
            _levelManager.OnPause += OnPause;
        }
    }

    private void OnDisable()
    {
        if (_levelManager)
        {
            _levelManager.OnPause -= OnPause;
        }
    }

    private void OnPause(bool paused)
    {
        if (paused && audioSource.isPlaying)
        {
            _wasPlayingBeforePause = true;
            audioSource.Pause();
        }
        else if (!paused && !audioSource.isPlaying && _wasPlayingBeforePause)
        {
            _wasPlayingBeforePause = false;
            audioSource.Play();
        }
        
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
        
        // Fade out sfx
        _hideSequence.Group(Tween.AudioVolume(audioSource, 0, showHideDuration));
        
        // Chain completion callback
        _hideSequence.OnComplete(() =>
        {
            _isVisible = false;
            OnWarningHidden?.Invoke();
            audioSource.clip = null;
        });
    }

    private void SetupWarningContent(SOWarning warning)
    {
        // Set icon
        if (iconImage)
        {
            iconImage.sprite = warning.Icon;
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
        warning.WarningSfx?.Play(audioSource);
        
        // Set initial states for show animation (start from offset)
        _rectTransform.localScale = _originalScale + warning.ShowScaleOffset;
        
        // Always start with alpha 0 and fade in
        canvasGroup.alpha = 0f;
    }

    private void BuildShowAnimation(SOWarning warning, ref Sequence sequence)
    {
        sequence.Group(Tween.ShakeLocalPosition(
            transform,
            warning.ShowShakeStrength,
            warning.ShowShakeDuration,
            warning.ShowShakeFrequency,
            easeBetweenShakes: warning.ShowShakeEase
        ));
        sequence.Group(Tween.ShakeLocalPosition(
            iconImage.transform,
            warning.ShowShakeStrength,
            warning.Duration,
            warning.ShowShakeFrequency,
            easeBetweenShakes: warning.ShowShakeEase, 
            startDelay: 0.1f
        ));
        sequence.Group(Tween.ShakeLocalPosition(
            warningIconImage.transform,
            warning.ShowShakeStrength,
            warning.Duration,
            warning.ShowShakeFrequency,
            easeBetweenShakes: warning.ShowShakeEase, 
            startDelay: 0.2f
        ));
        

        sequence.Group(Tween.Scale(
            transform,
            _originalScale,
            warning.ShowScaleDuration,
            warning.ShowScaleEase
        ));
        sequence.Group(Tween.Alpha(
            canvasGroup,
            1f,
            showHideDuration
        ));
    }

    private void BuildHideAnimation(SOWarning warning, ref Sequence sequence)
    {
        sequence.Group(Tween.ShakeLocalPosition(
            transform,
            warning.HideShakeStrength,
            warning.HideShakeDuration,
            warning.HideShakeFrequency,
            easeBetweenShakes: warning.HideShakeEase
        ));
        sequence.Group(Tween.Scale(
            transform,
            _originalScale + warning.HideScaleOffset,
            warning.HideScaleDuration,
            warning.HideScaleEase
        ));
    }
}