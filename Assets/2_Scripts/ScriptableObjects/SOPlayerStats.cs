
using DNExtensions;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/New Player Stats")]
public class SOPlayerStats : ScriptableObject
{
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
