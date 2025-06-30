


using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    public int currency = 0;
    public List<LevelProgress> levelProgresses = new();
}



[Serializable]
public class LevelProgress
{
    public string scenePath;
    public bool isCompleted;
    public List<int> bestScores;
    
    public LevelProgress(string path)
    {
        scenePath = path;
        isCompleted = false;
        bestScores = new List<int>();
    }
    
    public int GetTopScore()
    {
        if (bestScores.Count == 0) return 0;
        return bestScores[0];
    }
}
