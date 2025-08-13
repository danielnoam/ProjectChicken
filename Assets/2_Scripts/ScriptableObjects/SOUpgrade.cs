using System;
using UnityEngine;
using VInspector;


public abstract class SOUpgrade : ScriptableObject
{
    
    [Header("Upgrade Information")]
    [SerializeField] protected string itemName = "A name";
    [SerializeField] protected string itemDescription = "Does something";
    [SerializeField] protected GameObject itemGfx;
    [SerializeField, ReadOnly] protected int itemID;
    
    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public GameObject ItemGfx => itemGfx;
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