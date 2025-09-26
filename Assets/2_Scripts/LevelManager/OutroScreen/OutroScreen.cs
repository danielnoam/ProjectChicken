
using System;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OutroScreen : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.5f;

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button continueButton;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private CameraManager cameraManager;
    
    
    [Separator]
    [SerializeField, ReadOnly] private bool isVisible;
    [SerializeField, ReadOnly] private Selectable currentSelectable;
    [SerializeField, ReadOnly] private List<Selectable> selectables = new List<Selectable>();
    
    private Sequence _outroScreenSequence;
    public event Action OnScreenOpened;
    public event Action OnScreenClosed;
    
    
    
    private void OnValidate()
    {
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        
        if (!cameraManager)
        {
            cameraManager = FindFirstObjectByType<CameraManager>();
        }
        
        
        this.ValidateRefs();

        if (levelManager)
        {
            transform.position = levelManager.PlayerPosition;
        }
        
    }


    private void Awake()
    {
        Hide(false);
        SetupButtons();
    }


    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnPause += OnPause;
            levelManager.LevelManagerInput.OnNavigateActionEvent += OnNavigateAction;
        }
    }
    
    private void OnDisable()
    {
        if (levelManager)
        {
            levelManager.OnPause -= OnPause;
            levelManager.LevelManagerInput.OnNavigateActionEvent -= OnNavigateAction;
        }
    }
    
    private void OnNavigateAction(InputAction.CallbackContext callbackContext)
    {
        if (!isVisible || currentSelectable || levelManager.IsGamePaused) return;
        
        
        SelectFirstAvailableButton();

    }

    private void OnPause(bool paused)
    {
        if (isVisible)
        {
            canvasGroup.interactable = !paused;
        }
    }

    private void SetupButtons()
    {

        if (returnButton)
        {
            SetupSelectable(returnButton);
            selectables.Add(returnButton);
            
            returnButton.onClick.AddListener(() =>
            {
                levelManager.ReturnToMainMenu();
                Hide(true);
            });
            

        }
        
        if (continueButton)
        {
            SetupSelectable(continueButton);
            selectables.Add(continueButton);
            
            continueButton.onClick.AddListener(() =>
            {
                levelManager.LoadNextLevel();
                Hide(true);
            });
        }
    }

    public void Show(bool nextLevelIsAvailable)
    {
        isVisible = true;
        continueButton.interactable = nextLevelIsAvailable;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        
        if (_outroScreenSequence.isAlive) _outroScreenSequence.Stop();
        
        _outroScreenSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1, animationDuration))
            .OnComplete((() => OnScreenOpened?.Invoke()));
    }

    public void Hide(bool animate)
    {
        isVisible = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (_outroScreenSequence.isAlive) _outroScreenSequence.Stop();
        canvasGroup.alpha = 0f;

        if (animate)
        {
            _outroScreenSequence = Sequence.Create()
                .Group(Tween.Alpha(canvasGroup, 0, animationDuration));
        }

        OnScreenClosed?.Invoke();
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
    

    #endregion Selectables
}
