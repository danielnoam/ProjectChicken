using UnityEngine;
using UnityEngine.Splines;
using VInspector;



[CreateAssetMenu(fileName = "New Level Stage", menuName = "Scriptable Objects/New Level Stage")]
public class SOLevelStage : ScriptableObject
{

    [Header("Stage Settings")]
    [SerializeField] private StageType stageType;
    [ShowIf("stageType", StageType.Checkpoint)] 
    [SerializeField, Min(0.1f)] private float stageDuration = 5;
    [EndIf]
    [ShowIf("stageType", StageType.EnemyWave)]
    [SerializeField] private SerializedDictionary<ChickenController,int> enemyCount = new SerializedDictionary<ChickenController, int>();
    [SerializeField] private float enemyStageOffset;
    [EndIf]
    
    [Header("Path Settings")]
    [SerializeField] private float pathFollowSpeed = 3f;
    [SerializeField] private SplineAnimate.AlignAxis upAxis = SplineAnimate.AlignAxis.YAxis;
    [SerializeField] private SplineAnimate.AlignAxis forwardAxis = SplineAnimate.AlignAxis.ZAxis;
    [SerializeField] private SplineAnimate.AlignmentMode alignmentMode = SplineAnimate.AlignmentMode.SplineElement;
    
    [Header("Player Settings")]
    [SerializeField] private bool allowPlayerMovement = true;
    [SerializeField] private bool allowPlayerAim = true;
    [SerializeField] private bool allowPlayerShooting = true;
    [SerializeField] private float playerStageOffset;
    

    public StageType StageType => stageType;
    public float StageDuration => stageType == StageType.Checkpoint ? stageDuration : 0f;
    public SerializedDictionary<ChickenController, int> EnemyCount => enemyCount;
    public float EnemyStageOffset => enemyStageOffset;
    public float PathFollowSpeed => pathFollowSpeed;
    public SplineAnimate.AlignAxis UpAxis => upAxis;
    public SplineAnimate.AlignAxis ForwardAxis => forwardAxis;
    public SplineAnimate.AlignmentMode AlignmentMode => alignmentMode;
    public bool AllowPlayerMovement => allowPlayerMovement;
    public bool AllowPlayerAim => allowPlayerAim;
    public bool AllowPlayerShooting => allowPlayerShooting;
    public float PlayerStageOffset => playerStageOffset;



}