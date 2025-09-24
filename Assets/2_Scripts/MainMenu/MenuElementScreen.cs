using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuElementScreen : MenuElement
{
    [Header("Screen")]
    [SerializeField] private MenuScreen menuScreen;
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
        if (menuScreen)
        {
            menuScreen.OnMenuScreenOpened += MenuOpened;
            menuScreen.OnMenuScreenClosed += MenuClosed;
        }
    }
    
    private void OnDestroy()
    {
        if (menuScreen)
        {
            menuScreen.OnMenuScreenOpened -= MenuOpened;
            menuScreen.OnMenuScreenClosed -= MenuClosed;
        }
    }
    
    protected override void OnInteract()
    {
        if (menuScreen)
        {
            menuScreen.Show();
        }
    }
    
    protected override void OnFinishedInteraction()
    {
        if (menuScreen)
        {
            menuScreen.Hide();
        }
        _currentSelectable = null;
    }

    protected override void OnStopInteraction()
    {
        if (menuScreen)
        {
            menuScreen.Hide();
        }
        _currentSelectable = null;
    }
    
    protected override void OnNavigate(InputAction.CallbackContext context)
    {
        base.OnNavigate(context);
        
        if (menuScreen && menuScreen.IsVisible && !_currentSelectable)
        {
            SelectFirstAvailableButton();
        }
    }
    
    private void MenuOpened()
    {
        SelectFirstAvailableButton();
    }
    
    private void MenuClosed()
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