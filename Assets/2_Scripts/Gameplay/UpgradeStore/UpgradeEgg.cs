using System;
using DNExtensions;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeEgg : MonoBehaviour
{

    [Header("Show/Hide Animation")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private Ease animationEase = Ease.InOutBack;
    [SerializeField] private float yOffset = -150;
    
    [Header("Hover Animation")]
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
    [SerializeField] private Image iconImage;
    [SerializeField] private Transform upgradeGfxHolder;
    [SerializeField] private AudioSource audioSource;

    private bool _isSelected;
    private SOUpgradeBase _upgrade;
    private Vector3 _startPosition;
    private Vector3 _startScale;
    private Vector3 _startRotation;
    private Vector3 _upgradeInfoGroupStartScale;
    private Sequence _animationSequence;
    private Sequence _upgradeInfoSequence;

    public event Action<SOUpgradeBase> OnUpgradeSelected;
    
    private static readonly int Hover = Animator.StringToHash("Hover");
    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Open = Animator.StringToHash("Open2");


    private void Awake()
    {
        _startPosition = transform.localPosition;
        _startScale = transform.localScale;
        _startRotation = transform.localEulerAngles;
        _upgradeInfoGroupStartScale = upgradeInfoGroup.transform.localScale;
        button?.onClick.AddListener(SelectUpgrade);
        Reset(false);

    }
    
    public void SetUpgrade(SOUpgradeBase upgrade, float startDelay)
    {
        if (!upgrade)
        {
            Reset(false);
            return;
        }
        
        _upgrade = upgrade;
        nameText.text = upgrade.ItemName;
        descriptionText.text = upgrade.ItemDescription;
        iconImage = upgrade.ItemIcon;
        Instantiate(_upgrade.ItemGfx, upgradeGfxHolder);
        
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
                .ChainDelay(startDelay)
            .Chain(Tween.LocalPositionY(transform,yOffset,_startPosition.y, animationDuration,animationEase))
            .Group(Tween.Alpha(mainCanvasGroup, mainCanvasGroup.alpha,1, animationDuration, startDelay: animationDuration/3))
            ;
    }

    public void Reset(bool animate)
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();

        
        animator?.SetTrigger(Idle);
        _isSelected = false;
        _upgrade = null;
        transform.localScale = _startScale;
        transform.localEulerAngles = _startRotation;
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
        
        _isSelected = true;
        animator?.SetTrigger(Open);
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
                .Group(Tween.Alpha(mainCanvasGroup, 0, animationDuration))
            ;
        OnUpgradeSelected?.Invoke(_upgrade);
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