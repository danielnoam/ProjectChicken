using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;
using VInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Resource Magnet Upgrade", menuName = "Scriptable Objects/New Resource Magnet Upgrade")]
public class SOResourceMagnetUpgrade : ScriptableObject, IStoreItem
{
    
    [Header("Resource Magnet Upgrade")]
    [SerializeField, Min(1)] private float magnetUpgradeAmount = 3;

    
    [Header("Store Interface")]
    [SerializeField] private string itemName = "Resource Magnet Upgrade";
    [SerializeField] private string itemDescription = $"Makes the ship resource magnet radius bigger by 3";
    [SerializeField, Min(0)] private int itemCost = 75;
    [SerializeField] private List<InterfaceReference<IStoreItem>> neededItemsToUnlock = new  List<InterfaceReference<IStoreItem>>();
    [SerializeField, ReadOnly] private int itemID;
    
    public float  MagnetUpgradeAmount => magnetUpgradeAmount;
    
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