using System.Collections.Generic;

public class SavePointData
{

    public readonly int StageIndex;
    public readonly int Score;
    public readonly int PlayerHealth;
    public readonly int PlayerCurrency;
    public readonly SOWeaponData PlayerActiveWeapon;
    public readonly Dictionary<SOUpgradeBase, int> PlayerUpgrades;


    public SavePointData(int stageIndex,int score,int health,int currency,Dictionary<SOUpgradeBase, int> upgrades, SOWeaponData activeWeapon = null)
    {
        StageIndex = stageIndex;
        Score = score;
        PlayerHealth = health;
        PlayerCurrency = currency;
        PlayerUpgrades = upgrades;
        PlayerActiveWeapon = activeWeapon;
    }
}