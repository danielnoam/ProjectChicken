public class SavePointInformation
{

    public readonly int StageIndex;
    public readonly int Score;
    public readonly int PlayerHealth;
    public readonly float PlayerShield;
    public readonly int PlayerCurrency;
    public readonly SOWeaponData PlayerSpecialWeapon;


    public SavePointInformation(int stageIndex,int score, int health, float shield,int currency, SOWeaponData specialWeapon = null)
    {
        this.StageIndex = stageIndex;
        this.Score = score;
        this.PlayerHealth = health;
        this.PlayerShield = shield;
        this.PlayerCurrency = currency;
        this.PlayerSpecialWeapon = specialWeapon;
    }
}