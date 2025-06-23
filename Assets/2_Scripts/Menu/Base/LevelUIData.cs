using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LevelUIData 
{
    public SOLevel soLevel;
    public GameObject levelGfx;
    public Button levelButton;
    
    public LevelUIData (SOLevel level, GameObject gfx, Button button)
    {
        soLevel = level;
        levelGfx = gfx;
        levelButton = button;
    }
}