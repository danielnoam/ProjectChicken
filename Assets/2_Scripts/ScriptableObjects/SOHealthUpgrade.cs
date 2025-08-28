using System;
using System.Collections.Generic;
using AYellowpaper;
using UnityEngine;
using VInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Health Upgrade", menuName = "Scriptable Objects/New Health Upgrade")]
public class SOHealthUpgrade : SOUpgradeBase
{
    
    [Header("Health Upgrade")]
    [SerializeField, Min(1)] private int  healthUpgradeAmount = 1;
    

    
    public override void ApplyUpgrade(RailPlayer player)
    {
        player?.AddHealthUpgrade(this,healthUpgradeAmount);
    }
    
    public override bool CanBeOfferedToPlayer(RailPlayer player)
    {
        if (!base.CanBeOfferedToPlayer(player)) return false;
        if (player.Health.CurrentHealth >= player.GameSettings.MaxHealth) return false;
    
        return true;
    }
}