using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuElementLevelSelection : MenuElement
{
    [Header("Level Selection")]
    [SerializeField] private SOLevel[] levels;
    
    [Header("References")]
    [SerializeField] private MenuElementLaunchLever launchLever;
    [SerializeField] private CanvasGroup levelsInfoCanvasGroup;
    [SerializeField] private Transform levelGfxParent;
    [SerializeField] private Transform levelButtonParent;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI levelDifficultyText;
    [SerializeField] private TextMeshProUGUI levelDescriptionText;
    [SerializeField] private Button levelButtonPrefab;
    

    private readonly List<LevelUIData> _levelUIData = new List<LevelUIData>();
    private LevelUIData _currentlyShownLevel;
    private LevelUIData _previouslyShownLevel;
    private LevelUIData _selectedLevel;
    
    public SOLevel SelectedLevel => _selectedLevel?.soLevel;
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
                var uiData = new LevelUIData(level, levelGfx, levelButton);
                _levelUIData.Add(uiData);
                
                // Set up button click event
                levelButton.onClick.AddListener(() => SelectLevel(uiData));
                
                // Set up hover events
                EventTrigger eventTrigger = levelButton.GetComponent<EventTrigger>();
                if (eventTrigger)
                {
                    EventTrigger.Entry entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.PointerEnter;
                    entry.callback.AddListener((eventData) => ShowLevelInfo(uiData));
                    eventTrigger.triggers.Add(entry);
                    
                    EventTrigger.Entry entry2 = new EventTrigger.Entry();
                    entry2.eventID = EventTriggerType.PointerExit;
                    entry2.callback.AddListener((eventData) => HideLevelInfo(uiData));
                    eventTrigger.triggers.Add(entry2);
                }
            }
            else
            {
                // If no button prefab, still create UI elements for consistency
                var uiData = new LevelUIData(level, levelGfx, null);
                _levelUIData.Add(uiData);
            }
        }
        
        ToggleLevelCanvas(false);
    }
    
    protected override void OnInteract()
    {
        ToggleLevelCanvas(true);

        // var levelToShow = _selectedLevel ?? _levelUIData.FirstOrDefault();
        if (_selectedLevel != null)
        {
            ShowLevelInfo(_selectedLevel);
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
    
    private void ShowLevelInfo(LevelUIData levelUI)
    {
        if (!levelUI?.soLevel) return;
        
        // Hide previously shown level
        if (_currentlyShownLevel != null && _currentlyShownLevel != levelUI)
        {
            _previouslyShownLevel = _currentlyShownLevel;
            if (_previouslyShownLevel.levelGfx)
            {
                _previouslyShownLevel.levelGfx.SetActive(false);
            }
        }

        // Set the current level
        _currentlyShownLevel = levelUI;
        
        // Update UI text
        levelNameText.text = levelUI.soLevel.LevelName;
        levelDescriptionText.text = levelUI.soLevel.LevelDescription;
        switch (levelUI.soLevel.LevelDifficulty)
        {
            case LevelDifficulty.None:
                levelDifficultyText.text = "";
                break;
            case LevelDifficulty.Tutorial:
                levelDifficultyText.text = "Tutorial";
                break;
            case LevelDifficulty.Easy:
                levelDifficultyText.text = "*";
                break;
            case LevelDifficulty.Medium:
                levelDifficultyText.text = "**";
                break;
            case LevelDifficulty.Hard:
                levelDifficultyText.text = "***";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Show the level graphics
        if (levelUI.levelGfx)
        {
            levelUI.levelGfx.SetActive(true);
        }
    }
    
    private void HideLevelInfo(LevelUIData levelUI)
    {
        if (!levelUI?.soLevel) return;

        // Don't hide if this is the selected level
        if (_selectedLevel != null && _selectedLevel == levelUI)
        {
            return;
        }
        
        // If we have a selected level, show that instead
        if (_selectedLevel != null && _selectedLevel != levelUI)
        {
            ShowLevelInfo(_selectedLevel);
            return;
        }
        
        // Hide the graphics and clear text
        if (_currentlyShownLevel?.levelGfx)
        {
            _currentlyShownLevel.levelGfx.SetActive(false);
        }
        
        levelNameText.text = "";
        levelDescriptionText.text = "";
        levelDifficultyText.text = "";
    }
    
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
        switch (menuController.LaunchMissionMode)
        {
            case LaunchMissionMode.None:
                _selectedLevel.soLevel?.LoadLevel();
                break;
            case LaunchMissionMode.Manual:
                break;
            case LaunchMissionMode.Auto:
                FinishedInteraction();
                launchLever.Launch();
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

    private void ToggleLevelCanvas(bool state)
    {
        if (!levelsInfoCanvasGroup) return;
        
        levelsInfoCanvasGroup.alpha = state ? 1 : 0;
        levelsInfoCanvasGroup.interactable = state;
        levelsInfoCanvasGroup.blocksRaycasts = state;
    }
    
}