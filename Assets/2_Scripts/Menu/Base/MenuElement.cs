using System;
using DNExtensions;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using PrimeTween;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using VInspector;
using Sequence = PrimeTween.Sequence;

[SelectionBase]
[RequireComponent(typeof(CinemachineImpulseSource))]
[RequireComponent(typeof(ControllerVibrationSource))]
public abstract class MenuElement : MonoBehaviour
{
    
    [Header("Element Settings")]
    [SerializeField] private bool canSelect = true;
    [SerializeField] private Color elementColor = Color.white;
    [SerializeField, Min(0.1f)] private float visualElementsTweenDuration = 0.3f;
    [SerializeField] private string labelText;
    [SerializeField, Range(0, 1)] private float labelAlphaWhenInteracting = 0.15f;
    [SerializeField, Range(0, 1)] private float labelAlphaWhenDeselected = 0.15f;
    [SerializeField, Range(0, 10)] private float outlineWidthWhenSelected = 4;
    [SerializeField] private Vector3 cameraLootAtOffset;

    
    [Foldout("References")]
    [SerializeField] private Outline outline;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup labelCanvasGroup;
    [SerializeField] private SOAudioEvent selectSfx;
    [SerializeField] private SOAudioEvent interactSfx;
    [SerializeField, Parent, HideInInspector] protected MenuController menuController;
    [SerializeField, Parent, HideInInspector] protected AudioSource audioSource;
    [SerializeField, Self, HideInInspector] protected ControllerVibrationSource controllerVibrationSource;
    [SerializeField, Self, HideInInspector] protected CinemachineImpulseSource cinemachineImpulseSource;
    [SerializeField, Child(Flag.Optional), HideInInspector] private CinemachineCamera interactionCamera;
    [EndFoldout]
    
    
    protected enum ElementState { Deselected, Selected, Interacting, Disabled }
    protected ElementState CurrentVisualState;
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
    
    private void OnEnable()
    {
        menuController.menuInput.OnNavigateAction += OnNavigate;
        menuController.menuInput.OnSubmitAction += OnSubmit;
        menuController.menuInput.OnCancelAction += OnCancel;
    }

    private void OnDisable()
    {
        menuController.menuInput.OnNavigateAction -= OnNavigate;
        menuController.menuInput.OnSubmitAction -= OnSubmit;
        menuController.menuInput.OnCancelAction -= OnCancel;
    }
    
    public void Deselect()
    {
        SetState(ElementState.Deselected);
        OnDeselected();
    }

    public void Select()
    {
        if (!canSelect) return;
        
        selectSfx?.Play(audioSource);
        SetState(ElementState.Selected);
        OnSelected();
    }

    public void Interact()
    {
        if (!canSelect) return;
        
        interactSfx?.Play(audioSource);
        SetState(ElementState.Interacting);
        OnInteract();
    }
    
    protected void FinishedInteraction()
    {
        SetState(ElementState.Selected);
        menuController?.InteractionFinished(this);
        OnFinishedInteraction();
    }
    
    public void StopInteraction()
    {
        SetState(ElementState.Selected);
        OnStopInteraction();
    }
    
    
    private void SetUp()
    {
        label.text = labelText;
        outline.OutlineColor = elementColor;
        if (interactionCamera) interactionCamera.Priority = 0;
        SetState(ElementState.Deselected, instant: true);
    }
    
    private void SetState(ElementState state, bool instant = false)
    {
        if (_visualElementsSequence.isAlive) _visualElementsSequence.Stop();
        
        CurrentVisualState = state;
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
                case ElementState.Deselected:
                    targetAlpha = labelAlphaWhenDeselected;
                    targetColor = Color.white;
                    targetOutlineWidth = 0f;
                    break;
                case ElementState.Selected:
                    targetAlpha = 1f;
                    targetColor = elementColor;
                    targetOutlineWidth = outlineWidthWhenSelected;
                    break;
                case ElementState.Interacting:
                    targetAlpha = labelAlphaWhenInteracting;
                    targetColor = Color.white;
                    targetOutlineWidth = 0;
                    break;
                case ElementState.Disabled:
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
            var newState = state ? ElementState.Deselected : ElementState.Disabled;
            SetState(newState);
        }
    }

    

    public void OnMouseEnter()
    {
        if (menuController.menuInput.IsCurrentDeviceGamepad) return;
        menuController?.MouseEnteredElement(this);
    }
    
    public void OnMouseDown()
    {
        if (menuController.menuInput.IsCurrentDeviceGamepad) return;
        menuController?.MousePressedElement(this);
    }
    
    
    protected virtual void OnNavigate(InputAction.CallbackContext context)
    {
        if (!context.performed || CurrentVisualState != ElementState.Interacting) return;
        
        
    }
    
    protected virtual  void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed || CurrentVisualState != ElementState.Interacting ) return;
    }
    
    protected virtual  void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed || CurrentVisualState != ElementState.Interacting) return;
    }

    
    protected abstract void OnSelected();
    protected abstract void OnDeselected();
    protected abstract void OnSetUp();
    protected abstract void OnInteract();
    protected abstract void OnFinishedInteraction();
    protected abstract void OnStopInteraction();
    

}