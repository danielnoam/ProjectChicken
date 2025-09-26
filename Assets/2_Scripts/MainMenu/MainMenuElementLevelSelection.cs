using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using DNExtensions.MenuSystem;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VInspector;
using Sequence = PrimeTween.Sequence;

public class MainMenuElementLevelSelection : MainMenuElement
{
    
    [Header("Level Selection")]
    [SerializeField] private LaunchMissionMode launchMissionMode;
    [SerializeField] private SOLevel[] levels;
    
    [Foldout("References")]
    [SerializeField] private CanvasGroup levelsSelectionCanvas;
    [SerializeField] private Transform levelGfxParent;
    [SerializeField] private Transform levelButtonParent;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI levelDifficultyText;
    [SerializeField] private TextMeshProUGUI levelDescriptionText;
    [SerializeField] private TextMeshProUGUI levelBestScoreText;
    [SerializeField] private TMPWriter levelNameWriter;
    [SerializeField] private TMPWriter levelDifficultyWriter;
    [SerializeField] private TMPWriter levelDescriptionWriter;
    [SerializeField] private TMPWriter levelBestScoreWriter;
    [SerializeField] private Button levelButtonPrefab;
    [EndFoldout]

    [Separator]
    [SerializeField,DNExtensions.ReadOnly] private Selectable currentSelectable;
    private readonly List<LevelUIData> _levelUIData = new List<LevelUIData>();
    private Coroutine _writerDelayRoutine;
    private LevelUIData _currentlyShownLevel;
    private LevelUIData _selectedLevel;
    private Sequence _levelInfoCanvasSequence;
    
    
    
    public SOLevel SelectedLevel => _selectedLevel?.soLevel;
    public LaunchMissionMode LaunchMissionMode => launchMissionMode;
    public event Action<LaunchMissionMode> OnLevelSelected;
    public event Action OnLevelDeselected;
    
    
    
    
    protected override void OnSelected()
    {
    }

    protected override void OnDeselected()
    {
    }

    protected override void OnSetUp()
    {
        foreach (var level in levels)
        {
            GameObject levelGfx = null;
            
            if (levelGfxParent)
            {
                levelGfx = Instantiate(level.LevelGfxPrefab, levelGfxParent);
                levelGfx.SetActive(false);
            }

            if (levelButtonPrefab)
            {
                var levelButton = Instantiate(levelButtonPrefab, levelButtonParent);
                var levelUIData = new LevelUIData(level, levelGfx, levelButton);
                var levelButtonAnimator = levelButton.GetComponent<SelectableAnimator>();
                if (levelButtonAnimator) levelButtonAnimator.audioSource = audioSource;
                _levelUIData.Add(levelUIData);
                SetupSelectable(levelButton, levelUIData);
            }
        }
        
        levelNameText.text = "";
        levelDescriptionText.text = "";
        levelDifficultyText.text = "";
        levelBestScoreText.text = "";
        ToggleLevelCanvas(false, false);
    }
    
    protected override void OnInteract()
    {
        ToggleLevelCanvas(true);
        
        if (_selectedLevel != null)
        {
            ShowLevelInfo(_selectedLevel);
            currentSelectable = _selectedLevel.levelButton;
            currentSelectable.Select();
        }
        else
        {
            if (_writerDelayRoutine != null)
            {
                StopCoroutine(_writerDelayRoutine);
            }
            _writerDelayRoutine = StartCoroutine(StartWritersWithDelay());
            SelectFirstAvailableButton();
        }
    }
    
    protected override void OnFinishedInteraction()
    {
        ToggleLevelCanvas(false);
        if (_currentlyShownLevel != null)
        {
            HideLevelInfo(_currentlyShownLevel);
            _currentlyShownLevel = null;
        }

        currentSelectable = null;
    }
    
