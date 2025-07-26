using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemUIData : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform gfx;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image costIcon;
    [SerializeField] private Transform costTransform;
    [SerializeField] private CanvasGroup canvasGroup;

    private bool _hasItem;
    private bool _interactingWithShelf;
    private bool _isSelected;
    private Sequence _iconSequence;
    private Sequence _canvasSequence;
    private Camera _camera;
    public IStoreItem StoreItem { get; private set; }

    public event Action<StoreItemUIData> OnItemBoughtEvent;
    public event Action<StoreItemUIData> OnMouseEnterEvent;
    public event Action<StoreItemUIData> OnMouseExitEvent;
    public event Action<StoreItemUIData> OnMouseDownEvent;


    private void Awake()
    {
        _camera = Camera.main;
    }

    private void OnMouseEnter()
    {
        if (!_interactingWithShelf) return;
        OnMouseEnterEvent?.Invoke(this);
    }
    
    private void OnMouseExit()
    {
        if (!_interactingWithShelf) return;
        OnMouseExitEvent?.Invoke(this);
    }
    
    private void OnMouseDown()
    {
        if (!_interactingWithShelf) return;
        OnMouseDownEvent?.Invoke(this);
    }

    // Rotate to look at camera
    // private void Update()
    // {
    //     if (!_isSelected || !_camera) return;
    //     
    //     var targetRotation = Quaternion.LookRotation(-_camera.transform.position - -canvasGroup.transform.position);
    //     targetRotation.x = canvasGroup.transform.rotation.x;
    //     targetRotation.z = canvasGroup.transform.rotation.z;
    //     canvasGroup.transform.rotation = Quaternion.Slerp(canvasGroup.transform.rotation, targetRotation, Time.deltaTime * 10f);
    // }

    public void SetupItem(IStoreItem storeItem)
    {
        if (storeItem == null) return;

        if (storeItem.ItemGfx)
        {
            var newGfx = Instantiate(storeItem.ItemGfx,gfx);
        }
        StoreItem = storeItem;
        nameText.text = storeItem.ItemName;
        descriptionText.text = storeItem.ItemDescription;
        _hasItem = SaveManager.HasStoreItem(storeItem.ItemID);

        if (_hasItem)
        {
            costText.text = "Purchased";
            var iconColor = costIcon.color;
            iconColor.a = 0f;
            costIcon.color = iconColor;
        }
        else
        {
            costText.text = storeItem.ItemCost.ToString();
        }

        SetSelected(false, true);
    }
    
    
    public void TryPurchase()
    {
        if (StoreItem == null) return;

        if (_hasItem)
        {
            if (_iconSequence.isAlive) _iconSequence.Stop();

            costTransform.transform.localScale = Vector3.one;
            
            _iconSequence = Sequence.Create()
                .Group(Tween.PunchScale(costTransform.transform, Vector3.one * 1f, 0.2f, 1))
                .OnComplete(() => costTransform.transform.localScale = Vector3.one);
            return;
        }

        var currentCurrency = SaveManager.GetCurrency();

        if (currentCurrency >= StoreItem.ItemCost)
        {
            SaveManager.UpdatePlayerCurrency(currentCurrency - StoreItem.ItemCost);
            SaveManager.UpdatePlayerBoughtItems(StoreItem.ItemID);
            costText.text = "Purchased";
            var iconColor = costIcon.color;
            iconColor.a = 0f;
            costIcon.color = iconColor;
            _hasItem = true;
            OnItemBoughtEvent?.Invoke(this);
        }
        else
        {
            if (_iconSequence.isAlive) _iconSequence.Stop();
            costTransform.transform.localScale = Vector3.one;
            
            _iconSequence = Sequence.Create()
                .Group(Tween.PunchScale(costTransform.transform, Vector3.one * 1f, 0.2f, 1))
                .Group(Tween.Color(costIcon, Color.red, 0.1f))
                .Chain(Tween.Color(costIcon, Color.white, 0.1f))
                .OnComplete(() => costTransform.transform.localScale = Vector3.one);
        }
    }

    public void SetSelected(bool state, bool instant = false)
    {

        _isSelected = state;
        if (instant)
        {
            canvasGroup.alpha = state ? 1 : 0;
            canvasGroup.transform.localScale = state ? Vector3.one : Vector3.zero;
            return;
        }
        
        if (_canvasSequence.isAlive) _canvasSequence.Stop();
        _canvasSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, state ? 1 : 0, state ? 0.2f : 0.1f))
            .Group(Tween.Scale(canvasGroup.transform, state ? Vector3.one : Vector3.zero, 0.2f, ease: Ease.OutBack))
            
            ;

    }

    public void ToggleInteractingWithShelf(bool state)
    {
        _interactingWithShelf = state;
        
        if (!state)
        {
            SetSelected(false);
        }
    }
}