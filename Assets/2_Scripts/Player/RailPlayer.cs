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
public class RailPlayer : MonoBehaviour
{

    [Header("Positions")]
    [SerializeField] private Transform cameraPositions;
    [SerializeField] private Transform followCameraTarget;
    [SerializeField] private Transform storeCameraTarget;
    [SerializeField] private Transform storeCameraLookAtTarget;
    [SerializeField] private Transform textFollowPosition;
    
    [Header("References")]
    [SerializeField] private SOPlayerStats playerStats;
    [SerializeField] private LevelManager levelManager;
    [SerializeField, Self, HideInInspector] private RailPlayerInput input;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming aiming;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement movement;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem weaponSystem;
    [SerializeField, Self, HideInInspector] private RailPlayerResourceCollector resourceCollector;
    [SerializeField, Self, HideInInspector] private RailPlayerHealth health;


    
    
    public Dictionary<SOUpgradeBase, int> Upgrades { get; private set; } = new Dictionary<SOUpgradeBase, int>();
    public RailPlayerAiming Aiming => aiming;
    public RailPlayerWeaponSystem WeaponSystem => weaponSystem;
    public RailPlayerMovement Movement => movement;
    public RailPlayerResourceCollector ResourceCollector => resourceCollector;
    public RailPlayerHealth Health => health;
    public LevelManager LevelManager => levelManager;
    public SOPlayerStats PlayerStats => playerStats;
    
    
    
    
    private void OnValidate()
    {
        this.ValidateRefs();
        
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
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
        
        if (runProgress.PlayerUpgrades != null)
        {
            Upgrades = runProgress.PlayerUpgrades;
            
            var upgradesCopy = new Dictionary<SOUpgradeBase, int>(runProgress.PlayerUpgrades);
            foreach (var upgrade in upgradesCopy)
            {
                upgrade.Key.ApplyUpgrade(this);
            }
        }
        else
        {
            Upgrades = new Dictionary<SOUpgradeBase, int>();
        }
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
        
        if (savePoint.PlayerUpgrades != null)
        {
            Upgrades = savePoint.PlayerUpgrades;
            
            var upgradesCopy = new Dictionary<SOUpgradeBase, int>(savePoint.PlayerUpgrades);
            foreach (var upgrade in upgradesCopy)
            {
                upgrade.Key.ApplyUpgrade(this);
            }
        }
        else
        {
            Upgrades = new Dictionary<SOUpgradeBase, int>();
        }
    }
    

    
    
    #region Upgrades ---------------------------------------------------------------------------------
    
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
    
    

    #region  Helpers ---------------------------------------------------------------------------------
    
    public Transform GetFollowCameraTarget()
    {
        return followCameraTarget ? followCameraTarget : transform;
    }
    
    public Transform GetStoreCameraTarget()
    {
        return storeCameraTarget ? storeCameraTarget : transform;
    }
    
    public Transform GetStoreCameraLookAtTarget()
    {
        return storeCameraLookAtTarget ? storeCameraLookAtTarget : transform;
    }

    public Transform GetTextFollowPosition()
    {
        return textFollowPosition ? textFollowPosition : transform;
    }
    
    public Transform GetRandomCameraPosition()
    {
        if (!cameraPositions) return transform;
        
        int randomIndex = UnityEngine.Random.Range(0, cameraPositions.childCount);
        return cameraPositions.GetChild(randomIndex);
    }
    
    #endregion Helpers ---------------------------------------------------------------------------------
}