using System;
using DNExtensions;
using DNExtensions.VFXManager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using VInspector;

public class CinematicManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] protected bool hideCursor = true;
    
    
    [Header("References")]
    [SerializeField] protected SceneField targetScene;
    [SerializeField] protected CinematicInput cinematicInput;
    [SerializeField] protected PlayableDirector playableDirector;
    [SerializeField] protected SOVFEffectsSequence awakeSequence;
    [SerializeField] protected SOVFEffectsSequence endSequence;
    [SerializeField] protected SOVFEffectsSequence targetSceneStartSequence;

    private void Awake()
    {
        if (hideCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
        }
        
        if (IsThereCinematic()) VFXManager.Instance?.PlayVFX(awakeSequence);
    }

    private void Start()
    {
        StartCinematic();
    }

    private void OnEnable()
    {
        if (playableDirector)
        {
            playableDirector.stopped += OnTimelineStopped;
        }

        if (cinematicInput)
        {
            cinematicInput.OnSkipActionEvent += OnSkipAction;
        }
    }

    private void OnDisable()
    {
        if (playableDirector)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }
        
        if (cinematicInput)
        {
            cinematicInput.OnSkipActionEvent -= OnSkipAction;
        }
    }
    

    private void OnTimelineStopped(PlayableDirector director)
    {
        OnCinematicComplete();
    }

    private void OnSkipAction(InputAction.CallbackContext callbackContext)
    {
        if (IsCinematicPlaying())
        {
            StopCinematic();
        }
        else
        {
            LoadTargetScene();
        }
       
    }
    
    private void OnCinematicComplete()
    {
        LoadTargetScene();
    }
    
    private void LoadTargetScene()
    {
        if (targetScene == null) return;
        TransitionManager.TransitionToScene(targetScene, endSequence, targetSceneStartSequence);
    }
    

    [Button]
    private void StartCinematic()
    {
        playableDirector?.Play();
    }

    [Button]
    private void StopCinematic()
    {
        playableDirector?.Stop();
    }
    
    
    private bool IsCinematicPlaying()
    {
        return playableDirector && playableDirector.state == PlayState.Playing;
    }

    private bool IsThereCinematic()
    {
        return playableDirector && playableDirector.playableAsset;
    }
    
}