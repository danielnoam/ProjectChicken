using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;
using VInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Dodge Upgrade", menuName = "Scriptable Objects/New Dodge Upgrade")]
public class SODodgeUpgrade : SOUpgradeBase
{
    
    [Header("Dodge Upgrade")]
    [SerializeField, Min(1)] private int dodgeUpgradeAmount = 1;
    

    
    public override void ApplyUpgrade(RailPlayer player)
    {
        player?.AddDodgeUpgrade(this,dodgeUpgradeAmount);
    }
}