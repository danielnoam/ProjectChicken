
using DNExtensions;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuElementShopSign : MenuElement
{
    
    [Header("Shop Settings")]
    [SerializeField] private float delayBeforeLoad = 1.5f;
    [SerializeField] private SceneField sceneToLoad;
    
    private Sequence _loadSceneSequence;
    
    
    protected override void OnSelected()
    {

    }

    protected override void OnDeselected()
    {

    }

    protected override void OnSetUp()
    {

    }

    protected override void OnInteract()
    {
        if (sceneToLoad == null) return;
        
        if (_loadSceneSequence.isAlive) _loadSceneSequence.Stop();
        _loadSceneSequence = Sequence.Create()
                .ChainDelay(delayBeforeLoad)
                .OnComplete(FinishedInteraction);
    }

    protected override void OnFinishedInteraction()
    {
        SceneManager.LoadScene(sceneToLoad.SceneName);
    }
    
    protected override void OnStopInteraction()
    {
        if (_loadSceneSequence.isAlive) _loadSceneSequence.Stop();
    }
    
}
