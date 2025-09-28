using System;
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public enum UpgradeRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
}

public abstract class SOUpgradeBase : ScriptableObject
{
    [SerializeField, ReadOnly] protected int itemID;
    
    [Header("Upgrade Information")]
    [SerializeField] protected string itemName = "A name";
    [SerializeField] protected string itemDescription = "Does something";
    [SerializeField] protected Sprite itemIcon;
    [SerializeField] protected GameObject itemGfx;
    [SerializeField] protected UpgradeRarity itemRarity = UpgradeRarity.Common;
    [SerializeField] protected bool isStackable;
    [ShowIf("isStackable"),SerializeField, Min(1)] protected int maxStacks = 1;[EndIf]
    [SerializeField] protected SOUpgradeBase[] itemNeededToUnlock = Array.Empty<SOUpgradeBase>();

    
    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public Sprite ItemIcon => itemIcon;
    public GameObject ItemGfx => itemGfx;
    public UpgradeRarity ItemRarity => itemRarity;
    public bool IsStackable => isStackable;
    public int MaxStacks => maxStacks;
    public SOUpgradeBase[] ItemNeededToUnlock => itemNeededToUnlock;
    public int ItemID => itemID;


    private void OnValidate()
    {
        if (!isStackable) maxStacks = 1;
    }

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
    
    public virtual bool CanBeOfferedToPlayer(RailPlayer player)
    {
        
        if (!IsStackable && player.HasUpgrade(this)) return false;
    
        if (IsStackable && player.GetUpgradeCount(this) >= MaxStacks) return false;
        
        if (ItemNeededToUnlock is { Length: > 0 })
        {
            foreach (var requiredItem in ItemNeededToUnlock)
            {
                if (requiredItem && !player.HasUpgrade(requiredItem))
                {
                    return false;
                }
            }
        }
    
        return true;
    }

    public abstract void ApplyUpgrade(RailPlayer player);
    

}