    protected override void OnStopInteraction()
    {
        ToggleLevelCanvas(false);
        if (_currentlyShownLevel != null)
        {
            HideLevelInfo(_currentlyShownLevel);
            _currentlyShownLevel = null;
        }
        currentSelectable = null;
    }
    
    
    protected override void OnNavigate(InputAction.CallbackContext context)
    {
        base.OnNavigate(context);
        
        if (currentSelectable) return;

        SelectFirstAvailableButton();
    }
    
    private void SelectLevel(LevelUIData levelUI)
    {
        if (!levelUI?.soLevel) return;
        
        if (levelUI == _selectedLevel)
        {
            DeselectLevel();
            return;
        }
        
        DeselectLevel();
        _selectedLevel = levelUI;
        

        if (_selectedLevel.levelButton)
        {
            _selectedLevel.levelButton.image.color = _selectedLevel.levelButton.colors.pressedColor;
        }
        
        OnLevelSelected?.Invoke(launchMissionMode);
        

        switch (launchMissionMode)
        {
            case LaunchMissionMode.None:
                _selectedLevel?.soLevel?.LoadLevel();
                break;
            case LaunchMissionMode.Manual:
                break;
            case LaunchMissionMode.ManualAutoExit:
                FinishedInteraction();
                break;
            case LaunchMissionMode.Auto:
                FinishedInteraction();
                break;
        }
    }
    

