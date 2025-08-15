using System;
using UnityEngine;
using VInspector;

public enum UpgradeRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
}

public abstract class SOUpgradeBase : ScriptableObject
{
    
    [Header("Upgrade Information")]
    [SerializeField] protected string itemName = "A name";
    [SerializeField] protected string itemDescription = "Does something";
    [SerializeField] protected GameObject itemGfx;
    [SerializeField] protected UpgradeRarity itemRarity = UpgradeRarity.Common;
    [SerializeField] protected SOUpgradeBase[] itemNeededToUnlock = Array.Empty<SOUpgradeBase>();
    [SerializeField, ReadOnly] protected int itemID;
    
    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public GameObject ItemGfx => itemGfx;
    public UpgradeRarity ItemRarity => itemRarity;
    public SOUpgradeBase[] ItemNeededToUnlock => itemNeededToUnlock;
    public int ItemID => itemID;
    
    
    private void OnEnable()
    {
        EnsureUniqueID();
    }

    
    private void EnsureUniqueID()
    {
        if (itemID == 0)
        {
            itemID = Guid.NewGuid().GetHashCode();
        }
    }

    public abstract void ApplyUpgrade(RailPlayer player);

}