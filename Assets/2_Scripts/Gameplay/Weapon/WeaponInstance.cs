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

    
    public SOWeaponData CurrentWeaponData { get; private set; }
    public WeaponGfx CurrentWeaponGfx { get; private set; }
    public Transform[] CurrentWeaponBarrels { get; private set; }
    public ControllerVibrationSource  ControllerVibrationSource { get; private set; }
    public CinemachineImpulseSource CinemachineImpulseSource {  get; private set; }
    
    
    private float _currentSpread;
    private float _lastShotTime;
    private float NormalizedSpread => CurrentWeaponData ? Mathf.InverseLerp(CurrentWeaponData.SpreadRange.minValue, CurrentWeaponData.SpreadRange.maxValue, _currentSpread) : 0f;

    
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
                
        InitializeSpread();
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
        CurrentWeaponData = weaponData;
        CurrentWeaponGfx = weaponGfx;
        CurrentWeaponBarrels = weaponBarrels;
    
        if (weaponData.WeaponUpgrades.Count == 0) return;
    
        // Get the highest level upgrade the player owns
        SOWeaponUpgrade activeWeaponUpgrade = player.GetHighestWeaponUpgrade(weaponData);
    
        if (activeWeaponUpgrade)
        {
            // Create a new runtime instance of weapon data
            CurrentWeaponData = ScriptableObject.CreateInstance<SOWeaponData>();
            CurrentWeaponData.name = weaponData.name + "_Upgraded";

            // Copy all base values from original
            CurrentWeaponData.CopyBaseWeaponData(weaponData);
            CurrentWeaponData.ApplyUpgradeData(activeWeaponUpgrade);
        
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
    
    #region Spread System ---------------------------------------------------------------------------------

    private void UpdateSpread()
    {
        if (!CurrentWeaponData) return;

        float currentTime = Time.time;
        float timeSinceLastShot = currentTime - _lastShotTime;
        
        if (timeSinceLastShot > 0.5f && _currentSpread > CurrentWeaponData.SpreadRange.minValue)
        {
            float spreadDecrease = CurrentWeaponData.SpreadDecayRate * Time.deltaTime;
            _currentSpread = Mathf.Max(_currentSpread - spreadDecrease, CurrentWeaponData.SpreadRange.minValue);
            weaponReticle?.SetSpreadVisualization(NormalizedSpread);
        }
    }

    private void AccumulateSpread()
    {
        if (!CurrentWeaponData) return;

        _lastShotTime = Time.time;
        
        _currentSpread += CurrentWeaponData.SpreadRate;
        _currentSpread = Mathf.Clamp(_currentSpread, CurrentWeaponData.SpreadRange.minValue, CurrentWeaponData.SpreadRange.maxValue);
    
        weaponReticle?.SetSpreadVisualization(NormalizedSpread);
    }
    

    private Vector3 CalculateSpreadDirection()
    {
        if (!CurrentWeaponData) return Vector3.zero;

        Vector3 randomDirection = Random.insideUnitCircle.normalized * _currentSpread;
        return new Vector3(randomDirection.x, randomDirection.y, 0f);
    }
    
    private void InitializeSpread()
    {
        if (CurrentWeaponData)
        {
            _currentSpread = CurrentWeaponData.SpreadRange.minValue;
        }
        _lastShotTime = 0f;
    }

    #endregion ---------------------------------------------------------------------------------

    
    #region Events ---------------------------------------------------------------------------------------------

    public void OnWeaponUpdate()
    {
        UpdateSpread();
    }
    
    public void OnWeaponSelected(bool allowShooting)
    {
        CurrentWeaponGfx?.Show();
        
        if (allowShooting)
        {
            weaponReticle?.Show();
        }
        else
        {
            weaponReticle?.Hide();
        }
    }
    
    public void OnWeaponDeselected(bool hideWeaponGfx = true)
    {
        if (hideWeaponGfx) CurrentWeaponGfx?.Hide();
        weaponReticle?.Hide();
    }
    
    public void OnWeaponUsed(RailPlayer owner)
    {

        Vector3 spreadDirection = CalculateSpreadDirection();
        CurrentWeaponGfx?.AnimateUsage();

        if (weaponReticle)
        {
            weaponReticle.PunchReticleSize(0.25f, 0.5f, 0.03f);
            weaponReticle.PunchReticlePosition(spreadDirection, 0.5f, 0.03f);
        }

        if (CurrentWeaponData && CurrentWeaponBarrels != null)
        {
            switch (CurrentWeaponData.WeaponType)
            {
                case WeaponType.Projectile:
                    Vector3[] barrelOffsets = CurrentWeaponData.BarrelAimOffsets;
                    for (int i = 0; i < CurrentWeaponBarrels.Length; i++)
                    {
                        var barrelPosition = CurrentWeaponBarrels[i];
                        if (barrelPosition)
                        {
                            Vector3 aimOffset = i < barrelOffsets.Length ? barrelOffsets[i] : Vector3.zero;
                            FireProjectileWeapon(owner, barrelPosition.position, aimOffset + spreadDirection);
                        }
                    }
                    break;
            
                case WeaponType.Hitscan:
                    foreach (var barrelPosition in CurrentWeaponBarrels)
                    {
                        if (barrelPosition)
                            FireHitscanWeapon(owner, barrelPosition.position, spreadDirection);
                    }
                    break;
            }
        }
        
        AccumulateSpread();
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
        if (!CurrentWeaponData.PlayerProjectilePrefab) return;
    
        if (CurrentWeaponData.MaxTargets == 1)
        {
            InstantiateProjectile(owner, position, owner.Aiming.GetTarget(CurrentWeaponData.TargetCheckRadius), aimOffset);
        } 
        else
        {
            ChickenStateController[] enemies = CurrentWeaponData.MaxTargets switch
            {
                0 => owner.Aiming.GetTargets(999, CurrentWeaponData.TargetCheckRadius),
                > 1 => owner.Aiming.GetTargets(CurrentWeaponData.MaxTargets, CurrentWeaponData.TargetCheckRadius),
                _ => System.Array.Empty<ChickenStateController>()
            };

            if (enemies.Length > 0)
            {
                foreach (ChickenStateController enemy in enemies)
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
    
    private void InstantiateProjectile(RailPlayer owner, Vector3 spawnPosition, ChickenStateController target = null, Vector3 aimOffset = default)
    {
        GameObject projectileObj = ObjectPooler.GetObjectFromPool(CurrentWeaponData.PlayerProjectilePrefab.gameObject, spawnPosition, Quaternion.identity);
        if (projectileObj && projectileObj.TryGetComponent(out PlayerProjectile projectile))
        {
            projectile.SetUpProjectile(owner, this, target, aimOffset);
        }
    }

    #endregion Projectile ---------------------------------------------------------------------------------

    
    #region Hitscan ----------------------------------------------------------------------------------

    private void FireHitscanWeapon(RailPlayer owner, Vector3 startPosition, Vector3 spreadDirection = default)
    {
        PlayFireEffect(startPosition, Quaternion.identity);
        

        foreach (var behavior in CurrentWeaponData.HitscanBehaviors)
        {
            behavior.OnStart(this, owner);
        }

        if (CurrentWeaponData.MaxTargets == 1)
        {
            ChickenStateController enemy = owner.Aiming.GetTarget(CurrentWeaponData.TargetCheckRadius);
            HitscanHit(owner, enemy, spreadDirection);
        } 
        else
        {
            ChickenStateController[] enemies = CurrentWeaponData.MaxTargets switch
            {
                0 => owner.Aiming.GetTargets(999, CurrentWeaponData.TargetCheckRadius),
                > 1 => owner.Aiming.GetTargets(CurrentWeaponData.MaxTargets, CurrentWeaponData.TargetCheckRadius),
                _ => System.Array.Empty<ChickenStateController>()
            };
            foreach (ChickenStateController enemy in enemies)
            {
                HitscanHit(owner, enemy, spreadDirection);
            }
        }
        
        foreach (var behavior in CurrentWeaponData.HitscanBehaviors)
        {
            behavior.OnEnd(this, owner);
        }
    }

    private void HitscanHit(RailPlayer owner, ChickenStateController enemy, Vector3 spreadDirection = default)
    {
        if (!enemy) return;

        foreach (var behavior in CurrentWeaponData.HitscanBehaviors)
        {
            behavior.OnHit(this, owner, enemy);
        }
        
        Vector3 impactPosition = enemy.transform.position;
        if (spreadDirection != Vector3.zero)
        {
            impactPosition += spreadDirection * 0.5f;
        }
    
        PlayImpactEffect(impactPosition, Quaternion.identity);
    }

    #endregion Hitscan ----------------------------------------------------------------------------------

    
    #region Effects ---------------------------------------------------------------------------------------
    
    public void PlayImpactEffect(Vector3 position, Quaternion rotation)
    {
        if (CurrentWeaponData.ImpactEffectPrefab)
        {
            Object.Instantiate(CurrentWeaponData.ImpactEffectPrefab.gameObject, position, rotation);
        }
        
        if (CurrentWeaponData.ImpactSound)
        {
            CurrentWeaponData.ImpactSound.PlayAtPoint(position);
        }
        
    }
    
    public void PlayFireEffect(Vector3 position, Quaternion rotation, AudioSource audioSource = null)
    {
        if (CurrentWeaponData.FireEffectPrefab)
        {
            Object.Instantiate(CurrentWeaponData.FireEffectPrefab.gameObject, position, rotation);
        }
        
        if (CurrentWeaponData.FireSound)
        {
            if (audioSource)
            {
                CurrentWeaponData.FireSound.Play(audioSource);
            }
            else
            {
                CurrentWeaponData.FireSound.PlayAtPoint(position);
            }
        }
    }
    

    #endregion Effects ---------------------------------------------------------------------------------------
}