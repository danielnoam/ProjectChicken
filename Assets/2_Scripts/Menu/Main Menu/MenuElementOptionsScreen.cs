using System;
using System.Linq;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VInspector;


public class MenuElementOptionsScreen : MenuElement
{
    [Header("Options Screen")]
    [SerializeField] private CanvasGroup optionsCanvas;
    [SerializeField] private Button nextPage;
    [SerializeField] private Button previousPage;
    [SerializeField] private CanvasGroup[] optionPages = Array.Empty<CanvasGroup>();
    [SerializeField] private Selectable[] selectables = Array.Empty<Selectable>();
    
    private Selectable _currentSelectable;
    private Sequence _optionsCanvasSequence;
    private int _currentOptionPageIndex;
    
    
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
        
        
        if (optionPages.Length > 0)
        {
            optionPages[0].interactable = true;
            optionPages[0].blocksRaycasts = true;
            optionPages[0].alpha = 1;
            
            for (var i = 1; i < optionPages.Length; i++)
            {
                optionPages[i].interactable = false;
                optionPages[i].blocksRaycasts = false;
                optionPages[i].alpha = 0;
            }
        }
        
        
        if (nextPage)
        {
            nextPage.onClick.AddListener(() =>
            {
                if (_currentOptionPageIndex < optionPages.Length - 1)
                {
                    optionPages[_currentOptionPageIndex].interactable = false;
                    optionPages[_currentOptionPageIndex].blocksRaycasts = false;
                    optionPages[_currentOptionPageIndex].alpha = 0;

                    _currentOptionPageIndex++;
                    optionPages[_currentOptionPageIndex].interactable = true;
                    optionPages[_currentOptionPageIndex].blocksRaycasts = true;
                    optionPages[_currentOptionPageIndex].alpha = 1;
                }
            });
        }
        
        if (previousPage)
        {
            previousPage.onClick.AddListener(() =>
            {
                if (_currentOptionPageIndex > 0)
                {
                    optionPages[_currentOptionPageIndex].interactable = false;
                    optionPages[_currentOptionPageIndex].blocksRaycasts = false;
                    optionPages[_currentOptionPageIndex].alpha = 0;

                    _currentOptionPageIndex--;
                    optionPages[_currentOptionPageIndex].interactable = true;
                    optionPages[_currentOptionPageIndex].blocksRaycasts = true;
                    optionPages[_currentOptionPageIndex].alpha = 1;
                }
            });
        }
        
        
        
        ToggleLevelCanvas(false, false);
    }
    
    protected override void OnInteract()
    {
        ToggleLevelCanvas(true);
        SelectFirstAvailableButton();
    }
    
    protected override void OnFinishedInteraction()
    {
        ToggleLevelCanvas(false);
        _currentSelectable = null;
    }

    protected override void OnStopInteraction()
    {
        ToggleLevelCanvas(false);
        _currentSelectable = null;
    }
    
    protected override void OnNavigate(InputAction.CallbackContext context)
    {
        base.OnNavigate(context);
        
        if (_currentSelectable) return;

        SelectFirstAvailableButton();
    }
    
    
    
    private void ToggleLevelCanvas(bool state, bool animate = true)
    {
        if (!optionsCanvas) return;
        if (_optionsCanvasSequence.isAlive) _optionsCanvasSequence.Stop();

        if (animate)
        {
            _optionsCanvasSequence = Sequence.Create()
                .Group(Tween.Alpha(optionsCanvas, state ? 1 : 0, 0.3f))
                .OnComplete(() =>
                {
                    optionsCanvas.interactable = state;
                    optionsCanvas.blocksRaycasts = state;
                });
        }
        else
        {
            optionsCanvas.alpha = state ? 1 : 0;
            optionsCanvas.interactable = state;
            optionsCanvas.blocksRaycasts = state;
        }

    }
    
    
    private void SelectFirstAvailableButton()
    {
        foreach (var selectable in selectables)
        {
            if (selectable.interactable)
            {
                selectable.Select();
                _currentSelectable = selectable;
                break;
            }
        }
    }

    
    
    
    private void SetupSelectable(Selectable selectable)
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
