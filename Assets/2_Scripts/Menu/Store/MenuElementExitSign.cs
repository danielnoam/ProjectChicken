using DNExtensions;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;

public class MenuElementExitSign : MenuElement
{
    
    [Header("Sign Settings")]
    [SerializeField] private float delayBeforeLoad = 1.5f;
    [SerializeField] private SceneField sceneToLoad;
    


    private Sequence loadSceneSequence;
    
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
        
        if (loadSceneSequence.isAlive) loadSceneSequence.Stop();
        loadSceneSequence = Sequence.Create()
            .ChainDelay(delayBeforeLoad)
            .OnComplete(FinishedInteraction);
    }

    protected override void OnFinishedInteraction()
    {
        SceneManager.LoadScene(sceneToLoad.SceneName);
    }
    
    protected override void OnStopInteraction()
    {
        if (loadSceneSequence.isAlive) loadSceneSequence.Stop();
    }
    
}
