using System;
using DNExtensions;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class UpgradeInfoDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    [SerializeField] private bool alwaysShowDetails;
    [SerializeField] private float fullHeight = 100f;
    [SerializeField] private SOAudioEvent showDetailsSfx;
    [SerializeField] private SOAudioEvent hideDetailsSfx;

    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image upgradeIcon;
    [SerializeField] private TextMeshProUGUI upgradeNameText;
    [SerializeField] private TextMeshProUGUI upgradeDescriptionText;
    [SerializeField] private AudioSource audioSource;
    
    private float _baseHeight;
    private Sequence _detailSequence;
    
    public SOUpgradeBase Upgrade { get; private set; }


    private void Awake()
    {
        _baseHeight = rectTransform.sizeDelta.y;
    }

    public void SetInfo(SOUpgradeBase upgrade)
    {
        if (!upgrade) return;
        
        if (alwaysShowDetails)
        {
           ShowDetails(false);
        }
        else
        {
            HideDetails(false);
        }
        
        Upgrade = upgrade;
        if (upgrade.ItemIcon) upgradeIcon.sprite = upgrade.ItemIcon.sprite;
        upgradeNameText.text = upgrade.ItemName;
        upgradeDescriptionText.text = upgrade.ItemDescription;
    }
    
    private void ShowDetails(bool animated = true)
    {

        if (_detailSequence.isAlive) _detailSequence.Stop();
        if (animated)
        {
            showDetailsSfx?.Play(audioSource);
            _detailSequence = Sequence.Create()
                .Group(Tween.UISizeDelta(rectTransform, new Vector2(rectTransform.sizeDelta.x, fullHeight), 0.2f));
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, fullHeight);
        }
    }
    
    private void HideDetails(bool animated = true)
    {
        if (_detailSequence.isAlive) _detailSequence.Stop();
        if (animated)
        {
            hideDetailsSfx?.Play(audioSource);
            _detailSequence = Sequence.Create()
                .Group(Tween.UISizeDelta(rectTransform, new Vector2(rectTransform.sizeDelta.x, _baseHeight), 0.2f));
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _baseHeight);
        }
        
    }

    
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (alwaysShowDetails) return;
        ShowDetails();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (alwaysShowDetails) return;
        
        HideDetails();
    }
}
