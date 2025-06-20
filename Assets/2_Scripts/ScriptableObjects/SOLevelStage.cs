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
    [SerializeField, Min(0)] private int waveScore = 1000;
    [SerializeField] private SerializedDictionary<ChickenController,int> enemyWave = new SerializedDictionary<ChickenController, int>();
    [SerializeField] private float enemyStageOffset;
    [SerializeField, Min(0)] private float delayBeforeNextStage = 1f;
    [EndIf]
    
    [Header("Path Settings")]
    [SerializeField] private float pathFollowSpeed = 3f;
    [SerializeField] private SplineComponent.AlignAxis upAxis = SplineComponent.AlignAxis.YAxis;
    [SerializeField] private SplineComponent.AlignAxis forwardAxis = SplineComponent.AlignAxis.ZAxis;
    [SerializeField] private SplineAnimate.AlignmentMode alignmentMode = SplineAnimate.AlignmentMode.SplineElement;
    
    [Header("Player Settings")]
    [SerializeField] private bool allowPlayerMovement = true;
    [SerializeField] private bool allowPlayerAim = true;
    [SerializeField] private bool allowPlayerShooting = true;
    [SerializeField] private float playerStageOffset;
    

    public StageType StageType => stageType;
    
    public float StageDuration => stageDuration;
    public int WaveScore => stageType == StageType.EnemyWave ? waveScore : 0;
    public SerializedDictionary<ChickenController, int> EnemyWave => enemyWave;
    public float EnemyStageOffset => enemyStageOffset;
    public float DelayBeforeNextStage => delayBeforeNextStage;
    
    public float PathFollowSpeed => pathFollowSpeed;
    public SplineComponent.AlignAxis UpAxis => upAxis;
    public SplineComponent.AlignAxis ForwardAxis => forwardAxis;
    public SplineAnimate.AlignmentMode AlignmentMode => alignmentMode;
    
    public bool AllowPlayerMovement => allowPlayerMovement;
    public bool AllowPlayerAim => allowPlayerAim;
    public bool AllowPlayerShooting => allowPlayerShooting;
    public float PlayerStageOffset => playerStageOffset;
    
    
    public bool IsTimeBasedStage => stageType is StageType.Checkpoint or StageType.Intro or StageType.Outro;



}