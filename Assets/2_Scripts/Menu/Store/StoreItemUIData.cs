
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemUIData : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject itemGfx;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemCostText;
    [SerializeField] private Image chickenIcon;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private IStoreItem _storeItem;
    private bool _hasItem;
    private bool _interactingWithShelf;
    public Action BoughtItem;
    
    
    
    public void SetupItem(IStoreItem storeItem)
    {
        if (storeItem == null) return;

        _storeItem = storeItem;
        itemNameText.text = storeItem.ItemName;
        itemDescriptionText.text = storeItem.ItemDescription;
        _hasItem = SaveManager.HasStoreItem(storeItem.ItemID);

        if (_hasItem)
        {
            itemCostText.text = "Purchased";
            var iconColor = chickenIcon.color;
            iconColor.a = 0f;
            chickenIcon.color = iconColor;
        }
        else
        {
            itemCostText.text = storeItem.ItemCost.ToString();
        }


        ToggleVisibility(false);
    }
    

    private void TryPurchaseItem()
    {
        if (_hasItem || _storeItem == null) return;

        var currentCurrency = SaveManager.GetCurrency();

        if (currentCurrency >= _storeItem.ItemCost)
        {
            SaveManager.UpdatePlayerCurrency(currentCurrency-_storeItem.ItemCost);
            SaveManager.UpdatePlayerBoughtItems(_storeItem.ItemID);
            itemCostText.text  = "Purchased";
            var iconColor = chickenIcon.color;
            iconColor.a = 0f;
            chickenIcon.color = iconColor;
            _hasItem = true;
            BoughtItem?.Invoke();
        }
        else
        {
            Debug.Log("Not enough chicken legs!");
        }
    }

    private void OnMouseEnter()
    {
        if (!_interactingWithShelf) return;
        
        ToggleVisibility(true);
    }
    
    private void OnMouseExit()
    {
        if (!_interactingWithShelf) return;
        ToggleVisibility(false);
    }
    
    private void OnMouseDown()
    {
        if (!_interactingWithShelf) return;
        
        TryPurchaseItem();
    }
    private void ToggleVisibility(bool toggle)
    {
        canvasGroup.alpha  = toggle ? 1 : 0;
    }

    public void ToggleInteractingWithShelf(bool toggle)
    {
        _interactingWithShelf = toggle;
        
        if (!toggle)
        {
            ToggleVisibility(false);
        }
    }
}