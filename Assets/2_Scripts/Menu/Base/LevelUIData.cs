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
    
    
    public void UpdateLevelUIState()
    {
        if (!soLevel || !levelButton) return;
        

        // check if there are needed levels
        if (soLevel.IsLocked || soLevel.LevelsToComplete.Count == 0 || soLevel.LevelsToComplete == null) return;
        
        
        // check if all needed levels are completed
        foreach (var neededLevel in soLevel.LevelsToComplete)
        {
            var neededLevelProgress = SaveManager.GetLevelProgress(neededLevel.GetScenePath());
            if (neededLevelProgress.isCompleted) continue;
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