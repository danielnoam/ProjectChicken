using System;
using UnityEngine;

[Serializable]
public abstract class StageTask
{
    [SerializeField,Multiline(lines: 2)] protected string description;
    [SerializeField] protected bool isCompleted;
    
    public bool IsCompleted => isCompleted;
    public string Description => description;
    
    public event Action<StageTask> OnTaskCompleted;
    
    public abstract void Initialize(LevelManager levelManager);
    public abstract void Cleanup();
    
    protected virtual void CompleteTask()
    {
        if (isCompleted) return;
        
        isCompleted = true;
        OnTaskCompleted?.Invoke(this);
    }
}

[Serializable]
public class CollectResourceTask : StageTask
{
    [SerializeField] private int targetAmount;
    [SerializeField] private ResourceType resourceType;
    private int _currentAmount;
    private LevelManager _levelManager;
    
    public override void Initialize(LevelManager levelManager)
    {
        this._levelManager = levelManager;
        _currentAmount = 0;
        isCompleted = false;
        
        if (levelManager.Player)
        {
            levelManager.Player.ResourceCollector.OnResourceCollected += OnResourceCollected;
        }
    }
    
    public override void Cleanup()
    {
        if (_levelManager?.Player)
        {
            _levelManager.Player.ResourceCollector.OnResourceCollected -= OnResourceCollected;
        }
    }
    
    private void OnResourceCollected(Resource resource)
    {
        if (resource.ResourceType == resourceType)
        {
            _currentAmount++;
            if (_currentAmount >= targetAmount)
            {
                CompleteTask();
            }
        }
    }
}

[System.Serializable]
public class AccumulateScoreTask : StageTask
{
    [SerializeField] private int targetScore;
    private int _accumulatedScore;
    private LevelManager _levelManager;
    
    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _accumulatedScore = levelManager.CurrentScore;
        isCompleted = false;
        
        levelManager.OnScoreChanged += OnScoreChanged;
    }
    
    public override void Cleanup()
    {
        if (_levelManager != null)
        {
            _levelManager.OnScoreChanged -= OnScoreChanged;
        }
    }
    
    private void OnScoreChanged(int newScore)
    {
        _accumulatedScore += newScore;
        if (_accumulatedScore >= targetScore)
        {
            CompleteTask();
        }
    }
}

[Serializable]
public class UseSpecificActionTask : StageTask
{
    public enum ActionType
    {
        Move,
        Dodge,
        Look,
        Attack,
        Overheat,
        OverheatMiniGameCompleted,
    }
    
    [SerializeField] private int timesRequired = 1;
    [SerializeField] private ActionType requiredAction;
    private int _currentCount;
    private LevelManager _levelManager;
    private RailPlayerInput _playerInput;
    private RailPlayerWeaponSystem _weaponSystem;
    private RailPlayerMovement _playerMovement;
    
    public override void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
        _currentCount = 0;
        isCompleted = false;
        
        if (levelManager.Player)
        {
            _playerInput = levelManager.Player.GetComponent<RailPlayerInput>();
            _weaponSystem = levelManager.Player.GetComponent<RailPlayerWeaponSystem>();
            _playerMovement = levelManager.Player.GetComponent<RailPlayerMovement>();
            
            SubscribeToActionEvent();
        }
    }
    
    public override void Cleanup()
    {
        UnsubscribeFromActionEvent();
    }
    
    private void SubscribeToActionEvent()
    {
        switch (requiredAction)
        {
            case ActionType.Move:
                if (_playerInput)
                    _playerInput.OnMoveEvent += OnMoveTriggered;
                break;
            case ActionType.Dodge:
                if (_playerMovement)
                    _playerMovement.OnDodge += OnDodgeTriggered;
                break;
            case ActionType.Look:
                if (_playerInput)
                    _playerInput.OnLookEvent += OnLookTriggered;
                break;
            case ActionType.Attack:
                if (_weaponSystem)
                    _weaponSystem.OnWeaponUsed += OnAttackTriggered;
                break;
            case ActionType.Overheat:
                if (_weaponSystem)
                    _weaponSystem.OnWeaponOverheatedEvent += OnOverheatTriggered;
                break;
            case ActionType.OverheatMiniGameCompleted:
                if (_weaponSystem)
                    _weaponSystem.OnWeaponHeatMiniGameSucceededEvent += OnOverheatMiniGameCompleted;
                break;
        }
    }
    
    private void UnsubscribeFromActionEvent()
    {
        switch (requiredAction)
        {
            case ActionType.Move:
                if (_playerInput)
                    _playerInput.OnMoveEvent -= OnMoveTriggered;
                break;
            case ActionType.Dodge:
                if (_playerMovement)
                    _playerMovement.OnDodge -= OnDodgeTriggered;
                break;
            case ActionType.Look:
                if (_playerInput)
                    _playerInput.OnLookEvent -= OnLookTriggered;
                break;
            case ActionType.Attack:
                if (_weaponSystem)
                    _weaponSystem.OnWeaponUsed -= OnAttackTriggered;
                break;
            case ActionType.Overheat:
                if (_weaponSystem)
                    _weaponSystem.OnWeaponOverheatedEvent -= OnOverheatTriggered;
                break;
            case ActionType.OverheatMiniGameCompleted:
                if (_weaponSystem)
                    _weaponSystem.OnWeaponHeatMiniGameSucceededEvent -= OnOverheatMiniGameCompleted;
                break;
        }
    }

    private void OnOverheatTriggered()
    {
        if (isCompleted) return;
        IncrementCount();
    }
    
    private void OnOverheatMiniGameCompleted()
    {
        if (isCompleted) return;
        IncrementCount();
    }

    private void OnMoveTriggered(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (isCompleted || !context.performed) return;
        
        Vector2 moveInput = context.ReadValue<Vector2>();
        if (moveInput.magnitude > 0.1f)
        {
            IncrementCount();
        }
    }
    
    private void OnDodgeTriggered()
    {
        if (isCompleted) return;
        IncrementCount();
    }
    
    private void OnLookTriggered(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (isCompleted || !context.performed) return;
        
        Vector2 lookInput = context.ReadValue<Vector2>();
        if (lookInput.magnitude > 0.1f)
        {
            IncrementCount();
        }
    }
    
    private void OnAttackTriggered(WeaponInstance weapon)
    {
        if (isCompleted) return;
        IncrementCount();
    }
    
    
    private void IncrementCount()
    {
        _currentCount++;
        if (_currentCount >= timesRequired)
        {
            CompleteTask();
        }
    }
}