using System;
using PrimeTween;
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
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform upgradeGfxHolder;

    private SOUpgrade _upgrade;
    private Vector3 _startPosition;
    private Vector3 _startScale;
    private Vector3 _startRotation;
    private Sequence _animationSequence;

    public event Action<SOUpgrade> onUpgradeSelected;

    private void Awake()
    {
        _startPosition = transform.localPosition;
        _startScale = transform.localScale;
        _startRotation = transform.localEulerAngles;
        button.onClick.AddListener(OnSelect);
        Reset(false);

    }
    
    public void SetUpgrade(SOUpgrade upgrade, float startDelay)
    {
        _upgrade = upgrade;
        Instantiate(_upgrade.ItemGfx, upgradeGfxHolder);
        
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
                .ChainDelay(startDelay)
            .Chain(Tween.LocalPositionY(transform,yOffset,_startPosition.y, animationDuration,animationEase))
            .Group(Tween.Alpha(canvasGroup, canvasGroup.alpha,1, animationDuration, startDelay: animationDuration/3))
            ;
    }

    public void Reset(bool animate)
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();

        _upgrade = null;
        transform.localScale = _startScale;
        transform.localEulerAngles = _startRotation;
        foreach (Transform child in upgradeGfxHolder)
        {
            Destroy(child.gameObject);
        }
        
        if (!animate)
        {
            canvasGroup.alpha = 0f;
            transform.localPosition = new Vector3(transform.localPosition.x, yOffset,transform.localPosition.z);
        }
        else
        {
            _animationSequence = Sequence.Create()
                .Group(Tween.Alpha(canvasGroup, 0, animationDuration))
                .Group(Tween.LocalPositionY(transform, yOffset, animationDuration))
                ;
        }
        
    }
    
    private void OnSelect()
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();
        
        onUpgradeSelected?.Invoke(_upgrade);
    }


    

}