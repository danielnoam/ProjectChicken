using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LevelUIData 
{
    public SOLevel soLevel;
    public GameObject levelGfx;
    public Button levelButton;
    public int bestScore;
    public bool isCompleted;
    
    public LevelUIData (SOLevel level, GameObject gfx, Button button, LevelProgress progress)
    {
        soLevel = level;
        levelGfx = gfx;
        levelButton = button;
        isCompleted = progress.isCompleted;
        bestScore = progress.GetTopScore();
    }
}