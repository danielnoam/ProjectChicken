
using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LevelManager))]
public class LevelManagerInput: InputReaderBase
{
    [SerializeField, Self, HideInInspector] private LevelManager levelManager;

    
    private InputActionMap _playerActionMap;
    private InputActionMap _uiActionMap;
    private InputAction _pauseAction;
    private float _pauseTimer;
    private bool _pauseInputHeld;
    
    public event Action<InputAction.CallbackContext> OnPauseActionEvent;



    protected override void Awake()
    {
        base.Awake();

        _playerActionMap = PlayerInput.actions.FindActionMap("Player");
        _uiActionMap = PlayerInput.actions.FindActionMap("UI");
        
        if (_playerActionMap == null)
        {
            Debug.LogError("Player Map not found. Please check the action maps in the Player Input component.");
            return;
        }
        
        if (_uiActionMap == null)
        {
            Debug.LogError("UI Map not found. Please check the action maps in the Player Input component.");
            return;
        }
        

        _pauseAction = _playerActionMap.FindAction("Pause");
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        
        SubscribeToAction(_pauseAction, OnPauseAction);
        
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
            levelManager.OnPause += OnPause;
        }
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        
        UnsubscribeFromAction(_pauseAction, OnPauseAction);
        
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
            levelManager.OnPause -= OnPause;
        }
    }

    private void OnPause(bool paused)
    {
        inputManager?.SetCursorVisibility(ShouldShowCursor());
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        inputManager?.SetCursorVisibility(ShouldShowCursor());
    }
    
    private void OnPauseAction(InputAction.CallbackContext context)
    {
        OnPauseActionEvent?.Invoke(context);
    }
    
    
    private bool ShouldShowCursor()
    {
        if (!levelManager) return true;
        
        if (levelManager.IsGamePaused)
        {
            return true;
        }
        
        switch (levelManager.CurrentStage.StageType)
        {
            case StageType.Delay: return false;
            case StageType.Store: return true;
            case StageType.EnemyWave: return false;
            case StageType.Intro: return false;
            case StageType.Outro: return levelManager.CurrentStage.ShowOutroMenu;
            default: return true;
        }
    }
}