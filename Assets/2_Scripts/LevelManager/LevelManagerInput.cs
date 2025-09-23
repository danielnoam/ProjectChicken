
using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LevelManager))]
public class LevelManagerInput: InputReaderBase
{
    [SerializeField, Self, HideInInspector] private LevelManager levelManager;

    
    private InputActionMap _playerActionMap;
    private InputAction _pauseAction;
    private float _pauseTimer;
    private bool _pauseInputHeld;
    
    public event Action<InputAction.CallbackContext> OnPauseActionEvent;



    protected override void Awake()
    {
        base.Awake();

        _playerActionMap = PlayerInput.actions.FindActionMap("Player");
        
        if (_playerActionMap == null)
        {
            Debug.LogError("Player Map not found. Please check the action maps in the Player Input component.");
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
        }
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        
        UnsubscribeFromAction(_pauseAction, OnPauseAction);
        
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }
    }
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage || !inputManager) return;

        switch (stage.StageType)
        {
            case StageType.Delay:
                inputManager.SetCursorVisibility(false);
                break;
            case StageType.Store:
                inputManager.SetCursorVisibility(true);
                break;
            case StageType.EnemyWave:
                inputManager.SetCursorVisibility(false);
                break;
            case StageType.Intro:
                inputManager.SetCursorVisibility(false);
                break;
            case StageType.Outro:
                inputManager.SetCursorVisibility(stage.ShowOutroMenu);
                break;
            default:
                inputManager.SetCursorVisibility(false);
                break;
        }
    }
    
    private void OnPauseAction(InputAction.CallbackContext context)
    {
        OnPauseActionEvent?.Invoke(context);
        
    }
}