using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeEgg : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private Ease animationEase = Ease.InOutBack;
    [SerializeField] private float yOffset = -150;
    
    [Header("Reference")]
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup upgradeInfoGroup;
    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Transform upgradeGfxHolder;

    private SOUpgradeBase _upgrade;
    private Vector3 _startPosition;
    private Vector3 _startScale;
    private Vector3 _startRotation;
    private Sequence _animationSequence;

    public event Action<SOUpgradeBase> OnUpgradeSelected;

    private void Awake()
    {
        _startPosition = transform.localPosition;
        _startScale = transform.localScale;
        _startRotation = transform.localEulerAngles;
        button.onClick.AddListener(OnSelect);
        Reset(false);

    }
    
    public void SetUpgrade(SOUpgradeBase upgrade, float startDelay)
    {
        if (!upgrade)
        {
            mainCanvasGroup.alpha = 0f;
            transform.localPosition = new Vector3(0,yOffset,0);
            return;
        }
        
        _upgrade = upgrade;
        nameText.text = upgrade.ItemName;
        descriptionText.text = upgrade.ItemDescription;
        
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

        _upgrade = null;
        transform.localScale = _startScale;
        transform.localEulerAngles = _startRotation;
        upgradeInfoGroup.alpha = 0f;
        foreach (Transform child in upgradeGfxHolder)
        {
            Destroy(child.gameObject);
        }
        
        if (!animate)
        {
            mainCanvasGroup.alpha = 0f;
            transform.localPosition = new Vector3(transform.localPosition.x, yOffset,transform.localPosition.z);
        }
        else
        {
            _animationSequence = Sequence.Create()
                .Group(Tween.Alpha(mainCanvasGroup, 0, animationDuration))
                .Group(Tween.LocalPositionY(transform, yOffset, animationDuration))
                ;
        }
        
    }
    
    private void OnSelect()
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();
        OnUpgradeSelected?.Invoke(_upgrade);
    }
    

    private void OnMouseEnter()
    {
        upgradeInfoGroup.alpha = 1f;
    }

    private void OnMouseExit()
    {
        upgradeInfoGroup.alpha = 0f;
    }
}