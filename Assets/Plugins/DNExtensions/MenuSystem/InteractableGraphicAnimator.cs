using System;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using VInspector;

namespace DNExtensions.MenuSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public class InteractableGraphicAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Selectable targetSelectable;
        [SerializeField] private Graphic[] graphics;
        
        [Header("Position")] 
        [SerializeField] private PositionEffectType positionEffectType = PositionEffectType.Shake;
        [ShowIf("IsOffsetMode"), SerializeField] private Vector3 positionOffset = new Vector3(0, 10, 0);[EndIf]
        [ShowIf("IsOffsetMode"), SerializeField] private float positionDuration = 0.15f;[EndIf]
        [ShowIf("IsOffsetMode"), SerializeField] private Ease positionEase = Ease.InOutBounce;[EndIf]
        [ShowIf("IsShakeMode"), SerializeField] private bool shakeOnDisable;[EndIf]
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
        [SerializeField] private Color disabledColor = Color.gray;
        [SerializeField] private float colorDuration = 0.15f;
        [SerializeField] private Ease colorEase = Ease.InOutQuad;

        [Space(10)] 
        [SerializeField, ReadOnly] private RectTransform[] rectTransforms;

        // Event for external components that want to listen to interactable changes
        public event Action<bool> OnInteractableChanged;

        private Vector3[] _originalPositions;
        private Vector3[] _originalScales;
        private Vector3[] _originalRotations;
        private Color[] _originalColors;
        private bool _interactable = true;
        
        private bool IsOffsetMode => positionEffectType == PositionEffectType.Offset;
        private bool IsShakeMode => positionEffectType == PositionEffectType.Shake;

        private enum PositionEffectType { None, Offset, Shake }

        /// <summary>
        /// Gets or sets the interactable state, triggering animations and events when changed
        /// </summary>
        public bool Interactable
        {
            get => _interactable;
            set
            {
                if (_interactable != value)
                {
                    _interactable = value;
                    if (targetSelectable != null)
                    {
                        targetSelectable.interactable = value;
                    }
                    
                    // Trigger animations
                    if (value)
                    {
                        OnInteractableEnabled();
                    }
                    else
                    {
                        OnInteractableDisabled();
                    }
                    
                    // Fire event for other components
                    OnInteractableChanged?.Invoke(value);
                }
            }
        }

        private void OnValidate()
        {
            CacheSelectable();
            CacheRectTransforms();
        }

        private void Awake()
        {
            CacheSelectable();
            CacheRectTransforms();
            CacheOriginalValues();
            
            if (targetSelectable != null)
            {
                _interactable = targetSelectable.interactable;
            }
        }

        private void Update()
        {
            // Check if the selectable's interactable state changed externally
            if (targetSelectable != null && targetSelectable.interactable != _interactable)
            {
                bool newState = targetSelectable.interactable;
                _interactable = newState;
                
                if (newState)
                {
                    OnInteractableEnabled();
                }
                else
                {
                    OnInteractableDisabled();
                }
                
                OnInteractableChanged?.Invoke(newState);
            }
        }

        private void OnDisable()
        {
            ResetToOriginalState();
        }

        private void CacheSelectable()
        {
            if (targetSelectable == null)
            {
                targetSelectable = GetComponent<Selectable>();
            }
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

        private void OnInteractableEnabled()
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

        private void OnInteractableDisabled()
        {
            switch (positionEffectType)
            {
                case PositionEffectType.Offset:
                    PlayPositionAnimation(false);
                    break;
                case PositionEffectType.Shake when shakeOnDisable:
                    PlayShakeAnimation();
                    break;
            }

            if (animateScale) PlayScaleAnimation(false);
            if (animateRotation) PlayRotateAnimation(false);
            if (animateColor) PlayColorAnimation(false);
        }

        private void PlayPositionAnimation(bool interactable)
        {
            if (graphics == null || _originalPositions == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null || rectTransforms[i] == null) continue;

                Vector3 endPosition = interactable ? _originalPositions[i] : _originalPositions[i] + positionOffset;
                Tween.UIAnchoredPosition3D(rectTransforms[i], endPosition, positionDuration, positionEase, useUnscaledTime: true);
            }
        }

        private void PlayRotateAnimation(bool interactable)
        {
            if (graphics == null || _originalRotations == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                Vector3 endRotation = interactable ? _originalRotations[i] : _originalRotations[i] + rotationOffset;
                Tween.LocalRotation(graphics[i].transform, endRotation, rotationDuration, rotationEase, useUnscaledTime: true);
            }
        }

        private void PlayScaleAnimation(bool interactable)
        {
            if (graphics == null || _originalScales == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                Vector3 endScale = interactable ? _originalScales[i] : _originalScales[i] * scaleMultiplier;
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

        private void PlayColorAnimation(bool interactable)
        {
            if (graphics == null || _originalColors == null) return;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;

                var endColor = interactable ? _originalColors[i] : disabledColor;
                Tween.Color(graphics[i], endColor, colorDuration, colorEase, useUnscaledTime: true);
            }
        }

        /// <summary>
        /// Manually trigger the interactable state animations
        /// </summary>
        /// <param name="interactable">Target interactable state</param>
        public void SetInteractableState(bool interactable)
        {
            Interactable = interactable;
        }

        /// <summary>
        /// Force refresh the animation state based on current selectable interactable value
        /// Call this if the selectable's interactable was changed externally
        /// </summary>
        public void SyncWithSelectable()
        {
            if (targetSelectable != null && targetSelectable.interactable != _interactable)
            {
                bool newState = targetSelectable.interactable;
                _interactable = newState;
                
                if (newState)
                {
                    OnInteractableEnabled();
                }
                else
                {
                    OnInteractableDisabled();
                }
                
                OnInteractableChanged?.Invoke(newState);
            }
        }

        /// <summary>
        /// Force animation state without changing the actual interactable value
        /// Useful for previewing animations in editor
        /// </summary>
        public void ForceAnimationState(bool enabledState)
        {
            if (enabledState)
            {
                OnInteractableEnabled();
            }
            else
            {
                OnInteractableDisabled();
            }
        }
    }
}