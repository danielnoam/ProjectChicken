using System;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using PrimeTween;
using Unity.Cinemachine;

[SelectionBase]
public abstract class MenuElement : MonoBehaviour
{
    
    [Header("Element Settings")]
    [SerializeField] private bool canSelect = true;
    [SerializeField] private string labelText;
    [SerializeField, Range(0, 1)] private float labelAlphaWhenDeselected = 0.25f;
    [SerializeField] private Color labelColorWhenSelected = Color.white;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup labelCanvasGroup;
    [SerializeField] private SOAudioEvent selectSfx;
    [SerializeField] private SOAudioEvent interactSfx;
    
    [Header("Camera Settings")]
    [SerializeField] private Vector3 targetOffset;
    [SerializeField, Child(Flag.Optional) ] private CinemachineCamera interactionCamera;
    
    
    [SerializeField, Parent, HideInInspector] protected MenuController menuController;
    [SerializeField, Parent, HideInInspector] protected AudioSource audioSource;

    private Color _startLabelColor;
    public bool CanSelect => canSelect;
    public Transform CameraLookAtPoint => transform;
    public CinemachineCamera InteractionCamera => interactionCamera;
    public Vector2 TargetOffset => targetOffset;

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
        ToggleLabel(false);
        OnDeselected();
    }

    public void Select()
    {
        if (!canSelect) return;
        
        selectSfx?.Play(audioSource);
        ToggleLabel(true);
        OnSelected();
    }

    public void Interact()
    {
        if (!canSelect) return;
        
        interactSfx?.Play(audioSource);
        OnInteract();
    }
    
    protected void FinishedInteraction()
    {
        menuController?.InteractionFinished(this);
        OnFinishedInteraction();
    }
    
    public void StopInteraction()
    {
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
        if (label) label.text = labelText;
        if (label) _startLabelColor = label.color;
        if (interactionCamera) interactionCamera.Priority = 0;
        ToggleLabel(false);
    }
    
    private void ToggleLabel(bool state)
    {
        if (labelCanvasGroup) 
        {
            if (!canSelect)
            {
                labelCanvasGroup.alpha = 0;
            }
            else
            {
                labelCanvasGroup.alpha = state ? 1 : labelAlphaWhenDeselected;
            }

        }
    
        if (label)
        {
            if (!canSelect)
            {
                label.color = _startLabelColor;
            }
            else
            {
                label.color = state ? labelColorWhenSelected : _startLabelColor;
            }
        }
    }
    
    protected void ToggleCanSelect(bool state, bool labelState)
    {
        canSelect = state;
        ToggleLabel(labelState);
    }
    
    
    
    
    protected abstract void OnSelected();
    protected abstract void OnDeselected();
    protected abstract void OnSetUp();
    protected abstract void OnInteract();
    protected abstract void OnFinishedInteraction();
    protected abstract void OnStopInteraction();
}
