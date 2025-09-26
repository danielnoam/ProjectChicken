using System;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using VInspector;

namespace DNExtensions.MenuSystem
{
    [DisallowMultipleComponent]
    public class SelectableGraphicAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SelectableAnimator selectableAnimator;
        [SerializeField] private Graphic[] graphics;
        
        [Header("Position")] 
        [SerializeField] private PositionEffectType positionEffectType = PositionEffectType.Shake;
        [ShowIf("IsOffsetMode"), SerializeField] private Vector3 positionOffset = new Vector3(0, 10, 0);[EndIf]
        [ShowIf("IsOffsetMode"), SerializeField] private float positionDuration = 0.15f;[EndIf]
        [ShowIf("IsOffsetMode"), SerializeField] private Ease positionEase = Ease.InOutBounce;[EndIf]
        [ShowIf("IsShakeMode"), SerializeField] private bool shakeOnDeselect;[EndIf]
        [ShowIf("IsShakeMode"), SerializeField] private Vector3 shakeStrength = new Vector3(3, 3, 0);[EndIf]
        [ShowIf("IsShakeMode"), SerializeField] private float shakeFrequency = 10f;[EndIf]
        [ShowIf("IsShakeMode"), SerializeField] private float shakeDuration = 0.5f;[EndIf]
        [ShowIf("IsShakeMode"), SerializeField] private Ease shakeEase = Ease.Default;[EndIf]

        [Header("Rotate")] 
        [SerializeField] private bool animateRotation;
        [SerializeField] private Vector3 rotationOffset = new Vector3(0, 0, 15);
        [SerializeField] private float rotationDuration = 0.15f;
        [SerializeField] private Ease rotationEase = Ease.InOutBounce;

        [Header("Scale")] 
        [SerializeField] private bool animateScale;
        [SerializeField] private float scaleMultiplier = 1.1f;
        [SerializeField] private float scaleDuration = 0.15f;
        [SerializeField] private Ease scaleEase = Ease.InOutBounce;

        [Header("Color")]
        [SerializeField] private bool animateColor;
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private float colorDuration = 0.15f;
        [SerializeField] private Ease colorEase = Ease.InOutQuad;

        [Space(10)] 
        [SerializeField, ReadOnly] private RectTransform[] rectTransforms;

        private Vector3[] _originalPositions;
        private Vector3[] _originalScales;
        private Vector3[] _originalRotations;
        private Color[] _originalColors;
        
        private bool IsOffsetMode => positionEffectType == PositionEffectType.Offset;
        private bool IsShakeMode => positionEffectType == PositionEffectType.Shake;

        private enum PositionEffectType { None, Offset, Shake }

        private void OnValidate()
        {
            CacheRectTransforms();
        }

        private void Awake()
        {
            CacheRectTransforms();
            CacheOriginalValues();
        }

        private void OnEnable()
        {
            SubscribeToMenuAnimator();
        }

        private void OnDisable()
        {
            UnsubscribeFromMenuAnimator();
            ResetToOriginalState();
        }

        private void CacheRectTransforms()
        {
            if (graphics == null || graphics.Length == 0) return;

            rectTransforms = new RectTransform[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    rectTransforms[i] = graphics[i].GetComponent<RectTransform>();
                }
            }
        }

        private void CacheOriginalValues()
        {
            if (graphics == null || graphics.Length == 0) return;

            int length = graphics.Length;
            _originalPositions = new Vector3[length];
            _originalScales = new Vector3[length];
            _originalRotations = new Vector3[length];
            _originalColors = new Color[length];

            for (int i = 0; i < length; i++)
            {
                if (graphics[i] != null)
                {
                    var transform = graphics[i].transform;
                    var rectTransform = rectTransforms[i];

                    _originalScales[i] = transform.localScale;
                    _originalRotations[i] = transform.localRotation.eulerAngles;
                    _originalPositions[i] = rectTransform ? rectTransform.anchoredPosition3D : transform.localPosition;
                    _originalColors[i] = graphics[i].color;
                }
            }
        }

        private void SubscribeToMenuAnimator()
        {
            if (selectableAnimator == null) return;

            selectableAnimator.OnSelectEvent += Select;
            selectableAnimator.OnDeselectEvent += Deselect;
        }

        private void UnsubscribeFromMenuAnimator()
        {
            if (selectableAnimator == null) return;

            selectableAnimator.OnSelectEvent -= Select;
            selectableAnimator.OnDeselectEvent -= Deselect;
        }

        private void ResetToOriginalState()
        {
            if (graphics == null || _originalPositions == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                var transform = graphics[i].transform;
                var rectTransform = rectTransforms[i];

                if (positionEffectType == PositionEffectType.Offset && rectTransform)
                {
                    rectTransform.anchoredPosition3D = _originalPositions[i];
                }
                if (animateScale)
                {
                    transform.localScale = _originalScales[i];
                }
                if (animateRotation)
                {
                    transform.localRotation = Quaternion.Euler(_originalRotations[i]);
                }
                if (animateColor)
                {
                    graphics[i].color = _originalColors[i];
                }
            }
        }

        private void Select()
        {
            switch (positionEffectType)
            {
                case PositionEffectType.Offset:
                    PlayPositionAnimation(true);
                    break;
                case PositionEffectType.Shake:
                    PlayShakeAnimation();
                    break;
            }

            if (animateScale) PlayScaleAnimation(true);
            if (animateRotation) PlayRotateAnimation(true);
            if (animateColor) PlayColorAnimation(true);
        }

        private void Deselect()
        {
            switch (positionEffectType)
            {
                case PositionEffectType.Offset:
                    PlayPositionAnimation(false);
                    break;
                case PositionEffectType.Shake when shakeOnDeselect:
                    PlayShakeAnimation();
                    break;
            }

            if (animateScale) PlayScaleAnimation(false);
            if (animateRotation) PlayRotateAnimation(false);
            if (animateColor) PlayColorAnimation(false);
        }

        private void PlayPositionAnimation(bool selected)
        {
            if (graphics == null || _originalPositions == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null || rectTransforms[i] == null) continue;

                Vector3 endPosition = selected ? _originalPositions[i] + positionOffset : _originalPositions[i];
                Tween.UIAnchoredPosition3D(rectTransforms[i], endPosition, positionDuration, positionEase, useUnscaledTime: true);
            }
        }

        private void PlayRotateAnimation(bool selected)
        {
            if (graphics == null || _originalRotations == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                Vector3 endRotation = selected ? _originalRotations[i] + rotationOffset : _originalRotations[i];
                Tween.LocalRotation(graphics[i].transform, endRotation, rotationDuration, rotationEase, useUnscaledTime: true);
            }
        }

        private void PlayScaleAnimation(bool selected)
        {
            if (graphics == null || _originalScales == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                Vector3 endScale = selected ? _originalScales[i] * scaleMultiplier : _originalScales[i];
                Tween.Scale(graphics[i].transform, endScale, scaleDuration, scaleEase, useUnscaledTime: true);
            }
        }

        private void PlayShakeAnimation()
        {
            if (graphics == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                Tween.ShakeLocalPosition(graphics[i].transform, shakeStrength, shakeDuration, shakeFrequency,
                    easeBetweenShakes: shakeEase, useUnscaledTime: true);
            }
        }

        private void PlayColorAnimation(bool selected)
        {
            if (graphics == null || _originalColors == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                var endColor = selected ? selectedColor : _originalColors[i];
                Tween.Color(graphics[i], endColor, colorDuration, colorEase, useUnscaledTime: true);
            }
        }
    }
}