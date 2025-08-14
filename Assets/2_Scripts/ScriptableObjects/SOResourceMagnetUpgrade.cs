using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;
using VInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Resource Magnet Upgrade", menuName = "Scriptable Objects/New Resource Magnet Upgrade")]
public class SOResourceMagnetUpgrade : SOUpgradeBase
{
    
    [Header("Resource Magnet Upgrade")]
    [SerializeField, Min(1)] private float magnetUpgradeAmount = 3;
    
    public override void ApplyUpgrade(RailPlayer player)
    {
        player?.AddMagnetSizeUpgrade(this,magnetUpgradeAmount);
    }
}