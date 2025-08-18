using System;
using DNExtensions.VFXManager;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "New Level Stage", menuName = "Scriptable Objects/New Level Stage")]
public class SOLevelStage : ScriptableObject
{
    [Header("Stage Settings")]
    [SerializeField] private string stageTitle = "";
    [SerializeField, Range(0f,2f)] private float stageWorldSpeed = 1f;
    [SerializeField] private bool showHUD = true;
    [SerializeField] private bool allowPlayerMovement = true;
    [SerializeField] private bool allowPlayerShootingAndAiming = true;

    
    [Header("Type")]
    [SerializeField] private StageType stageType;
    
    [ShowIf("IsTimeBasedStage")] 
    [SerializeField, Min(0.1f)] private float stageDuration = 5;
    [EndIf]
    
    [ShowIf("stageType", StageType.Store)]
    [SerializeField] private SOUpgradeBase[] upgradesPool = Array.Empty<SOUpgradeBase>();
    [EndIf]
    
    [ShowIf("stageType", StageType.EnemyWave)]
    [SerializeField, Min(0)] private float delayBeforeNextStage = 1f;
    [SerializeField, Min(0)] private int waveScoreWorth = 1000;
    [SerializeField] private SerializedDictionary<ChickenController,int> enemyWave = new SerializedDictionary<ChickenController, int>();
    [SerializeField] private FormationSettings formationSettings = new FormationSettings();
    [EndIf]
    


    
    public StageType StageType => stageType;
    public float StageWorldSpeed => stageWorldSpeed;
    public float StageDuration => stageDuration;
    public string StageTitle => stageTitle;
    public SerializedDictionary<ChickenController, int> EnemyWave => enemyWave;
    public float DelayBeforeNextStage => delayBeforeNextStage;
    public int WaveScoreWorth =>  waveScoreWorth;
    public FormationSettings FormationSettings => formationSettings;
    public bool IsTimeBasedStage => stageType is StageType.Checkpoint or StageType.Intro or StageType.Outro;
    public bool IsSavePointStage => stageType is StageType.Intro or StageType.Checkpoint or StageType.Store;
    public bool AllowPlayerMovement => allowPlayerMovement;
    public bool AllowPlayerShootingAndAiming => allowPlayerShootingAndAiming;

    public bool ShowHUD => showHUD;
    public  SOUpgradeBase[] UpgradesPool => upgradesPool;
    
    
}