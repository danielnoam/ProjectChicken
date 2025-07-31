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
    [SerializeField] private GameObject levelGfxPrefab;
    [SerializeField] private SceneField levelScene;
    [SerializeField] private SOVFEffectsSequence loadVFXSequence;


    
    [Header("Level Unlock")]
    [SerializeField] private List<SOLevel> levelsToComplete = new List<SOLevel>();
    
    public string LevelName => levelName;
    public string LevelDescription => levelDescription;
    public LevelDifficulty LevelDifficulty => levelDifficulty;
    public List<SOLevel> LevelsToComplete => levelsToComplete;
    
    
    public void LoadLevel()
    {
        TransitionManager.TransitionToScene(levelScene, loadVFXSequence);
        // levelScene?.LoadScene();
    }
    
    public GameObject SetUpGfx(Transform parent)
    {
        if (!levelGfxPrefab) return null;
        
        GameObject levelGfx = Instantiate(levelGfxPrefab, parent);
        
        levelGfx.SetActive(false);
        
        return levelGfx;
    }
    
    public string GetScenePath()
    {
        return levelScene.ScenePath;
    }
}
