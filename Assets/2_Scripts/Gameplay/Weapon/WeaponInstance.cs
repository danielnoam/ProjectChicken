using DNExtensions;
using Unity.Cinemachine;
using UnityEngine;

[System.Serializable]
public class WeaponUpgradeAssets
{
    [SerializeField] private SOWeaponUpgrade upgrade;
    [SerializeField] private WeaponGfx upgradeGfx;
    [SerializeField] private Transform[] upgradeBarrels;
    
    public SOWeaponUpgrade Upgrade => upgrade;
    public WeaponGfx UpgradeGfx => upgradeGfx;
    public Transform[] UpgradeBarrels => upgradeBarrels;
    
    public WeaponUpgradeAssets(SOWeaponUpgrade upgrade)
    {
        this.upgrade = upgrade;
    }
}

[System.Serializable]
public class WeaponInstance
{
    public SOWeaponData weaponData;
    public WeaponGfx weaponGfx;
    public ReticleVisualsController weaponReticle;
    public Transform[] weaponBarrels;
    public WeaponUpgradeAssets[] upgradeAssets;

    
    public SOWeaponData WeaponData { get; private set; }
    public WeaponGfx CurrentWeaponGfx { get; private set; }
    public Transform[] CurrentWeaponBarrels { get; private set; }
    public ControllerVibrationSource  ControllerVibrationSource { get; private set; }
    public CinemachineImpulseSource CinemachineImpulseSource {  get; private set; }
    
    
    

    public void SetUpWeaponInstance(RailPlayer player, ControllerVibrationSource controllerVibrationSource, CinemachineImpulseSource cinemachineImpulseSource)
    {
        ControllerVibrationSource = controllerVibrationSource;
        CinemachineImpulseSource = cinemachineImpulseSource;
        
        weaponGfx?.gameObject.SetActive(true);
        weaponGfx?.Hide(false);
        if (upgradeAssets != null)
        {
            foreach (var asset in upgradeAssets)
            {
                asset?.UpgradeGfx?.gameObject.SetActive(true);
                asset?.UpgradeGfx?.Hide(false);
            }
        }
        
        ApplyWeaponUpgrade(player);
    }

    
    private WeaponUpgradeAssets GetUpgradeAssets(SOWeaponUpgrade upgrade)
    {
        if (upgradeAssets == null) return null;
        
        foreach (var asset in upgradeAssets)
        {
            if (asset.Upgrade == upgrade)
                return asset;
        }
        return null;
    }
    
    
    public void ApplyWeaponUpgrade(RailPlayer player)
    {
        WeaponData = weaponData;
        CurrentWeaponGfx = weaponGfx;
        CurrentWeaponBarrels = weaponBarrels;
    
        if (weaponData.WeaponUpgrades.Count == 0) return;
    
        // Get the highest level upgrade the player owns
        SOWeaponUpgrade activeWeaponUpgrade = player.GetHighestWeaponUpgrade(weaponData);
    
        if (activeWeaponUpgrade)
        {
            // Create a new runtime instance of weapon data
            WeaponData = ScriptableObject.CreateInstance<SOWeaponData>();
            WeaponData.name = weaponData.name + "_Upgraded";

            // Copy all base values from original
            WeaponData.CopyBaseWeaponData(weaponData);
            WeaponData.ApplyUpgradeData(activeWeaponUpgrade);
        
            // Find the upgrade assets for this specific upgrade
            WeaponUpgradeAssets upgradeAsset = GetUpgradeAssets(activeWeaponUpgrade);
        
            if (upgradeAsset != null)
            {
                // Handle GFX override
                if (activeWeaponUpgrade.OverrideWeaponGfx && upgradeAsset.UpgradeGfx)
                {
                    CurrentWeaponGfx = upgradeAsset.UpgradeGfx;
                }
                else
                {
                    CurrentWeaponGfx = weaponGfx;
                }
            
                // Handle Barrels override
                if (activeWeaponUpgrade.OverrideWeaponBarrels && upgradeAsset.UpgradeBarrels is { Length: > 0 })
                {
                    CurrentWeaponBarrels = upgradeAsset.UpgradeBarrels;
                }
                else
                {
                    CurrentWeaponBarrels = weaponBarrels;
                }
            }
            else
            {
                // Fallback to base assets
                CurrentWeaponGfx = weaponGfx;
                CurrentWeaponBarrels = weaponBarrels;
            }
        }
    }

    

    #region Events ---------------------------------------------------------------------------------------------

    public void OnWeaponSelected(bool allowShooting)
    {
        CurrentWeaponGfx.Show();
        
        if (allowShooting)
        {
            weaponReticle?.Show();
        }
        else
        {
            weaponReticle?.Hide();
        }
    }
    
    public void OnWeaponDeselected()
    {
        CurrentWeaponGfx?.Hide();
        weaponReticle?.Hide();
    }

    
    public void OnWeaponUsed(RailPlayer owner)
    {
        weaponReticle?.PunchReticleSize(0.25f, 0.5f, 0.03f);
        CurrentWeaponGfx?.AnimateUsage();

        if (WeaponData && CurrentWeaponBarrels != null)
        {
            switch (WeaponData.WeaponType)
            {
                case WeaponType.Projectile:
                    
                    
                    Vector3[] barrelOffsets = WeaponData.BarrelAimOffsets;
                    for (int i = 0; i < CurrentWeaponBarrels.Length; i++)
                    {
                        var barrelPosition = CurrentWeaponBarrels[i];
                        if (barrelPosition)
                        {
                            Vector3 aimOffset = i < barrelOffsets.Length ? barrelOffsets[i] : Vector3.zero;
                            FireProjectileWeapon(owner, barrelPosition.position, aimOffset);
                        }
                    }
                    break;
                
                case WeaponType.Hitscan:
                    foreach (var barrelPosition in CurrentWeaponBarrels)
                    {
                        if (barrelPosition)
                            FireHitscanWeapon(owner, barrelPosition.position);
                    }
                    break;
            }
        }
    }

