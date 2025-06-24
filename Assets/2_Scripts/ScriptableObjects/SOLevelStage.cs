using UnityEngine;
using UnityEngine.Splines;
using VInspector;

[CreateAssetMenu(fileName = "New Level Stage", menuName = "Scriptable Objects/New Level Stage")]
public class SOLevelStage : ScriptableObject
{
    [Header("Stage Settings")]
    [SerializeField] private StageType stageType;
    [ShowIf("IsTimeBasedStage")] 
    [SerializeField, Min(0.1f)] private float stageDuration = 5;
    [EndIf]
    [ShowIf("stageType", StageType.EnemyWave)]
    [SerializeField] private float enemyPositionOffset;
    [SerializeField, Min(0)] private float delayBeforeNextStage = 1f;
    [SerializeField, Min(0)] private int waveScore = 1000;
    [SerializeField] private SerializedDictionary<ChickenController,int> enemyWave = new SerializedDictionary<ChickenController, int>();
    [SerializeField] private FormationSettings formationSettings = new FormationSettings();
    [EndIf]
    
    [Header("Path Settings")]
    [SerializeField] private float pathFollowSpeed = 5f;
    [SerializeField] private SplineComponent.AlignAxis upAxis = SplineComponent.AlignAxis.YAxis;
    [SerializeField] private SplineComponent.AlignAxis forwardAxis = SplineComponent.AlignAxis.ZAxis;
    [SerializeField] private SplineAnimate.AlignmentMode alignmentMode = SplineAnimate.AlignmentMode.SplineElement;
    
    [Header("Player Settings")]
    [SerializeField] private bool allowPlayerMovement = true;
    [SerializeField] private bool allowPlayerAim = true;
    [SerializeField] private bool allowPlayerShooting = true;
    [SerializeField] private float playerPositionOffset;
    [SerializeField, ShowIf("IsGameplayStage")] private bool showPlayerKeybinds;
    

    

    // Stage properties
    public StageType StageType => stageType;
    public float StageDuration => stageDuration;
    public int WaveScore => stageType == StageType.EnemyWave ? waveScore : 0;
    public SerializedDictionary<ChickenController, int> EnemyWave => enemyWave;
    public float EnemyPositionOffset => enemyPositionOffset;
    public float DelayBeforeNextStage => delayBeforeNextStage;
    public FormationSettings FormationSettings => formationSettings;
    public bool IsTimeBasedStage => stageType is StageType.Checkpoint or StageType.Intro or StageType.Outro;
    public bool IsGameplayStage => stageType is StageType.EnemyWave or StageType.Checkpoint;
    
    
    // Spline properties
    public float PathFollowSpeed => pathFollowSpeed;
    public SplineComponent.AlignAxis UpAxis => upAxis;
    public SplineComponent.AlignAxis ForwardAxis => forwardAxis;
    public SplineAnimate.AlignmentMode AlignmentMode => alignmentMode;
    
    // Player properties
    public bool AllowPlayerMovement => allowPlayerMovement;
    public bool AllowPlayerAim => allowPlayerAim;
    public bool AllowPlayerShooting => allowPlayerShooting;
    public float PlayerPositionOffset => playerPositionOffset;
    public bool ShowPlayerKeybinds => showPlayerKeybinds;
    
    
}