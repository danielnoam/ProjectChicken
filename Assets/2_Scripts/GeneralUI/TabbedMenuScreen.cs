using System;
using DNExtensions;
using DNExtensions.MenuSystem;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class TabbedMenuScreen : MonoBehaviour
{
    [Header("Screen")]
    [SerializeField] private CanvasGroup screenCanvas;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup[] tabPanels = Array.Empty<CanvasGroup>();
    [SerializeField] private Button[] tabButtons = Array.Empty<Button>();
    
    
    private Sequence _canvasSequence;
    private int _currentTabIndex;
    
    public event Action OnMenuScreenOpened;
    public event Action OnMenuScreenClosed;
    public event Action<CanvasGroup> OnTabSelected;
    
    
    public bool IsVisible => screenCanvas.alpha > 0.5f;
    
    private void Start()
    {
        SetupTabs();
        SetupCloseButton();
        Hide(false); 
    }
    
    private void SetupTabs()
    {
        if (tabButtons.Length != tabPanels.Length)
        {
            Debug.LogError("Tab buttons and panels arrays must have the same length!");
        }

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int tabIndex = i; 
            tabButtons[i].onClick.AddListener(() => SelectTab(tabIndex));
        }
        
        if (tabPanels.Length > 0)
        {
            SelectTab(0);
        }
    }
    
    private void SetupCloseButton()
    {
        if (closeButton)
        {
            closeButton.onClick.AddListener((() => Hide(true)));
        }
    }
    
    public void Show(bool animate = true)
    {
        if (_canvasSequence.isAlive) 
            _canvasSequence.Stop();

        if (animate)
        {
            _canvasSequence = Sequence.Create()
                .Group(Tween.Alpha(screenCanvas, 1f, 0.3f))
                .OnComplete(() =>
                {
                    screenCanvas.interactable = true;
                    screenCanvas.blocksRaycasts = true;
                });
        }
        else
        {
            screenCanvas.alpha = 1f;
            screenCanvas.interactable = true;
            screenCanvas.blocksRaycasts = true;
        }
        
        OnMenuScreenOpened?.Invoke();
    }
    
    public void Hide(bool animate = true)
    {
        if (_canvasSequence.isAlive) 
            _canvasSequence.Stop();

        if (animate)
        {
            _canvasSequence = Sequence.Create()
                .Group(Tween.Alpha(screenCanvas, 0f, 0.3f))
                .OnComplete(() =>
                {
                    screenCanvas.interactable = false;
                    screenCanvas.blocksRaycasts = false;
                });
        }
        else
        {
            screenCanvas.alpha = 0f;
            screenCanvas.interactable = false;
            screenCanvas.blocksRaycasts = false;
        }
        
        OnMenuScreenClosed?.Invoke();
    }
    
    
    private void SelectTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabPanels.Length) return;
        
        
        _currentTabIndex = tabIndex; 
        
        for (int i = 0; i < tabPanels.Length; i++)
        {
            bool isActive = i == _currentTabIndex;
            
            tabPanels[i].alpha = isActive ? 1f : 0f;
            tabPanels[i].interactable = isActive;
            tabPanels[i].blocksRaycasts = isActive;
            
            tabButtons[i].interactable = !isActive;

            if (!tabButtons[i].interactable)
            {
                tabButtons[i].GetComponent<SelectableAnimator>().Deselect();
            }
            
        }
        
        OnTabSelected?.Invoke(tabPanels[_currentTabIndex]);
    }
    

}
