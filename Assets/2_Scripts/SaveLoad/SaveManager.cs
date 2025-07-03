using System;
using System.IO;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;

[DefaultExecutionOrder(-1)]
[SelectionBase]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    private static SettingsData _settingsData;
    private static string _settingsDataPath;
    private static PlayerProgressData _playerProgressData;
    private static string _playerProgressDataPath;
    private static bool _initialized;

    
    public event Action OnSettingsDataChanged;
    
    
    [Header("Default Settings")]
    [SerializeField] private ControlSchemeSettings defaultKeyboardMouseScheme = new (
        false, 
        false,
        0.1f, 
        0.3f, 
        AnimationCurve.Linear(0, 0, 1, 1),
        true,
        4f,
        3f,
        0.5f,
        0.5f,
        true,
        false,
        0.3f);
    
    [SerializeField] private ControlSchemeSettings defaultGamepadScheme = new (
        false,
        false, 
        2f, 
        0.3f,
        AnimationCurve.Linear(0, 0, 1, 1),
        true, 
        4f,
        3f, 
        0.5f, 
        0.5f, 
        true, 
        false, 
        0.3f);
    
    

    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        Initialize();
        
        PrimeTweenConfig.warnEndValueEqualsCurrent = false;
        
        // Delete it for now so we reset to default settings
        DeleteSettingsDataAndFile();
    }
    
    
    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }
    
    private static void Initialize()
    {
        if (_initialized) return;
        
        _playerProgressDataPath = Path.Combine(Application.persistentDataPath, "PlayerProgress.json");
        LoadPlayerProgressDataFromFile();
        
        _settingsDataPath = Path.Combine(Application.persistentDataPath, "Settings.json");
        LoadSettingsDataFromFile();
        _initialized = true;


        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        
        #if UNITY_EDITOR
        // Subscribe to play mode state changes to uninitialize when exiting play mode
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        #endif
    }

    
    private static void OnActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
    {

        if (previousActiveScene.buildIndex == -1) return;
        SaveAllDataToFiles();
        Debug.Log("bla");
    }
    
    
    
    private void OnApplicationQuit()
    {
        SaveAllDataToFiles();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && Application.isMobilePlatform)
        {
            SaveAllDataToFiles();
        }
    }


    #region File Handling ----------------------------------------------------------------------------------------------------------------------------
    
    private static void SaveAllDataToFiles()
    {
        SavePlayerProgressDataToFile();
        SaveSettingsDataToFile();
    }

    [Button]
    private static void SavePlayerProgressDataToFile()
    {
        if (!Application.isPlaying || !_initialized) return;
        
        try
        {
            string jsonData = JsonUtility.ToJson(_playerProgressData, true);
            File.WriteAllText(_playerProgressDataPath, jsonData);
            Debug.Log("Player progress saved successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }
    


    private static void LoadPlayerProgressDataFromFile()
    {
        try
        {
            if (File.Exists(_playerProgressDataPath))
            {
                string jsonData = File.ReadAllText(_playerProgressDataPath);
                _playerProgressData = JsonUtility.FromJson<PlayerProgressData>(jsonData);
                // Debug.Log("Game loaded successfully!");
            }
            else
            {
                _playerProgressData = new PlayerProgressData();
                Debug.Log("No player progress file found. Created new player progress data.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            _playerProgressData = new PlayerProgressData();
        }
    }
    

    
    [Button]
    private static void DeletePlayerProgressDataAndFile()
    {
        if (!Application.isPlaying || !_initialized) return;
        
        try
        {
            if (File.Exists(_playerProgressDataPath))
            {
                File.Delete(_playerProgressDataPath);
                _playerProgressData = new PlayerProgressData();
                Debug.Log("Player progress file deleted and reset to default.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete player progress file: {e.Message}");
        }
    }
    
    
    
    [Button]
    private static void SaveSettingsDataToFile()
    {
        if (!Application.isPlaying || !_initialized) return;

        try
        {
            string jsonData = JsonUtility.ToJson(_settingsData, true);
            File.WriteAllText(_settingsDataPath, jsonData);
            Debug.Log("Settings saved successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save settings: {e.Message}");
        }
        
    }

    
    private static void LoadSettingsDataFromFile()
    {
        try
        {
            if (File.Exists(_settingsDataPath))
            {
                string jsonData = File.ReadAllText(_settingsDataPath);
                _settingsData = JsonUtility.FromJson<SettingsData>(jsonData);
            }
            else
            {
                // Access through the instance
                _settingsData = new SettingsData(
                    Instance.defaultKeyboardMouseScheme, 
                    Instance.defaultGamepadScheme
                );
                Debug.Log("No settings file found. Created new settings data.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load settings: {e.Message}");
            _settingsData = new SettingsData(
                Instance.defaultKeyboardMouseScheme, 
                Instance.defaultGamepadScheme
            );
        }
        
        Instance?.OnSettingsDataChanged?.Invoke();
    }


    [Button]
    private static void DeleteSettingsDataAndFile()
    {
        if (!Application.isPlaying || !_initialized) return;

        try
        {
            if (File.Exists(_settingsDataPath))
            {
                File.Delete(_settingsDataPath);
                _settingsData = new SettingsData(
                    Instance.defaultKeyboardMouseScheme, 
                    Instance.defaultGamepadScheme
                );
                Debug.Log("Settings file deleted and reset to default.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete settings file: {e.Message}");
        }
        
        Instance?.OnSettingsDataChanged?.Invoke();
    }

    #endregion File Handling ----------------------------------------------------------------------------------------------------------------------------
    
    
    #region Data Update Methods ----------------------------------------------------------------------------------------------------------------------
    
    public static void UpdateLevelProgress(string scenePath, int score, bool completed = true)
    {
        EnsureInitialized();
        
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning("Scene path is empty, cannot save level progress!");
            return;
        }
        
        LevelProgress progress = GetLevelProgress(scenePath);
        if (progress != null)
        {
            progress.isCompleted = completed;
            
            // Add the new score
            progress.bestScores.Add(score);
        
            // Sort in descending order (highest scores first)
            progress.bestScores.Sort((a, b) => b.CompareTo(a));
        
            // Keep only the top scores
            if (progress.bestScores.Count > 5)
            {
                progress.bestScores.RemoveRange(5, progress.bestScores.Count - 5);
            }
        }
    }
    

    public static void UpdatePlayerProgress(int currency)
    {
        EnsureInitialized();
        _playerProgressData.currency = currency;
    }

    public static void  UpdateKeyboardControlScheme(ControlSchemeSettings newSettings)
    {
        EnsureInitialized();
        _settingsData.keyboardMouseScheme = newSettings;
    }
    
    public static void UpdateGamepadControlScheme(ControlSchemeSettings newSettings)
    {
        EnsureInitialized();
        _settingsData.gamepadScheme = newSettings;
    }
    
    #endregion Public Update Methods ----------------------------------------------------------------------------------------------------------------------
    
    
    #region Data Getters ----------------------------------------------------------------------------------------------------------------------
    
    public static LevelProgress GetLevelProgress(string scenePath)
    {
        EnsureInitialized();
        
        if (string.IsNullOrEmpty(scenePath)) return null;
        
        LevelProgress progress = _playerProgressData.levelProgresses.Find(p => p.scenePath == scenePath);
        
        if (progress == null)
        {
            progress = new LevelProgress(scenePath);
            _playerProgressData.levelProgresses.Add(progress);
        }
        
        return progress;
    }
    
    
    public static int GetCurrency()
    {
        EnsureInitialized();
        
        return _playerProgressData?.currency ?? 0;
    }
    
    public static ControlSchemeSettings GetKeyboardControlScheme()
    {
        EnsureInitialized();

        return _settingsData.keyboardMouseScheme;
    }
    
    public static ControlSchemeSettings GetGamepadControlScheme()
    {
        EnsureInitialized();

        return _settingsData.gamepadScheme;
    }
    
    #endregion Progress Getters ----------------------------------------------------------------------------------------------------------------------
    
    
#if UNITY_EDITOR
    #region Editor --------------------------------------------------------------------------------------------------------------------------

    // Force uninitialize in editor when exiting play mode
    private static void ForceUninitialize()
    {
        _initialized = false;
        _playerProgressData = null;
        _playerProgressDataPath = null;
        _settingsData = null;
        _settingsDataPath = null;
    }
    
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ForceUninitialize();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
    }

    #endregion Editor --------------------------------------------------------------------------------------------------------------------------
#endif



    
}