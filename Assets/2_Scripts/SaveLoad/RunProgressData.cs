using System.Collections.Generic;

public class RunProgressData
{

    public readonly int PlayerHealth;
    public readonly int PlayerCurrency;
    public readonly SOWeaponData PlayerSpecialWeapon;
    public readonly Dictionary<SOUpgradeBase, int> PlayerUpgrades;


    public RunProgressData(int health,int currency,Dictionary<SOUpgradeBase, int> upgrades, SOWeaponData specialWeapon = null)
    {
        PlayerHealth = health;
        PlayerCurrency = currency;
        PlayerUpgrades = upgrades;
        PlayerSpecialWeapon = specialWeapon;
    }
}