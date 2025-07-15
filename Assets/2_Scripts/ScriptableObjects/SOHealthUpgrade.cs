using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;
using VInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Health Upgrade", menuName = "Scriptable Objects/New Health Upgrade")]
public class SOHealthUpgrade : ScriptableObject, IStoreItem
{
    
    [Header("Health Upgrade")]
    [SerializeField, Min(1)] private int  healthUpgradeAmount = 1;

    
    [Header("Store Interface")]
    [SerializeField] private string itemName = "Health Upgrade";
    [SerializeField] private string itemDescription = "Adds 1 heart to the player";
    [SerializeField, Min(0)] private int itemCost = 150;
    [SerializeField] private List<InterfaceReference<IStoreItem>> neededItemsToUnlock = new  List<InterfaceReference<IStoreItem>>();
    [SerializeField, ReadOnly] private int itemID;
    
    public int  HealthUpgradeAmount => healthUpgradeAmount;
    
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