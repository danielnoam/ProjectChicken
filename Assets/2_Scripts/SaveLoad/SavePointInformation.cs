using System.Collections.Generic;

public class SavePointInformation
{

    public readonly int StageIndex;
    public readonly int Score;
    public readonly int PlayerCurrency;
    public readonly WeaponInstance PlayerSpecialWeapon;
    public readonly List<SOUpgradeBase> PlayerUpgrades;


    public SavePointInformation(int stageIndex,int score,int currency,List<SOUpgradeBase> upgrades, WeaponInstance specialWeapon = null)
    {
        StageIndex = stageIndex;
        Score = score;
        PlayerCurrency = currency;
        PlayerUpgrades = upgrades;
        PlayerSpecialWeapon = specialWeapon;
    }
}