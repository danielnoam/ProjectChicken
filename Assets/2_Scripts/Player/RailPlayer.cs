using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;

[SelectionBase]
[RequireComponent(typeof(RailPlayerInput))]
[RequireComponent(typeof(RailPlayerMovement))]
[RequireComponent(typeof(RailPlayerAiming))]
[RequireComponent(typeof(RailPlayerWeaponSystem))]
[RequireComponent(typeof(RailPlayerResourceCollector))]
[RequireComponent(typeof(RailPlayerHealth))]
[RequireComponent(typeof(RailPlayerCameraInterface))]
public class RailPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SOPlayerStats playerStats;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Transform textFollowPosition;
    [SerializeField, Self, HideInInspector] private RailPlayerInput input;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming aiming;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement movement;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem weaponSystem;
    [SerializeField, Self, HideInInspector] private RailPlayerResourceCollector resourceCollector;
    [SerializeField, Self, HideInInspector] private RailPlayerHealth health;
    [SerializeField, Self, HideInInspector] private RailPlayerCameraInterface cameraInterface;


    
    
    public Dictionary<SOUpgradeBase, int> Upgrades { get; private set; } = new Dictionary<SOUpgradeBase, int>();
    public RailPlayerAiming Aiming => aiming;
    public RailPlayerWeaponSystem WeaponSystem => weaponSystem;
    public RailPlayerMovement Movement => movement;
    public RailPlayerResourceCollector ResourceCollector => resourceCollector;
    public RailPlayerHealth Health => health;
    public RailPlayerCameraInterface CameraInterface => cameraInterface;
    public LevelManager LevelManager => levelManager;
    public SOPlayerStats PlayerStats => playerStats;
    

    
    
    private void OnValidate()
    {
        this.ValidateRefs();
        
        if (!levelManager)
        {
            levelManager = FindAnyObjectByType<LevelManager>();
        }
    }

    private void Awake()
    {
        Upgrades.Clear();
        aiming.SetUp();
        movement.SetUp();
        health.SetUp(playerStats.BaseHealth);
        resourceCollector.SetUp();
        weaponSystem.SetUp();
    }

    private void OnEnable()
    {
        levelManager.OnRunProgressLoaded += OnRunProgressLoaded;
        levelManager.OnRestartedFromSavePoint += RestartedFromSavePoint;
    }
    
    private void OnDisable()
    {
        levelManager.OnRunProgressLoaded -= OnRunProgressLoaded;
        levelManager.OnRestartedFromSavePoint -= RestartedFromSavePoint;
    }
    
    
    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out PassthroughObstacle gameObjectCenterer))
        {
            if (!gameObjectCenterer.PlayerIsPassingThrough)
            {
                Vector3 moveDirection = (transform.position - gameObjectCenterer.CenterObjectTransform.position).normalized;
                Movement.Push(moveDirection, 1f);
            }
        }
    }

    
    private void OnRunProgressLoaded(RunProgressData runProgress)
    {
        if (runProgress == null) return;

        Upgrades.Clear();
        health.SetUp(runProgress.PlayerHealth);
        resourceCollector.SetUp(runProgress.PlayerCurrency);
        weaponSystem.SetUp(runProgress.PlayerActiveWeapon);
        movement.SetUp();
        aiming.SetUp();
        
        RestoreUpgrades(runProgress.PlayerUpgrades);
    }
    
    private void RestartedFromSavePoint(SavePointData savePoint)
    {
        if (savePoint == null) return;

        Upgrades.Clear();
        health.SetUp(savePoint.PlayerHealth);
        resourceCollector.SetUp(savePoint.PlayerCurrency);
        weaponSystem.SetUp(savePoint.PlayerActiveWeapon);
        movement.SetUp();
        aiming.SetUp();
        
        RestoreUpgrades(savePoint.PlayerUpgrades);
    }
    

    public Transform GetTextFollowPosition()
    {
        return textFollowPosition ? textFollowPosition : transform;
    }
    
    
    #region Upgrades ---------------------------------------------------------------------------------
    
    // Rebuilds the upgrade stacks from saved counts. Each upgrade adds itself back to Upgrades as it is
    // applied, so applying it once per saved stack restores both the count and the effect.
    private void RestoreUpgrades(Dictionary<SOUpgradeBase, int> savedUpgrades)
    {
        Upgrades = new Dictionary<SOUpgradeBase, int>();
        if (savedUpgrades == null) return;

        foreach (var upgrade in savedUpgrades)
        {
            if (!upgrade.Key) continue;
            for (int i = 0; i < upgrade.Value; i++)
            {
                upgrade.Key.ApplyUpgrade(this);
            }
        }
    }
    
    public void AddHealthUpgrade(SOUpgradeBase upgrade, int amount)
    {
        // Upgrades.Add(upgrade); not adding to allow duplicates
        health.UpgradeHealthBy(amount);
    }
    
    public void AddMaxShieldUpgrade(SOUpgradeBase upgrade, float amount)
    {
        Upgrades[upgrade] = GetUpgradeCount(upgrade) + 1;
        health.UpgradeMaxShieldBy(amount);
    }
    
    public void AddMagnetSizeUpgrade(SOUpgradeBase upgrade, float amount)
    {
        Upgrades[upgrade] = GetUpgradeCount(upgrade) + 1;
        resourceCollector.UpgradeMagnetSizeBy(amount);
    }
    
    public void AddWeaponUpgrade(SOUpgradeBase upgrade, SOWeaponUpgrade weaponUpgrade)
    {
        Upgrades[upgrade] = GetUpgradeCount(upgrade) + 1;
        weaponSystem.AddWeaponUpgrade(weaponUpgrade);
    }
    
    public void AddMaxHeatUpgrade(SOUpgradeBase upgrade, float amount)
    {
        Upgrades[upgrade] = GetUpgradeCount(upgrade) + 1;
        weaponSystem.AddMaxHeatUpgrade(amount);
    }
    
    public void AddDodgeUpgrade(SOUpgradeBase upgrade, int amount)
    {
        Upgrades[upgrade] = GetUpgradeCount(upgrade) + 1;
        movement.UpgradeDodgeAccumulationBy(amount);
    }
    
    public bool HasUpgrade(SOUpgradeBase upgrade)
    {
        return Upgrades.ContainsKey(upgrade);
    }

    
    public int GetUpgradeCount(SOUpgradeBase upgrade)
    {
        return Upgrades.GetValueOrDefault(upgrade, 0);
    }
    
    public SOWeaponUpgrade GetHighestWeaponUpgrade(SOWeaponData weaponData)
    {
        SOWeaponUpgrade highestUpgrade = null;
        int highestIndex = -1;
        
        for (int i = 0; i < weaponData.WeaponUpgrades.Count; i++)
        {
            var weaponUpgrade = weaponData.WeaponUpgrades[i];
            if (HasUpgrade(weaponUpgrade))
            {
                if (i > highestIndex)
                {
                    highestIndex = i;
                    highestUpgrade = weaponUpgrade;
                }
            }
        }
        
        return highestUpgrade;
    }
    
    #endregion Upgrades ---------------------------------------------------------------------------------
    
}