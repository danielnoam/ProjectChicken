using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LevelUIData 
{
    public SOLevel soLevel;
    public GameObject levelGfx;
    public Button levelButton;
    public int bestScore;
    public bool isUnlocked;
    public bool isCompleted;
    
    public LevelUIData (SOLevel level, GameObject gfx, Button button, bool completed = false, bool unlocked  = true, int bestScore = 0)
    {
        soLevel = level;
        levelGfx = gfx;
        levelButton = button;
        isCompleted = completed;
        isUnlocked = unlocked;
        this.bestScore = bestScore;
    }
}