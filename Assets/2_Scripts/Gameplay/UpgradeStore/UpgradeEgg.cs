using System;
using DNExtensions;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UpgradeEgg : MonoBehaviour
{
    [Header("SFX")]
    [SerializeField] private SOAudioEvent showUpgradeSfx;
    [SerializeField] private SOAudioEvent hideUpgradeSfx;
    [SerializeField] private SOAudioEvent selectUpgradeSfx;
    
    [Header("Show/Hide Animation")]
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private Ease animationEase = Ease.InOutBack;
    [SerializeField] private float yOffset = -150;
    
    [Header("Idle Animation")]
    [SerializeField] private float bobbingSpeed = 2;
    [SerializeField] private float bobbingAmplitude = 1;
    [SerializeField, MinMaxRange(0,5)] private RangedFloat bobbingVarianceRange = new RangedFloat(0, 3);
    
    [Header("Info Panel")]
    [SerializeField] private float infoAnimationDuration = 0.2f;
    [SerializeField] private Ease infoAnimationEase = Ease.OutBack;
    [SerializeField] private SOAudioEvent showInfoSfx;
    [SerializeField] private SOAudioEvent hideInfoSfx;
    
    [Header("Reference")]
    [SerializeField] private Animator animator;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup upgradeInfoGroup;
    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image costIcon;
    [SerializeField] private Image iconImage;
    [SerializeField] private Transform gfx;
    [SerializeField] private Transform upgradeGfxHolder;
    [SerializeField] private AudioSource audioSource;

    private bool _isSelected;
    private SOUpgradeBase _upgrade;
    private RailPlayer _player; 
    private int _upgradeCost;
    private float _randomBob;
    private Vector3 _gfxStartPosition;
    private Vector3 _transformStartPosition;
    private Vector3 _upgradeInfoGroupStartScale;
    private Sequence _animationSequence;
    private Sequence _upgradeInfoSequence;

    public event Action<SOUpgradeBase> OnUpgradeBought; 
    
    private static readonly int Hover = Animator.StringToHash("Hover");
    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Open = Animator.StringToHash("Open");

    private void Awake()
    {
        _randomBob = bobbingVarianceRange.RandomValue;
        _transformStartPosition = transform.localPosition;
        _gfxStartPosition = gfx.localPosition;
        _upgradeInfoGroupStartScale = upgradeInfoGroup.transform.localScale;
        button?.onClick.AddListener(SelectUpgrade);
        Reset(false);
    }
    
    
    private void Update()
    {
        if (gfx)
        {
            gfx.localPosition = _gfxStartPosition + new Vector3(0, Mathf.Sin(Time.time + _randomBob * bobbingSpeed) * bobbingAmplitude, 0);
        }
    }
    
    public void SetPlayer(RailPlayer player)
    {
        _player = player;
    }
    
    public void SetUpgrade(SOUpgradeBase upgrade, int cost, float startDelay)
    {
        if (!upgrade)
        {
            Reset(false);
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
        
        showUpgradeSfx?.Play(audioSource);
        
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
                .ChainDelay(startDelay)
                .Chain(Tween.LocalPositionY(transform,yOffset,_transformStartPosition.y, animationDuration,animationEase))
                .Group(Tween.Alpha(mainCanvasGroup, mainCanvasGroup.alpha,1, animationDuration, startDelay: animationDuration/3))
            ;
    }

    private bool CanAffordUpgrade()
    {
        if (!_player || !_player.ResourceCollector) return false;
        return _player.ResourceCollector.CurrentCurrency >= _upgradeCost;
    }
    

    private void UpdateAffordabilityVisuals()
    {
        if (!CanAffordUpgrade())
        {
            mainCanvasGroup.alpha = 0.5f;
            button.interactable = false;
            costText.color = Color.red;
        }
        else
        {
            mainCanvasGroup.alpha = 1f;
            button.interactable = true;
            costText.color = Color.white;
        }
    }

    public void Reset(bool animate)
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();

        animator?.SetTrigger(Idle);
        _isSelected = false;
        _upgrade = null;
        _upgradeCost = 0;
        button.interactable = true;
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
            hideUpgradeSfx?.Play(audioSource);
            _animationSequence = Sequence.Create()
                .Group(Tween.Alpha(mainCanvasGroup, 0, animationDuration))
                .Group(Tween.Alpha(upgradeInfoGroup, 0, animationDuration))
                .Group(Tween.LocalPositionY(transform, yOffset, animationDuration))
                ;
        }
    }
    
    private void SelectUpgrade()
    {
        if (!_upgrade || _isSelected) return;
        
        if (!CanAffordUpgrade())
        {
            Debug.Log($"Cannot afford upgrade: {_upgrade.ItemName}. Cost: {_upgradeCost}, Current Currency: {_player.ResourceCollector.CurrentCurrency}");
            return;
        }
        
        selectUpgradeSfx?.Play(audioSource);
        _isSelected = true;
        animator?.SetTrigger(Open);
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
                .Group(Tween.Alpha(mainCanvasGroup, 0, animationDuration))
            ;
        OnUpgradeBought?.Invoke(_upgrade);
    }

    private void ShowInfo()
    {
        if (!_upgrade || _isSelected) return;
        
        animator?.SetTrigger(Hover);
        showInfoSfx?.Play(audioSource);
        if (_upgradeInfoSequence.isAlive) _upgradeInfoSequence.Stop();
        _upgradeInfoSequence = Sequence.Create()
            .Group(Tween.Alpha(upgradeInfoGroup, 1f, infoAnimationDuration * 0.7f))
            .Group(Tween.Scale(upgradeInfoGroup.transform, _upgradeInfoGroupStartScale, infoAnimationDuration, infoAnimationEase));
    }

    private void HideInfo()
    {
        if (!_upgrade) return;
        
        hideInfoSfx?.Play(audioSource);
        if (_upgradeInfoSequence.isAlive) _upgradeInfoSequence.Stop();
        _upgradeInfoSequence = Sequence.Create()
            .Group(Tween.Alpha(upgradeInfoGroup, 0f, infoAnimationDuration * 0.5f))
            .Group(Tween.Scale(upgradeInfoGroup.transform, Vector3.zero, infoAnimationDuration, infoAnimationEase));
    }

    private void OnMouseEnter()
    {
        ShowInfo();
    }

    private void OnMouseExit()
    {
        HideInfo();
    }

    private void OnMouseUpAsButton()
    {
        SelectUpgrade();
    }
}