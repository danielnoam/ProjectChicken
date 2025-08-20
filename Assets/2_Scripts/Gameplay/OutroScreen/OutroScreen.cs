
using System;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class OutroScreen : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.5f;

    [Header("References")]
    [SerializeField] private SOGameSettings gameSettings;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button continueButton;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private CameraManager cameraManager;
    
    
    private Sequence _outroScreenSequence;
    public event Action OnScreenOpend;
    public event Action OnScreenClosed;
    
    private void OnValidate()
    {
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        
        if (!cameraManager)
        {
            cameraManager = FindFirstObjectByType<CameraManager>();
        }
        
        
        this.ValidateRefs();

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
    
    
    private void SetupButtons()
    {

        if (returnButton)
        {
            returnButton.onClick.AddListener(() =>
            {
                levelManager.ReturnToMainMenu();
                Hide(true);
            });
        }
        
        if (continueButton)
        {
            continueButton.onClick.AddListener(() =>
            {
                levelManager.LoadNextLevel();
                Hide(true);
            });
        }
    }

    public void Show(bool nextLevelIsAvailable)
    {
        continueButton.interactable = nextLevelIsAvailable;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        
        if (_outroScreenSequence.isAlive) _outroScreenSequence.Stop();
        
        _outroScreenSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1, animationDuration))
            .OnComplete((() => OnScreenOpend?.Invoke()));
    }

    public void Hide(bool animate)
    {
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
