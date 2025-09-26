using System;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;


[SelectionBase]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(MainMenuInput))]
[RequireComponent(typeof(ControllerVibrationSource))]
public class MainMenuController : MonoBehaviour
{

    [Header("Menu Settings")]
    [SerializeField] private MainMenuElement[] menuElements;
    
    [Header("Controller Rumble")]
    [SerializeField] private ControllerVibrationEffectSettings vibrationOnInteract = new ControllerVibrationEffectSettings(0.03f, 0f, 0.3f);
    [SerializeField] private ControllerVibrationEffectSettings vibrationOnSelect = new ControllerVibrationEffectSettings(0.03f, 0f, 0.3f);
    
    [Header("References")]
    [SerializeField] private Transform defaultCameraPosition;
    [SerializeField] private Transform defaultCameraLookAtPoint;
    [SerializeField] private SOAudioEvent menuLoopSfx;
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;
    [SerializeField, Self, HideInInspector] public MainMenuInput mainMenuInput;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;
    
    
    private bool _isInteracting;
    private int _previousMenuElementIndex;
    private int _currentMenuElementIndex;
    private MainMenuElement _currentMainMenuElement;
    
    public Transform DefaultCameraLookAtPoint => defaultCameraLookAtPoint ? defaultCameraLookAtPoint : transform;
    public Transform DefaultCameraPosition => defaultCameraPosition ? defaultCameraPosition : transform;
    public Action<MainMenuElement> onElementSelected;
    public Action<MainMenuElement> onElementDeselected;
    public Action<MainMenuElement> onElementInteracted;
    public Action<MainMenuElement> onElementFinishedInteraction;

    private void OnValidate()
    {
        this.ValidateRefs();
    }
    

    private void Start()
    {
        FullScreenCAController.Instance?.ToggleOff();
        FullScreenHitFXController.Instance?.ToggleOff();
        menuLoopSfx?.Play(audioSource);
    }

    private void OnEnable()
    {
        mainMenuInput.OnNavigateAction += OnNavigate;
        mainMenuInput.OnSubmitAction += OnSubmit;
        mainMenuInput.OnCancelAction += OnCancel;
    }

    private void OnDisable()
    {
        mainMenuInput.OnNavigateAction -= OnNavigate;
        mainMenuInput.OnSubmitAction -= OnSubmit;
        mainMenuInput.OnCancelAction -= OnCancel;
    }

    
        
    #region Menu SetUp ------------------------------------------------------------------------------------------------------------
    
    [ContextMenu("Find All Menu Elements")]
    private void FindAllMenuElements()
    {
        if (menuElements.Length > 0)
        {
            menuElements = Array.Empty<MainMenuElement>();
        }
        menuElements = GetComponentsInChildren<MainMenuElement>();
    }

    #endregion Menu SetUp ------------------------------------------------------------------------------------------------------------
    

    #region Element Selection ----------------------------------------------------------------------------------------------------

    private void SelectMenuElement(int index)
    {
        if (_isInteracting || !menuElements[index].CanSelect) return;
        
        if (index < 0 || index >= menuElements.Length)
        {
            Debug.LogError("Invalid menu item index");
            return;
        }

        DisableSelection();
        controllerVibrationSource.Vibrate(vibrationOnSelect);

        _currentMenuElementIndex = index;
        _currentMainMenuElement = menuElements[index];
        _currentMainMenuElement.Select();
        onElementSelected?.Invoke(_currentMainMenuElement);
    }
    
    private void SelectNextMenuElement()
    {
        if (_isInteracting) return;
    
        if (_currentMenuElementIndex <= -1)
        {
            SelectFirstSelectableElement();
            return;
        }
    
        // Try to find the next selectable element
        int startIndex = _currentMenuElementIndex;
        int nextIndex = startIndex;
    
        do
        {
            nextIndex = (nextIndex + 1) % menuElements.Length;
        
            if (menuElements[nextIndex].CanSelect)
            {
                SelectMenuElement(nextIndex);
                return;
            }
        }
        while (nextIndex != startIndex); // Prevent infinite loop
        
    }

