using System;
using System.Collections.Generic;
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
    private static RunProgressData _runProgressData;
    private static string _runProgressDataPath;
    private static bool _initialized;
    private static int _sceneChangeCount;
    
    public event Action OnSettingsDataChanged;
    
    
    [Header("References")]
    [SerializeField] private SOPlayerStats playerStats;
    
    [Header("Default Settings")]
    [SerializeField] private VolumeSettings defaultVolumeSettings = new (1,1,1);
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
        _sceneChangeCount = 0;
        
        _runProgressDataPath = Path.Combine(Application.persistentDataPath, "RunProgress.json");
        LoadRunProgressDataFromFile();


        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        
        #if UNITY_EDITOR
        // Subscribe to play mode state changes to uninitialize when exiting play mode
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        #endif
    }

    
    private static void OnActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
    {
        _sceneChangeCount++;
        
        if (_sceneChangeCount <= 1)
        {
            return;
        }
    
        SaveAllDataToFiles();
    }
    
    
    
    private void OnApplicationQuit()
    {
        SaveAllDataToFiles();
        ResetRunProgressData();
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


    private static void SavePlayerProgressDataToFile()
    {
        if (!Application.isPlaying || !_initialized) return;
        
        try
        {
            string jsonData = JsonUtility.ToJson(_playerProgressData, true);
            File.WriteAllText(_playerProgressDataPath, jsonData);
            // Debug.Log("Player progress saved successfully!");
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
                // Debug.Log("No player progress file found. Created new player progress data.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            _playerProgressData = new PlayerProgressData();
        }
    }
    

    
    [Button]
    private static void ResetPlayerProgressData()
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
    
    
    

    private static void SaveSettingsDataToFile()
    {
        if (!_initialized) return;

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
        
        
        Instance?.OnSettingsDataChanged?.Invoke();
        
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
                    Instance.defaultGamepadScheme,
                    Instance.defaultVolumeSettings
                );
                // Debug.Log("No settings file found. Created new settings data.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load settings: {e.Message}");
            _settingsData = new SettingsData(
                Instance.defaultKeyboardMouseScheme, 
                Instance.defaultGamepadScheme,
                Instance.defaultVolumeSettings
            );
        }
        
        Instance?.OnSettingsDataChanged?.Invoke();
    }


    [Button]
    private static void ResetSettingsData()
    {
        if (!_initialized) return;

        try
        {
            if (File.Exists(_settingsDataPath))
            {
                File.Delete(_settingsDataPath);
                _settingsData = new SettingsData(
                    Instance.defaultKeyboardMouseScheme, 
                    Instance.defaultGamepadScheme,
                    Instance.defaultVolumeSettings
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
    
    
    
    private static void SaveRunProgressDataToFile()
    {
        if (!_initialized) return;

        try
        {
            string jsonData = JsonUtility.ToJson(_runProgressData, true);
            File.WriteAllText(_runProgressDataPath, jsonData);
            Debug.Log("Run progress saved successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save run progress: {e.Message}");
        }
    }
    
    private static void LoadRunProgressDataFromFile()
    {
        try
        {
            if (File.Exists(_runProgressDataPath))
            {
                string jsonData = File.ReadAllText(_runProgressDataPath);
                _runProgressData = JsonUtility.FromJson<RunProgressData>(jsonData);
            }
            else
            {
                _runProgressData = new RunProgressData(Instance.playerStats.BaseHealth,0, new Dictionary<SOUpgradeBase, int>(), null);
                // Debug.Log("No run progress file found. Created new run progress data.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load run progress: {e.Message}");
            _runProgressData = new RunProgressData(Instance.playerStats.BaseHealth,0, new Dictionary<SOUpgradeBase, int>(), null);
        }
    }
    
    public static void ResetRunProgressData()
    {
        if (!_initialized) return;

        try
        {
            if (File.Exists(_runProgressDataPath))
            {
                File.Delete(_runProgressDataPath);
                _runProgressData = new RunProgressData(Instance.playerStats.BaseHealth,0, new Dictionary<SOUpgradeBase, int>(), null);
                Debug.Log("Run progress file deleted and reset to default.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete run progress file: {e.Message}");
        }
        
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
    
    
    
    public static void UpdateRunProgress(RunProgressData newRunProgress)
    {
        EnsureInitialized();
        _runProgressData = newRunProgress;
        SaveRunProgressDataToFile();
    }
    
    
    public static void  UpdateKeyboardControlScheme(ControlSchemeSettings newSettings)
    {
        EnsureInitialized();
        _settingsData.keyboardMouseScheme = newSettings;
        SaveSettingsDataToFile();
    }
    
    public static void UpdateGamepadControlScheme(ControlSchemeSettings newSettings)
    {
        EnsureInitialized();
        _settingsData.gamepadScheme = newSettings;
        SaveSettingsDataToFile();
    }

    public static void UpdateVolumeSettings(VolumeSettings volumeSettings)
    {
        EnsureInitialized();
        _settingsData.volumeSettings = volumeSettings;
        SaveSettingsDataToFile();
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
    
    public static PlayerProgressData GetPlayerProgressData()
    {
        EnsureInitialized();
        return _playerProgressData;
    }
    
    public static RunProgressData GetRunProgressData()
    {
        EnsureInitialized();
        return _runProgressData;
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
    
    public static VolumeSettings GetVolumeSettings()
    {
        EnsureInitialized();
        return _settingsData.volumeSettings;
    }
    
    #endregion Progress Getters ----------------------------------------------------------------------------------------------------------------------

    
    
#if UNITY_EDITOR
    #region Editor --------------------------------------------------------------------------------------------------------------------------

    
    [Button]
    private void OpenSaveFolder()
    {
        string saveFolder = Application.persistentDataPath;
        System.Diagnostics.Process.Start(saveFolder);
    }
    
    
    // Force uninitialize in editor when exiting play mode
    private static void ForceUninitialize()
    {
        _initialized = false;
        _sceneChangeCount = 0;
        _playerProgressData = null;
        _playerProgressDataPath = null;
        _settingsData = null;
        _settingsDataPath = null;
        
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
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