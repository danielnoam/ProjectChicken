using DNExtensions;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using VInspector;
using Sequence = PrimeTween.Sequence;

[SelectionBase]
[RequireComponent(typeof(CinemachineImpulseSource))]
[RequireComponent(typeof(ControllerVibrationSource))]
public abstract class MainMenuElement : MonoBehaviour
{
    
    [Header("Element Settings")]
    [SerializeField] private bool canSelect = true;
    [SerializeField] private Color elementColor = Color.white;
    [SerializeField] private Vector3 cameraLootAtOffset;
    [Foldout("Navigation")]
    public MainMenuElement upElement;
    public MainMenuElement downElement;
    public MainMenuElement leftElement;
    public MainMenuElement rightElement;
    [EndFoldout]

    
    [Foldout("References")]
    [SerializeField] private Outline outline;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup labelCanvasGroup;
    [SerializeField] private SOAudioEvent selectSfx;
    [SerializeField] private SOAudioEvent interactSfx;
    [SerializeField, Parent, HideInInspector] protected MainMenuController mainMenuController;
    [SerializeField, Parent, HideInInspector] protected AudioSource audioSource;
    [SerializeField, Self, HideInInspector] protected ControllerVibrationSource controllerVibrationSource;
    [SerializeField, Self, HideInInspector] protected CinemachineImpulseSource cinemachineImpulseSource;
    [SerializeField, Child(Flag.Optional), HideInInspector] private CinemachineCamera interactionCamera;
    [EndFoldout]
    
    
    
    [Min(0.1f)] private const float visualElementsTweenDuration = 0.3f;
    [Range(0, 1)] private const float labelAlphaWhenInteracting = 0f;
    [Range(0, 1)] private const float labelAlphaWhenDeselected = 0.5f;
    [Range(0, 10)] private const float outlineWidthWhenSelected = 7f;
    
    protected enum ElementState { Deselected, Selected, Interacting, Disabled }
    protected ElementState currentVisualState;
    private Sequence _visualElementsSequence;
    private float _baseLabelFontSize;
    
    
    public bool CanSelect => canSelect;
    public Transform CameraLookAtPoint => transform;
    public CinemachineCamera InteractionCamera => interactionCamera;
    public Vector2 CameraLookAtOffset => cameraLootAtOffset;
    
    

    private void OnValidate()
    {
        this.ValidateRefs();
    }

    private void Awake()
    {
        SetUp();
        OnSetUp();
    }
    
    private void OnEnable()
    {
        mainMenuController.mainMenuInput.OnNavigateAction += OnNavigate;
        mainMenuController.mainMenuInput.OnSubmitAction += OnSubmit;
        mainMenuController.mainMenuInput.OnCancelAction += OnCancel;
    }

    private void OnDisable()
    {
        mainMenuController.mainMenuInput.OnNavigateAction -= OnNavigate;
        mainMenuController.mainMenuInput.OnSubmitAction -= OnSubmit;
        mainMenuController.mainMenuInput.OnCancelAction -= OnCancel;
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
        mainMenuController?.InteractionFinished(this);
        OnFinishedInteraction();
    }
    
    public void StopInteraction()
    {
        SetState(ElementState.Selected);
        OnStopInteraction();
    }
    
    
    private void SetUp()
    {
        outline.OutlineColor = elementColor;
        _baseLabelFontSize = label.fontSize;
        if (interactionCamera) interactionCamera.Priority = 0;
        SetState(ElementState.Deselected, instant: true);
    }
    
    private void SetState(ElementState state, bool instant = false)
    {
        if (_visualElementsSequence.isAlive) _visualElementsSequence.Stop();
        
        currentVisualState = state;
        float targetAlpha;
        float targetOutlineWidth;
        float targetLabelFontSize;
        Color targetLabelColor;
        
        if (!canSelect)
        {
            targetAlpha = 0f;
            targetLabelColor = Color.white;
            targetOutlineWidth = 0f;
            targetLabelFontSize = _baseLabelFontSize;
        }
        else
        {
            switch (state)
            {
                case ElementState.Deselected:
                    targetAlpha = labelAlphaWhenDeselected;
                    targetOutlineWidth = 0f;
                    targetLabelColor = Color.white;
                    targetLabelFontSize = _baseLabelFontSize;
                    break;
                case ElementState.Selected:
                    targetAlpha = 1f;
                    targetOutlineWidth = outlineWidthWhenSelected;
                    targetLabelColor = Color.white;
                    targetLabelFontSize = _baseLabelFontSize * 1.1f;
                    break;
                case ElementState.Interacting:
                    targetAlpha = labelAlphaWhenInteracting;
                    targetOutlineWidth = 0;
                    targetLabelColor = Color.white;
                    targetLabelFontSize = _baseLabelFontSize;
                    break;
                case ElementState.Disabled:
                    targetAlpha = 0f;
                    targetOutlineWidth = 0f;
                    targetLabelColor = Color.white;
                    targetLabelFontSize = _baseLabelFontSize;
                    break;
                default:
                    targetAlpha = labelAlphaWhenDeselected;
                    targetOutlineWidth = 0f;
                    targetLabelColor = Color.white;
                    targetLabelFontSize = _baseLabelFontSize;
                    break;
            }
        }
        

        if (instant)
        {
            labelCanvasGroup.alpha = targetAlpha;
            label.color = targetLabelColor;
            outline.OutlineWidth = targetOutlineWidth;
        }
        else
        {
            _visualElementsSequence = Sequence.Create();
            _visualElementsSequence.Group(Tween.Alpha(labelCanvasGroup, targetAlpha, visualElementsTweenDuration));
            _visualElementsSequence.Group(Tween.Color(label, targetLabelColor, visualElementsTweenDuration));
            _visualElementsSequence.Group(Tween.TextFontSize(label, targetLabelFontSize, visualElementsTweenDuration));
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
        if (mainMenuController.mainMenuInput.IsCurrentDeviceGamepad) return;
        mainMenuController?.MouseEnteredElement(this);
    }
    
    public void OnMouseDown()
    {
        if (mainMenuController.mainMenuInput.IsCurrentDeviceGamepad) return;
        mainMenuController?.MousePressedElement(this);
    }
    
    
    protected virtual void OnNavigate(InputAction.CallbackContext context)
    {
        if (!context.performed || currentVisualState != ElementState.Interacting) return;
        
        
    }
    
    protected virtual  void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed || currentVisualState != ElementState.Interacting ) return;
    }
    
    protected virtual  void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed || currentVisualState != ElementState.Interacting) return;
    }

    
    protected abstract void OnSelected();
    protected abstract void OnDeselected();
    protected abstract void OnSetUp();
    protected abstract void OnInteract();
    protected abstract void OnFinishedInteraction();
    protected abstract void OnStopInteraction();
    

}