    private void DeselectLevel()
    {
        if (_selectedLevel == null) return;
        
        if (_selectedLevel.levelButton)
        {
            _selectedLevel.levelButton.image.color = _selectedLevel.levelButton.colors.normalColor;
        }
        
        _selectedLevel = null;
        OnLevelDeselected?.Invoke();
    }



    
    
    
    #region Level Info ---------------------------------------------------------------------------------

    
    private void ShowLevelInfo(LevelUIData levelUI)
    {
        if (!levelUI?.soLevel || _currentlyShownLevel == levelUI) return;
        
        _currentlyShownLevel = levelUI;

        levelNameText.text = levelUI.soLevel.LevelName;


        if (levelUI.levelButton.interactable)
        {
            levelDescriptionText.text = levelUI.soLevel.LevelDescription;


            if (levelUI.isCompleted && levelUI.soLevel.LevelDifficulty != LevelDifficulty.None)
            {
                levelBestScoreText.text = $"Best Score: \n{levelUI.bestScore:D7}";
            }
            else
            {
                levelBestScoreText.text = $"";
            }

        }
        else
        {
            levelBestScoreText.text = $"";
            levelDescriptionText.text = $"Complete these levels to unlock:";
            
            foreach (var level in levelUI.soLevel.LevelsNeededToUnlock)
            {
                levelDescriptionText.text += $"\n {level.name}" ;
            }

        }

        
        
        switch (levelUI.soLevel.LevelDifficulty)
        {
            case LevelDifficulty.None:
                levelDifficultyText.text = "";
                break;
            case LevelDifficulty.Easy:
                levelDifficultyText.text = "Easy";
                break;
            case LevelDifficulty.Medium:
                levelDifficultyText.text = "Medium";
                break;
            case LevelDifficulty.Hard:
                levelDifficultyText.text = "Hard";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        if (_writerDelayRoutine != null)
        {
            StopCoroutine(_writerDelayRoutine);
        }
        _writerDelayRoutine = StartCoroutine(StartWritersWithDelay());


        SetActiveLevelGraphics(levelUI);
    }

    private void HideLevelInfo(LevelUIData levelUI)
    {
        if (!levelUI?.soLevel) return;

        if (_selectedLevel != null)
        {
            if (_selectedLevel == levelUI)
            {
                // Don't hide if this is the selected level
                return;
            }
            else
            {
                // If we have a selected level, show that instead
                _currentlyShownLevel = null;
                ShowLevelInfo(_selectedLevel);
                return;
            }
        }
        
        _currentlyShownLevel = null;
        SetActiveLevelGraphics(null);
        levelNameText.text = "";
        levelDescriptionText.text = "";
        levelDifficultyText.text = "";
        levelBestScoreText.text = "";
    }


    private void SetActiveLevelGraphics(LevelUIData activeLevel)
    {
        foreach (var levelData in _levelUIData)
        {
            if (levelData.levelGfx)
            {
                levelData.levelGfx.SetActive(false);
            }
        }
        
        if (activeLevel?.levelGfx)
        {
            activeLevel.levelGfx.SetActive(true);
        }
    }
    
    
    private void ToggleLevelCanvas(bool state, bool animate = true)
    {
        if (!levelsSelectionCanvas) return;
        if (_levelInfoCanvasSequence.isAlive) _levelInfoCanvasSequence.Stop();

        if (animate)
        {
            _levelInfoCanvasSequence = Sequence.Create()
                .Group(Tween.Alpha(levelsSelectionCanvas, state ? 1 : 0, 0.3f))
                .OnComplete(() =>
                {
                    levelsSelectionCanvas.interactable = state;
                    levelsSelectionCanvas.blocksRaycasts = state;
                });
        }
        else
        {
            levelsSelectionCanvas.alpha = state ? 1 : 0;
            levelsSelectionCanvas.interactable = state;
            levelsSelectionCanvas.blocksRaycasts = state;
        }

    }
    
    private IEnumerator StartWritersWithDelay()
    {
        levelDifficultyWriter.ResetWriter();
        levelDescriptionWriter.ResetWriter();
        levelNameWriter.RestartWriter();
        levelBestScoreWriter.RestartWriter();
    
        yield return new WaitForSeconds(0.2f);
        levelDifficultyWriter.RestartWriter();
    
        yield return new WaitForSeconds(0.2f);
        levelDescriptionWriter.RestartWriter();
    }

    #endregion Level Info ---------------------------------------------------------------------------------
    
    
    #region Button ------------------------------------------------------------------------------

    private void SelectFirstAvailableButton()
    {
        foreach (var levelUIData in _levelUIData)
        {
            if (levelUIData.levelButton.interactable)
            {
                levelUIData.levelButton.Select();
                currentSelectable = levelUIData.levelButton;
                break;
            }
        }
    }
    
    private void SetupSelectable(Button button, LevelUIData levelUIData)
    {
        button.onClick.AddListener(() => SelectLevel(levelUIData));
        button.gameObject.name = $"Button{levelUIData.soLevel.LevelName}";
        button.GetComponentInChildren<TextMeshProUGUI>().text = levelUIData.soLevel.LevelName;
    
        var eventTrigger = button.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();
        AddEventTriggerEntry(eventTrigger, EventTriggerType.Select, (eventData) => OnSelectableSelected(eventData, levelUIData));
        AddEventTriggerEntry(eventTrigger, EventTriggerType.Deselect, (eventData) => OnSelectableDeselected(eventData, levelUIData));
    }
    
    private void AddEventTriggerEntry(EventTrigger eventTrigger, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        var existingEntry = eventTrigger.triggers.FirstOrDefault(entry => entry.eventID == type);

        if (existingEntry != null)
        {
            existingEntry.callback.AddListener(callback);
        }
        else
        {
            var newEntry = new EventTrigger.Entry
            {
                eventID = type,
                callback = new EventTrigger.TriggerEvent()
            };
            newEntry.callback.AddListener(callback);
            eventTrigger.triggers.Add(newEntry);
        }
    }
    
    
    private void OnSelectableSelected(BaseEventData eventData, LevelUIData levelUIData)
    {
        if (currentVisualState != ElementState.Interacting  || !eventData.selectedObject.activeSelf) return;

        currentSelectable = eventData.selectedObject.GetComponent<Selectable>();
        
        ShowLevelInfo(levelUIData);
    }

    private void OnSelectableDeselected(BaseEventData eventData, LevelUIData levelUIData)
    {
        if (currentVisualState != ElementState.Interacting || !eventData.selectedObject.activeSelf || !currentSelectable) return;
        
        currentSelectable = null;
        
        HideLevelInfo(levelUIData);
    }
    

    #endregion Button Setup ------------------------------------------------------------------------------

    
}