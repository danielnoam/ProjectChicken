using System.Collections.Generic;

public class SavePointInformation
{

    public readonly int StageIndex;
    public readonly int Score;
    public readonly int PlayerHealth;
    public readonly float PlayerShield;
    public readonly int PlayerCurrency;
    public readonly SOWeaponData PlayerSpecialWeapon;
    public readonly List<SOUpgradeBase> PlayerUpgrades;


    public SavePointInformation(int stageIndex,int score, int health, float shield,int currency,List<SOUpgradeBase> upgrades, SOWeaponData specialWeapon = null)
    {
        StageIndex = stageIndex;
        Score = score;
        PlayerHealth = health;
        PlayerShield = shield;
        PlayerCurrency = currency;
        PlayerUpgrades = upgrades;
        PlayerSpecialWeapon = specialWeapon;
    }
}