    private void SelectPreviousMenuElement()
    {
        if (_isInteracting) return;
    
        if (_currentMenuElementIndex <= -1)
        {
            SelectFirstSelectableElement();
            return;
        }
    
        // Try to find the previous selectable element
        int startIndex = _currentMenuElementIndex;
        int previousIndex = startIndex;
    
        do
        {
            previousIndex = (previousIndex - 1 + menuElements.Length) % menuElements.Length;
        
            if (menuElements[previousIndex].CanSelect)
            {
                SelectMenuElement(previousIndex);
                return;
            }
        }
        while (previousIndex != startIndex); // Prevent infinite loop
    }

    private void SelectFirstSelectableElement()
    {
        for (int i = 0; i < menuElements.Length; i++)
        {
            if (menuElements[i].CanSelect)
            {
                SelectMenuElement(i);
                return;
            }
        }
    }
    
    private void DisableSelection()
    {
        if (!_currentMainMenuElement) return;
        
        _currentMainMenuElement.Deselect();
        onElementDeselected?.Invoke(_currentMainMenuElement);
        _previousMenuElementIndex = _currentMenuElementIndex;
        _currentMainMenuElement = null;
    }
    


    #endregion Element Selection ----------------------------------------------------------------------------------------------------


    #region Element Interaction -----------------------------------------------------------------------------------------------------
    
    private void InteractWithElement(MainMenuElement element)
    {
        if (_isInteracting || !element.CanSelect) return;
        
        controllerVibrationSource.Vibrate(vibrationOnInteract);
        _isInteracting = true;
        element?.Interact();
        onElementInteracted?.Invoke(element);
    }
    
    private void StopInteraction(MainMenuElement element)
    {
        controllerVibrationSource.Vibrate(vibrationOnInteract);
        _isInteracting = false;
        element?.StopInteraction();
        onElementFinishedInteraction?.Invoke(element);
    }
    
    public void InteractionFinished(MainMenuElement element)
    {
        _isInteracting = false;
        onElementFinishedInteraction?.Invoke(element);
    }

    #endregion Element Interaction -----------------------------------------------------------------------------------------------------
    

    #region Input -------------------------------------------------------------------------------------------------------------------

    
    public void MousePressedElement (MainMenuElement element)
    {
        if (_isInteracting || _currentMainMenuElement != element) return;

        InteractWithElement(element);
    }
    
    public void MouseEnteredElement(MainMenuElement element)
    {
        if (_isInteracting || _currentMainMenuElement == element || mainMenuInput.IsCurrentDeviceGamepad) return;
        
        SelectMenuElement(Array.IndexOf(menuElements, element));
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        Vector2 input = context.ReadValue<Vector2>();
        
        if (!_currentMainMenuElement)
        {
            if (input.y > 0 || input.x < 0) // Up or Left
            {
                SelectNextMenuElement();
            }
            else if (input.y < 0 || input.x > 0) // Down or Right
            {
                SelectPreviousMenuElement();
            }
        }
        else
        {

            if (input.y > 0) // Up
            {
                if (_currentMainMenuElement.upElement)
                {
                    int upIndex = Array.IndexOf(menuElements, _currentMainMenuElement.upElement);
                    if (upIndex >= 0) SelectMenuElement(upIndex);
                }

            }
            else if (input.y < 0) // Down
            {
                if (_currentMainMenuElement.downElement)
                {
                    int downIndex = Array.IndexOf(menuElements, _currentMainMenuElement.downElement);
                    if (downIndex >= 0) SelectMenuElement(downIndex);
                }

            }
            else if (input.x > 0) // Right
            {
                if (_currentMainMenuElement.rightElement)
                {
                    int rightIndex = Array.IndexOf(menuElements, _currentMainMenuElement.rightElement);
                    if (rightIndex >= 0) SelectMenuElement(rightIndex);
                }

            }
            else if (input.x < 0) // Left
            {
                if (_currentMainMenuElement.leftElement)
                {
                    int leftIndex = Array.IndexOf(menuElements, _currentMainMenuElement.leftElement);
                    if (leftIndex >= 0) SelectMenuElement(leftIndex);
                }

            }
        }
    }

    
    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        
        if (_currentMainMenuElement)
        {
            InteractWithElement(_currentMainMenuElement);
        }
        else
        {
            SelectMenuElement(0);
        }
    }
    
    private void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;


        if (_currentMainMenuElement)
        {
            if (!_isInteracting)
            {
                DisableSelection();
            }
            else
            {
                StopInteraction(_currentMainMenuElement);
            }
        }
    }

    


    #endregion Input -------------------------------------------------------------------------------------------------------------------
    

}
