using System;
using System.Collections.Generic;
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
    [SerializeField] private int maxPlayerHealth = 3;
    [SerializeField] private float maxPlayerShield = 150;
    [SerializeField] private float maxPlayerMagnetSize = 20;
    
    [Header("Boundaries")]
    [SerializeField, Min(0)] private Vector2 enemyBoundary = new Vector2(45f,30f);
    [SerializeField, Min(0)] private Vector2 playerBoundary = new Vector2(40f,25f);
    
    [Header("Positions")]
    [SerializeField] private float playerPositionMultiplier = -30f;
    [SerializeField] private float enemyPositionMultiplier = 30f;
    
    [Header("References")]
    [SerializeField] private SceneField mainMenuScene;

    
    public float TimeToPause => timeToPause;
    public int MaxPlayerHealth => maxPlayerHealth;
    public float MaxPlayerShield => maxPlayerShield;
    public float MaxPlayerMagnetSize => maxPlayerMagnetSize;
    public Vector2 PlayerBoundary => playerBoundary;
    public Vector2 EnemyBoundary => enemyBoundary;
    public float PlayerPositionMultiplier => playerPositionMultiplier;
    public float EnemyPositionMultiplier => enemyPositionMultiplier;
    public int BonusThreshold => bonusThreshold;
    public int EnemyWaveScoreWorth =>  enemyWaveScoreWorth;
    public SceneField MainMenuScene => mainMenuScene;

}
