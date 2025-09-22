using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class OptionsScreen : MonoBehaviour
{
    [Header("Options Screen")]
    [SerializeField] private CanvasGroup optionsCanvas;
    [SerializeField] private Button closeButton;
    
    [Header("Tabs")]
    [SerializeField] private Button[] tabButtons = Array.Empty<Button>();
    [SerializeField] private CanvasGroup[] tabPanels = Array.Empty<CanvasGroup>();
    
    private Sequence _optionsCanvasSequence;
    private int _currentTabIndex;
    
    public event Action OnOptionsOpened;
    public event Action OnOptionsClosed;
    
    public bool IsVisible => optionsCanvas.alpha > 0.5f;
    
    private void Awake()
    {
        SetupTabs();
        SetupCloseButton();
        Hide(false); 
    }
    
    private void SetupTabs()
    {
        // Ensure arrays match in length
        if (tabButtons.Length != tabPanels.Length)
        {
            Debug.LogError("Tab buttons and panels arrays must have the same length!");
            return;
        }
        
        // Setup tab buttons
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int tabIndex = i; // Capture for closure
            tabButtons[i].onClick.AddListener(() => SelectTab(tabIndex));
        }
        
        // Initialize first tab as active
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
        if (_optionsCanvasSequence.isAlive) 
            _optionsCanvasSequence.Stop();

        if (animate)
        {
            _optionsCanvasSequence = Sequence.Create()
                .Group(Tween.Alpha(optionsCanvas, 1f, 0.3f))
                .OnComplete(() =>
                {
                    optionsCanvas.interactable = true;
                    optionsCanvas.blocksRaycasts = true;
                });
        }
        else
        {
            optionsCanvas.alpha = 1f;
            optionsCanvas.interactable = true;
            optionsCanvas.blocksRaycasts = true;
        }
        
        OnOptionsOpened?.Invoke();
    }
    
    public void Hide(bool animate = true)
    {
        if (_optionsCanvasSequence.isAlive) 
            _optionsCanvasSequence.Stop();

        if (animate)
        {
            _optionsCanvasSequence = Sequence.Create()
                .Group(Tween.Alpha(optionsCanvas, 0f, 0.3f))
                .OnComplete(() =>
                {
                    optionsCanvas.interactable = false;
                    optionsCanvas.blocksRaycasts = false;
                });
        }
        else
        {
            optionsCanvas.alpha = 0f;
            optionsCanvas.interactable = false;
            optionsCanvas.blocksRaycasts = false;
        }
        
        OnOptionsClosed?.Invoke();
    }
    
    
    private void SelectTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabPanels.Length) return;
        
        _currentTabIndex = tabIndex;
        UpdateTabStates();
    }
    
    private void UpdateTabStates()
    {
        for (int i = 0; i < tabPanels.Length; i++)
        {
            bool isActive = i == _currentTabIndex;
            
            tabPanels[i].alpha = isActive ? 1f : 0f;
            tabPanels[i].interactable = isActive;
            tabPanels[i].blocksRaycasts = isActive;
            
            tabButtons[i].interactable = !isActive;
        }
    }
}
