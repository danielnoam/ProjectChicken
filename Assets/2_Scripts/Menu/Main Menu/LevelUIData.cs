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
    
    public LevelUIData (SOLevel level, GameObject gfx, Button button)
    {
        soLevel = level;
        levelGfx = gfx;
        levelButton = button;
        var progress = SaveManager.GetLevelProgress(level.GetScenePath());

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
        
        UpdateLevelUIProgressState();
    }
    
    
    public void UpdateLevelUIProgressState()
    {
        if (!soLevel || !levelButton) return;
        
        if (soLevel.LevelsToComplete.Count == 0 || soLevel.LevelsToComplete == null) return;
        
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
            UpdateLevelUIProgressState();
        }
    }
}