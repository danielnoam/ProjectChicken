using System;
using System.Linq;
using DNExtensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonInfo : MonoBehaviour
{
    
    
    [Header("References")]
    [SerializeField] private Selectable selectable;
    [SerializeField] private InfoDisplay infoDisplay;


    private void Awake()
    {
        SetupSelectable();
    }

    private void SetupSelectable()
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
        infoDisplay.Show();
    }

    private void OnSelectableDeselected(BaseEventData eventData)
    {
        if (!eventData.selectedObject.activeSelf) return;
        infoDisplay.Hide();
    }

}
