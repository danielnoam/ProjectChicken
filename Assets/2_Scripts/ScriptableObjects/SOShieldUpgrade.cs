using System;
using System.Collections.Generic;
using UnityEngine;
using DNExtensions.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Shield Upgrade", menuName = "Scriptable Objects/New Shield Upgrade")]
public class SOShieldUpgrade : SOUpgradeBase
{
    
    [Header("Shield Upgrade")]
    [SerializeField, Min(1)] private float  shieldUpgradeAmount = 25;
    
    
    public override void ApplyUpgrade(RailPlayer player)
    {
        player?.AddMaxShieldUpgrade(this,shieldUpgradeAmount);
    }
    
}