using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;



public class OutroScreen : NavigatableUIScreen
{
    [SerializeField] private Button returnButton;
    [SerializeField] private Button continueButton;
    
    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    

    
    private Sequence _outroScreenSequence;
    public event Action OnScreenOpened;
    public event Action OnScreenClosed;
    
    
    protected override void OnValidate()
    {
        base.OnValidate();

        if (levelManager)
        {
            transform.position = levelManager.PlayerPosition;
        }
    }

    private void Awake()
    {
        Hide(false);
        SetupButtons();
    }

    protected override void OnStageChanged(SOLevelStage stage)
    {
        base.OnStageChanged(stage);
        
        if (stage.StageType == StageType.Outro && stage.ShowOutroMenu)
        {
            Show(stage.NextLevel.IsSceneValid());
        }
    }

    private void SetupButtons()
    {
        if (returnButton)
        {
            AddSelectable(returnButton);
            returnButton.onClick.AddListener(() =>
            {
                levelManager.ReturnToMainMenu();
                Hide(true);
            });
        }
        
        if (continueButton)
        {
            AddSelectable(continueButton);
            continueButton.onClick.AddListener(() =>
            {
                levelManager.LoadNextLevel();
                Hide(true);
            });
        }
    }

    private void Show(bool nextLevelIsAvailable)
    {
        isVisible = true;
        continueButton.interactable = nextLevelIsAvailable;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (_outroScreenSequence.isAlive) _outroScreenSequence.Stop();
        
        _outroScreenSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1, animationDuration))
            .OnComplete((() => OnScreenOpened?.Invoke()));
    }

    private void Hide(bool animate)
    {
        isVisible = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (_outroScreenSequence.isAlive) _outroScreenSequence.Stop();
        canvasGroup.alpha = 0f;

        if (animate)
        {
            _outroScreenSequence = Sequence.Create()
                .Group(Tween.Alpha(canvasGroup, 0, animationDuration));
        }

        OnScreenClosed?.Invoke();
    }
}