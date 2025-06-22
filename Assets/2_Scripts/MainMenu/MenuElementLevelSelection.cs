using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuElementLevelSelection : MenuElement
{
    [Header("Level Selection")]
    [SerializeField] private CanvasGroup levelsInfoCanvasGroup;
    [SerializeField] private Transform levelGfxParent;
    [SerializeField] private Transform levelButtonParent;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI levelDescriptionText;
    [SerializeField] private Button levelButtonPrefab;
    [SerializeField] private SOLevel[] levels;
    
    
    private readonly Dictionary<SOLevel, GameObject> _levelGfxs = new Dictionary<SOLevel, GameObject>();
    private SOLevel _selectedLevel;
    private SOLevel _previousSelectedLevel;
    
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
                button.onClick.AddListener(() => level.LoadLevel());
                button.GetComponentInChildren<TextMeshProUGUI>().text = level.LevelName;
                button.gameObject.name = $"Button{level.LevelName}";
                EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
                if (eventTrigger)
                {
                    EventTrigger.Entry entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.PointerEnter;
                    entry.callback.AddListener((eventData) => SelectLevel(level));
                    eventTrigger.triggers.Add(entry);
                    
                    EventTrigger.Entry entry2 = new EventTrigger.Entry();
                    entry2.eventID = EventTriggerType.PointerExit;
                    entry2.callback.AddListener((eventData) => DeselectLevel(level));
                    eventTrigger.triggers.Add(entry2);
                }
            }

        }
        
        
        
        ToggleLevelCanvas(false);
    }
    
    protected override void OnInteract()
    {
        ToggleLevelCanvas(true);
        SelectLevel(levels[0]);
    }
    
    protected override void OnFinishedInteraction()
    {
        ToggleLevelCanvas(false);
        DeselectLevel(_selectedLevel);
        _selectedLevel = null;
    }
    
    protected override void OnStopInteraction()
    {
        ToggleLevelCanvas(false);
        DeselectLevel(_selectedLevel);
        _selectedLevel = null;
    }
    
    
    private void SelectLevel(SOLevel level)
    {
        if (!level) return;
        
        
        // Disable selected level
        if (_selectedLevel)
        {
            _previousSelectedLevel = _selectedLevel;
            _levelGfxs[_previousSelectedLevel].SetActive(false);
        }

        // Set the level
        _selectedLevel = level;
        
        // Set the level name and description
        levelNameText.text = level.LevelName;
        levelDescriptionText.text = level.LevelDescription;

        // Show the selected level gfx
        _levelGfxs[level].SetActive(true);
    }
    
    private void DeselectLevel(SOLevel level)
    {
        if (!level) return;

        _levelGfxs[_selectedLevel].SetActive(false);
        levelNameText.text = "";
        levelDescriptionText.text = "";
    }
    
    

    private void ToggleLevelCanvas(bool state)
    {
        if (!levelsInfoCanvasGroup) return;
        
        levelsInfoCanvasGroup.alpha = state ? 1 : 0;
        levelsInfoCanvasGroup.interactable = state;
        levelsInfoCanvasGroup.blocksRaycasts = state;
    }
}
