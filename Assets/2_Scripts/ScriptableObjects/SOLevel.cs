using System.Collections.Generic;
using DNExtensions;
using DNExtensions.VFXManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;


[CreateAssetMenu(fileName = "New Level", menuName = "Scriptable Objects/New Level")]
public class SOLevel : ScriptableObject
{
    [Header("Level Settings")]
    [SerializeField] private string levelName;
    [SerializeField] private LevelDifficulty levelDifficulty;
    [SerializeField, Multiline(3)] private string levelDescription;
    [SerializeField] private SceneField levelScene;
    [SerializeField] private GameObject levelGfxPrefab;
    
    
    [Header("Level Unlock")]
    [SerializeField] private List<SOLevel> levelsToComplete = new List<SOLevel>();
    
    
    [Header("Effects")]
    [SerializeField] private SOVFEffectsSequence loadVFXSequence;
    [SerializeField] private SOVFEffectsSequence introVFXSequence;
    [SerializeField] private SOVFEffectsSequence outroVFXSequence;


    

    
    public string LevelName => levelName;
    public string LevelDescription => levelDescription;
    public GameObject LevelGfxPrefab => levelGfxPrefab;
    public LevelDifficulty LevelDifficulty => levelDifficulty;
    public List<SOLevel> LevelsToComplete => levelsToComplete;
    public SOVFEffectsSequence IntroVFXSequence => introVFXSequence;
    public SOVFEffectsSequence OutroVFXSequence => outroVFXSequence;
    
    
    public void LoadLevel()
    {
        TransitionManager.TransitionToScene(levelScene, loadVFXSequence);
    }
    
    public string GetScenePath()
    {
        return levelScene.ScenePath;
    }
}
