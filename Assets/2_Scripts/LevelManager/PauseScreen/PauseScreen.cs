using System;
using System.Linq;
using DNExtensions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseScreen : NavigatableUIScreen
{
    [SerializeField] private CanvasGroup pauseScreenCanvas;
    [SerializeField] private CanvasGroup optionsScreenCanvas;
    [SerializeField] private Button pauseScreenButton;
    [SerializeField] private Button optionsScreenButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    

    private CanvasGroup _currentScreen;
    public bool IsAtPauseScreen => _currentScreen == pauseScreenCanvas;
    

    private void Awake()
    {
        HidePauseMenu();
        SetupButtons();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
        if (levelManager)
        {
            levelManager.LevelManagerInput.OnCancelActionEvent += OnCancelAction;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        if (levelManager)
        {
            levelManager.LevelManagerInput.OnCancelActionEvent -= OnCancelAction;
        }
    }

    private void SetupButtons()
    {
        if (resumeButton)
        {
            AddSelectable(resumeButton);
            resumeButton.onClick.AddListener(() =>
            {
                levelManager?.SetPausedState(false);
            });
        }

        if (mainMenuButton)
        {
            AddSelectable(mainMenuButton);
            mainMenuButton.onClick.AddListener(() =>
            {
                levelManager?.SetPausedState(false);
                levelManager?.ReturnToMainMenu(0);
            });
        }

        if (optionsScreenButton)
        {
            AddSelectable(optionsScreenButton);
            optionsScreenButton.onClick.AddListener(() =>
            {
                SetActiveScreen(optionsScreenCanvas);
            });
        }
        
        if (pauseScreenButton)
        {
            AddSelectable(pauseScreenButton);
            pauseScreenButton.onClick.AddListener(() =>
            {
                SetActiveScreen(pauseScreenCanvas);
            });
        }
    }

    private void OnCancelAction(InputAction.CallbackContext callbackContext)
    {
        if (isVisible && _currentScreen != pauseScreenCanvas)
        {
            SetActiveScreen(pauseScreenCanvas);
        }
    }

    protected override void OnPause(bool paused)
    {
        switch (paused)
        {
            case true when !isVisible:
                ShowPauseMenu();
                break;
            case false when isVisible:
                HidePauseMenu();
                break;
        }
    }
    
    private void ShowPauseMenu()
    {
        isVisible = true;
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        SetActiveScreen(pauseScreenCanvas);
    }
    
    private void HidePauseMenu()
    {
        isVisible = false;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        SetOptionsScreen(false);
        SetPauseScreenCanvas(false);
    }
    
    private void SetActiveScreen(CanvasGroup screen)
    {
        _currentScreen = screen;
        SetPauseScreenCanvas(screen == pauseScreenCanvas);
        SetOptionsScreen(screen == optionsScreenCanvas);
        
        if (screen) SelectFirstAvailableButton();
    }
    
    private void SetPauseScreenCanvas(bool active)
    {
        pauseScreenCanvas.alpha = active ? 1 : 0;
        pauseScreenCanvas.interactable = active;
        pauseScreenCanvas.blocksRaycasts = active;
    }
    
    private void SetOptionsScreen(bool active)
    {
        optionsScreenCanvas.alpha = active ? 1 : 0;
        optionsScreenCanvas.interactable = active;
        optionsScreenCanvas.blocksRaycasts = active;
    }
}