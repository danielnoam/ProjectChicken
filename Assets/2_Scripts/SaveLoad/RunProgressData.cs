using System.Collections.Generic;

public class RunProgressData
{

    public readonly int PlayerHealth;
    public readonly int PlayerCurrency;
    public readonly SOWeaponData PlayerActiveWeapon;
    public readonly Dictionary<SOUpgradeBase, int> PlayerUpgrades;


    public RunProgressData(int health,int currency,Dictionary<SOUpgradeBase, int> upgrades, SOWeaponData activeWeapon = null)
    {
        PlayerHealth = health;
        PlayerCurrency = currency;
        PlayerUpgrades = upgrades;
        PlayerActiveWeapon = activeWeapon;
    }
}