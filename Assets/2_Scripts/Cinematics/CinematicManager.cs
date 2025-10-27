using System;
using DNExtensions;
using DNExtensions.VFXManager;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using VInspector;

public class CinematicManager : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField] protected float audioSourceVolumeTweenDuration = 3f;
    [SerializeField] protected bool hideCursor = true;
    
    [Header("References")]
    [SerializeField] protected SceneField targetScene;
    [SerializeField] protected CinematicInput cinematicInput;
    [SerializeField] protected PlayableDirector playableDirector;
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected SOVFEffectsSequence awakeSequence;
    [SerializeField] protected SOVFEffectsSequence endSequence;
    [SerializeField] protected SOVFEffectsSequence targetSceneStartSequence;
    
    
    protected Tween audioSourceVolumeTween;
    protected bool skippingCinematic;
    protected float audioSourceVolume;

    protected void Awake()
    {

        if (audioSource)
        {
            audioSourceVolume = audioSource.volume;
            audioSource.volume = 0;
        }
    }

    protected virtual void Start()
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
    
    
    protected virtual void OnCinematicComplete()
    {
        LoadTargetScene();
    }

    protected virtual void OnTimelineStopped(PlayableDirector director)
    {
        OnCinematicComplete();
    }

    private void OnSkipAction(InputAction.CallbackContext callbackContext)
    {
        if (skippingCinematic) return;
        skippingCinematic = true;
        if (IsCinematicPlaying())
        {
            StopCinematic();
        }
        else
        {
            LoadTargetScene();
        }
       
    }
    

    
    protected void LoadTargetScene(bool fullTransition = true)
    {
        if (targetScene == null) return;

        if (fullTransition)
        {
            TransitionManager.TransitionToScene(targetScene, endSequence, targetSceneStartSequence);
        }
        else
        {
            TransitionManager.TransitionToScene(targetScene, null, targetSceneStartSequence);
        }

    }
    

    [Button]
    protected void StartCinematic()
    {
        if (IsThereCinematic()) VFXManager.Instance?.PlayVFX(awakeSequence);
        
        if (hideCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
        }
        
        playableDirector.Play();
        
        if (audioSource)
        {
            audioSourceVolumeTween = Tween.AudioVolume(audioSource, audioSourceVolume, audioSourceVolumeTweenDuration);
        }
        
    }

    [Button]
    protected void StopCinematic()
    {
        playableDirector?.Stop();
        
        if (audioSource)
        {
            audioSourceVolumeTween = Tween.AudioVolume(audioSource, 0, audioSourceVolumeTweenDuration);
        }
        
    }
    
    
    protected bool IsCinematicPlaying()
    {
        return playableDirector && playableDirector.state == PlayState.Playing;
    }

    protected bool IsThereCinematic()
    {
        return playableDirector && playableDirector.playableAsset;
    }
    
}