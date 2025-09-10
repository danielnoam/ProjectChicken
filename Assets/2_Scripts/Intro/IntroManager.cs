using System;
using System.Linq;
using DNExtensions;
using DNExtensions.VFXManager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using VInspector;


public class IntroManager : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private SceneField mainMenuScene;
    [SerializeField] private SOVFEffectsSequence awakeSequence;
    [SerializeField] private SOVFEffectsSequence endSequence;
    [SerializeField] private SOVFEffectsSequence mainMenuStartSequence;

    

    private void Start()
    {
        VFXManager.Instance?.PlayVFX(awakeSequence);
        PlayIntro();
    }

    private void OnEnable()
    {
        if (playableDirector)
        {
            playableDirector.stopped += OnTimelineStopped;
        }
    }

    private void OnDisable()
    {
        if (playableDirector)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }
    }
    

    private void OnTimelineStopped(PlayableDirector director)
    {
        LoadMainMenuScene();
    }

    private void Update()
    {
        if (playableDirector.state != PlayState.Playing) return;
        
        var keyboard = InputSystem.GetDevice<Keyboard>();
        var gamepad = InputSystem.GetDevice<Gamepad>();
    
        var keyboardKeys = keyboard != null && (keyboard.escapeKey.isPressed || keyboard.enterKey.isPressed || keyboard.spaceKey.isPressed);
        var gamepadButtons = gamepad != null && (gamepad.buttonSouth.isPressed || gamepad.selectButton.isPressed);
    
        if (keyboardKeys || gamepadButtons)
        {
            StopIntro();
        }
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

    [Button]
    private void ToggleIntro()
    {
        if (playableDirector.state == PlayState.Playing)
        {
            playableDirector.Pause();
        }
        else
        {
            playableDirector.Play();
        }
    }
    

    private void LoadMainMenuScene()
    {
        TransitionManager.TransitionToScene(mainMenuScene, endSequence, mainMenuStartSequence);
    }
}
