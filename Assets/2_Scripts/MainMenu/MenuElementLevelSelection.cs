using System;
using System.Collections.Generic;
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
    [SerializeField] private TextMeshProUGUI levelDescriptionText;
    [SerializeField] private Button levelButtonPrefab;
    
    
    private readonly Dictionary<SOLevel, GameObject> _levelGfxs = new Dictionary<SOLevel, GameObject>();
    private SOLevel _showLevelInfo;
    private SOLevel _previousLevelInfo;
    private SOLevel _selectedLevel;
    private Button _selectedLevelButton;
    
    public SOLevel SelectedLevel => _selectedLevel;
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
            // Instantiate all the level gfxs and assign them to there respective level
            if (levelGfxParent)
            {
                GameObject levelGfx = level.SetUpGfx(levelGfxParent);
                _levelGfxs.Add(level, levelGfx ? levelGfx : null);
            }

            
            // Create a button for each level
            if (levelButtonPrefab)
            {
                Button button = Instantiate(levelButtonPrefab, levelButtonParent);
                button.onClick.AddListener(() => SelectLevel(level, button));
                button.GetComponentInChildren<TextMeshProUGUI>().text = level.LevelName;
                button.gameObject.name = $"Button{level.LevelName}";
                EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
                if (eventTrigger)
                {
                    EventTrigger.Entry entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.PointerEnter;
                    entry.callback.AddListener((eventData) => ShowLevelInfo(level));
                    eventTrigger.triggers.Add(entry);
                    
                    EventTrigger.Entry entry2 = new EventTrigger.Entry();
                    entry2.eventID = EventTriggerType.PointerExit;
                    entry2.callback.AddListener((eventData) => HideLevelInfo(level));
                    eventTrigger.triggers.Add(entry2);
                }
            }

        }
        
        
        
        ToggleLevelCanvas(false);
    }
    
    protected override void OnInteract()
    {
        ToggleLevelCanvas(true);

        ShowLevelInfo(_selectedLevel ? _selectedLevel : levels[0]);
    }
    
    protected override void OnFinishedInteraction()
    {
        ToggleLevelCanvas(false);
        HideLevelInfo(_showLevelInfo);
        _showLevelInfo = null;
    }
    
    protected override void OnStopInteraction()
    {
        ToggleLevelCanvas(false);
        HideLevelInfo(_showLevelInfo);
        _showLevelInfo = null;
    }
    
    
    private void ShowLevelInfo(SOLevel level)
    {
        if (!level) return;
        
        
        // Disable selected level
        if (_showLevelInfo)
        {
            _previousLevelInfo = _showLevelInfo;
            _levelGfxs[_previousLevelInfo].SetActive(false);
        }

        // Set the level
        _showLevelInfo = level;
        
        // Set the level name and description
        levelNameText.text = level.LevelName;
        levelDescriptionText.text = level.LevelDescription;

        // Show the selected level gfx
        _levelGfxs[level].SetActive(true);
    }
    
    private void HideLevelInfo(SOLevel level)
    {
        if (!level) return;

        if (_selectedLevel)
        {
            if (_selectedLevel == level)
            {
                return;    
            }
            else
            {
                ShowLevelInfo(_selectedLevel);
                return;
            }
            
        }
        
        _levelGfxs[_showLevelInfo].SetActive(false);
        levelNameText.text = "";
        levelDescriptionText.text = "";
    }
    
    
    private void SelectLevel(SOLevel level, Button button)
    {
        if (!level) return;
        
        if (level == _selectedLevel)
        {
            DeselectLevel();
            return;
        }
        
        DeselectLevel();
        _selectedLevel = level;
        _selectedLevelButton = button;
        _selectedLevelButton.image.color = Color.blue;
        
        OnLevelSelected?.Invoke();
        
        
        switch (mainMenuController.LaunchMissionMode)
        {
            case LaunchMissionMode.None:
                LoadSelectedLevel();
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
        if (!_selectedLevel) return;
        
        _selectedLevel = null;
        _selectedLevelButton.image.color = Color.white;
        _selectedLevelButton = null;
        OnLevelDeselected?.Invoke();
    }
    

    private void ToggleLevelCanvas(bool state)
    {
        if (!levelsInfoCanvasGroup) return;
        
        levelsInfoCanvasGroup.alpha = state ? 1 : 0;
        levelsInfoCanvasGroup.interactable = state;
        levelsInfoCanvasGroup.blocksRaycasts = state;
    }
    
    private void LoadSelectedLevel()
    {
        if (!_selectedLevel) return;

        _selectedLevel.LoadLevel();
    }

}
