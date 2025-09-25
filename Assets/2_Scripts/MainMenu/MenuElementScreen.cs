using System;
using System.Linq;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuElementScreen : MenuElement
{
    [Header("Screen")]
    [SerializeField] private MenuScreen menuScreen;
    [SerializeField] private Selectable[] selectables = Array.Empty<Selectable>();
    
    
    [Separator]
    [SerializeField,ReadOnly]  private Selectable currentSelectable;

    
    protected override void OnSelected()
    {

    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {
        
        foreach (var selectable in selectables)
        {
            SetupSelectable(selectable);
        }
        
        if (menuScreen)
        {
            menuScreen.OnMenuScreenOpened += OnMenuOpened;
            menuScreen.OnMenuScreenClosed += OnMenuClosed;
            menuScreen.OnTabSelected += OnMenuTabSelected;
        }
    }
    
    private void OnDestroy()
    {
        if (menuScreen)
        {
            menuScreen.OnMenuScreenOpened -= OnMenuOpened;
            menuScreen.OnMenuScreenClosed -= OnMenuClosed;
            menuScreen.OnTabSelected -= OnMenuTabSelected;
        }
    }
    
    protected override void OnInteract()
    {
        if (menuScreen)
        {
            menuScreen.Show();
        }
    }
    
    protected override void OnFinishedInteraction()
    {
        if (menuScreen)
        {
            menuScreen.Hide();
        }
        currentSelectable = null;
    }

    protected override void OnStopInteraction()
    {
        if (menuScreen)
        {
            menuScreen.Hide();
        }
        currentSelectable = null;
    }
    
    protected override void OnNavigate(InputAction.CallbackContext context)
    {
        base.OnNavigate(context);
        
        if (menuScreen && menuScreen.IsVisible && !currentSelectable)
        {
            SelectFirstAvailableButton();
        }
    }
    
    private void OnMenuOpened()
    {
        SelectFirstAvailableButton();
    }
    
    private void OnMenuClosed()
    {
        currentSelectable = null;
    }
    
    private void OnMenuTabSelected(CanvasGroup tab)
    {
        SelectFirstAvailableButton();
    }
    
    
    


    #region Selectables

    private void SelectFirstAvailableButton()
    {
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
        if (currentVisualState != ElementState.Interacting  || !eventData.selectedObject.activeSelf) return;

        currentSelectable = eventData.selectedObject.GetComponent<Selectable>();
    }

    private void OnSelectableDeselected(BaseEventData eventData)
    {
        if (currentVisualState != ElementState.Interacting || !eventData.selectedObject.activeSelf || !currentSelectable) return;
        
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