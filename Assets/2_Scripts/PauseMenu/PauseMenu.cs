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
    

    [Separator]
    [SerializeField, ReadOnly] private bool isVisible;
    [SerializeField, ReadOnly]  private CanvasGroup currentScreen;
    [SerializeField, ReadOnly]  private Selectable currentSelectable;
    
    
    public bool IsAtPauseScreen => currentScreen == pauseScreen;
    

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
                SetActiveScreen(optionsScreen);
            });
        }
        
        if (pauseScreenButton)
        {
            pauseScreenButton.onClick.AddListener(() =>
            {
                SetActiveScreen(pauseScreen);
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
            menuInput.OnCancelAction += OnCancel;
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
            menuInput.OnCancelAction -= OnCancel;
        }
        
    }

    private void OnCancel(InputAction.CallbackContext callbackContext)
    {
        if (isVisible && currentScreen != pauseScreen)
        {
            SetActiveScreen(pauseScreen);
        }
        
    }

    private void OnNavigate(InputAction.CallbackContext callbackContext)
    {
        if (isVisible && !currentSelectable)
        {
            SelectFirstAvailableButton();
        }
        
    }

    private void OnPause(bool paused)
    {
        switch (paused)
        {
            case true when !isVisible:
                SetPauseMenuVisible(true);
                break;
            case false when isVisible:
                SetPauseMenuVisible(false);
                break;
        }
    }
    
    private void SetPauseMenuVisible(bool visible)
    {
        isVisible = visible;
        pauseMenuCanvasGroup.alpha = visible ? 1 : 0;
        pauseMenuCanvasGroup.interactable = visible;
        pauseMenuCanvasGroup.blocksRaycasts = visible;

        if (visible)
        {
            SetActiveScreen(pauseScreen);
        }
    }
    
    
    private void SetActiveScreen(CanvasGroup screen)
    {
        currentScreen = screen;
        SetPauseScreen(screen == pauseScreen);
        SetOptionsScreen(screen == optionsScreen);
        
        SelectFirstAvailableButton();
    }
    
    private void SetPauseScreen(bool active)
    {
        pauseScreen.alpha = active ? 1 : 0;
        pauseScreen.interactable = active;
        pauseScreen.blocksRaycasts = active;
    }
    private void SetOptionsScreen(bool active)
    {
        optionsScreen.alpha = active ? 1 : 0;
        optionsScreen.interactable = active;
        optionsScreen.blocksRaycasts = active;
    }
    
    
    #region Selectables

    private void SelectFirstAvailableButton()
    {
        if (!isVisible) return;
        
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
