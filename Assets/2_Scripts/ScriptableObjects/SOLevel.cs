using System;
using DNExtensions;
using DNExtensions.VFXManager;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Scriptable Objects/New Level")]
public class SOLevel : ScriptableObject
{
    [Header("Settings")]
    [SerializeField] private string levelName;
    [SerializeField] private LevelDifficulty levelDifficulty;
    [SerializeField] private string levelDescription;
    [SerializeField] private SOLevelStage[] levelStages = Array.Empty<SOLevelStage>();
    [SerializeField] private SOLevel[] levelsNeededToUnlock = Array.Empty<SOLevel>();
    
    [Header("References")]
    [SerializeField] private SceneField levelScene;
    [SerializeField] private GameObject levelGfxPrefab;
    [SerializeField] private SOVFEffectsSequence loadVFXSequence;
    

    
    public string LevelName => levelName;
    public string LevelDescription => levelDescription;
    public GameObject LevelGfxPrefab => levelGfxPrefab;
    public LevelDifficulty LevelDifficulty => levelDifficulty;
    public SOLevel[] LevelsNeededToUnlock => levelsNeededToUnlock;
    public SOLevelStage[] LevelStages => levelStages;
    
    
    public void LoadLevel()
    {
        TransitionManager.TransitionToScene(levelScene, loadVFXSequence);
    }
    
    public string GetScenePath()
    {
        return levelScene.ScenePath;
    }
}
