using System.Collections.Generic;

public class SavePointData
{

    public readonly int StageIndex;
    public readonly int Score;
    public readonly int PlayerHealth;
    public readonly int PlayerCurrency;
    public readonly SOWeaponData PlayerSpecialWeapon;
    public readonly Dictionary<SOUpgradeBase, int> PlayerUpgrades;


    public SavePointData(int stageIndex,int score,int health,int currency,Dictionary<SOUpgradeBase, int> upgrades, SOWeaponData specialWeapon = null)
    {
        StageIndex = stageIndex;
        Score = score;
        PlayerHealth = health;
        PlayerCurrency = currency;
        PlayerUpgrades = upgrades;
        PlayerSpecialWeapon = specialWeapon;
    }
}