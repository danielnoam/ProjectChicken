using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class MenuElementOptionsScreen : MenuElement
{

    [SerializeField] private Selectable[] selectables = Array.Empty<Selectable>();
    private Selectable _currentSelectable;
    
    
    protected override void OnSelected()
    {

    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {

    }
    
    protected override void OnInteract()
    {

    }
    
    protected override void OnFinishedInteraction()
    {
        
    }

    protected override void OnStopInteraction()
    {
        
    }
    
    protected override void OnNavigate(InputAction.CallbackContext context)
    {
        base.OnNavigate(context);
    }

    protected override void OnSubmit(InputAction.CallbackContext context)
    {
        base.OnSubmit(context);
    }

    protected override void OnCancel(InputAction.CallbackContext context)
    {
        base.OnCancel(context);
    }
    
    
    
    private void SetupSelectable(Selectable selectable, LevelUIData levelUIData)
    {
        var eventTrigger = selectable.GetComponent<EventTrigger>() ?? selectable.gameObject.AddComponent<EventTrigger>();
        AddEventTriggerEntry(eventTrigger, EventTriggerType.Select, OnSelectableSelected);
        AddEventTriggerEntry(eventTrigger, EventTriggerType.Deselect, OnSelectableDeselected);
        AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerEnter, OnSelectablePointerEnter);
        AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerExit, OnSelectablePointerExit);
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
        if (CurrentVisualState != ElementState.Interacting  || !eventData.selectedObject.activeSelf) return;

        _currentSelectable = eventData.selectedObject.GetComponent<Selectable>();
        

    }

    private void OnSelectableDeselected(BaseEventData eventData)
    {
        if (CurrentVisualState != ElementState.Interacting || !eventData.selectedObject.activeSelf || !_currentSelectable) return;
        
        _currentSelectable = null;
        
    }

    private void OnSelectablePointerEnter(BaseEventData eventData)
    {
        if (CurrentVisualState != ElementState.Interacting) return;

        if (eventData is PointerEventData pointerEventData)
        {
            pointerEventData.selectedObject = pointerEventData.pointerEnter;
        }
    }

    private void OnSelectablePointerExit(BaseEventData eventData)
    {
        if (CurrentVisualState != ElementState.Interacting) return;

        if (eventData is PointerEventData pointerEventData)
        {
            pointerEventData.selectedObject = null;
        }
    }
}
