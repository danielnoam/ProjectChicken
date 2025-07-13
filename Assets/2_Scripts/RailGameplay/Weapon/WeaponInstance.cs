using System;
using DNExtensions;
using UnityEngine;

[Serializable]
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

[Serializable]
public class WeaponInstance
{
    public SOWeaponData baseWeaponData;
    public Transform weaponGfx;
    public WeaponReticle weaponReticle;
    public Transform[] weaponBarrels;
    public WeaponUpgradeAssets[] upgradeAssets;
    
    public SOWeaponData weaponData { get; private set; }
    public Transform currentWeaponGfx { get; private set; }
    public Transform[] currentWeaponBarrels { get; private set; }

    public void SetUpWeaponInstance()
    {
        weaponData = baseWeaponData;
        currentWeaponGfx = weaponGfx;
        currentWeaponBarrels = weaponBarrels;
        
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
            weaponData = ScriptableObject.CreateInstance<SOWeaponData>();
            weaponData.name = baseWeaponData.name + "_Upgraded";

            // Copy all base values from original
            weaponData.CopyBaseWeaponData(baseWeaponData);
            weaponData.ApplyUpgradeData(activeWeaponUpgrade);
            
            // Find the upgrade assets for this specific upgrade
            WeaponUpgradeAssets upgradeAsset = GetUpgradeAssets(activeWeaponUpgrade);
            
            if (upgradeAsset != null)
            {
                // Handle GFX override
                if (activeWeaponUpgrade.OverrideWeaponGfx && upgradeAsset.UpgradeGfx)
                {
                    currentWeaponGfx = upgradeAsset.UpgradeGfx;
                }
                else
                {
                    currentWeaponGfx = weaponGfx;
                }
                
                // Handle Barrels override
                if (activeWeaponUpgrade.OverrideWeaponBarrels && upgradeAsset.UpgradeBarrels is { Length: > 0 })
                {
                    currentWeaponBarrels = upgradeAsset.UpgradeBarrels;
                }
                else
                {
                    currentWeaponBarrels = weaponBarrels;
                }
            }
            else
            {
                // Fallback to base assets
                currentWeaponGfx = weaponGfx;
                currentWeaponBarrels = weaponBarrels;
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
        if (currentWeaponGfx) currentWeaponGfx.gameObject.SetActive(true);
        
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
        

        if (weaponData && currentWeaponBarrels != null)
        {
            switch (weaponData.WeaponType)
            {
                case WeaponType.Projectile:
                    foreach (var barrelPosition in currentWeaponBarrels)
                    {
                        if (barrelPosition)
                            FireProjectileWeapon(owner, barrelPosition.position);
                    }
                    break;
                
                case WeaponType.Hitscan:
                    foreach (var barrelPosition in currentWeaponBarrels)
                    {
                        if (barrelPosition)
                            FireHitscanWeapon(owner, barrelPosition.position);
                    }
                    break;
            }
        }
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
        if (!weaponData.PlayerProjectilePrefab) return;
        

        if (weaponData.MaxTargets == 1)
        {
            InstantiateProjectile(owner, position, owner.GetTarget(weaponData.TargetCheckRadius));
        } 
        else
        {
            ChickenController[] enemies = weaponData.MaxTargets switch
            {
                0 => owner.GetAllTargets(999, weaponData.TargetCheckRadius),
                > 1 => owner.GetAllTargets(weaponData.MaxTargets, weaponData.TargetCheckRadius),
                _ => Array.Empty<ChickenController>()
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
        GameObject projectileObj = ObjectPooler.GetObjectFromPool(weaponData.PlayerProjectilePrefab.gameObject, spawnPosition, Quaternion.identity);
        if (projectileObj && projectileObj.TryGetComponent(out PlayerProjectile projectile))
        {
            projectile.SetUpProjectile(weaponData, owner, this, target);
        }
    }

    #endregion Projectile ---------------------------------------------------------------------------------

    
    #region Hitscan ----------------------------------------------------------------------------------

    private void FireHitscanWeapon(RailPlayer owner, Vector3 startPosition)
    {
        PlayFireEffect(startPosition, Quaternion.identity);
        
        // weaponData.HitscanBehaviors now contains the upgraded behaviors
        foreach (var behavior in weaponData.HitscanBehaviors)
        {
            behavior.OnStart(weaponData, owner);
        }

        if (weaponData.MaxTargets == 1)
        {
            ChickenController enemy = owner.GetTarget(weaponData.TargetCheckRadius);
            HitscanHit(owner, enemy);
        } 
        else
        {
            ChickenController[] enemies = weaponData.MaxTargets switch
            {
                0 => owner.GetAllTargets(999, weaponData.TargetCheckRadius),
                > 1 => owner.GetAllTargets(weaponData.MaxTargets, weaponData.TargetCheckRadius),
                _ => Array.Empty<ChickenController>()
            };
            foreach (ChickenController enemy in enemies)
            {
                HitscanHit(owner, enemy);
            }
        }
        
        foreach (var behavior in weaponData.HitscanBehaviors)
        {
            behavior.OnEnd(weaponData, owner);
        }
    }

    private void HitscanHit(RailPlayer owner, ChickenController enemy)
    {
        if (!enemy) return;

        foreach (var behavior in weaponData.HitscanBehaviors)
        {
            behavior.OnHit(weaponData, owner, enemy);
        }
        
        PlayImpactEffect(enemy.transform.position, Quaternion.identity);
    } 

    #endregion Hitscan ----------------------------------------------------------------------------------

    
    #region Effects ---------------------------------------------------------------------------------------
    
    public void PlayImpactEffect(Vector3 position, Quaternion rotation)
    {
        if (weaponData.ImpactEffectPrefab)
        {
            UnityEngine.Object.Instantiate(weaponData.ImpactEffectPrefab.gameObject, position, rotation);
        }
        
        if (weaponData.ImpactSound)
        {
            weaponData.ImpactSound.PlayAtPoint(position);
        }

        if (weaponData.ShakeCameraOnImpact)
        {
            CameraManager.Instance?.ShakeCamera(weaponData.ImpactShakeSettings.impulseShape, weaponData.ImpactShakeSettings.intensity, weaponData.ImpactShakeSettings.duration);
        }
    }
    
    public void PlayFireEffect(Vector3 position, Quaternion rotation, AudioSource audioSource = null)
    {
        if (weaponData.FireEffectPrefab)
        {
            UnityEngine.Object.Instantiate(weaponData.FireEffectPrefab.gameObject, position, rotation);
        }
        
        if (weaponData.FireSound)
        {
            if (audioSource)
            {
                weaponData.FireSound.Play(audioSource);
            }
            else
            {
                weaponData.FireSound.PlayAtPoint(position);
            }
        }
        
        if (weaponData.ShakeCameraOnFire)
        {
            CameraManager.Instance?.ShakeCamera(weaponData.FireShakeSettings.impulseShape, weaponData.FireShakeSettings.intensity, weaponData.FireShakeSettings.duration);
        }
    }

    #endregion Effects ---------------------------------------------------------------------------------------
}