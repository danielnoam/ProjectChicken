using PrimeTween;
using UnityEngine;

public class MenuElementEjectButton : MenuElement
{
    
    [Header("Button Press Animation")]
    [SerializeField] private float delayBeforeQuit = 0.5f;
    [SerializeField] private float animationDuration = 0.75f;
    [SerializeField] private Vector3 buttonPressedPosition = new Vector3(0, -0.5f, 0);
    [SerializeField] protected Ease animationEase = Ease.Default;

    
    
    [Header("References")]
    [SerializeField] private SOAudioEvent buttonPressedSfx;
    [SerializeField] private Transform buttonTransform;
    
    private Sequence _buttonPressSequence;
    private Vector3 _buttonStartPos;

    
    protected override void OnSelected()
    {

    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {
        if (buttonTransform) _buttonStartPos = buttonTransform.localPosition;
    }

    protected override void OnInteract()
    {
        if (!buttonTransform)
        {
            FinishedInteraction();
            return;
        }
        
        
        if (_buttonPressSequence.isAlive) _buttonPressSequence.Stop();

        float animationTime = animationDuration / 2;

        _buttonPressSequence = Sequence.Create()
                .Group(Tween.LocalPosition(buttonTransform, startValue: _buttonStartPos,endValue: buttonPressedPosition, duration: animationTime, ease: animationEase))
                .ChainCallback(() => buttonPressedSfx?.Play(audioSource))
                .Chain(Tween.LocalPosition(buttonTransform, startValue: buttonPressedPosition,endValue: _buttonStartPos, duration: animationTime, ease: animationEase))
                .ChainDelay(delayBeforeQuit)
                .OnComplete(FinishedInteraction)
            ;
    }

    protected override void OnFinishedInteraction()
    {
        if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
        {
            Application.Quit();
        }
    }
    
    protected override void OnStopInteraction()
    {
        if (_buttonPressSequence.isAlive) _buttonPressSequence.Stop();
        
        _buttonPressSequence = Sequence.Create()
                .Group(Tween.LocalPosition(buttonTransform, startValue: buttonPressedPosition,endValue: _buttonStartPos, duration: animationDuration, ease: animationEase))
            ;
    }
    
}
