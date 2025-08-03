using System;
using System.Linq;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VInspector;


public class MenuElementOptionsScreen : MenuElement
{
    [Header("Options Screen")]
    [SerializeField] private CanvasGroup optionsCanvas;
    [SerializeField] private Button nextPage;
    [SerializeField] private Button previousPage;
    [SerializeField] private CanvasGroup[] optionPages = Array.Empty<CanvasGroup>();
    [SerializeField] private Selectable[] selectables = Array.Empty<Selectable>();
    
    private Selectable _currentSelectable;
    private Sequence _optionsCanvasSequence;
    private int _currentOptionPageIndex;
    
    
    protected override void OnSelected()
    {

    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {
        
        if (optionPages.Length > 0)
        {
            optionPages[0].interactable = true;
            optionPages[0].blocksRaycasts = true;
            optionPages[0].alpha = 1;
            
            for (var i = 1; i < optionPages.Length; i++)
            {
                optionPages[i].interactable = false;
                optionPages[i].blocksRaycasts = false;
                optionPages[i].alpha = 0;
            }
        }
        
        
        if (nextPage)
        {
            nextPage.onClick.AddListener(() =>
            {
                if (_currentOptionPageIndex < optionPages.Length - 1)
                {
                    optionPages[_currentOptionPageIndex].interactable = false;
                    optionPages[_currentOptionPageIndex].blocksRaycasts = false;
                    optionPages[_currentOptionPageIndex].alpha = 0;

                    _currentOptionPageIndex++;
                    optionPages[_currentOptionPageIndex].interactable = true;
                    optionPages[_currentOptionPageIndex].blocksRaycasts = true;
                    optionPages[_currentOptionPageIndex].alpha = 1;
                }
            });
        }
        
        if (previousPage)
        {
            previousPage.onClick.AddListener(() =>
            {
                if (_currentOptionPageIndex > 0)
                {
                    optionPages[_currentOptionPageIndex].interactable = false;
                    optionPages[_currentOptionPageIndex].blocksRaycasts = false;
                    optionPages[_currentOptionPageIndex].alpha = 0;

                    _currentOptionPageIndex--;
                    optionPages[_currentOptionPageIndex].interactable = true;
                    optionPages[_currentOptionPageIndex].blocksRaycasts = true;
                    optionPages[_currentOptionPageIndex].alpha = 1;
                }
            });
        }
        
        
        
        ToggleLevelCanvas(false, false);
    }
    
    protected override void OnInteract()
    {
        ToggleLevelCanvas(true);
        SelectFirstAvailableButton();
    }
    
    protected override void OnFinishedInteraction()
    {
        ToggleLevelCanvas(false);
        _currentSelectable = null;
    }

    protected override void OnStopInteraction()
    {
        ToggleLevelCanvas(false);
        _currentSelectable = null;
    }
    
    protected override void OnNavigate(InputAction.CallbackContext context)
    {
        base.OnNavigate(context);
        
        if (_currentSelectable) return;

        SelectFirstAvailableButton();
    }
    
    
    
    private void ToggleLevelCanvas(bool state, bool animate = true)
    {
        if (!optionsCanvas) return;
        if (_optionsCanvasSequence.isAlive) _optionsCanvasSequence.Stop();

        if (animate)
        {
            _optionsCanvasSequence = Sequence.Create()
                .Group(Tween.Alpha(optionsCanvas, state ? 1 : 0, 0.3f))
                .OnComplete(() =>
                {
                    optionsCanvas.interactable = state;
                    optionsCanvas.blocksRaycasts = state;
                });
        }
        else
        {
            optionsCanvas.alpha = state ? 1 : 0;
            optionsCanvas.interactable = state;
            optionsCanvas.blocksRaycasts = state;
        }

    }
    
    
    private void SelectFirstAvailableButton()
    {
        foreach (var selectable in selectables)
        {
            if (selectable.interactable)
            {
                selectable.Select();
                _currentSelectable = selectable;
                break;
            }
        }
    }
    
}
