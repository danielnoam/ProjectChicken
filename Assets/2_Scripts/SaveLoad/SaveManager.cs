using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;


public static class SaveManager
{
    private static PlayerSaveData _playerData;
    private static string _playerDataPath;
    private static bool _initialized = false;
    
    

    #region SetUp ------------------------------------------------------------------------------------------------------------------------------------

    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }
    
    public static void Initialize()
    {
        if (_initialized) return;
        
        _playerDataPath = Path.Combine(Application.persistentDataPath, "PlayerSave.json");
        LoadPlayerData();
        _initialized = true;
        
        Debug.Log($"SaveManager initialized. Save path: {_playerDataPath}");
        
        #if UNITY_EDITOR
        // Subscribe to play mode state changes to uninitialize when exiting play mode
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        #endif
    }
    
    
    
    #if UNITY_EDITOR
    // Force uninitialize in editor when exiting play mode
    private static void ForceUninitialize()
    {
        _initialized = false;
        _playerData = null;
        _playerDataPath = null;
    }
    
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ForceUninitialize();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
    }
    #endif

    #endregion SetUp ------------------------------------------------------------------------------------------------------------------------------------
    
    
    #region Player Data Handling ----------------------------------------------------------------------------------------------------------------------------
    
    

    private static void SavePlayerData()
    {
        if (!_initialized) return;
        
        try
        {
            string jsonData = JsonUtility.ToJson(_playerData, true);
            File.WriteAllText(_playerDataPath, jsonData);
            Debug.Log("Game saved successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }
    

    private static void LoadPlayerData()
    {
        try
        {
            if (File.Exists(_playerDataPath))
            {
                string jsonData = File.ReadAllText(_playerDataPath);
                _playerData = JsonUtility.FromJson<PlayerSaveData>(jsonData);
                Debug.Log("Game loaded successfully!");
            }
            else
            {
                _playerData = new PlayerSaveData();
                Debug.Log("No save file found. Created new save data.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            _playerData = new PlayerSaveData();
        }
    }
    

    
    private static void DeletePlayerData()
    {
        if (!_initialized) return;
        
        try
        {
            if (File.Exists(_playerDataPath))
            {
                File.Delete(_playerDataPath);
                _playerData = new PlayerSaveData();
                Debug.Log("Save file deleted and reset to default.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save file: {e.Message}");
        }
    }
    
    

    private static void ResetAllProgress()
    {
        EnsureInitialized();
        _playerData = new PlayerSaveData();
        SavePlayerData();
        Debug.Log("All progress reset!");
    }
    

    
    #endregion Player Data Handling ----------------------------------------------------------------------------------------------------------------------------
    
    
    #region Progress Update Methods ----------------------------------------------------------------------------------------------------------------------
    
    
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

            
            SavePlayerData();
        }
    }
    

    public static void UpdatePlayerProgress(int currency)
    {
        EnsureInitialized();
        _playerData.currency = currency;
        SavePlayerData();
    }
    
    #endregion Public Update Methods ----------------------------------------------------------------------------------------------------------------------
    
    
    #region Progress Getters ----------------------------------------------------------------------------------------------------------------------
    
    public static LevelProgress GetLevelProgress(string scenePath)
    {
        EnsureInitialized();
        
        if (string.IsNullOrEmpty(scenePath)) return null;
        
        LevelProgress progress = _playerData.levelProgresses.Find(p => p.scenePath == scenePath);
        
        if (progress == null)
        {
            progress = new LevelProgress(scenePath);
            _playerData.levelProgresses.Add(progress);
        }
        
        return progress;
    }
    
    
    public static int GetCurrency()
    {
        EnsureInitialized();
        
        return _playerData?.currency ?? 0;
    }
    
    #endregion Progress Getters ----------------------------------------------------------------------------------------------------------------------
    
    
}