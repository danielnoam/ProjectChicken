
using DNExtensions;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Scriptable Objects/New Game Settings")]
public class SOGameSettings : ScriptableObject
{
    [Header("General")]
    [SerializeField, Min(0)] private float timeToPause = 3f;
    [SerializeField, Min(0)] private int scoreBonusThreshold = 50000;
    [SerializeField, Min(0)] private int enemyWaveScoreWorth = 1000;
    
    [Header("Player Upgrades")]
    [SerializeField] private int maxPlayerHealth = 3;
    [SerializeField] private float maxPlayerShield = 150;
    [SerializeField] private float maxPlayerHeat = 150;
    [SerializeField] private int maxPlayerDodgeAccumulation = 3;
    [SerializeField] private float maxPlayerMagnetSize = 20;
    
    [Header("Boundaries")]
    [SerializeField, Min(0)] private Vector2 enemyBoundary = new Vector2(45f,30f);
    [SerializeField, Min(0)] private Vector2 playerBoundary = new Vector2(40f,25f);
    [SerializeField] private Vector3 playerBoundaryOffset;
    [SerializeField] private Vector3 enemyBoundaryOffset;
    [SerializeField] private float playerPositionMultiplier = -30f;
    [SerializeField] private float enemyPositionMultiplier = 30f;
    
    [Header("References")]
    [SerializeField] private SceneField mainMenuScene;

    
    public float TimeToPause => timeToPause;
    public int MaxPlayerHealth => maxPlayerHealth;
    public float MaxPlayerShield => maxPlayerShield;
    public float MaxPlayerMagnetSize => maxPlayerMagnetSize;
    public float MaxPlayerHeat => maxPlayerHeat;
    public int MaxPlayerDodgeAccumulation => maxPlayerDodgeAccumulation;
    public Vector3 PlayerBoundaryOffset => playerBoundaryOffset;
    public Vector3 EnemyBoundaryOffset => enemyBoundaryOffset;
    public Vector2 PlayerBoundary => playerBoundary;
    public Vector2 EnemyBoundary => enemyBoundary;
    public float PlayerPositionMultiplier => playerPositionMultiplier;
    public float EnemyPositionMultiplier => enemyPositionMultiplier;
    public int ScoreBonusThreshold => scoreBonusThreshold;
    public int EnemyWaveScoreWorth =>  enemyWaveScoreWorth;
    public SceneField MainMenuScene => mainMenuScene;

}
