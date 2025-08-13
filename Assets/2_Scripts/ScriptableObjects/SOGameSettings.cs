using System;
using AYellowpaper;
using DNExtensions;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Scriptable Objects/New Game Settings")]
public class SOGameSettings : ScriptableObject
{
    [Header("General")]
    [SerializeField, Min(0)] private float timeToPause = 3f;
    [SerializeField, Min(0)] private int bonusThreshold = 50000;
    [SerializeField, Min(0)] private int enemyWaveScoreWorth = 1000;
    
    [Header("Boundaries")]
    [SerializeField, Min(0)] private Vector2 enemyBoundary = new Vector2(45f,30f);
    [SerializeField, Min(0)] private Vector2 playerBoundary = new Vector2(40f,25f);
    
    [Header("Positions")]
    [SerializeField] private float playerPositionMultiplier = -30f;
    [SerializeField] private float enemyPositionMultiplier = 30f;
    
    [Header("References")]
    [SerializeField] private SceneField mainMenuScene;
    [SerializeField] private ChanceList<InterfaceReference<IStoreItem, ScriptableObject>> upgradesPool = new ChanceList<InterfaceReference<IStoreItem, ScriptableObject>>();
    [SerializeField] private SOHealthUpgrade[] healthUpgrades = Array.Empty<SOHealthUpgrade>();
    [SerializeField] private SOShieldUpgrade[] shieldUpgrades = Array.Empty<SOShieldUpgrade>();
    [SerializeField] private SOResourceMagnetUpgrade[] resourceMagnetUpgrades = Array.Empty<SOResourceMagnetUpgrade>();
    
    public float TimeToPause => timeToPause;
    public Vector2 PlayerBoundary => playerBoundary;
    public Vector2 EnemyBoundary => enemyBoundary;
    public float PlayerPositionMultiplier => playerPositionMultiplier;
    public float EnemyPositionMultiplier => enemyPositionMultiplier;
    public int BonusThreshold => bonusThreshold;
    public int EnemyWaveScoreWorth =>  enemyWaveScoreWorth;
    public SceneField MainMenuScene => mainMenuScene;
    public SOHealthUpgrade[] HealthUpgrades => healthUpgrades;
    public SOShieldUpgrade[] ShieldUpgrades => shieldUpgrades;
    public SOResourceMagnetUpgrade[] ResourceMagnetUpgrades => resourceMagnetUpgrades;
    public ChanceList<InterfaceReference<IStoreItem, ScriptableObject>> UpgradesPool => upgradesPool;
}
