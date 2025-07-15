using System;
using DNExtensions;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using PrimeTween;
using Unity.Cinemachine;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(Outline))]
[RequireComponent(typeof(CinemachineImpulseSource))]
[RequireComponent(typeof(ControllerRumbleSource))]
public abstract class MenuElement : MonoBehaviour
{
    
    [Header("Element Settings")]
    [SerializeField] private bool canSelect = true;
    [SerializeField] private Color elementColor = Color.white;
    [SerializeField, Min(0.1f)] private float visualElementsTweenDuration = 0.3f;
    [SerializeField] private string labelText;
    [SerializeField, Range(0, 1)] private float labelAlphaWhenDeselected = 0.15f;
    [SerializeField, Range(0, 10)] private float outlineWidthWhenSelected = 4;
    [SerializeField] private Vector3 cameraLootAtOffset;

    
    [Foldout("References")]
    [SerializeField, Self(Flag.Editable)] private Outline outline;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup labelCanvasGroup;
    [SerializeField] private SOAudioEvent selectSfx;
    [SerializeField] private SOAudioEvent interactSfx;
    [SerializeField, Parent, HideInInspector] protected MenuController menuController;
    [SerializeField, Parent, HideInInspector] protected AudioSource audioSource;
    [SerializeField, Self, HideInInspector] protected ControllerRumbleSource controllerRumbleSource;
    [SerializeField, Self, HideInInspector] protected CinemachineImpulseSource cinemachineImpulseSource;
    [SerializeField, Child(Flag.Optional), HideInInspector] private CinemachineCamera interactionCamera;
    [EndFoldout]
    
    
    private enum VisualState { Deselected, Selected, Interacting, Disabled }
    private VisualState _currentVisualState;
    private Sequence _visualElementsSequence;
    
    public bool CanSelect => canSelect;
    public Transform CameraLookAtPoint => transform;
    public CinemachineCamera InteractionCamera => interactionCamera;
    public Vector2 CameraLookAtOffset => cameraLootAtOffset;
    
    

    private void OnValidate()
    {
        this.ValidateRefs();
        if (label) label.text = labelText;
    }

    private void Awake()
    {
        SetUp();
        OnSetUp();
    }
    
    public void Deselect()
    {
        SetVisualState(VisualState.Deselected);
        OnDeselected();
    }

    public void Select()
    {
        if (!canSelect) return;
        
        selectSfx?.Play(audioSource);
        SetVisualState(VisualState.Selected);
        OnSelected();
    }

    public void Interact()
    {
        if (!canSelect) return;
        
        interactSfx?.Play(audioSource);
        SetVisualState(VisualState.Interacting);
        OnInteract();
    }
    
    protected void FinishedInteraction()
    {
        SetVisualState(VisualState.Selected);
        menuController?.InteractionFinished(this);
        OnFinishedInteraction();
    }
    
    public void StopInteraction()
    {
        SetVisualState(VisualState.Selected);
        OnStopInteraction();
    }
    

    public void OnMouseEnter()
    {
        menuController?.MouseEnteredElement(this);
    }
    
    public void OnMouseDown()
    {
        menuController?.MousePressedElement(this);
    }
    
    
    private void SetUp()
    {
        label.text = labelText;
        outline.OutlineColor = elementColor;
        if (interactionCamera) interactionCamera.Priority = 0;
        SetVisualState(VisualState.Deselected, instant: true);
    }
    
    private void SetVisualState(VisualState state, bool instant = false)
    {
        if (_visualElementsSequence.isAlive) _visualElementsSequence.Stop();
        
        _currentVisualState = state;
        float targetAlpha;
        float targetOutlineWidth;
        Color targetColor;
        
        if (!canSelect)
        {
            targetAlpha = 0f;
            targetColor = Color.white;
            targetOutlineWidth = 0f;
        }
        else
        {
            switch (state)
            {
                case VisualState.Deselected:
                    targetAlpha = labelAlphaWhenDeselected;
                    targetColor = Color.white;
                    targetOutlineWidth = 0f;
                    break;
                case VisualState.Selected:
                    targetAlpha = 1f;
                    targetColor = elementColor;
                    targetOutlineWidth = outlineWidthWhenSelected;
                    break;
                case VisualState.Interacting:
                    targetAlpha = 0;
                    targetColor = elementColor;
                    targetOutlineWidth = 0;
                    break;
                case VisualState.Disabled:
                    targetAlpha = 0f;
                    targetColor = Color.white;
                    targetOutlineWidth = 0f;
                    break;
                default:
                    targetAlpha = labelAlphaWhenDeselected;
                    targetColor = Color.white;
                    targetOutlineWidth = 0f;
                    break;
            }
        }
        

        if (instant)
        {
            labelCanvasGroup.alpha = targetAlpha;
            label.color = targetColor;
            outline.OutlineWidth = targetOutlineWidth;
        }
        else
        {
            _visualElementsSequence = Sequence.Create();
            _visualElementsSequence.Group(Tween.Alpha(labelCanvasGroup, targetAlpha, visualElementsTweenDuration));
            _visualElementsSequence.Group(Tween.Color(label, targetColor, visualElementsTweenDuration));
            _visualElementsSequence.Group(Tween.Custom(
                startValue: outline.OutlineWidth,
                endValue: targetOutlineWidth,
                duration: visualElementsTweenDuration,
                onValueChange: value => outline.OutlineWidth = value));
        }
    }
    
    protected void ToggleCanSelect(bool state, bool refreshVisuals = true)
    {
        canSelect = state;
        if (refreshVisuals)
        {
            var newState = state ? VisualState.Deselected : VisualState.Disabled;
            SetVisualState(newState);
        }
    }
    
    
    protected abstract void OnSelected();
    protected abstract void OnDeselected();
    protected abstract void OnSetUp();
    protected abstract void OnInteract();
    protected abstract void OnFinishedInteraction();
    protected abstract void OnStopInteraction();
}