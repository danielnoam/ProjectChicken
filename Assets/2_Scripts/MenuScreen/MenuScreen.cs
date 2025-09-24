using System;
using DNExtensions;
using DNExtensions.MenuSystem;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class MenuScreen : MonoBehaviour
{
    [Header("Screen")]
    [SerializeField] private CanvasGroup optionsCanvas;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup[] tabPanels = Array.Empty<CanvasGroup>();
    [SerializeField] private Button[] tabButtons = Array.Empty<Button>();
    
    [Header("References")]
    [SerializeField] private SceneField introScene;
    [SerializeField] private SceneField creditsScene;
    
    private Sequence _optionsCanvasSequence;
    private int _currentTabIndex;
    
    public event Action OnMenuScreenOpened;
    public event Action OnMenuScreenClosed;
    
    public bool IsVisible => optionsCanvas.alpha > 0.5f;
    
    private void Start()
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
        
        OnMenuScreenOpened?.Invoke();
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
        
        OnMenuScreenClosed?.Invoke();
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

            if (!tabButtons[i].interactable)
            {
                tabButtons[i].GetComponent<SelectableAnimator>().Deselect();
            }
        }
    }
    
    public void LoadIntroScene()
    {
        introScene?.LoadScene();
    }
    
    public void LoadCreditsScene()
    {
        creditsScene?.LoadScene();
    }
}
