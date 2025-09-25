
using DNExtensions;
using DNExtensions.VFXManager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using VInspector;


public class IntroManager : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private CinematicInput cinematicInput;
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private SceneField mainMenuScene;
    [SerializeField] private SOVFEffectsSequence awakeSequence;
    [SerializeField] private SOVFEffectsSequence endSequence;
    [SerializeField] private SOVFEffectsSequence mainMenuStartSequence;

    

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        VFXManager.Instance?.PlayVFX(awakeSequence);
        PlayIntro();
    }

    private void OnEnable()
    {
        if (playableDirector)
        {
            playableDirector.stopped += OnTimelineStopped;
        }

        cinematicInput.OnSkipActionEvent += OnSkipAction;
    }

    private void OnDisable()
    {
        if (playableDirector)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }
        
        
        cinematicInput.OnSkipActionEvent -= OnSkipAction;
        
    }
    

    private void OnTimelineStopped(PlayableDirector director)
    {
        LoadMainMenuScene();
    }
    
    
    private void OnSkipAction(InputAction.CallbackContext callbackContext)
    {
        Debug.Log("Skip");
        StopIntro();
    }

    [Button]
    private void PlayIntro()
    {
        playableDirector.Play();
    }

    [Button]
    private void StopIntro()
    {
        playableDirector.Stop();

    }
    

    private void LoadMainMenuScene()
    {
        TransitionManager.TransitionToScene(mainMenuScene, endSequence, mainMenuStartSequence);
    }
}
