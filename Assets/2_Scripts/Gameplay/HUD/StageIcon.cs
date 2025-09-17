
using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;


public class StageIcon : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Vector2 smallSize;
    
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image outerIcon;
    [SerializeField] private Image innerIcon;

    private Vector2 _defaultSize;
    private Sequence _activeSequence;
    
    public void Initialize(Sprite sprite, Color color)
    {
        _defaultSize = rectTransform.sizeDelta;
        rectTransform.sizeDelta = smallSize;
        innerIcon.sprite = sprite;
        outerIcon.color = color;
    }
    
    public void SetCurrent(bool isActive)
    {
        if (_activeSequence.isAlive) _activeSequence.Stop();

        innerIcon.color = isActive && innerIcon.sprite ? Color.white : Color.clear;
        _activeSequence = Sequence.Create()
            .Group(Tween.UISizeDelta(rectTransform, isActive ? _defaultSize : smallSize, duration));
    }
}