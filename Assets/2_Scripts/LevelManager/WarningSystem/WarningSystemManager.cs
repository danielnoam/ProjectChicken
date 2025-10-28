using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using VInspector;

public class WarningSystemManager : MonoBehaviour
{
    public static WarningSystemManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private WarningUI warningUI;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private EnemySpawner enemySpawner;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;
    [SerializeField] private SOWarning testWarning;
    
    private readonly List<SOWarning> _warnings = new List<SOWarning>();
    private SOWarning _currentWarning;
    private bool _warningPlaying;
    private bool _hasShownLowHealthWarning;
    private bool _hasShownCriticalHealthWarning;
    
    public event Action<SOWarning> OnWarningFinished;
    public event Action<SOWarning> OnWarningStarted;
    
    private void OnValidate()
    {
        if (!levelManager) levelManager = FindFirstObjectByType<LevelManager>();
        if (!player) player = FindFirstObjectByType<RailPlayer>();
        if (!enemySpawner) enemySpawner = FindFirstObjectByType<EnemySpawner>();
        this.ValidateRefs();
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    }
    
    private void OnEnable()
    {
        if (warningUI)
        {
            warningUI.OnWarningHidden += OnWarningHidden;
            warningUI.OnWarningCompleted += OnWarningCompleted;
        }
        
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
        }
        
    }
    
    private void OnDisable()
    {
        if (warningUI)
        {
            warningUI.OnWarningHidden -= OnWarningHidden;
            warningUI.OnWarningCompleted -= OnWarningCompleted;
        }
        
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }

        
    }
    
    private void Update()
    {
        if (_warningPlaying || _warnings.Count == 0) return;
        
        PlayNextWarning();
    }
    
    private void OnWarningHidden()
    {
        _warningPlaying = false;
    }
    
    private void OnWarningCompleted()
    {
        OnWarningFinished?.Invoke(_currentWarning);
        _warningPlaying = false;
        
        if (_warnings.Count == 0)
        {
            warningUI.HideWarning(_currentWarning);
        }
    }
    
   
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        if (stage.StageType is StageType.Intro or StageType.Store)
        {
            ClearWarnings();
        }
        
        AddWarning(stage.StartWarning);
    }
    
    public void AddWarning(SOWarning warning)
    {
        if (!warning) return;
        
        _warnings.Add(warning);
    }
    
    private void PlayNextWarning()
    {
        if (_warnings.Count == 0) return;
        
        var warning = _warnings[0];
        _warnings.RemoveAt(0);
        PlayWarning(warning);
    }
    
    public void PlayWarning(SOWarning warning)
    {
        _warningPlaying = true;
        _currentWarning = warning;
        warningUI.ShowWarning(warning);
        
        OnWarningStarted?.Invoke(warning);
    }
    
    private void ClearWarnings()
    {
        _warnings.Clear();
        
        if (_currentWarning)
        {
            warningUI.HideWarning(_currentWarning);
        }
        
        _currentWarning = null;
        _warningPlaying = false;
        

    }
    
    [Button]
    private void AddTestWarning()
    {
        AddWarning(testWarning);
    }
    
    [Button]
    private void PlayTestWarning()
    {
        PlayWarning(testWarning);
    }
}