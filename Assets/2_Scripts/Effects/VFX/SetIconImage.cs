using System;
using DNExtensions.Systems.VFXManager;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Swaps the VFXManager icon sprite (and optionally its color), immediately or after a delay.
/// The DNExtensions package dropped its icon effects; the game's circle transitions still need this one.
/// </summary>
[Serializable]
[SerializableSelectorName("Icon Image", "Icon")]
public class SetIconImage : EffectBase
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private bool setColor;
    [SerializeField] private Color color = Color.white;
    [Tooltip("Delay before applying the effect, in seconds. Set to 0 for immediate application. Should not exceed the sequence duration.")]
    [SerializeField, Min(0)] private float delay;

    private Image _image;
    private Sequence _sequence;

    protected override bool Initialize()
    {
        if (!VFXManager.Instance) return false;

        _image = VFXManager.Instance.IconImage;
        if (!_image)
        {
            Debug.LogWarning("IconImage not found in VFXManager. SetIconImage effect will not play.", VFXManager.Instance);
            return false;
        }

        return true;
    }

    protected override void OnPlayEffect(float sequenceDuration)
    {
        if (_sequence.isAlive) _sequence.Stop();

        var applyDelay = Mathf.Min(delay, sequenceDuration);
        if (applyDelay > 0)
        {
            _sequence = Sequence.Create(useUnscaledTime: true)
                .ChainDelay(applyDelay)
                .OnComplete(Apply);
        }
        else
        {
            Apply();
        }
    }

    protected override void OnResetEffect()
    {
        if (_sequence.isAlive) _sequence.Stop();
        if (!_image || !VFXManager.Instance) return;

        var defaults = VFXManager.Instance.DefaultIconImage;
        _image.sprite = defaults.Sprite;
        if (setColor) _image.color = defaults.Color;
    }

    private void Apply()
    {
        _image.sprite = sprite;
        if (setColor) _image.color = color;
    }
}
