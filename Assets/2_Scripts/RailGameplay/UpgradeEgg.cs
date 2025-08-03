using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeEgg : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float animationDuration = 1;
    
    [Header("Reference")]
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform upgradeGfxHolder;

    private IStoreItem _upgrade;
    private Vector3 _startPosition;
    private Vector3 _startScale;
    private Vector3 _startRotation;
    private Sequence _animationSequence;

    public event Action<IStoreItem> onUpgradeSelected;

    private void Awake()
    {
        _startPosition = transform.position;
        _startScale = transform.localScale;
        _startRotation = transform.eulerAngles;
        canvasGroup.alpha = 0f;
        button.onClick.AddListener(Select);

    }

    private void Select()
    {
        foreach (Transform child in upgradeGfxHolder)
        {
            Destroy(child.gameObject);
        }
        
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 0, animationDuration));
        
        onUpgradeSelected?.Invoke(_upgrade);
    }

    public void SetUpgrade(IStoreItem upgrade)
    {
        _upgrade = upgrade;
        Instantiate(_upgrade.ItemGfx, upgradeGfxHolder);
        
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1, animationDuration));
    }
    

}