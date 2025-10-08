using System;
using DNExtensions;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;

public class MusicManager : MonoBehaviour
{

    public static MusicManager Instance { get; private set; }
    
    
    [Header("Settings")]
    [SerializeField, Range(0,1)] private float playVolume = 0.3f;
    [SerializeField, Range(0,1)] private float pausedVolume = 0.15f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private AudioClip mainMenuTheme;
    [SerializeField] private AudioClip[] testThemes = Array.Empty<AudioClip>();
    
    [Header("References")]
    [SerializeField] private SceneField mainMenuScene;
    [SerializeField] private SceneField introScene;
    [SerializeField] private SceneField creditsScene;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource audioSource2;
    [SerializeField] private TextMeshProUGUI musicText;
    
    [Separator]
    [SerializeField, DNExtensions.ReadOnly] private AudioSource currentAudioSource;
    [SerializeField, DNExtensions.ReadOnly] private AudioClip currentClip;
    [SerializeField, DNExtensions.ReadOnly] private LevelManager levelManager;
    private Sequence _audioSourceSequence;

    private bool IsPlaying => currentAudioSource && currentAudioSource.isPlaying;
    private bool IsPaused =>  LevelManager.Instance && LevelManager.Instance.IsGamePaused;


    private void Awake()
    {
        if (Instance && Instance != this)
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
            levelManager.OnPause -= OnPause;
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
        if (scene.name == mainMenuScene.SceneName)
        {
            PlayMusic(mainMenuTheme);
        }
        else if (IsSceneNotLevelScene(scene))
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
    
    
    private void OnPause(bool paused)
    {
        if (!IsPlaying) return;

        if (_audioSourceSequence.isAlive) _audioSourceSequence.Stop();
        _audioSourceSequence = Sequence.Create(useUnscaledTime: true);
        _audioSourceSequence.Group(FadeAudioSource(currentAudioSource, paused ? pausedVolume : playVolume));
    }
    
    

    [Button]
    private void PlayMusic(AudioClip clip)
    {
        if (!clip) return;
    
        if (_audioSourceSequence.isAlive) _audioSourceSequence.Stop();
        _audioSourceSequence = Sequence.Create(useUnscaledTime: true);
    
        var oldAudioSource = currentAudioSource;
        var audioSourceToUse = currentAudioSource == audioSource ? audioSource2 : audioSource;
    
        audioSourceToUse.clip = clip;
        currentClip = clip;
        if (musicText) musicText.text = clip.name;
        audioSourceToUse.volume = 0;
        audioSourceToUse.Play();
        currentAudioSource = audioSourceToUse;
    
        _audioSourceSequence.Group(FadeAudioSource(audioSourceToUse,  IsPaused ? pausedVolume : playVolume));
    
        if (oldAudioSource && oldAudioSource.isPlaying)
        {
            _audioSourceSequence.Group(FadeAudioSource(oldAudioSource, 0));
            _audioSourceSequence.ChainCallback(() => { 
                oldAudioSource.Stop();
                oldAudioSource.clip = null; 
            });
        }
    }
    
    [Button]
    private void StopMusic()
    {
        if (_audioSourceSequence.isAlive) _audioSourceSequence.Stop();


        if (IsPlaying)
        {
            _audioSourceSequence = Sequence.Create(useUnscaledTime: true);
            _audioSourceSequence.Group(FadeAudioSource(currentAudioSource, 0));
            _audioSourceSequence.ChainCallback(() => {
                currentAudioSource.Stop();
                currentAudioSource.clip = null;
                if (musicText) musicText.text = "";
                currentAudioSource = null;
                currentClip = null;
            });
        }
    }
    
    [Button]
    private void PlayTestMusic()
    {
        if (testThemes.Length == 0) return;
    
        int nextIndex = 0;
    
        if (currentAudioSource && currentAudioSource.isPlaying)
        {
            for (int i = 0; i < testThemes.Length; i++)
            {
                if (currentAudioSource.clip == testThemes[i])
                {
                    nextIndex = (i + 1) % testThemes.Length;
                    break;
                }
            }
        }
    
        PlayMusic(testThemes[nextIndex]);
    }
    
    private Sequence FadeAudioSource(AudioSource audioSource, float targetVolume)
    {
        return Sequence.Create(Tween.AudioVolume(audioSource,targetVolume, fadeDuration));
    }

    private void FindLevelManager()
    {
        levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager)
        {
            levelManager.OnLevelSet -= OnLevelSet;
            levelManager.OnPause -= OnPause;
            levelManager.OnLevelSet += OnLevelSet;
            levelManager.OnPause += OnPause;
        }
    }
    

    private bool IsSceneNotLevelScene(Scene scene)
    {
        return scene.name == mainMenuScene.SceneName || scene.name == introScene.SceneName || scene.name == creditsScene.SceneName;
    }


}
