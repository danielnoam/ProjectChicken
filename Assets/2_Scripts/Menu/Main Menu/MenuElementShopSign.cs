
using DNExtensions;
using DNExtensions.VFXManager;
using PrimeTween;
using UnityEngine;

public class MenuElementShopSign : MenuElement
{
    
    [Header("Shop Settings")]
    [SerializeField] private float delayBeforeLoad = 1.5f;
    [SerializeField] private SceneField sceneToLoad;
    [SerializeField] private SOVFEffectsSequence introSequence;
    
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
                .ChainCallback(() => {controllerVibrationSource.Vibrate(0.05f,0, delayBeforeLoad);})
                .ChainDelay(delayBeforeLoad)
                .OnComplete(FinishedInteraction);
    }

    protected override void OnFinishedInteraction()
    {
        sceneToLoad.LoadScene();
    }
    
    protected override void OnStopInteraction()
    {
        if (_loadSceneSequence.isAlive) _loadSceneSequence.Stop();
    }
    
}
