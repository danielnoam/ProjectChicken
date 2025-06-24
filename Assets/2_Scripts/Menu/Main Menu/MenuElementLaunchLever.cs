using System;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;

public class MenuElementLaunchLever : MenuElement
{
    
    [Header("Lever Press Animation")]
    [SerializeField] private float delayBeforeLaunch = 0.5f;
    [SerializeField] private float animationDuration = 0.75f;
    [SerializeField] private Vector3 leverPressedRotation = new Vector3(55f, 0, 0);
    [SerializeField] protected Ease animationEase = Ease.Default;
    
    [Header("Emission Settings")]
    [SerializeField, ColorUsage(false, true)] private Color emissionColorOn = Color.white;
    [SerializeField] private Color emissionColorOff = Color.black;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float stateLerpSpeed = 5f;
    [SerializeField] private SOAudioEvent leverPressedSfx;
    
    [Header("References")]
    [SerializeField] private MenuElementLevelSelection levelSelection;
    [SerializeField] private Transform leverPivotTransform;
    [SerializeField] private Renderer selectedLevelLight;

    
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private Sequence _leverPressSequence;
    private Vector3 _leverStartRot;
    private Material _selectedLevelMaterial;
    private Color _currentEmissionColor;
    

    protected override void OnSelected()
    {
        
    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {
        if (leverPivotTransform) _leverStartRot = leverPivotTransform.localEulerAngles;

        // Set up the light material
        if (selectedLevelLight)
        {
            _selectedLevelMaterial = selectedLevelLight.material;
            _currentEmissionColor = _selectedLevelMaterial.GetColor(EmissionColor);
            _selectedLevelMaterial.SetColor(EmissionColor, emissionColorOff);
        }
        
        levelSelection.OnLevelSelected += OnLevelSelected;
        levelSelection.OnLevelDeselected += OnLevelDeselected;
        ToggleCanSelect(false, false);
    }

    protected override void OnInteract()
    {
        if (!leverPivotTransform || !levelSelection.SelectedLevel)
        {
            FinishedInteraction();
            return;
        }
        
        Launch();
    }

    protected override void OnFinishedInteraction()
    {
        if (!levelSelection.SelectedLevel) return;
        
        levelSelection.SelectedLevel.LoadLevel();
    }
    
    protected override void OnStopInteraction()
    {
        if (_leverPressSequence.isAlive) _leverPressSequence.Stop();
        
        _leverPressSequence = Sequence.Create()
                .Group(Tween.LocalRotation(leverPivotTransform, startValue: leverPressedRotation,endValue: _leverStartRot, duration: animationDuration, ease: animationEase))
            ;
    }
    
    private void OnLevelSelected()
    {
        ToggleCanSelect(true, false);
    }
    
    private void OnLevelDeselected()
    {
        ToggleCanSelect(false, false);
    }

    public void Launch()
    {
        if (_leverPressSequence.isAlive) _leverPressSequence.Stop();

        float delayBeforeAnimation = levelSelection.LaunchMissionMode == LaunchMissionMode.Auto ? 1.5f : 0f;
        
        _leverPressSequence = Sequence.Create()
                .Group(Tween.LocalRotation(leverPivotTransform,startDelay: delayBeforeAnimation, startValue: _leverStartRot,endValue: leverPressedRotation, duration: animationDuration, ease: animationEase))
                .ChainCallback(() => leverPressedSfx?.Play(audioSource))
                .ChainDelay(delayBeforeLaunch)
                .OnComplete(FinishedInteraction)
            ;
    }
    
    private void Update()
    {
        UpdateMaterialEmission();
    }
    
    private void UpdateMaterialEmission()
    {
        if (!_selectedLevelMaterial) return;
        
        
        Color targetColor;
            
        if (levelSelection.SelectedLevel)
        {
            // Create pulsing effect using sine wave
            float pulseValue = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0 to 1 range
            targetColor = Color.Lerp(emissionColorOff, emissionColorOn, pulseValue);
        }
        else
        {
            targetColor = emissionColorOff;
        }
            
        _currentEmissionColor = Color.Lerp(_currentEmissionColor, targetColor, Time.deltaTime * stateLerpSpeed);
        _selectedLevelMaterial.SetColor(EmissionColor, _currentEmissionColor);
    }
}