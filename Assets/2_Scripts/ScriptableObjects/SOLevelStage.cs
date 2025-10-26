using System;
using Core.Attributes;
using DNExtensions;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "New Level Stage", menuName = "Scriptable Objects/New Level Stage")]
public class SOLevelStage : ScriptableObject
{
    [Header("Stage Settings")]
    [SerializeField] private string stageTitle = "";
    [SerializeField, Range(0f,2f)] private float worldSpeed = 1f;
    [SerializeField] private bool isCheckpoint;
    [SerializeField] private bool allowSkip;
    [SerializeField] private bool playRandomMessages;
    [SerializeField, CreateEditableAsset] private SORadioMessage startRadioMessage;
    [SerializeField, CreateEditableAsset] private SOWarning startWarning;
    [SerializeReference] private StageEvent[] events = Array.Empty<StageEvent>();
    
    
    [Header("Type")]
    [SerializeField] private StageType stageType;
    
    [ShowIf("IsTimeBasedStage")] 
    [SerializeField, Min(0.1f)] private float stageDuration = 5;
    [EndIf]
    
    [ShowIf("IsEnemyOrTaskStage")] 
    [SerializeField, Min(0)] private float delayBeforeNextStage = 2f;
    [EndIf]
    
    [ShowIf("stageType", StageType.Outro)]
    [SerializeField] private OutroMode outroMode = OutroMode.LoadMainMenu;
    [SerializeField, ShowIf("ShowNextLevel")] private SceneField nextLevel;
    [EndIf]
    
    [ShowIf("stageType", StageType.Store)]
    [SerializeField] private bool allowToCloseStore = true;
    [SerializeField] private SOUpgradeBase[] upgradesPool = Array.Empty<SOUpgradeBase>();
    [SerializeField] private RarityWeights poolRarityWeights = new RarityWeights();
    [SerializeField] private RarityCosts poolRarityCosts = new RarityCosts();
    [EndIf]
    
    [ShowIf("stageType", StageType.EnemyWave)]
    [SerializeField, Min(0)] private int waveScoreWorth = 1000;
    [SerializeField, Range(0, 50)] private int enemyAmount = 4;
    [SerializeField] private ChanceList<ChickenStateController> enemyTypes = new ChanceList<ChickenStateController>();
    [SerializeField] private FormationStageData formationStageData = new FormationStageData();
    [EndIf]
    
    [ShowIf("stageType", StageType.Task)]
    [SerializeField] private bool requireAllTasks = true;
    [SerializeReference] private StageTask[] tasks = Array.Empty<StageTask>();
    [EndIf]

    
    [Header("HUD")]
    [SerializeField] private bool showHUD = true;
    [SerializeField] private bool showScore = true;
    [SerializeField] private bool showStagesProgression = true;
    [SerializeField] private bool showStatsBar = true;
    
    [Header("Player")]
    [SerializeField] private bool allowPlayerMovement = true;
    [SerializeField] private bool allowPlayerDodge = true;
    [SerializeField] private bool allowPlayerAiming = true;
    [SerializeField] private bool allowPlayerShooting = true;
    [SerializeField] private bool allowPlayerHeatSystem = true;

    
    public bool IsTimeBasedStage => stageType is StageType.Delay or StageType.Intro or StageType.Outro;
    public bool IsEnemyOrTaskStage => stageType is StageType.EnemyWave or StageType.Task;
    public bool ShowNextLevel => outroMode is OutroMode.LoadNextLevel or OutroMode.ShowOutroMenu;
    
    
    public StageType StageType => stageType;
    public float WorldSpeed => worldSpeed;
    public float StageDuration => stageDuration;
    public string StageTitle => stageTitle;
    public SORadioMessage StartRadioMessage => startRadioMessage;
    public SOWarning StartWarning => startWarning;
    public StageEvent[] Events => events;
    public bool IsCheckpoint => isCheckpoint;
    public bool PlayRandomMessages => playRandomMessages;
    public OutroMode OutroMode => outroMode;
    public bool ShowOutroMenu => outroMode == OutroMode.ShowOutroMenu;
    public bool AllowToCloseStore => allowToCloseStore;
    public SceneField NextLevel => nextLevel;
    public int EnemyAmount => enemyAmount;
    public ChanceList<ChickenStateController> EnemyTypes => enemyTypes;
    public float DelayBeforeNextStage => delayBeforeNextStage;
    public int WaveScoreWorth =>  waveScoreWorth;
    public FormationStageData FormationStageData => formationStageData;
    public  SOUpgradeBase[] UpgradesPool => upgradesPool;
    public RarityWeights PoolRarityWeights => poolRarityWeights;
    public RarityCosts PoolRarityCosts => poolRarityCosts;
    public StageTask[] Tasks => tasks;
    public bool RequireAllTasks => requireAllTasks;
    
    public bool ShowHUD => showHUD;
    public bool ShowStatsBar => showStatsBar;
    public bool ShowStagesProgression => showStagesProgression;
    public bool ShowScore => showScore;
    
    
    public bool AllowPlayerMovement => allowPlayerMovement;
    public bool AllowPlayerAiming => allowPlayerAiming;
    public bool AllowPlayerDodge => allowPlayerDodge;
    public bool AllowPlayerShooting => allowPlayerShooting;
    public bool AllowPlayerHeatSystem => allowPlayerHeatSystem;
    public bool AllowSkip => allowSkip;
}