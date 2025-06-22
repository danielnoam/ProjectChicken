using CustomAttribute;
using UnityEngine;
using UnityEngine.SceneManagement;


[CreateAssetMenu(fileName = "New Level", menuName = "Scriptable Objects/New Level")]
public class SOLevel : ScriptableObject
{
    [Header("Level Settings")]
    [SerializeField] private string levelName;
    [SerializeField, Multiline(3)] private string levelDescription;
    [SerializeField] private GameObject levelGfxPrefab;
    [SerializeField] private SceneField levelScene;


    public string LevelName => levelName;
    public string LevelDescription => levelDescription;

    
    
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
}
