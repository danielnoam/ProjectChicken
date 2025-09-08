using System;
using DNExtensions;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "New Level Stage", menuName = "Scriptable Objects/New Level Stage")]
public class SOLevelStage : ScriptableObject
{
    [Header("Stage Settings")]
    [SerializeField] private string stageTitle = "";
    [SerializeField] private SORadioMessage startRadioMessage;
    [SerializeField, Range(0f,2f)] private float worldSpeed = 1f;
    [SerializeField] private bool isCheckpoint;
    [SerializeField] private bool showHUD = true;
    [SerializeField] private bool allowPlayerMovement = true;
    [SerializeField] private bool allowPlayerShootingAndAiming = true;
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
    [SerializeField] private bool showOutroMenu;
    [SerializeField] private SceneField nextLevel;
    [EndIf]
    
    [ShowIf("stageType", StageType.Store)]
    [SerializeField] private bool allowToCloseStore = true;
    [SerializeField] private SOUpgradeBase[] upgradesPool = Array.Empty<SOUpgradeBase>();
    [EndIf]
    
    [ShowIf("stageType", StageType.EnemyWave)]
    [SerializeField, Min(0)] private int waveScoreWorth = 1000;
    [SerializeField] private SerializedDictionary<ChickenStateController,int> enemyWave = new SerializedDictionary<ChickenStateController, int>();
    [SerializeField] private FormationStageData formationStageData = new FormationStageData();
    [EndIf]
    
    [ShowIf("stageType", StageType.Task)]
    [SerializeField] private bool requireAllTasks = true;
    [SerializeReference] private StageTask[] tasks = Array.Empty<StageTask>();

    [EndIf]


    
    public bool IsTimeBasedStage => stageType is StageType.Delay or StageType.Intro or StageType.Outro;
    public bool IsEnemyOrTaskStage => stageType is StageType.EnemyWave or StageType.Task;
    
    
    public StageType StageType => stageType;
    public float WorldSpeed => worldSpeed;
    public float StageDuration => stageDuration;
    public string StageTitle => stageTitle;
    public SORadioMessage StartRadioMessage => startRadioMessage;
    public StageEvent[] Events => events;
    public bool IsCheckpoint => isCheckpoint;
    public bool ShowOutroMenu => showOutroMenu;
    public bool AllowToCloseStore => allowToCloseStore;
    public SceneField NextLevel => nextLevel;
    public SerializedDictionary<ChickenStateController, int> EnemyWave => enemyWave;
    public float DelayBeforeNextStage => delayBeforeNextStage;
    public int WaveScoreWorth =>  waveScoreWorth;
    public FormationStageData FormationStageData => formationStageData;
    public bool AllowPlayerMovement => allowPlayerMovement;
    public bool AllowPlayerShootingAndAiming => allowPlayerShootingAndAiming;
    public bool ShowHUD => showHUD;
    public  SOUpgradeBase[] UpgradesPool => upgradesPool;
    public StageTask[] Tasks => tasks;
    public bool RequireAllTasks => requireAllTasks;
    
    
}