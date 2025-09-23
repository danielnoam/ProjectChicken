using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{

    
    [Header("References")]
    [SerializeField] private CanvasGroup pauseMenuCanvasGroup;
    [SerializeField] private CanvasGroup pauseScreen;
    [SerializeField] private CanvasGroup optionsScreen;
    [SerializeField] private Button pauseScreenButton;
    [SerializeField] private Button optionsScreenButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;


    private bool _isVisible;

    private void Awake()
    {
        SetPauseMenuVisible(false);
        SetOptionsScreen(false);
        SetPauseScreen(false);


        if (resumeButton)
        {
            resumeButton.onClick.AddListener(() =>
            {
                levelManager?.SetPausedState(false);
            });
        }

        if (mainMenuButton)
        {
            mainMenuButton.onClick.AddListener(() =>
            {
                levelManager?.SetPausedState(false);
                levelManager?.ReturnToMainMenu(0);
            });
        }

        if (optionsScreenButton)
        {
            optionsScreenButton.onClick.AddListener(() =>
            {
                SetPauseScreen(false);
                SetOptionsScreen(true);
            });
        }
        
        if (pauseScreenButton)
        {
            pauseScreenButton.onClick.AddListener(() =>
            {
                SetPauseScreen(true);
                SetOptionsScreen(false);
            });
        }
    }

    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnPause += OnPause;
        }
    }

    private void OnDisable()
    {
        
        if (levelManager)
        {
            levelManager.OnPause -= OnPause;
        }
        
        
    }

    private void OnPause(bool paused)
    {
        switch (paused)
        {
            case true when !_isVisible:
                SetPauseMenuVisible(true);
                break;
            case false when _isVisible:
                SetPauseMenuVisible(false);
                break;
        }
    }
    
    private void SetPauseMenuVisible(bool visible)
    {
        _isVisible = visible;
        pauseMenuCanvasGroup.alpha = visible ? 1 : 0;
        pauseMenuCanvasGroup.interactable = visible;
        pauseMenuCanvasGroup.blocksRaycasts = visible;

        if (visible)
        {
            SetPauseScreen(true);
            SetOptionsScreen(false);
        }
    }
    
    
    private void SetPauseScreen(bool active)
    {
        pauseScreen.alpha = active ? 1 : 0;
        pauseScreen.interactable = active;
        pauseScreen.blocksRaycasts = active;
    }
    private void SetOptionsScreen(bool active)
    {
        optionsScreen.alpha = active ? 1 : 0;
        optionsScreen.interactable = active;
        optionsScreen.blocksRaycasts = active;
    }
}
