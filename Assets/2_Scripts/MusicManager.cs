using System;
using DNExtensions;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{

    public static MusicManager Instance { get; private set; }
    
    
    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.25f;
    
    [Header("References")]
    [SerializeField] private SceneField mainMenuScene;
    [SerializeField] private SceneField introScene;
    [SerializeField] private SceneField creditsScene;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource audioSource2;
    
    [Separator]
    [SerializeField, ReadOnly] private AudioSource currentAudioSource;
    [SerializeField, ReadOnly] private LevelManager levelManager;
    private Sequence _audioSourceSequence;

    private bool IsPlaying => currentAudioSource && currentAudioSource.isPlaying;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        if (!levelManager)
        {
            FindLevelManager();
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        
        if (levelManager)
        {
            levelManager.OnLevelSet -= OnLevelSet;
        }
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        
    }


    private void OnSceneUnloaded(Scene scene)
    {
        if (levelManager)
        {
            levelManager.OnLevelSet -= OnLevelSet;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        
        if (IsSceneNotLevelScene(scene))
        {
            if (IsPlaying) StopMusic();
        }
        else
        {
            FindLevelManager();
        }
    }

    private void OnLevelSet(SOLevel level)
    {
        if (!level) return;
        
        PlayMusic(level.LevelTheme);
    }
    
    


    private void PlayMusic(AudioClip clip)
    {
        if (!clip) return;
        
        if (_audioSourceSequence.isAlive) _audioSourceSequence.Complete();
        
        _audioSourceSequence = Sequence.Create();
        if (IsPlaying)
        {
            _audioSourceSequence.Group(FadeAudioSource(currentAudioSource, 0));
        }
        
        var audioSourceToUse = currentAudioSource == audioSource ? audioSource2 : audioSource;
        audioSourceToUse.clip = clip;
        currentAudioSource = audioSourceToUse;
        _audioSourceSequence.Group(FadeAudioSource(audioSourceToUse, 1));
    }
    
    private void StopMusic()
    {
        if (_audioSourceSequence.isAlive) _audioSourceSequence.Complete();


        if (IsPlaying)
        {
            _audioSourceSequence = Sequence.Create();
            _audioSourceSequence.Group(FadeAudioSource(currentAudioSource, 0));
        }

        currentAudioSource = null;
    }
    
    private Sequence FadeAudioSource(AudioSource audioSource, float targetVolume)
    {
        return Sequence.Create(Tween.AudioVolume(audioSource,targetVolume, fadeDuration, useUnscaledTime: false));
    }

    private void FindLevelManager()
    {
        levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager)
        {
            levelManager.OnLevelSet += OnLevelSet;
        }
    }

    private bool IsSceneNotLevelScene(Scene scene)
    {
        return scene.name == mainMenuScene.SceneName || scene.name == introScene.SceneName || scene.name == creditsScene.SceneName;
    }


}
