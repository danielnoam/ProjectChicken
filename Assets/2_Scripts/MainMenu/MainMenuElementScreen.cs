using System;
using System.Linq;
using DNExtensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenuElementScreen : MainMenuElement
{
    [Header("Screen")]
    [SerializeField] private TabbedMenuScreen tabbedMenuScreen;
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
        
        if (tabbedMenuScreen)
        {
            tabbedMenuScreen.OnMenuScreenOpened += TabbedMenuOpened;
            tabbedMenuScreen.OnMenuScreenClosed += TabbedMenuClosed;
            tabbedMenuScreen.OnTabSelected += TabbedMenuTabSelected;
        }
    }
    
    private void OnDestroy()
    {
        if (tabbedMenuScreen)
        {
            tabbedMenuScreen.OnMenuScreenOpened -= TabbedMenuOpened;
            tabbedMenuScreen.OnMenuScreenClosed -= TabbedMenuClosed;
            tabbedMenuScreen.OnTabSelected -= TabbedMenuTabSelected;
        }
    }
    
    protected override void OnInteract()
    {
        if (tabbedMenuScreen)
        {
            tabbedMenuScreen.Show();
        }
    }
    
    protected override void OnFinishedInteraction()
    {
        if (tabbedMenuScreen)
        {
            tabbedMenuScreen.Hide();
        }
        currentSelectable = null;
    }

    protected override void OnStopInteraction()
    {
        if (tabbedMenuScreen)
        {
            tabbedMenuScreen.Hide();
        }
        currentSelectable = null;
    }
    
    protected override void OnNavigate(InputAction.CallbackContext context)
    {
        base.OnNavigate(context);
        
        if (tabbedMenuScreen && tabbedMenuScreen.IsVisible && !currentSelectable)
        {
            SelectFirstAvailableButton();
        }
    }
    
    private void TabbedMenuOpened()
    {
        SelectFirstAvailableButton();
    }
    
    private void TabbedMenuClosed()
    {
        currentSelectable = null;
    }
    
    private void TabbedMenuTabSelected(CanvasGroup tab)
    {
        // SelectFirstAvailableButton();
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