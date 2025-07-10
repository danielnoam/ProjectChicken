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

        if (progress != null)
        {
            isCompleted = progress.isCompleted;
            bestScore = progress.GetTopScore();
        }
        else
        {
            isCompleted = false;
            bestScore = 0;
        }
    }
    
    
    public void UpdateLevelUIState()
    {
        if (!soLevel || !levelButton) return;
        

        // check if there are needed levels
        if (soLevel.IsLocked || soLevel.LevelsToComplete.Count == 0 || soLevel.LevelsToComplete == null) return;
        
        // check if all needed levels are completed
        foreach (var neededLevel in soLevel.LevelsToComplete)
        {
            var neededLevelProgress = SaveManager.GetLevelProgress(neededLevel.GetScenePath());
            if (neededLevelProgress == null || neededLevelProgress.isCompleted) continue;
            levelButton.interactable = false;
            break;
        }
    }
    
    public void LoadLevelProgress()
    {

        LevelProgress progress = SaveManager.GetLevelProgress(soLevel.GetScenePath());

        if (progress != null)
        {
            isCompleted = progress.isCompleted;
            bestScore = progress.GetTopScore();
            UpdateLevelUIState();
        }
    }
}