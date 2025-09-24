using System;
using System.Linq;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Selectables")]
    [SerializeField] private Selectable[] selectables = Array.Empty<Selectable>();
    [SerializeField,ReadOnly]  private Selectable currentSelectable;
    
    [Header("References")]
    [SerializeField] private MenuInput menuInput;
    [SerializeField] private CanvasGroup pauseMenuCanvasGroup;
    [SerializeField] private CanvasGroup pauseScreen;
    [SerializeField] private CanvasGroup optionsScreen;
    [SerializeField] private Button pauseScreenButton;
    [SerializeField] private Button optionsScreenButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    


    private bool _isVisible;

    private void Awake()
    {
        SetPauseMenuVisible(false);
        SetOptionsScreen(false);
        SetPauseScreen(false);


        if (resumeButton)
        {
            resumeButton.onClick.AddListener(() =>
            {
                levelManager?.SetPausedState(false);
            });
        }

        if (mainMenuButton)
        {
            mainMenuButton.onClick.AddListener(() =>
            {
                levelManager?.SetPausedState(false);
                levelManager?.ReturnToMainMenu(0);
            });
        }

        if (optionsScreenButton)
        {
            optionsScreenButton.onClick.AddListener(() =>
            {
                SetPauseScreen(false);
                SetOptionsScreen(true);
            });
        }
        
        if (pauseScreenButton)
        {
            pauseScreenButton.onClick.AddListener(() =>
            {
                SetPauseScreen(true);
                SetOptionsScreen(false);
            });
        }


        foreach (var selectable in selectables)
        {
            SetupSelectable(selectable);
        }
    }

    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnPause += OnPause;
        }

        if (menuInput)
        {
            menuInput.OnNavigateAction += OnNavigate;
        }
    }

    private void OnDisable()
    {
        
        if (levelManager)
        {
            levelManager.OnPause -= OnPause;
        }
        
        if (menuInput)
        {
            menuInput.OnNavigateAction -= OnNavigate;
        }
        
    }

    private void OnNavigate(InputAction.CallbackContext callbackContext)
    {
        if (_isVisible && !currentSelectable)
        {
            SelectFirstAvailableButton();
        }
        
    }

    private void OnPause(bool paused)
    {
        switch (paused)
        {
            case true when !_isVisible:
                SetPauseMenuVisible(true);
                break;
            case false when _isVisible:
                SetPauseMenuVisible(false);
                break;
        }
    }
    
    private void SetPauseMenuVisible(bool visible)
    {
        _isVisible = visible;
        pauseMenuCanvasGroup.alpha = visible ? 1 : 0;
        pauseMenuCanvasGroup.interactable = visible;
        pauseMenuCanvasGroup.blocksRaycasts = visible;

        if (visible)
        {
            SetPauseScreen(true);
            SetOptionsScreen(false);
        }
        
        SelectFirstAvailableButton();
    }
    
    
    private void SetPauseScreen(bool active)
    {
        pauseScreen.alpha = active ? 1 : 0;
        pauseScreen.interactable = active;
        pauseScreen.blocksRaycasts = active;
        
        SelectFirstAvailableButton();
    }
    private void SetOptionsScreen(bool active)
    {
        optionsScreen.alpha = active ? 1 : 0;
        optionsScreen.interactable = active;
        optionsScreen.blocksRaycasts = active;
        
        SelectFirstAvailableButton();
    }
    
    
    #region Selectables

    private void SelectFirstAvailableButton()
    {
        if (!_isVisible) return;
        
        foreach (var selectable in selectables)
        {
            if (selectable && selectable.interactable)
            {
                selectable.Select();
                currentSelectable = selectable;
                break;
            }
        }
    }
    
    private void SetupSelectable(Selectable selectable)
    {
        var eventTrigger = selectable.GetComponent<EventTrigger>() ?? selectable.gameObject.AddComponent<EventTrigger>();
        AddEventTriggerEntry(eventTrigger, EventTriggerType.Select, OnSelectableSelected);
        AddEventTriggerEntry(eventTrigger, EventTriggerType.Deselect, OnSelectableDeselected);
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
    
    
    private void OnSelectableSelected(BaseEventData eventData)
    {
        if ( !eventData.selectedObject.activeSelf) return;

        currentSelectable = eventData.selectedObject.GetComponent<Selectable>();
    }

    private void OnSelectableDeselected(BaseEventData eventData)
    {
        if ( !eventData.selectedObject.activeSelf || !currentSelectable) return;
        
        currentSelectable = null;
    }


    [VInspector.Button]
    private void FindAllSelectables()
    {
        selectables = Array.Empty<Selectable>();
        selectables = GetComponentsInChildren<Selectable>(true);
    }

    #endregion Selectables
}
