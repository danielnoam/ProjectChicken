
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public abstract class NavigatableUIScreen : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField, ReadOnly] protected bool isVisible;
    [SerializeField, ReadOnly] protected Selectable currentSelectable;
    [SerializeField, ReadOnly] protected List<Selectable> selectables = new List<Selectable>();
    
    [Header("References")]
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField, Parent(Flag.Optional), HideInInspector] protected LevelManager levelManager;
    
    protected virtual void OnValidate()
    {
        this.ValidateRefs();
        
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        
        if (!canvasGroup)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    protected virtual void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnPause += OnPause;
            levelManager.OnStageChanged += OnStageChanged;
            levelManager.LevelManagerInput.OnNavigateActionEvent += OnNavigateAction;
        }
    }
    
    protected virtual void OnDisable()
    {
        if (levelManager)
        {
            levelManager.OnPause -= OnPause;
            levelManager.OnStageChanged -= OnStageChanged;
            levelManager.LevelManagerInput.OnNavigateActionEvent -= OnNavigateAction;
        }
    }

    protected virtual void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
    }

    protected virtual void OnNavigateAction(InputAction.CallbackContext callbackContext)
    {
        if (!isVisible || currentSelectable || levelManager.IsGamePaused) return;
        SelectFirstAvailableButton();
    }

    protected virtual void OnPause(bool paused)
    {
        if (!isVisible || !canvasGroup) return;
        
        canvasGroup.interactable = !paused;
    }
    

    protected void AddSelectable(Selectable selectable)
    {
        if (!selectable || selectables.Contains(selectable)) return;
        
        selectables.Add(selectable);
        SetupSelectable(selectable);
    }
    
    protected void SelectFirstAvailableButton()
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

    protected void AddAllChildSelectables()
    {
        var allSelectables = GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in allSelectables)
        {
            AddSelectable(selectable);
        }
	
        
    }
    
    private void SetupSelectable(Selectable selectable)
    {
        var eventTrigger =  selectable.GetOrAddComponent<EventTrigger>();
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
        if (!eventData.selectedObject.activeSelf) return;
        currentSelectable = eventData.selectedObject.GetComponent<Selectable>();
    }

    private void OnSelectableDeselected(BaseEventData eventData)
    {
        if (!eventData.selectedObject.activeSelf || !currentSelectable) return;
        currentSelectable = null;
    }
}