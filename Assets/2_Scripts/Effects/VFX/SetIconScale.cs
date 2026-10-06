using System;
using DNExtensions.Systems.VFXManager;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scales the VFXManager icon between two values. Used for the circle iris transitions.
/// The DNExtensions package dropped its icon effects, so the game keeps its own.
/// </summary>
[Serializable]
[SerializableSelectorName("Icon Scale", "Icon")]
public class SetIconScale : EffectBase
{
    [Tooltip("If true, the effect will use the default scale of the icon image. If false, it will use the start value.")]
    [SerializeField] private bool useDefaultScale;
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 endScale = Vector3.one;
    [SerializeField] private Ease ease = Ease.Linear;

    private Image _image;
    private Sequence _sequence;

    protected override bool Initialize()
    {
        if (!VFXManager.Instance) return false;

        _image = VFXManager.Instance.IconImage;
        if (!_image)
        {
            Debug.LogWarning("IconImage not found in VFXManager. SetIconScale effect will not play.", VFXManager.Instance);
            return false;
        }

        return true;
    }

    protected override void OnPlayEffect(float sequenceDuration)
    {
        var scale = useDefaultScale ? VFXManager.Instance.DefaultIconImage.LocalScale : startScale;

        if (_sequence.isAlive) _sequence.Stop();
        _sequence = Sequence.Create(useUnscaledTime: true)
            .Group(Tween.Scale(_image.rectTransform, scale, endScale, GetEffectDuration(sequenceDuration), ease, startDelay: GetStartDelay(sequenceDuration)));
    }

    protected override void OnResetEffect()
    {
        if (_sequence.isAlive) _sequence.Stop();
        if (!_image || !VFXManager.Instance) return;

        _image.rectTransform.localScale = VFXManager.Instance.DefaultIconImage.LocalScale;
    }
}
