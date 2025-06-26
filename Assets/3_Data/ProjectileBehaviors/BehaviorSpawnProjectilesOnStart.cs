using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;


public class BehaviorSpawnProjectilesOnStart : ProjectileBehaviorBase
{
    [SerializeField] private float targetRadius = 10f;
    [SerializeField] private float spawnRadius = 1f;
    [SerializeField] private int maxProjectiles = 3;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private PlayerProjectile projectilePrefab;
    [SerializeReference] private List<ProjectileBehaviorBase> projectileBehaviors;

    
    private List<ChickenController> _targets;
    private int _spawnedProjectiles;

    public override void OnBehaviorSpawn(PlayerProjectile projectile, RailPlayer owner)
    {
        _targets = new List<ChickenController>();
        _spawnedProjectiles = 0;
        _targets = owner.GetAllTargets(maxProjectiles, targetRadius).ToList();

        if (_targets.Count <= 0 && _targets == null) return;
        foreach (var target in _targets)
        {
            Vector3 spawnPosition = projectile.transform.position;
            
            if (target && _spawnedProjectiles < maxProjectiles)
            {
                float angle = (360f / maxProjectiles) * _spawnedProjectiles * Mathf.Deg2Rad;
        
                Vector3 offset = new Vector3(Mathf.Cos(angle) * spawnRadius, Mathf.Sin(angle) * spawnRadius, 0f);
        
                Vector3 circleSpawnPosition = spawnPosition + offset;
                SpawnProjectile(projectile, owner, circleSpawnPosition, target);
                
                _spawnedProjectiles++;
            }
        }


    }

    public override void OnBehaviorMovement(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnBehaviorCollision(PlayerProjectile projectile, RailPlayer owner, ChickenController collision)
    {

    }

    public override void OnBehaviorDestroy(PlayerProjectile projectile, RailPlayer owner)
    {

    }

    public override void OnBehaviorDrawGizmos(PlayerProjectile projectile, RailPlayer owner)
    {

    }
    
    private void SpawnProjectile(PlayerProjectile projectile, RailPlayer owner, Vector3 spawnPosition, ChickenController target)
    {
        GameObject spawnedObj = Object.Instantiate(projectilePrefab.gameObject, spawnPosition, Quaternion.identity);
        PlayerProjectile miniProjectile = spawnedObj.GetComponent<PlayerProjectile>();
        miniProjectile.SetUpMiniProjectile(projectileBehaviors, projectileDamage, projectile.Weapon, owner, target);
    }
    
}