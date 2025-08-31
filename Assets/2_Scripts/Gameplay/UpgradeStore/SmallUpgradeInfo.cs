using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SmallUpgradeInfo : MonoBehaviour
{
    [SerializeField] private float fullHeight = 100f;
    [SerializeField] private float colliderFullSize = 100;

    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private BoxCollider boxCollider;
    [SerializeField] private Image upgradeIcon;
    [SerializeField] private TextMeshProUGUI upgradeNameText;
    [SerializeField] private TextMeshProUGUI upgradeDescriptionText;
    
    private float _baseHeight;
    private float _colliderBaseHeight;
    private Sequence _detailSequence;
    
    public SOUpgradeBase Upgrade { get; private set; }


    private void Awake()
    {
        _baseHeight = rectTransform.sizeDelta.y;
        _colliderBaseHeight = boxCollider.size.y;
    }

    public void SetInfo(SOUpgradeBase upgrade)
    {
        if (!upgrade) return;
        
        HideDetails(false);
        Upgrade = upgrade;
        if (upgrade.ItemIcon) upgradeIcon.sprite = upgrade.ItemIcon.sprite;
        upgradeNameText.text = upgrade.ItemName;
        upgradeDescriptionText.text = upgrade.ItemDescription;
    }
    
    private void ShowDetails()
    {
        if (_detailSequence.isAlive) _detailSequence.Stop();
        _detailSequence = Sequence.Create()
            .Group(Tween.UISizeDelta(rectTransform, new Vector2(rectTransform.sizeDelta.x, fullHeight), 0.25f));
        
        boxCollider.size = new Vector2(boxCollider.size.x, colliderFullSize);
    }
    
    private void HideDetails(bool animated = true)
    {
        if (_detailSequence.isAlive) _detailSequence.Stop();
        if (animated)
        {
            _detailSequence = Sequence.Create()
                .Group(Tween.UISizeDelta(rectTransform, new Vector2(rectTransform.sizeDelta.x, _baseHeight), 0.25f));
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _baseHeight);
        }

        boxCollider.size = new Vector2(boxCollider.size.x, _colliderBaseHeight);
    }

    
    public void OnMouseEnter()
    {
        ShowDetails();
    }
    
    public void OnMouseExit()
    {
        HideDetails();
    }
}
