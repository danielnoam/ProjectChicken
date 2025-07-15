using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;
using VInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Shield Upgrade", menuName = "Scriptable Objects/New Shield Upgrade")]
public class SOShieldUpgrade : ScriptableObject, IStoreItem
{
    
    [Header("Shield Upgrade")]
    [SerializeField, Min(1)] private float  shieldUpgradeAmount = 25;

    
    [Header("Store Interface")]
    [SerializeField] private string itemName = "Shield Upgrade";
    [SerializeField] private string itemDescription = "Adds 25 hit points to the shield";
    [SerializeField, Min(0)] private int itemCost = 150;
    [SerializeField] private List<InterfaceReference<IStoreItem>> neededItemsToUnlock = new  List<InterfaceReference<IStoreItem>>();
    [SerializeField, ReadOnly] private int itemID;
    
    
    public float  ShieldUpgradeAmount => shieldUpgradeAmount;
    
    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public int ItemCost => itemCost;
    public List<InterfaceReference<IStoreItem>> NeededItemsToUnlockToUnlock => neededItemsToUnlock;
    
    
    public int ItemID { get => itemID; set => itemID = value; }
    
    
    private void OnEnable()
    {
        IStoreItem.EnsureUniqueID(this);
    }
    
    
}