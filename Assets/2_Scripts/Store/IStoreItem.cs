



using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;

public interface IStoreItem
{
    string ItemName { get; }
    string ItemDescription { get; }
    int ItemCost { get; }
    List<InterfaceReference<IStoreItem>> NeededItemsToUnlockToUnlock { get; }
    int ItemID { get; set; }
    
    
    static int GenerateUniqueID()
    {
        return Guid.NewGuid().GetHashCode();
    }
    
    static void EnsureUniqueID(IStoreItem item)
    {
        if (item.ItemID == 0)
        {
            item.ItemID = GenerateUniqueID();
        }
    }
}