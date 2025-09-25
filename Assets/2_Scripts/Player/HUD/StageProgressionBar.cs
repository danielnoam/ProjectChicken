using System;
using System.Collections.Generic;
using DNExtensions;
using KBCore.Refs;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageProgressionBar : MonoBehaviour
{
    [Header("StageIcon")]
    [Tooltip("Icon size range in pixels. Min = smallest size (many stages), Max = largest size (few stages)")]
    [SerializeField, MinMaxRange(5, 50f)] private RangedFloat iconSizeRange = new RangedFloat(8f, 40f);
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private Ease animationEase = Ease.OutElastic;
    
    [Header("Enemy Info")]
    [SerializeField] private bool showEnemyInfo = true;
    [SerializeField] private float enemyInfoAnimationDuration = 0.3f;
    [SerializeField] private float enemyInfoAnimationPunchScale = 0.2f;
    
    
    [Header("Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite introSprite;
    [SerializeField] private Sprite outroSprite;
    [SerializeField] private Sprite enemyWaveSprite;
    [SerializeField] private Sprite storeSprite;

    [Header("Colors")]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color introColor;
    [SerializeField] private Color outroColor;
    [SerializeField] private Color enemyWaveColor;
    [SerializeField] private Color storeColor;

    [Header("References")]
    [SerializeField] private CanvasGroup enemiesRemainingCanvasGroup;
    [SerializeField] private TextMeshProUGUI enemiesRemainingText;
    [SerializeField] private Transform stageIconHolder;
    [SerializeField] private HorizontalLayoutGroup horizontalLayoutGroup;
    [SerializeField] private StageIcon stageIconPrefab;
    [SerializeField,Scene(Flag.EditableAnywhere)] private EnemySpawner enemySpawner;
    [SerializeField,Scene(Flag.EditableAnywhere)] private LevelManager levelManger;

    private Vector2 _stageIconFullSize;
    private SOLevel _currentLevel; 
    private int _currentVisualStageIndex = -1;
    private int _logicalStageIndex = -1;
    private readonly List<(SOLevelStage stage, StageIcon icon)> _stageIcons = new List<(SOLevelStage, StageIcon)>();


    private void OnValidate()
    {
        if (!enemySpawner) enemySpawner = FindAnyObjectByType<EnemySpawner>();
        if (!levelManger) levelManger = FindAnyObjectByType<LevelManager>();
        this.ValidateRefs();
    }

    private void OnEnable()
    {
        if (enemySpawner)
        {
            enemySpawner.OnEnemyDeath += OnEnemyDeath;
        }
    }

    private void OnDisable()
    {
        if (enemySpawner)
        {
            enemySpawner.OnEnemyDeath -= OnEnemyDeath;
        }
    }
    

    private void Update()
    {
        if (levelManger && showEnemyInfo)
        {
            enemiesRemainingText.text = $"{levelManger.EnemiesLeft}";
        }

    }
    
    private void OnEnemyDeath(ChickenStateController enemy)
    {
        Tween.PunchScale(enemiesRemainingText.transform, Vector3.one * enemyInfoAnimationPunchScale, enemyInfoAnimationDuration, 1);
    }


    public void Initialize(SOLevel level)
    {
        if (!level || !stageIconPrefab) return;
        
        _currentLevel = level;
        _logicalStageIndex = -1;
        _currentVisualStageIndex = -1;
        _stageIconFullSize = stageIconPrefab.GetComponent<RectTransform>().sizeDelta;
        
        _stageIcons.Clear();
        foreach (Transform child in stageIconHolder)
        {
            if (child != stageIconHolder)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        // Count visual stages
        int visualStageCount = 0;
        foreach (SOLevelStage stage in level.LevelStages)
        {
            if (stage && !IsNonVisualStage(stage.StageType))
            {
                visualStageCount++;
            }
        }

        Vector2 iconSize = CalculateAdaptiveIconSize(visualStageCount);
        
        // Create stage icons
        foreach (SOLevelStage stage in level.LevelStages)
        {
            if (!stage || IsNonVisualStage(stage.StageType)) continue;
            
            var innerIcon = GetStageSprite(stage.StageType);
            var outerIconColor = GetStageColor(stage.StageType);
            
            StageIcon stageIcon = Instantiate(stageIconPrefab, stageIconHolder);
            stageIcon.Initialize(innerIcon, outerIconColor, iconSize);
            _stageIcons.Add((stage, stageIcon));
        }
    }
    
    private Sprite GetStageSprite(StageType stageType)
    {
        return stageType switch
        {
            StageType.Store => storeSprite,
            StageType.EnemyWave => enemyWaveSprite,
            StageType.Intro => introSprite,
            StageType.Outro => outroSprite,
            _ => defaultSprite
        };
    }
    
    private Color GetStageColor(StageType stageType)
    {
        return stageType switch
        {
            StageType.Store => storeColor,
            StageType.EnemyWave => enemyWaveColor,
            StageType.Intro => introColor,
            StageType.Outro => outroColor,
            _ => defaultColor
        };
    }
    

    public void SetCurrentStage(SOLevelStage stage)
    {
        if (!stage || !_currentLevel) return;
    
        enemiesRemainingCanvasGroup.alpha = stage.StageType == StageType.EnemyWave && showEnemyInfo ? 1 : 0;
    
        int stageIndexInLevel = FindStageIndexInLevel(stage);
        if (stageIndexInLevel == -1) return;
    
        _logicalStageIndex = stageIndexInLevel;
    
        // Only update visual progression if the new stage is visual
        if (!IsNonVisualStage(stage.StageType))
        {
            // Deactivate current visual stage only when switching to another visual stage
            if (_currentVisualStageIndex >= 0 && _currentVisualStageIndex < _stageIcons.Count)
            {
                _stageIcons[_currentVisualStageIndex].icon.SetCurrent(false, animationDuration, animationEase);
            }
        
            int visualIndex = FindVisualIndexForLogicalStage(stageIndexInLevel);
            if (visualIndex >= 0 && visualIndex < _stageIcons.Count)
            {
                _currentVisualStageIndex = visualIndex;
                _stageIcons[visualIndex].icon.SetCurrent(true, animationDuration, animationEase);
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
    
    private Vector2 CalculateAdaptiveIconSize(int visualStageCount)
    {
        float availableWidth = GetAvailableWidth();
        float maxFittingWidth = CalculateMaxFittingIconWidth(visualStageCount, availableWidth);
        float finalIconWidth = Mathf.Min(iconSizeRange.maxValue, maxFittingWidth);
        finalIconWidth = Mathf.Max(finalIconWidth, iconSizeRange.minValue);
        
        return new Vector2(finalIconWidth, _stageIconFullSize.x);
    }

    private float GetAvailableWidth()
    {
        if (!horizontalLayoutGroup) return 500f;
        
        RectTransform rectTransform = horizontalLayoutGroup.GetComponent<RectTransform>();
        if (!rectTransform) return 500f;
        
        float availableWidth = rectTransform.rect.width - horizontalLayoutGroup.padding.left - horizontalLayoutGroup.padding.right;
        
        return availableWidth;
    }

    private float CalculateMaxFittingIconWidth(int iconCount, float availableWidth)
    {
        if (iconCount <= 0) return iconSizeRange.maxValue;
        if (iconCount == 1) return Mathf.Clamp(availableWidth, iconSizeRange.minValue, iconSizeRange.maxValue);
        
        float totalSpacing = (iconCount - 1) * horizontalLayoutGroup.spacing;
        float currentStageIconWidth = _stageIconFullSize.x;
        
        // Available width for remaining icons
        float remainingWidth = availableWidth - totalSpacing - currentStageIconWidth;
        float remainingIconCount = iconCount - 1;
        
        // Calculate width for non-current icons
        float maxIconWidth = remainingWidth / remainingIconCount;
        
        // Clamp between our min/max range
        maxIconWidth = Mathf.Clamp(maxIconWidth, iconSizeRange.minValue, iconSizeRange.maxValue);
        
        return maxIconWidth;
    }
    
    private bool IsNonVisualStage(StageType stageType)
    {
        return stageType == StageType.Delay || stageType == StageType.Task;
    }
}