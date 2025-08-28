using UnityEngine;

public class TargetReticle : MonoBehaviour
{
    [Header("Reticle Settings")]
    [SerializeField] private float aimLockDuration = 0.6f;
    [SerializeField] private float overheatPunchStrength = 1.2f;
    [SerializeField] private float normalPunchStrength = 0.2f;
    [SerializeField] private float punchDuration = 0.3f;

    [Header("References")]
    [SerializeField] private ReticleVisualsController reticle;
    [SerializeField] private RailPlayer player;
    


    private void OnEnable()
    {
        if (player)
        {
            player.WeaponSystem.OnWeaponUsed += OnWeaponUsed;
            player.WeaponSystem.OnWeaponHeatUpdatedEvent += OnHeatUpdated;
            player.WeaponSystem.OnWeaponOverheatedEvent += OnWeaponOverheated;
            player.WeaponSystem.OnWeaponHeatResetEvent += OnWeaponHeatReset; 
            player.WeaponSystem.OnAllowShootingChangedEvent += OnAllowShootingChanged;
            player.Aiming.OnAimLockStateChange += OnAimLockStateChanged;
            player.Health.OnDeath += OnPlayerDeath;
            player.LevelManager.OnStageChanged += OnStageChanged;
        }
    }

    private void OnDisable()
    {
        if (player)
        {
            player.WeaponSystem.OnWeaponUsed -= OnWeaponUsed;
            player.WeaponSystem.OnWeaponHeatUpdatedEvent -= OnHeatUpdated;
            player.WeaponSystem.OnWeaponOverheatedEvent -= OnWeaponOverheated;
            player.WeaponSystem.OnWeaponHeatResetEvent -= OnWeaponHeatReset; 
            player.WeaponSystem.OnAllowShootingChangedEvent -= OnAllowShootingChanged;
            player.Aiming.OnAimLockStateChange -= OnAimLockStateChanged;
            player.Health.OnDeath -= OnPlayerDeath;
            player.LevelManager.OnStageChanged -= OnStageChanged;
        }
    }
    

    private void OnWeaponUsed(WeaponInstance weaponInstance)
    {
        reticle?.PunchReticleSize(normalPunchStrength, punchDuration);
    }

    private void OnHeatUpdated(float heat)
    {
        var normalizedHeat = heat / player.WeaponSystem.MaxWeaponHeat;
        reticle?.SetEmissionStrength(normalizedHeat);
    }

    private void OnWeaponOverheated()
    {
        reticle?.PunchReticleSize(overheatPunchStrength, punchDuration);
        reticle?.SetEmissionStrength(1f);
    }

    private void OnWeaponHeatReset()
    {
        reticle?.SetEmissionStrength(0f);
    }

    private void OnAllowShootingChanged(bool allowShooting)
    {
        if (allowShooting)
        {
            reticle?.Show();
        }
        else
        {
            reticle?.Hide();
        }
    }

    private void OnAimLockStateChanged(bool isLocked, ChickenController target)
    {
        if (!player.WeaponSystem.AllowShooting) return;

        if (isLocked)
        {
            reticle?.EnableAimLockSize(aimLockDuration);
        }
        else
        {
            reticle?.DisableAimLockSize(aimLockDuration);
        }
    }

    private void OnPlayerDeath()
    {
        reticle?.Hide();
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        bool allowShooting = stage.AllowPlayerShootingAndAiming;
        
        if (allowShooting)
        {
            reticle?.Show();
        }
        else
        {
            reticle?.Hide();
        }
    }
    
}