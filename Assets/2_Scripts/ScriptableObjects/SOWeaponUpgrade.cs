using System;
using System.Collections.Generic;
using DNExtensions;
using UnityEngine;
using VInspector;


[CreateAssetMenu(fileName = "New Weapon Upgrade", menuName = "Scriptable Objects/New Weapon Upgrade")]
public class SOWeaponUpgrade : ScriptableObject, IStoreItem
{
    
    
    [Header("Weapon Upgrade")]
    [SerializeField] private SOWeaponData baseWeapon;
    
    [Header("Override Weapon Settings")]
    [SerializeField, Min(0)] private float fireRate = 1f;
    [SerializeField, Min(0), Tooltip("0 = Infinite targets")] private int maxTargets = 1;
    [SerializeField, Min(0.1f)] private float targetCheckRadius = 3f;
    [ShowIf("WeaponType", WeaponType.Projectile)]
    [SerializeReference] private List<ProjectileBehaviorBase> projectileBehaviors = new List<ProjectileBehaviorBase>(); [EndIf]
    [ShowIf("WeaponType", WeaponType.Hitscan)]
    [SerializeReference] private List<HitscanBehaviorBase> hitscanBehaviors = new List<HitscanBehaviorBase>(); [EndIf]
    
    
    [Header("Store Interface")]
    [SerializeField] private string itemName = "New Store Item";
    [SerializeField] private string itemDescription = "An Item";
    [SerializeField, Min(0)] private int itemCost = 10;
    [SerializeField, VInspector.ReadOnly] private int itemID;
    
    
    public WeaponType  WeaponType => baseWeapon ? baseWeapon.WeaponType : WeaponType.Projectile;
    public float FireRate => fireRate;
    public int MaxTargets => maxTargets;
    public float TargetCheckRadius => targetCheckRadius;
    public List<ProjectileBehaviorBase> ProjectileBehaviors => projectileBehaviors;
    public List<HitscanBehaviorBase> HitscanBehaviors => hitscanBehaviors;


    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public int ItemCost => itemCost;
    public int ItemID { get => itemID; set => itemID = value; }
    
    
    private void OnEnable()
    {
        IStoreItem.EnsureUniqueID(this);
    }
}

