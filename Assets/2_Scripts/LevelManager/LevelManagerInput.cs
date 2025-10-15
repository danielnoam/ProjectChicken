
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
    private InputAction _submitAction;
    private InputAction _cancelAction;
    private InputAction _navigateAction;
    private InputAction _skipAction;
    private float _pauseTimer;
    private bool _pauseInputHeld;
    
    public event Action<InputAction.CallbackContext> OnPauseActionEvent;
    public event Action<InputAction.CallbackContext> OnSubmitActionEvent;
    public event Action<InputAction.CallbackContext> OnCancelActionEvent;
    public event Action<InputAction.CallbackContext> OnNavigateActionEvent;
    public event Action<InputAction.CallbackContext> OnSkipActionEvent;



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
        _submitAction = _uiActionMap.FindAction("Submit");
        _cancelAction = _uiActionMap.FindAction("Cancel");
        _navigateAction = _uiActionMap.FindAction("Navigate");
        _skipAction = _playerActionMap.FindAction("Skip");
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        
        SubscribeToAction(_pauseAction, OnPauseAction);
        SubscribeToAction(_submitAction, OnSubmitAction);
        SubscribeToAction(_cancelAction, OnCancelAction);
        SubscribeToAction(_navigateAction, OnNavigateAction);
        SubscribeToAction(_skipAction, OnSkipAction);
        
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
        UnsubscribeFromAction(_submitAction, OnSubmitAction);
        UnsubscribeFromAction(_cancelAction, OnCancelAction);
        UnsubscribeFromAction(_navigateAction, OnNavigateAction);
        UnsubscribeFromAction(_skipAction, OnSkipAction);
        
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
            levelManager.OnPause -= OnPause;
        }
    }

    private void OnSkipAction(InputAction.CallbackContext context)
    {
        OnSkipActionEvent?.Invoke(context);
    }


    private void OnPauseAction(InputAction.CallbackContext context)
    {
        OnPauseActionEvent?.Invoke(context);
    }
    
    private void OnSubmitAction(InputAction.CallbackContext context)
    {
        
        OnSubmitActionEvent?.Invoke(context);

    }
    
    private void OnCancelAction(InputAction.CallbackContext context)
    {
        OnCancelActionEvent?.Invoke(context);
        
    }
    
    
    private void OnNavigateAction(InputAction.CallbackContext context)
    {
        OnNavigateActionEvent?.Invoke(context);

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
            case StageType.Task: return false;
            default: return true;
        }
    }
}