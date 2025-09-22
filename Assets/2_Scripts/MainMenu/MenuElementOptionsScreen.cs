using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuElementOptionsScreen : MenuElement
{
    [Header("Options Screen")]
    [SerializeField] private OptionsScreen optionsScreen;
    [SerializeField] private Selectable[] selectables = Array.Empty<Selectable>();
    
    private Selectable _currentSelectable;
    
    protected override void OnSelected()
    {

    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {
        if (optionsScreen)
        {
            optionsScreen.OnOptionsOpened += OnOptionsOpened;
            optionsScreen.OnOptionsClosed += OnOptionsClosed;
        }
    }
    
    private void OnDestroy()
    {
        if (optionsScreen)
        {
            optionsScreen.OnOptionsOpened -= OnOptionsOpened;
            optionsScreen.OnOptionsClosed -= OnOptionsClosed;
        }
    }
    
    protected override void OnInteract()
    {
        if (optionsScreen)
        {
            optionsScreen.Show();
        }
    }
    
    protected override void OnFinishedInteraction()
    {
        if (optionsScreen)
        {
            optionsScreen.Hide();
        }
        _currentSelectable = null;
    }

    protected override void OnStopInteraction()
    {
        if (optionsScreen)
        {
            optionsScreen.Hide();
        }
        _currentSelectable = null;
    }
    
    protected override void OnNavigate(InputAction.CallbackContext context)
    {
        base.OnNavigate(context);
        
        if (optionsScreen && optionsScreen.IsVisible && !_currentSelectable)
        {
            SelectFirstAvailableButton();
        }
    }
    
    private void OnOptionsOpened()
    {
        SelectFirstAvailableButton();
    }
    
    private void OnOptionsClosed()
    {
        _currentSelectable = null;
    }
    
    private void SelectFirstAvailableButton()
    {
        foreach (var selectable in selectables)
        {
            if (selectable && selectable.interactable)
            {
                selectable.Select();
                _currentSelectable = selectable;
                break;
            }
        }
    }
}