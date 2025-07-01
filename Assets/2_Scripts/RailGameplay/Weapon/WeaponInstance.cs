using System;
using UnityEngine;

[Serializable]
public class WeaponInstance
{
    public SOWeapon weaponData;
    public Transform weaponGfx;
    public WeaponReticle weaponReticle;
    public Transform[] weaponBarrels;
    
    
    public void OnWeaponSelected()
    {
        weaponGfx?.gameObject.SetActive(true);
        weaponReticle?.Show();
    }
    
    public void OnWeaponDeselected()
    {
        weaponGfx?.gameObject.SetActive(false);
        weaponReticle?.Hide();
    }

    public void OnWeaponUsed(RailPlayer owner, Transform[] barrelPositions)
    {
        weaponReticle?.PunchReticleSize(0.25f, 0.3f);
        FireWeapon(owner, barrelPositions);
    }

    public void OnWeaponOverheat()
    {
        weaponReticle?.PunchReticleSize(1f, 0.3f);
    }

    public void OnAimLocked(float size = 0.5f)
    {
        weaponReticle?.TweenReticleSize(size, 0.3f);
    }
    
    public void OnAimUnlocked(float size = 1f)
    {
        weaponReticle?.TweenReticleSize(size, 0.3f);
    }
    
    
    private void FireWeapon(RailPlayer owner, Transform[] barrelPositions)
    {
        if (!weaponData) return;
        
        switch (weaponData.WeaponType)
        {
            case WeaponType.Projectile:

                foreach (var barrelPosition in barrelPositions)
                {
                    FireProjectileWeapon(owner, barrelPosition.position);
                }

                break;
            case WeaponType.Hitscan:
                
                foreach (var barrelPosition in barrelPositions)
                {
                    FireHitscanWeapon(owner, barrelPosition.position);
                }

                break;
        }
    }
    
    
    
    
    #region Projectile  ---------------------------------------------------------------------------------

    

    private void FireProjectileWeapon(RailPlayer owner ,Vector3 position)
    {
        if (!weaponData.PlayerProjectilePrefab) return;
        
        if (weaponData.MaxTargets == 1)
        {
            InstantiateProjectile(owner,position, owner.GetTarget(weaponData.TargetCheckRadius));
            
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
    
    
    private void InstantiateProjectile(RailPlayer owner,Vector3 spawnPosition, ChickenController target = null)
    {
        GameObject projectileObj = UnityEngine.Object.Instantiate(weaponData.PlayerProjectilePrefab.gameObject, spawnPosition, Quaternion.identity);
        if (projectileObj.TryGetComponent(out PlayerProjectile projectile))
        {
            projectile.SetUpProjectile(weaponData, owner, this, target);
        }

    }

    #endregion Projectile  ---------------------------------------------------------------------------------

    

    #region Hitscan ----------------------------------------------------------------------------------

    private void FireHitscanWeapon(RailPlayer owner,Vector3 startPosition)
    {
        PlayFireEffect(startPosition, Quaternion.identity);
        
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

    
    
    
    private void HitscanHit(RailPlayer owner,ChickenController enemy)
    {
        if (!enemy) return;

        foreach (var behavior in weaponData.HitscanBehaviors)
        {
            behavior.OnHit(weaponData, owner, enemy);
        }

        enemy.TakeDamage(weaponData.Damage);
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

    
    
    