    public void OnHeatChanged(float heat)
    {
        var normalizedHeat = heat;
        weaponReticle?.SetEmissionStrength(normalizedHeat);
    }

    public void OnWeaponOverheat()
    {
        weaponReticle?.PunchReticleSize(1f, 0.5f, 0.03f);
    }

    public void OnAimLocked()
    {
        weaponReticle?.EnableAimLockSize(0.4f);
    }
    
    public void OnAimUnlocked(float duration = 0.4f)
    {
        weaponReticle?.DisableAimLockSize(duration);
    }
    
    #endregion Events ---------------------------------------------------------------------------------------------
    

    #region Projectile ---------------------------------------------------------------------------------

    private void FireProjectileWeapon(RailPlayer owner, Vector3 position, Vector3 aimOffset)
    {
        if (!WeaponData.PlayerProjectilePrefab) return;
    
        if (WeaponData.MaxTargets == 1)
        {
            InstantiateProjectile(owner, position, owner.Aiming.GetTarget(WeaponData.TargetCheckRadius), aimOffset);
        } 
        else
        {
            ChickenController[] enemies = WeaponData.MaxTargets switch
            {
                0 => owner.Aiming.GetTargets(999, WeaponData.TargetCheckRadius),
                > 1 => owner.Aiming.GetTargets(WeaponData.MaxTargets, WeaponData.TargetCheckRadius),
                _ => System.Array.Empty<ChickenController>()
            };

            if (enemies.Length > 0)
            {
                foreach (ChickenController enemy in enemies)
                {
                    if (enemy)
                    {
                        InstantiateProjectile(owner, position, enemy, aimOffset);
                    }
                }
            }
            else
            {
                InstantiateProjectile(owner, position, null, aimOffset);
            }
        }
    }
    
    private void InstantiateProjectile(RailPlayer owner, Vector3 spawnPosition, ChickenController target = null, Vector3 aimOffset = default)
    {
        GameObject projectileObj = ObjectPooler.GetObjectFromPool(WeaponData.PlayerProjectilePrefab.gameObject, spawnPosition, Quaternion.identity);
        if (projectileObj && projectileObj.TryGetComponent(out PlayerProjectile projectile))
        {
            projectile.SetUpProjectile(owner, this, target, aimOffset);
        }
    }

    #endregion Projectile ---------------------------------------------------------------------------------

    
    #region Hitscan ----------------------------------------------------------------------------------

    private void FireHitscanWeapon(RailPlayer owner, Vector3 startPosition)
    {
        PlayFireEffect(startPosition, Quaternion.identity);
        
        // weaponData.HitscanBehaviors now contains the upgraded behaviors
        foreach (var behavior in WeaponData.HitscanBehaviors)
        {
            behavior.OnStart(this, owner);
        }

        if (WeaponData.MaxTargets == 1)
        {
            ChickenController enemy = owner.Aiming.GetTarget(WeaponData.TargetCheckRadius);
            HitscanHit(owner, enemy);
        } 
        else
        {
            ChickenController[] enemies = WeaponData.MaxTargets switch
            {
                0 => owner.Aiming.GetTargets(999, WeaponData.TargetCheckRadius),
                > 1 => owner.Aiming.GetTargets(WeaponData.MaxTargets, WeaponData.TargetCheckRadius),
                _ => System.Array.Empty<ChickenController>()
            };
            foreach (ChickenController enemy in enemies)
            {
                HitscanHit(owner, enemy);
            }
        }
        
        foreach (var behavior in WeaponData.HitscanBehaviors)
        {
            behavior.OnEnd(this, owner);
        }
    }

    private void HitscanHit(RailPlayer owner, ChickenController enemy)
    {
        if (!enemy) return;

        foreach (var behavior in WeaponData.HitscanBehaviors)
        {
            behavior.OnHit(this, owner, enemy);
        }
        
        PlayImpactEffect(enemy.transform.position, Quaternion.identity);
    } 

    #endregion Hitscan ----------------------------------------------------------------------------------

    
    #region Effects ---------------------------------------------------------------------------------------
    
    public void PlayImpactEffect(Vector3 position, Quaternion rotation)
    {
        if (WeaponData.ImpactEffectPrefab)
        {
            Object.Instantiate(WeaponData.ImpactEffectPrefab.gameObject, position, rotation);
        }
        
        if (WeaponData.ImpactSound)
        {
            WeaponData.ImpactSound.PlayAtPoint(position);
        }
        
    }
    
    public void PlayFireEffect(Vector3 position, Quaternion rotation, AudioSource audioSource = null)
    {
        if (WeaponData.FireEffectPrefab)
        {
            Object.Instantiate(WeaponData.FireEffectPrefab.gameObject, position, rotation);
        }
        
        if (WeaponData.FireSound)
        {
            if (audioSource)
            {
                WeaponData.FireSound.Play(audioSource);
            }
            else
            {
                WeaponData.FireSound.PlayAtPoint(position);
            }
        }
    }
    

    #endregion Effects ---------------------------------------------------------------------------------------
}