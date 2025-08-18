using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using KBCore.Refs;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(RailPlayerInput))]
[RequireComponent(typeof(RailPlayerMovement))]
[RequireComponent(typeof(RailPlayerAiming))]
[RequireComponent(typeof(RailPlayerWeaponSystem))]
[RequireComponent(typeof(RailPlayerResourceCollector))]
[RequireComponent(typeof(RailPlayerHealth))]
[RequireComponent(typeof(ControllerVibrationSource))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class RailPlayer : MonoBehaviour
{
    [Header("Camera Positions")]
    [SerializeField] private Transform cameraPositions;
    [SerializeField] private Transform followCameraTarget;
    [SerializeField] private Transform storeCameraTarget;
    [SerializeField] private Transform storeCameraLookAtTarget;
    
    [Header("References")]
    [SerializeField, Self, HideInInspector] private RailPlayerInput input;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming aiming;
    [SerializeField, Self, HideInInspector] private RailPlayerMovement movement;
    [SerializeField, Self, HideInInspector] private RailPlayerWeaponSystem weaponSystem;
    [SerializeField, Self, HideInInspector] private RailPlayerResourceCollector resourceCollector;
    [SerializeField, Self, HideInInspector] private RailPlayerHealth health;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;
    [SerializeField, Self, HideInInspector] private CinemachineImpulseSource cinemachineImpulseSource;
    [SerializeField] private SOGameSettings gameSettings;
    [SerializeField] private LevelManager levelManager;
    
    
    private float _pauseTimer;
    private bool _pauseInputHeld;
    
    public List<SOUpgradeBase> Upgrades { get; private set; } = new List<SOUpgradeBase>();
    public RailPlayerAiming Aiming => aiming;
    public RailPlayerWeaponSystem WeaponSystem => weaponSystem;
    public RailPlayerMovement Movement => movement;
    public RailPlayerResourceCollector ResourceCollector => resourceCollector;
    public RailPlayerHealth Health => health;
    public LevelManager LevelManager => levelManager;
    public SOGameSettings GameSettings => gameSettings;
    

    public event Action<float> OnPauseTimerChanged;
    public event Action OnPause;
    
    
    
    private void OnValidate()
    {
        this.ValidateRefs();
        
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
    }
    
    
    private void OnEnable()
    {
        levelManager.OnRestartedFromSavePoint += RestartedFromSavePoint;
        input.OnPauseActionEvent += OnPauseAction;
    }
    
    private void OnDisable()
    {
        levelManager.OnRestartedFromSavePoint -= RestartedFromSavePoint;
        input.OnPauseActionEvent -= OnPauseAction;
    }
    
    private void Update()
    {
        if (_pauseInputHeld)
        {
            _pauseTimer += Time.deltaTime;
            OnPauseTimerChanged?.Invoke(_pauseTimer / gameSettings.TimeToPause);
            if (_pauseTimer >= gameSettings.TimeToPause)
            {
                OnPause?.Invoke();
            }
        }
    }
    

    
    private void RestartedFromSavePoint(SavePointInformation savePoint)
    {
        if (savePoint == null) return;

        Upgrades = savePoint.PlayerUpgrades;
    }
    
    private void OnPauseAction(InputAction.CallbackContext context)
    {
        if (!Health.IsAlive()) return;
        
        if (context.started)
        {
            _pauseInputHeld = true;
            _pauseTimer = 0f;
            OnPauseTimerChanged?.Invoke(_pauseTimer / gameSettings.TimeToPause);
        }
        else if (context.canceled)
        {
            _pauseInputHeld = false;
            _pauseTimer = 0f;
            OnPauseTimerChanged?.Invoke(_pauseTimer / gameSettings.TimeToPause);
        }
    }

    
    
    #region Upgrades
    
    public void AddHealthUpgrade(SOUpgradeBase upgrade, int amount)
    {
        Upgrades.Add(upgrade);
        health.UpgradeHealthBy(amount);
    }
    
    public void AddMaxShieldUpgrade(SOUpgradeBase upgrade, float amount)
    {
        Upgrades.Add(upgrade);
        health.UpgradeMaxShieldBy(amount);
    }
    
    public void AddMagnetSizeUpgrade(SOUpgradeBase upgrade, float amount)
    {
        Upgrades.Add(upgrade);
        resourceCollector.UpgradeMagnetSizeBy(amount);
    }
    
    public void AddWeaponUpgrade(SOUpgradeBase upgrade, SOWeaponUpgrade weaponUpgrade)
    {
        Upgrades.Add(upgrade);
        weaponSystem.AddWeaponUpgrade(weaponUpgrade);
    }
    
    public void AddMaxHeatUpgrade(SOUpgradeBase upgrade, float amount)
    {
        Upgrades.Add(upgrade);
        weaponSystem.AddMaxHeatUpgrade(amount);
    }
    
    public void AddDodgeUpgrade(SOUpgradeBase upgrade, int amount)
    {
        Upgrades.Add(upgrade);
        movement.UpgradeDodgeAccumulationBy(amount);
    }
    
    public bool HasUpgrade(int itemID)
    {
        if (itemID == 0 || Upgrades.Count <= 0) return false;
        return Upgrades.Any(upgrade => upgrade.ItemID == itemID);
    }
    
    public SOWeaponUpgrade GetHighestWeaponUpgrade(SOWeaponData weaponData)
    {
        SOWeaponUpgrade highestUpgrade = null;
        int highestIndex = -1;
        
        for (int i = 0; i < weaponData.WeaponUpgrades.Count; i++)
        {
            var weaponUpgrade = weaponData.WeaponUpgrades[i];
            if (HasUpgrade(weaponUpgrade.ItemID))
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
    
    #endregion
    

    #region Camera Helpers
    
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
    
    public Transform GetRandomCameraPosition()
    {
        if (!cameraPositions) return transform;
        
        int randomIndex = UnityEngine.Random.Range(0, cameraPositions.childCount);
        return cameraPositions.GetChild(randomIndex);
    }
    
    #endregion
}