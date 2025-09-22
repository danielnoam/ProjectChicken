
using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;


public class StageIcon : MonoBehaviour
{
    [Header("Color")]
    [SerializeField] private Color activeInnerColor = Color.white;
    [SerializeField] private Color inactiveInnerColor = new Color(1, 1, 1, 0.2f);
    
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image outerIcon;
    [SerializeField] private Image innerIcon;

    private Vector2 _smallSize;
    private Vector2 _defaultSize;
    private Sequence _activeSequence;
    
    public void Initialize(Sprite sprite, Color color, Vector2 smallSize)
    {
        _defaultSize = rectTransform.sizeDelta;
        _smallSize = smallSize;
        rectTransform.sizeDelta = _smallSize;
        innerIcon.sprite = sprite;
        outerIcon.color = color;
        innerIcon.color = innerIcon.sprite  ? inactiveInnerColor : Color.clear;
    }
    
    public void SetCurrent(bool isActive, float duration, Ease easeType)
    {
        if (_activeSequence.isAlive) _activeSequence.Stop();

        if (!innerIcon.sprite)
        {
            innerIcon.color = Color.clear;
        }

        _activeSequence = Sequence.Create()
            .Group(Tween.UISizeDelta(rectTransform, isActive ? _defaultSize : _smallSize, duration, easeType));
        
        if (innerIcon.sprite)
        {
            _activeSequence.Group(Tween.Color(innerIcon, isActive  ? activeInnerColor : inactiveInnerColor, duration, easeType));
        }
    }
}