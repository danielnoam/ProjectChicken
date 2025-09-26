using System;
using System.Linq;
using DNExtensions;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeEgg : MonoBehaviour
{
    [Header("Buy")]
    [SerializeField] private Sprite buyIcon;
    [SerializeField] private Sprite boughtIcon;
    [SerializeField] private SOAudioEvent boughtUpgradeSfx;
    
    [Header("Show/Hide")]
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private Ease animationEase = Ease.InOutBack;
    [SerializeField] private float yOffset = -150;
    [SerializeField] private SOAudioEvent showUpgradeSfx;
    [SerializeField] private SOAudioEvent hideUpgradeSfx;
    
    [Header("Idle")]
    [SerializeField] private float bobbingSpeed = 2;
    [SerializeField] private float bobbingAmplitude = 2;
    [SerializeField, MinMaxRange(0,5)] private RangedFloat bobbingVarianceRange = new RangedFloat(0, 3);
    
    [Header("Info Panel")]
    [SerializeField] private float infoAnimationDuration = 0.2f;
    [SerializeField] private Ease infoAnimationEase = Ease.OutBack;
    [SerializeField] private Color affordableColor = Color.green;
    [SerializeField] private Color unaffordableColor = Color.red;
    [SerializeField] private SOAudioEvent showInfoSfx;
    [SerializeField] private SOAudioEvent hideInfoSfx;
    
    
    [Header("Reference")]
    [SerializeField] private Animator animator;
    [SerializeField] private Button button;
    [SerializeField] private Image buttonIcon;
    [SerializeField] private CanvasGroup upgradeInfoGroup;
    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image costIcon;
    [SerializeField] private Image iconImage;
    [SerializeField] private Transform gfx;
    [SerializeField] private Transform upgradeGfxHolder;
    [SerializeField] private Outline eggOutline;
    [SerializeField] private AudioSource audioSource;

    private bool _wasBought;
    private SOUpgradeBase _upgrade;
    private UpgradeStore _store;
    private RailPlayer _player;
    private LevelManager _levelManager;
    private int _upgradeCost;
    private float _randomBob;
    private Vector3 _gfxStartPosition;
    private Vector3 _transformStartPosition;
    private Vector3 _upgradeInfoGroupStartScale;
    private Sequence _animationSequence;
    private Sequence _upgradeInfoSequence;
    private Sequence _outlineSequence;
    
    public Button Button => button;

    public event Action<SOUpgradeBase> OnUpgradeBought; 
    
    private static readonly int Hover = Animator.StringToHash("Hover");
    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Open = Animator.StringToHash("Open");

    public void Setup(UpgradeStore store)
    {
        _store = store;
        _player = _store.Player;
        _levelManager = _store.LevelManager;
        
        eggOutline.OutlineWidth = 0f;
        _randomBob = bobbingVarianceRange.RandomValue;
        _transformStartPosition = transform.localPosition;
        _gfxStartPosition = gfx.localPosition;
        _upgradeInfoGroupStartScale = upgradeInfoGroup.transform.localScale;


        if (button)
        {
            var eventTrigger =  button.GetOrAddComponent<EventTrigger>();
            AddEventTriggerEntry(eventTrigger, EventTriggerType.Select, OnButtonSelected);
            AddEventTriggerEntry(eventTrigger, EventTriggerType.Deselect, OnButtonDeselected);
            button.onClick.AddListener(BuyUpgrade);
        }
        
        ResetUpgrade(false);
    }


    private void OnButtonSelected(BaseEventData baseEventData)
    {
        UpdateAffordabilityVisuals();
        ShowInfo();
    }
    
    
    private void OnButtonDeselected(BaseEventData baseEventData)
    {
        HideInfo();
    }
    
    
    
    private void Update()
    {
        if (gfx)
        {
            gfx.localPosition = _gfxStartPosition + new Vector3(0, Mathf.Sin(Time.time + _randomBob * bobbingSpeed) * bobbingAmplitude, 0);
        }
    }
    
    public void SetUpgrade(bool animate, SOUpgradeBase upgrade, int cost, float startDelay = 0)
    {
        if (!upgrade)
        {
            ResetUpgrade(false);
            return;
        }
        
        _upgrade = upgrade;
        _upgradeCost = cost;
        nameText.text = upgrade.ItemName;
        descriptionText.text = upgrade.ItemDescription;

        if (cost > 0)
        {
            costIcon.color = Color.white;
            costText.text = $"{cost}";
            UpdateAffordabilityVisuals();
        }
        else
        {
            costIcon.color = Color.clear;
            costText.text = $"";
        }

        iconImage.sprite = upgrade.ItemIcon;
        Instantiate(_upgrade.ItemGfx, upgradeGfxHolder);

        if (animate)
        {
            if (_animationSequence.isAlive) _animationSequence.Stop();
            _animationSequence = AnimateIn(startDelay);
        }

    }
    
    public void ResetUpgrade(bool animate)
    {
        if (gameObject.activeInHierarchy) animator?.SetTrigger(Idle);
        _wasBought = false;
        _upgrade = null;
        _upgradeCost = 0;
        button.interactable = true;
        button.GetComponentInChildren<TextMeshProUGUI>().text = $"Buy";
        buttonIcon.sprite = buyIcon;
        costText.color = Color.white;
        
        foreach (Transform child in upgradeGfxHolder)
        {
            Destroy(child.gameObject);
        }
        
        if (!animate)
        {
            mainCanvasGroup.alpha = 0f;
            upgradeInfoGroup.alpha = 0f;
            transform.localPosition = new Vector3(transform.localPosition.x, yOffset,transform.localPosition.z);
        }
        else
        {
            if (_animationSequence.isAlive) _animationSequence.Stop();
            _animationSequence = AnimateOut(0);
        }
    }

    public void RerollUpgrade(SOUpgradeBase upgrade, int cost, float startDelay)
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
            .Group(AnimateOut(startDelay))
            .ChainCallback(() => {ResetUpgrade(false); })
            .ChainCallback(() => {SetUpgrade(false, upgrade, cost); })
            .Group(AnimateIn(startDelay));
    }


    
    private void BuyUpgrade()
    {
        if (!_upgrade || _wasBought || _levelManager.IsGamePaused) return;
        
        if (!CanAffordUpgrade())
        {
            return;
        }
        
        boughtUpgradeSfx?.Play(audioSource);
        _wasBought = true;
        animator?.SetTrigger(Open);
        UpdateAffordabilityVisuals();
        
        OnUpgradeBought?.Invoke(_upgrade);
    }
    

    private void UpdateAffordabilityVisuals()
    {
        if (_wasBought)
        {
            button.interactable = true;
            button.GetComponentInChildren<TextMeshProUGUI>().text = $"Bought";
            buttonIcon.sprite = boughtIcon;
            costText.text = $"";
            costIcon.color = Color.clear;
            eggOutline.OutlineColor = Color.clear;
        }
        else if (!CanAffordUpgrade())
        {
            button.interactable = true;
            button.GetComponentInChildren<TextMeshProUGUI>().text = $"Buy";
            buttonIcon.sprite = buyIcon;
            costText.color = unaffordableColor;
            eggOutline.OutlineColor = unaffordableColor;
        }
        else
        {
            button.interactable = true;
            button.GetComponentInChildren<TextMeshProUGUI>().text = $"Buy";
            buttonIcon.sprite = buyIcon;
            costText.color = affordableColor;
            eggOutline.OutlineColor = affordableColor;
        }
    }
    
    private void ShowInfo()
    {
        if (!_upgrade) return;
        
        if (!_wasBought) animator?.SetTrigger(Hover);
        
        showInfoSfx?.Play(audioSource);
        if (_upgradeInfoSequence.isAlive) _upgradeInfoSequence.Stop();
        _upgradeInfoSequence = Sequence.Create()
            .Group(Tween.Alpha(upgradeInfoGroup, 1f, infoAnimationDuration * 0.7f))
            .Group(Tween.Scale(upgradeInfoGroup.transform, _upgradeInfoGroupStartScale, infoAnimationDuration, infoAnimationEase))
            .Group(FadeOutline(infoAnimationDuration, 0, true));
    }

    private void HideInfo()
    {
        if (!_upgrade) return;
        
        hideInfoSfx?.Play(audioSource);
        if (_upgradeInfoSequence.isAlive) _upgradeInfoSequence.Stop();
        _upgradeInfoSequence = Sequence.Create()
            .Group(Tween.Alpha(upgradeInfoGroup, 0f, infoAnimationDuration * 0.5f))
            .Group(Tween.Scale(upgradeInfoGroup.transform, Vector3.zero, infoAnimationDuration, infoAnimationEase))
            .Group(FadeOutline(infoAnimationDuration, 0, false));
    }
    

    
    private bool CanAffordUpgrade()
    {
        if (!_player || !_player.ResourceCollector) return false;
        return _player.ResourceCollector.CurrentCurrency >= _upgradeCost;
    }
    
    
    private Sequence AnimateIn(float startDelay)
    {
        showUpgradeSfx?.Play(audioSource);

        var sequence = Sequence.Create()
                .ChainDelay(startDelay)
                .Chain(Tween.LocalPositionY(transform,yOffset,_transformStartPosition.y, animationDuration,animationEase))
                .Group(Tween.Alpha(mainCanvasGroup, mainCanvasGroup.alpha,1, animationDuration, startDelay: animationDuration/3))
            ;
        
        return sequence;
    }
    
    
    private Sequence AnimateOut(float startDelay)
    {
        hideUpgradeSfx?.Play(audioSource);
        
        var sequence = Sequence.Create()
                .ChainDelay(startDelay)
                .Group(Tween.Alpha(mainCanvasGroup, 0, animationDuration))
                .Group(Tween.Alpha(upgradeInfoGroup, 0, animationDuration))
                .Group(Tween.LocalPositionY(transform, yOffset, animationDuration))
            ;
        
        return sequence;
    }

    private Sequence FadeOutline(float duration, float startDelay, bool animateIn)
    {
        var endValue = 0f;
        
        
        if (animateIn)
        {
            endValue = 3f;
        }

        
        var sequence = Sequence.Create()
                .ChainDelay(startDelay)
                .Group(Tween.Custom(eggOutline.OutlineWidth, endValue, duration: duration, onValueChange: (value) => eggOutline.OutlineWidth = value))
            ;

        return sequence;
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
    
    


}