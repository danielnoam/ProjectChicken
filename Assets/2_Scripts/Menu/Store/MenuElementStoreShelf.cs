using System;
using System.Collections.Generic;
using AYellowpaper;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuElementStoreShelf : MenuElement
{
    [Header("Shelf Items")]
    [SerializeField] private InterfaceReference<IStoreItem, ScriptableObject>[] shelfItems = Array.Empty<InterfaceReference<IStoreItem, ScriptableObject>>();
    
    [Header("References")]
    [SerializeField] private StoreItem storeItemPrefab;
    [SerializeField] private Transform storeItemHolder;
    
    private readonly List<StoreItem> _shelfItemsList = new List<StoreItem>();
    private int _currentStoreItemIndex;
    
    public event Action<StoreItem> OnStoreItemBoughtEvent;
    
    
    
    protected override void OnSelected()
    {

    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {
        PrimeTweenConfig.warnTweenOnDisabledTarget = false;
        
        if (shelfItems.Length <= 0)
        {
            ToggleCanSelect(false);
        }
        else
        {
            if (!storeItemPrefab) return;
            
            foreach (var item in shelfItems)
            {
                var storeItem = Instantiate(storeItemPrefab, storeItemHolder ? storeItemHolder : transform);
                storeItem.name = item.Value.ItemName;
                storeItem.SetupItem(item.Value);
                
                storeItem.OnItemBoughtEvent += StoreItemBought;
                storeItem.OnMouseEnterEvent += OnStoreItemMouseEnter;
                storeItem.OnMouseExitEvent += OnStoreItemMouseExit;
                storeItem.OnMouseDownEvent += OnStoreItemMouseDown;
                
                _shelfItemsList.Add(storeItem);

                if (item.Value.NeededItemsToUnlockToUnlock.Count > 0)
                {
                    foreach (var neededItem in item.Value.NeededItemsToUnlockToUnlock)
                    {
                        if (SaveManager.HasStoreItem(neededItem.Value.ItemID)) continue;
                        storeItem.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
    
    private void OnDisable()
    {
        foreach (var item in _shelfItemsList)
        {
            if (item != null)
            {
                item.OnItemBoughtEvent -= StoreItemBought;
                item.OnMouseEnterEvent -= OnStoreItemMouseEnter;
                item.OnMouseExitEvent -= OnStoreItemMouseExit;
                item.OnMouseDownEvent -= OnStoreItemMouseDown;
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

        _currentStoreItemIndex = 0;
    }

    protected override void OnStopInteraction()
    {
        foreach (var item in _shelfItemsList)
        {
            item.ToggleInteractingWithShelf(false);
        }

        _currentStoreItemIndex = 0;
    }

    private void StoreItemBought(StoreItem boughtItem)
    {
        foreach (var item in _shelfItemsList)
        {
            if (item.IStoreItem.NeededItemsToUnlockToUnlock.Count <= 0) continue;
            foreach (var neededItem in item.IStoreItem.NeededItemsToUnlockToUnlock)
            {
                item.gameObject.SetActive(SaveManager.HasStoreItem(neededItem.Value.ItemID));
            }
        }
        
        OnStoreItemBoughtEvent?.Invoke(boughtItem);
    }
    
    private void OnStoreItemMouseEnter(StoreItem item)
    {
        if (CurrentVisualState != VisualState.Interacting || _shelfItemsList.Count <= 0) return;
        
        _shelfItemsList[_currentStoreItemIndex]?.SetSelected(false);
        _currentStoreItemIndex = _shelfItemsList.IndexOf(item);
        item.SetSelected(true);
    }

    private void OnStoreItemMouseExit(StoreItem item)
    {
        if (CurrentVisualState != VisualState.Interacting || _shelfItemsList.Count <= 0) return;
        
        if (item == _shelfItemsList[_currentStoreItemIndex])
        {
            item.SetSelected(false);
            _currentStoreItemIndex = 0;
        }
    }

    private void OnStoreItemMouseDown(StoreItem item)
    {
        if (CurrentVisualState != VisualState.Interacting || _shelfItemsList.Count <= 0) return;
        
        item?.TryPurchase();
    }

    protected override void OnNavigate(InputAction.CallbackContext context)
    {
        if (!context.performed || CurrentVisualState != VisualState.Interacting || _shelfItemsList.Count <= 0) return;
   

        var activeItems = new List<StoreItem>();
        foreach (var item in _shelfItemsList)
        {
            if (item.gameObject.activeInHierarchy)
            {
                activeItems.Add(item);
            }
        }
   
        if (activeItems.Count <= 0) return;
   

        int currentActiveIndex = activeItems.IndexOf(_shelfItemsList[_currentStoreItemIndex]);
        if (currentActiveIndex == -1) currentActiveIndex = 0;
   
        if (context.ReadValue<Vector2>().y > 0 || context.ReadValue<Vector2>().x > 0)
        {
            _shelfItemsList[_currentStoreItemIndex]?.SetSelected(false);
       
            currentActiveIndex++;
            if (currentActiveIndex >= activeItems.Count)
            {
                currentActiveIndex = 0;
            }
       
            _currentStoreItemIndex = _shelfItemsList.IndexOf(activeItems[currentActiveIndex]);
            _shelfItemsList[_currentStoreItemIndex].SetSelected(true);
        }
        else if (context.ReadValue<Vector2>().y < 0 || context.ReadValue<Vector2>().x < 0)
        {
            _shelfItemsList[_currentStoreItemIndex]?.SetSelected(false);

            currentActiveIndex--;
            if (currentActiveIndex < 0)
            {
                currentActiveIndex = activeItems.Count - 1;
            }

            _currentStoreItemIndex = _shelfItemsList.IndexOf(activeItems[currentActiveIndex]);
            _shelfItemsList[_currentStoreItemIndex].SetSelected(true);
        }
    }
    
    protected override void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed || CurrentVisualState != VisualState.Interacting || _shelfItemsList.Count <= 0) return;
        
        if (_shelfItemsList.Count <= 0 || _currentStoreItemIndex >= _shelfItemsList.Count) return;
        _shelfItemsList[_currentStoreItemIndex].TryPurchase();
    }
    
    protected override void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed || CurrentVisualState != VisualState.Interacting || _shelfItemsList.Count <= 0) return;
    }
    
}