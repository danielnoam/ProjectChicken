using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;
using VInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Max Heat Upgrade", menuName = "Scriptable Objects/New Max Heat Upgrade")]
public class SOMaxHeatUpgrade : SOUpgradeBase
{
    
    [Header("Dodge Upgrade")]
    [SerializeField, Min(1)] private float maxHeatUpgradeAmount = 25;
    

    
    public override void ApplyUpgrade(RailPlayer player)
    {
        player?.AddMaxHeatUpgrade(this,maxHeatUpgradeAmount);
    }
}