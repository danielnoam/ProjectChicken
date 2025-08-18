
using DNExtensions;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Scriptable Objects/New Game Settings")]
public class SOGameSettings : ScriptableObject
{
    [Header("General")]
    [SerializeField, Min(0)] private float timeToPause = 3f;
    [SerializeField, Min(0)] private int scoreBonusThreshold = 50000;
    [SerializeField] private SceneField mainMenuScene;
    
    
    [Header("World Boundaries")]
    [SerializeField, Min(0)] private Vector2 enemyBoundary = new Vector2(45f,30f);
    [SerializeField, Min(0)] private Vector2 playerBoundary = new Vector2(40f,25f);
    [SerializeField] private Vector3 playerBoundaryOffset;
    [SerializeField] private Vector3 enemyBoundaryOffset;
    [SerializeField] private float playerPositionMultiplier = -30f;
    [SerializeField] private float enemyPositionMultiplier = 30f;
    
    
    [Header("Player Base Stats")]
    [SerializeField, Min(0)] private int baseHealth = 2;
    [SerializeField, Min(0)] private float baseShield = 100f;
    [SerializeField, Min(0)] private float baseMagnetRadius = 14f;
    [SerializeField] private int baseDodgeAccumulation = 1;
    [SerializeField, Min(0f)] private float baseMaxHeat = 100f;
    
    [Header("Player Max Stats")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float maxShield = 150;
    [SerializeField] private float maxHeat = 150;
    [SerializeField] private int maxDodgeAccumulation = 3;
    [SerializeField] private float maxMagnetSize = 20;
    

    public float TimeToPause => timeToPause;
    public int ScoreBonusThreshold => scoreBonusThreshold;
    public SceneField MainMenuScene => mainMenuScene;
    
    
    public Vector3 PlayerBoundaryOffset => playerBoundaryOffset;
    public Vector3 EnemyBoundaryOffset => enemyBoundaryOffset;
    public Vector2 PlayerBoundary => playerBoundary;
    public Vector2 EnemyBoundary => enemyBoundary;
    public float PlayerPositionMultiplier => playerPositionMultiplier;
    public float EnemyPositionMultiplier => enemyPositionMultiplier;
    
    
    
    
    
    public int BaseHealth => baseHealth;
    public float BaseShield => baseShield;
    public float BaseMagnetRadius => baseMagnetRadius;
    public int BaseDodgeAccumulation => baseDodgeAccumulation;
    public float BaseMaxHeat => baseMaxHeat;
    
    
    
    public int MaxHealth => maxHealth;
    public float MaxShield => maxShield;
    public float MaxMagnetSize => maxMagnetSize;
    public float MaxHeat => maxHeat;
    public int MaxDodgeAccumulation => maxDodgeAccumulation;

    
    

}
