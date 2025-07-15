
using DNExtensions;
using Unity.Cinemachine;
using UnityEngine;

[System.Serializable]
public class WeaponUpgradeAssets
{
    [SerializeField] private SOWeaponUpgrade upgrade;
    [SerializeField] private Transform upgradeGfx;
    [SerializeField] private Transform[] upgradeBarrels;
    
    public SOWeaponUpgrade Upgrade => upgrade;
    public Transform UpgradeGfx => upgradeGfx;
    public Transform[] UpgradeBarrels => upgradeBarrels;
    
    public WeaponUpgradeAssets(SOWeaponUpgrade upgrade)
    {
        this.upgrade = upgrade;
    }
}

[System.Serializable]
public class WeaponInstance
{
    public SOWeaponData baseWeaponData;
    public Transform weaponGfx;
    public WeaponReticle weaponReticle;
    public Transform[] weaponBarrels;
    public WeaponUpgradeAssets[] upgradeAssets;
    
    public SOWeaponData WeaponData { get; private set; }
    public Transform CurrentWeaponGfx { get; private set; }
    public Transform[] CurrentWeaponBarrels { get; private set; }
    public ControllerRumbleSource  ControllerRumbleSource { get; private set; }
    public CinemachineImpulseSource CinemachineImpulseSource {  get; private set; }

    public void SetUpWeaponInstance(ControllerRumbleSource controllerRumbleSource, CinemachineImpulseSource cinemachineImpulseSource)
    {
        WeaponData = baseWeaponData;
        CurrentWeaponGfx = weaponGfx;
        CurrentWeaponBarrels = weaponBarrels;
        ControllerRumbleSource = controllerRumbleSource;
        CinemachineImpulseSource = cinemachineImpulseSource;
        
        
        if (baseWeaponData.WeaponUpgrades.Count == 0) return;
        
        // Find the highest level upgrade the player owns
        SOWeaponUpgrade latestUpgrade = null;
        int highestIndex = -1;
    
        for (int i = 0; i < baseWeaponData.WeaponUpgrades.Count; i++)
        {
            var weaponUpgrade = baseWeaponData.WeaponUpgrades[i];
            if (SaveManager.HasStoreItem(weaponUpgrade.ItemID))
            {
                if (i > highestIndex)
                {
                    highestIndex = i;
                    latestUpgrade = weaponUpgrade;
                }
            }
        }

        var activeWeaponUpgrade = latestUpgrade;
        
        if (activeWeaponUpgrade)
        {
            // Create a new runtime instance of weapon data
            WeaponData = ScriptableObject.CreateInstance<SOWeaponData>();
            WeaponData.name = baseWeaponData.name + "_Upgraded";

            // Copy all base values from original
            WeaponData.CopyBaseWeaponData(baseWeaponData);
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
                // Debug.LogWarning($"No upgrade assets found for upgrade '{activeWeaponUpgrade.ItemName}' on weapon '{baseWeaponData.WeaponName}'");
            }
            
            // Debug.Log($"Applied upgrade '{activeWeaponUpgrade.ItemName}' to weapon '{baseWeaponData.WeaponName}'");
        }
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
    
    public void OnWeaponSelected(bool allowShooting)
    {
        // Hide all GFX first
        if (weaponGfx) weaponGfx.gameObject.SetActive(false);
        
        // Hide all upgrade GFX
        if (upgradeAssets != null)
        {
            foreach (var asset in upgradeAssets)
            {
                if (asset.UpgradeGfx) asset.UpgradeGfx.gameObject.SetActive(false);
            }
        }
        
        // Show current active GFX
        if (CurrentWeaponGfx) CurrentWeaponGfx.gameObject.SetActive(true);
        
        UpdateReticleVisibility(allowShooting);
    }
    
    public void OnWeaponDeselected()
    {
        // Hide all GFX
        if (weaponGfx) weaponGfx.gameObject.SetActive(false);
        
        // Hide all upgrade GFX
        if (upgradeAssets != null)
        {
            foreach (var asset in upgradeAssets)
            {
                if (asset.UpgradeGfx) asset.UpgradeGfx.gameObject.SetActive(false);
            }
        }
        
        weaponReticle?.Hide();
    }

    public void OnWeaponUsed(RailPlayer owner)
    {
        weaponReticle?.PunchReticleSize(0.25f, 0.5f, 0.03f);
        

        if (WeaponData && CurrentWeaponBarrels != null)
        {
            switch (WeaponData.WeaponType)
            {
                case WeaponType.Projectile:
                    foreach (var barrelPosition in CurrentWeaponBarrels)
                    {
                        if (barrelPosition)
                            FireProjectileWeapon(owner, barrelPosition.position);
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
    
    public void UpdateReticleVisibility(bool allowShooting)
    {
        if (allowShooting)
        {
            weaponReticle?.Show();
        }
        else
        {
            weaponReticle?.Hide();
        }
    }
    

    #region Projectile ---------------------------------------------------------------------------------

    private void FireProjectileWeapon(RailPlayer owner, Vector3 position)
    {
        if (!WeaponData.PlayerProjectilePrefab) return;
        

        if (WeaponData.MaxTargets == 1)
        {
            InstantiateProjectile(owner, position, owner.GetTarget(WeaponData.TargetCheckRadius));
        } 
        else
        {
            ChickenController[] enemies = WeaponData.MaxTargets switch
            {
                0 => owner.GetAllTargets(999, WeaponData.TargetCheckRadius),
                > 1 => owner.GetAllTargets(WeaponData.MaxTargets, WeaponData.TargetCheckRadius),
                _ => System.Array.Empty<ChickenController>()
            };

            if (enemies.Length > 0)
            {
                foreach (ChickenController enemy in enemies)
                {
                    if (enemy)
                    {
                        InstantiateProjectile(owner, position, enemy);
                    }
                }
            }
            else
            {
                InstantiateProjectile(owner, position, null);
            }
        }
    }
    
    private void InstantiateProjectile(RailPlayer owner, Vector3 spawnPosition, ChickenController target = null)
    {
        GameObject projectileObj = ObjectPooler.GetObjectFromPool(WeaponData.PlayerProjectilePrefab.gameObject, spawnPosition, Quaternion.identity);
        if (projectileObj && projectileObj.TryGetComponent(out PlayerProjectile projectile))
        {
            projectile.SetUpProjectile(WeaponData, owner, this, target);
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
            ChickenController enemy = owner.GetTarget(WeaponData.TargetCheckRadius);
            HitscanHit(owner, enemy);
        } 
        else
        {
            ChickenController[] enemies = WeaponData.MaxTargets switch
            {
                0 => owner.GetAllTargets(999, WeaponData.TargetCheckRadius),
                > 1 => owner.GetAllTargets(WeaponData.MaxTargets, WeaponData.TargetCheckRadius),
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