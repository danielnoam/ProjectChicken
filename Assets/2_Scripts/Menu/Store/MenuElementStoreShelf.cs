using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;

public class MenuElementStoreShelf : MenuElement
{
    
    [Header("Shelf Items")]
    [SerializeField] private InterfaceReference<IStoreItem, ScriptableObject>[] shelfItems = Array.Empty<InterfaceReference<IStoreItem, ScriptableObject>>();
    
    [Header("References")]
    [SerializeField] private StoreItemUIData storeItemUIDataPrefab;
    [SerializeField] private Transform storeItemHolder;
    
    
    private readonly List<StoreItemUIData> _shelfItemsList = new List<StoreItemUIData>();
    public Action OnStoreItemBought;
    
    
    protected override void OnSelected()
    {

    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {
        
        if (shelfItems.Length <= 0)
        {
            ToggleCanSelect(false);
        }
        else
        {
            if (!storeItemUIDataPrefab) return;
            
            foreach (var item in shelfItems)
            {
                var storeItem = Instantiate(storeItemUIDataPrefab, storeItemHolder ? storeItemHolder : transform);
                storeItem.SetupItem(item.Value);
                storeItem.BoughtItem +=  () => OnStoreItemBought?.Invoke();
                _shelfItemsList.Add(storeItem);
            }
            
        }
    }
    
    protected override void OnInteract()
    {
        foreach (var item in _shelfItemsList)
        {
            item.ToggleInteractingWithShelf(true);
        }
    }
    
    protected override void OnFinishedInteraction()
    {
        foreach (var item in _shelfItemsList)
        {
            item.ToggleInteractingWithShelf(false);
        }
    }

    protected override void OnStopInteraction()
    {
        foreach (var item in _shelfItemsList)
        {
            item.ToggleInteractingWithShelf(false);
        }
    }
}
