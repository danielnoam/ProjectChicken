using System;
using KBCore.Refs;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;



[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(MenuInput))]
public class MenuController : MonoBehaviour
{

    [Header("Menu Settings")]
    [SerializeField] private MenuElement[] menuElements;
    
    
    [Header("References")]
    [SerializeField] private Transform defaultCameraLookAtPoint;
    [SerializeField, Self, HideInInspector] private AudioSource audioSource;
    [SerializeField, Self, HideInInspector] private MenuInput menuInput;
    
    
    private bool _isInteracting;
    private int _previousMenuElementIndex;
    private int _currentMenuElementIndex;
    private MenuElement _currentMenuElement;
    
    public Transform DefaultCameraLookAtPoint => defaultCameraLookAtPoint ? defaultCameraLookAtPoint : transform;
    public Action<MenuElement> OnElementSelected;
    public Action<MenuElement> OnElementDeselected;
    public Action<MenuElement> OnElementInteracted;
    public Action<MenuElement> OnElementFinishedInteraction;

    private void OnValidate()
    {
        this.ValidateRefs();
    }

    private void OnEnable()
    {
        menuInput.OnNavigateAction += OnNavigate;
        menuInput.OnSubmitAction += OnSubmit;
        menuInput.OnCancelAction += OnCancel;
    }

    private void OnDisable()
    {
        menuInput.OnNavigateAction -= OnNavigate;
        menuInput.OnSubmitAction -= OnSubmit;
        menuInput.OnCancelAction -= OnCancel;
    }

    
        
    #region Menu SetUp ------------------------------------------------------------------------------------------------------------
    
    [ContextMenu("Find All Menu Elements")]
    private void FindAllMenuElements()
    {
        if (menuElements.Length > 0)
        {
            menuElements = Array.Empty<MenuElement>();
        }
        menuElements = GetComponentsInChildren<MenuElement>();
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

        _currentMenuElementIndex = index;
        _currentMenuElement = menuElements[index];
        _currentMenuElement.Select();
        OnElementSelected?.Invoke(_currentMenuElement);
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
        if (!_currentMenuElement) return;
        
        _currentMenuElement.Deselect();
        OnElementDeselected?.Invoke(_currentMenuElement);
        _previousMenuElementIndex = _currentMenuElementIndex;
        _currentMenuElement = null;
    }
    


    #endregion Element Selection ----------------------------------------------------------------------------------------------------


    #region Element Interaction -----------------------------------------------------------------------------------------------------
    
    private void InteractWithElement(MenuElement element)
    {
        if (_isInteracting || !element.CanSelect) return;
        
        
        _isInteracting = true;
        element?.Interact();
        OnElementInteracted?.Invoke(element);
    }
    
    private void StopInteraction(MenuElement element)
    {
        _isInteracting = false;
        element?.StopInteraction();
        OnElementFinishedInteraction?.Invoke(element);
    }
    
    public void InteractionFinished(MenuElement element)
    {
        _isInteracting = false;
        OnElementFinishedInteraction?.Invoke(element);
    }

    #endregion Element Interaction -----------------------------------------------------------------------------------------------------
    

    #region Input -------------------------------------------------------------------------------------------------------------------

    
    public void MousePressedElement (MenuElement element)
    {
        if (_isInteracting || _currentMenuElement != element) return;

        InteractWithElement(element);
    }
    
    public void MouseEnteredElement(MenuElement element)
    {
        if (_isInteracting || _currentMenuElement == element) return;
        
        SelectMenuElement(Array.IndexOf(menuElements, element));
    }
    
    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        
        if (context.ReadValue<Vector2>().y > 0 || context.ReadValue<Vector2>().x < 0)
        {
            SelectNextMenuElement();
        }
        else if (context.ReadValue<Vector2>().y < 0 || context.ReadValue<Vector2>().x > 0)
        {
            SelectPreviousMenuElement();
        }

    }
    
    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        
        if (_currentMenuElement)
        {
            InteractWithElement(_currentMenuElement);
        }
        else
        {
            SelectMenuElement(0);
        }
    }
    
    private void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;


        if (_currentMenuElement)
        {
            if (!_isInteracting)
            {
                DisableSelection();
            }
            else
            {
                StopInteraction(_currentMenuElement);
            }
        }

    }

    


    #endregion Input -------------------------------------------------------------------------------------------------------------------
    

}
