using System;
using PrimeTween;
using UnityEngine;

public class MenuElementLaunchLever : MenuElement
{
    
    [Header("Lever Press Animation")]
    [SerializeField] private float delayBeforeLaunch = 0.5f;
    [SerializeField] private float animationDuration = 0.75f;
    [SerializeField] private Vector3 leverPressedRotation = new Vector3(55f, 0, 0);
    [SerializeField] protected Ease animationEase = Ease.Default;
    
    
    [Header("References")]
    [SerializeField] private MenuElementLevelSelection levelSelection;
    [SerializeField] private SOAudioEvent leverPressedSfx;
    [SerializeField] private Transform leverPivotTransform;

    
    private Sequence _leverPressSequence;
    private Vector3 _leverStartRot;
    

    protected override void OnSelected()
    {
        
    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {
        if (leverPivotTransform) _leverStartRot = leverPivotTransform.localEulerAngles;

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

        float delayBeforeAnimation = menuController.LaunchMissionMode == LaunchMissionMode.Auto ? 1.5f : 0f;
        
        _leverPressSequence = Sequence.Create()
                .Group(Tween.LocalRotation(leverPivotTransform,startDelay: delayBeforeAnimation, startValue: _leverStartRot,endValue: leverPressedRotation, duration: animationDuration, ease: animationEase))
                .ChainCallback(() => leverPressedSfx?.Play(audioSource))
                .ChainDelay(delayBeforeLaunch)
                .OnComplete(FinishedInteraction)
            ;
    }
    
}
