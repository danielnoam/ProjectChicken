using System;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using PrimeTween;
using Unity.Cinemachine;

public abstract class MenuElement : MonoBehaviour
{
    
    [Header("Element Settings")]
    [SerializeField] private string labelText;
    [SerializeField, Range(0, 1)] private float labelAlphaWhenDeselected = 0.25f;
    [SerializeField] private Color labelColorWhenSelected = Color.white;
    [SerializeField] private Transform cameraLookAtPoint;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup labelCanvasGroup;
    [SerializeField, Child(Flag.Optional)] private CinemachineCamera interactionCamera;
    [SerializeField, Parent] protected MainMenuController mainMenuController;
    [SerializeField, Parent] protected AudioSource audioSource;
    

    private Color _startLabelColor;
    public Transform CameraLookAtPoint => cameraLookAtPoint ? cameraLookAtPoint : transform;
    public CinemachineCamera InteractionCamera => interactionCamera;

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

    private void SetUp()
    {
        if (label) label.text = labelText;
        if (label) _startLabelColor = label.color;
        if (interactionCamera) interactionCamera.Priority = 0;
        ToggleLabel(false);
    }
    
    private void ToggleLabel(bool state)
    {
        if (labelCanvasGroup) labelCanvasGroup.alpha = state ? 1 : labelAlphaWhenDeselected;
        if (label) label.color = state ? labelColorWhenSelected : _startLabelColor;
    }
    
    public void Deselect()
    {
        ToggleLabel(false);
        OnDeselected();
    }

    public void Select()
    {
        ToggleLabel(true);
        OnSelected();
    }

    public void Interact()
    {
        OnInteract();
    }
    
    public void StopInteraction()
    {
        OnStopInteraction();
    }

    public void OnMouseEnter()
    {
        mainMenuController?.MouseEnteredElement(this);
    }
    
    public void OnMouseExit()
    {
        mainMenuController?.MouseExitedElement(this);
    }
    
    public void OnMouseDown()
    {
        mainMenuController?.MousePressedElement(this);
    }

    protected void FinishedInteraction()
    {
        mainMenuController?.InteractionFinished(this);
        OnFinishedInteraction();
    }
    
    
    
    
    protected abstract void OnSelected();
    protected abstract void OnDeselected();
    protected abstract void OnSetUp();
    protected abstract void OnInteract();
    protected abstract void OnFinishedInteraction();
    protected abstract void OnStopInteraction();
}
