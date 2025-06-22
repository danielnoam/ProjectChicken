using System;
using KBCore.Refs;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;



[RequireComponent(typeof(AudioSource))]
public class MainMenuController : MonoBehaviour
{

    [Header("Menu Settings")]
    [SerializeField] private LaunchMissionMode launchMissionMode;
    [SerializeField] private MenuElement[] menuElements;
    
    
    [Header("References")]
    [SerializeField] private Transform defaultCameraLookAtPoint;
    [SerializeField, Self] private AudioSource audioSource;
    
    
    private bool _isInteracting;
    private int _previousMenuElementIndex;
    private int _currentMenuElementIndex;
    private MenuElement _currentMenuElement;
    
    public Transform DefaultCameraLookAtPoint => defaultCameraLookAtPoint ? defaultCameraLookAtPoint : transform;
    public LaunchMissionMode LaunchMissionMode => launchMissionMode;
    public Action<MenuElement> OnElementSelected;
    public Action<MenuElement> OnElementDeselected;
    public Action<MenuElement> OnElementInteracted;
    public Action<MenuElement> OnElementFinishedInteraction;
    
    


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SelectPreviousMenuElement();

        } 
        
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SelectNextMenuElement();
        }
        

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_currentMenuElement)
            {
                InteractWithElement(_currentMenuElement);
            }
            else
            {
                SelectMenuElement(0);
            }

        }
        
        if (Input.GetKeyDown(KeyCode.Escape) && _currentMenuElement)
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
    

    #region Element Selection ----------------------------------------------------------------------------------------------------

    private void SelectMenuElement(int index)
    {
        if (_isInteracting) return;
        
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
            SelectMenuElement(0);
            return;
        }
        
        int nextIndex = (_currentMenuElementIndex + 1) % menuElements.Length;
        SelectMenuElement(nextIndex);
    }
    
    private void SelectPreviousMenuElement()
    {
        if (_isInteracting) return;
        
        if (_currentMenuElementIndex <= -1)
        {
            SelectMenuElement(0);
            return;
        }
        
        int previousIndex = (_currentMenuElementIndex - 1 + menuElements.Length) % menuElements.Length;
        SelectMenuElement(previousIndex);
    }
    
    private void DisableSelection()
    {
        if (!_currentMenuElement) return;
        
        _currentMenuElement.Deselect();
        OnElementDeselected?.Invoke(_currentMenuElement);
        _previousMenuElementIndex = _currentMenuElementIndex;
        _currentMenuElement = null;
    }
    
    private void InteractWithElement(MenuElement element)
    {
        if (_isInteracting) return;
        
        
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
    
    public void MouseEnteredElement(MenuElement element)
    {
        if (_isInteracting || _currentMenuElement == element) return;
        
        SelectMenuElement(Array.IndexOf(menuElements, element));
    }
    
    public void MousePressedElement (MenuElement element)
    {
        if (_isInteracting || _currentMenuElement != element) return;

        InteractWithElement(element);
    }

    #endregion Element Selection ----------------------------------------------------------------------------------------------------
    
    
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
    

}
