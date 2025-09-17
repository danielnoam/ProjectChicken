using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



public class StageProgressionBar : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite enemyWaveSprite;
    [SerializeField] private Sprite storeSprite;

    [Header("Colors")]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color enemyWaveColor;
    [SerializeField] private Color storeColor;

    [Header("References")]
    [SerializeField] private StageIcon stageIconPrefab;
    [SerializeField] private TextMeshProUGUI enemiesRemainingText;
    [SerializeField] private Transform stageIconHolder;
    
    
    private SOLevel _currentLevel; 
    private int _currentVisualStageIndex = -1;
    private int _logicalStageIndex = -1;
    private readonly List<(SOLevelStage stage, StageIcon icon)> _stageIcons = new List<(SOLevelStage, StageIcon)>();


    private void Update()
    {
        if (EnemySpawner.Instance) enemiesRemainingText.text = $"Enemies Left: {EnemySpawner.Instance.ActiveEnemyCount}";
    }

    private bool IsNonVisualStage(StageType stageType)
    {
        return stageType == StageType.Delay || stageType == StageType.Task;
    }
    
    
    public void Initialize(SOLevel level)
    {
        _currentLevel = level;
        _logicalStageIndex = -1;
        _currentVisualStageIndex = -1;
        
        foreach (SOLevelStage stage in level.LevelStages)
        {
            if (!stage || IsNonVisualStage(stage.StageType)) continue;
            
            var innerIcon = defaultSprite;
            var outerIconColor = defaultColor;
            
            switch (stage.StageType)
            {
                case StageType.Store:
                    innerIcon = storeSprite;
                    outerIconColor = storeColor;
                    break;
                case StageType.EnemyWave:
                    innerIcon = enemyWaveSprite;
                    outerIconColor = enemyWaveColor;
                    break;
            }
            
            StageIcon stageIcon = Instantiate(stageIconPrefab, stageIconHolder);
            stageIcon.Initialize(innerIcon, outerIconColor);
            _stageIcons.Add((stage, stageIcon));
        }
    }
    
    public void SetCurrentStage(SOLevelStage stage)
    {
        if (!stage || !_currentLevel) return;
        
        if (stage.StageType == StageType.EnemyWave)
        {
            enemiesRemainingText.alpha = 1;
        }
        else
        {
            enemiesRemainingText.alpha = 0;
        }
        
        if (_currentVisualStageIndex >= 0 && _currentVisualStageIndex < _stageIcons.Count)
        {
            _stageIcons[_currentVisualStageIndex].icon.SetCurrent(false);
        }
        
        int stageIndexInLevel = FindStageIndexInLevel(stage);
        if (stageIndexInLevel == -1) return;
        
        _logicalStageIndex = stageIndexInLevel;
        
        if (!IsNonVisualStage(stage.StageType))
        {
            int visualIndex = FindVisualIndexForLogicalStage(stageIndexInLevel);
            if (visualIndex >= 0 && visualIndex < _stageIcons.Count)
            {
                _currentVisualStageIndex = visualIndex;
                _stageIcons[visualIndex].icon.SetCurrent(true);
            }
        }
    }
    
    private int FindStageIndexInLevel(SOLevelStage targetStage)
    {
        for (int i = _logicalStageIndex + 1; i < _currentLevel.LevelStages.Length; i++)
        {
            if (_currentLevel.LevelStages[i] == targetStage)
                return i;
        }
        return -1;
    }
    
    private int FindVisualIndexForLogicalStage(int logicalIndex)
    {
        int visualIndex = 0;
        for (int i = 0; i <= logicalIndex && i < _currentLevel.LevelStages.Length; i++)
        {
            SOLevelStage stage = _currentLevel.LevelStages[i];
            if (!IsNonVisualStage(stage.StageType))
            {
                if (i == logicalIndex)
                    return visualIndex;
                visualIndex++;
            }
        }
        return -1;
    }
}