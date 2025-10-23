using UnityEngine;
using UnityEngine.UI;

public class SkipIndicatorController : MonoBehaviour
{
    [SerializeField] private UIElementAlphaEffector alphaEffector;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private LevelManager levelManager;
    
    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnCanSkipStage += HandleCanSkipChanged;
        }
    }
    
    private void OnDisable()
    {
        if (levelManager)
        {
            levelManager.OnCanSkipStage -= HandleCanSkipChanged;
        }
    }
    
    private void Awake()
    {
        canvasGroup.alpha = 0f;
    }
    
    private void HandleCanSkipChanged(bool canSkip)
    {
        if (canSkip)
        {
            canvasGroup.alpha = 1f;
            alphaEffector?.StartEffect();
        }
        else
        {
            canvasGroup.alpha = 0f;
        }
    }

}