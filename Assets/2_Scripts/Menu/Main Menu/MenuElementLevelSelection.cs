using System;
using System.Collections;
using System.Collections.Generic;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuElementLevelSelection : MenuElement
{
    
    [Header("Level Selection")]
    [SerializeField] private LaunchMissionMode launchMissionMode;
    [SerializeField] private SOLevel[] levels;
    
    [Header("References")]
    [SerializeField] private MenuElementLaunchLever launchLever;
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

    private readonly List<LevelUIData> _levelUIData = new List<LevelUIData>();
    private Coroutine _writerDelayRoutine;
    private LevelUIData _currentlyShownLevel;
    private LevelUIData _selectedLevel;

    
    public SOLevel SelectedLevel => _selectedLevel?.soLevel;
    public LaunchMissionMode LaunchMissionMode => launchMissionMode;
    public event Action OnLevelSelected;
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

            // Instantiate level graphics
            if (levelGfxParent)
            {
                levelGfx = level.SetUpGfx(levelGfxParent);
            }

            // Create button for each level
            if (levelButtonPrefab)
            {
                var levelButton = Instantiate(levelButtonPrefab, levelButtonParent);
                levelButton.GetComponentInChildren<TextMeshProUGUI>().text = level.LevelName;
                levelButton.gameObject.name = $"Button{level.LevelName}";
                
                // Create the UI element container
                var uiData = new LevelUIData(level, levelGfx, levelButton, SaveManager.GetLevelProgress(level.GetScenePath()));
                _levelUIData.Add(uiData);
                
                // Set up button click event
                levelButton.onClick.AddListener(() => SelectLevel(uiData));
                
                // Set up hover events
                EventTrigger eventTrigger = levelButton.GetComponent<EventTrigger>();
                if (eventTrigger)
                {
                    EventTrigger.Entry entry = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerEnter
                    };
                    entry.callback.AddListener((eventData) => ShowLevelInfo(uiData));
                    eventTrigger.triggers.Add(entry);
                    
                    EventTrigger.Entry entry2 = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.PointerExit
                    };
                    entry2.callback.AddListener((eventData) => HideLevelInfo(uiData));
                    eventTrigger.triggers.Add(entry2);
                }
                
                
                // check if all needed levels are completed
                if (level.IsLocked)
                {
                    if (level.LevelsToComplete.Count == 0 || level.LevelsToComplete == null) continue;
                    foreach (var neededLevel in level.LevelsToComplete)
                    {
                        var neededLevelProgress = SaveManager.GetLevelProgress(neededLevel.GetScenePath());
                        if (neededLevelProgress.isCompleted) continue;
                        levelButton.interactable = false;
                        break;
                    }
                }
            }
        }
        
        levelNameText.text = "";
        levelDescriptionText.text = "";
        levelDifficultyText.text = "";
        levelBestScoreText.text = "";
        ToggleLevelCanvas(false);
    }
    
    protected override void OnInteract()
    {
        ToggleLevelCanvas(true);
        
        if (_selectedLevel != null)
        {
            ShowLevelInfo(_selectedLevel);
        }
        else
        {
            if (_writerDelayRoutine != null)
            {
                StopCoroutine(_writerDelayRoutine);
            }
            _writerDelayRoutine = StartCoroutine(StartWritersWithDelay());
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
    }
    
    protected override void OnStopInteraction()
    {
        ToggleLevelCanvas(false);
        if (_currentlyShownLevel != null)
        {
            HideLevelInfo(_currentlyShownLevel);
            _currentlyShownLevel = null;
        }
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

            levelBestScoreText.text = levelUI.isCompleted ? $"Best Score: \n{levelUI.bestScore:D7}" : $"";
        }
        else
        {
            levelBestScoreText.text = $"";
            levelDescriptionText.text = $"Complete these levels to unlock:";
            
            foreach (var level in levelUI.soLevel.LevelsToComplete)
            {
                levelDescriptionText.text += $"\n {level.name}" ;
            }

        }

        
        
        switch (levelUI.soLevel.LevelDifficulty)
        {
            case LevelDifficulty.None:
                levelDifficultyText.text = "";
                break;
            case LevelDifficulty.Tutorial:
                levelDifficultyText.text = "Tutorial";
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
        // First, hide all level graphics
        foreach (var levelData in _levelUIData)
        {
            if (levelData.levelGfx)
            {
                levelData.levelGfx.SetActive(false);
            }
        }
        
        // Then show only the active one
        if (activeLevel?.levelGfx)
        {
            activeLevel.levelGfx.SetActive(true);
        }
    }
    
    
    private void ToggleLevelCanvas(bool state)
    {
        if (!levelsSelectionCanvas) return;
        
        levelsSelectionCanvas.alpha = state ? 1 : 0;
        levelsSelectionCanvas.interactable = state;
        levelsSelectionCanvas.blocksRaycasts = state;
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


    #region Level Selection ---------------------------------------------------------------------------------

    private void SelectLevel(LevelUIData levelUI)
    {
        if (!levelUI?.soLevel) return;
        
        // If clicking the same level, deselect it
        if (levelUI == _selectedLevel)
        {
            DeselectLevel();
            return;
        }
        
        // Deselect previous level and select new one
        DeselectLevel();
        _selectedLevel = levelUI;
        
        // Update button visual state
        if (_selectedLevel.levelButton)
        {
            _selectedLevel.levelButton.image.color = Color.blue;
        }
        
        OnLevelSelected?.Invoke();
        
        // Handle launch mode
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
                launchLever?.Launch();
                break;
        }
    }
    

    private void DeselectLevel()
    {
        if (_selectedLevel == null) return;
        
        // Reset button visual state
        if (_selectedLevel.levelButton)
        {
            _selectedLevel.levelButton.image.color = Color.white;
        }
        
        _selectedLevel = null;
        OnLevelDeselected?.Invoke();
    }
    
    
    #endregion Level Selection ---------------------------------------------------------------------------------


}