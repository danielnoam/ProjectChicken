using System.Collections.Generic;
using CustomAttribute;
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

    
    [Header("Level Unlock")]
    [SerializeField] private bool isLocked;
    [SerializeField, ShowIf("isLocked")] private List<SOLevel> levelsToComplete = new List<SOLevel>();
    
    public string LevelName => levelName;
    public string LevelDescription => levelDescription;
    public LevelDifficulty LevelDifficulty => levelDifficulty;
    public bool IsLocked => isLocked;
    public List<SOLevel> LevelsToComplete => levelsToComplete;
    
    
    public void LoadLevel()
    {
        if (levelScene == null) return;
        
        SceneManager.LoadScene(levelScene.BuildIndex);
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
