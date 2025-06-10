using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectile : MonoBehaviour
{
    
    public Rigidbody Rigidbody { get; private set; }
    public SOWeaponData WeaponDataData { get; private set; }
    public RailPlayer Owner { get; private set;  }
    public ChickenEnemy Target { get; private set;  }
    public Vector3 StartDirection { get; private set; }
    public float Damage { get; private set; }
    public float Lifetime { get; private set; }
    public bool IsInitialized { get; private set; }




    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        if (!IsInitialized) return;
        
        CheckLiftTime();
    }


    private void FixedUpdate()
    {
        if (!IsInitialized) return;
        
        MoveProjectile();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsInitialized) return;
        
        if (other.TryGetComponent(out ChickenEnemy enemy))
        {
            ProjectileHit(enemy);
        }


    }
    
    

        
    #region Base -------------------------------------------------------------------------

    private void CheckLiftTime()
    {
        Lifetime -= Time.deltaTime;
        if (Lifetime <= 0f)
        {
            DestroyProjectile();
        }
    }
    
    
    public void SetUpProjectile(SOWeaponData weaponDataData, RailPlayer player )
    {
        if (IsInitialized) return;
        
        // Set up the projectile
        WeaponDataData = weaponDataData;
        Owner = player;
        Lifetime = weaponDataData.ProjectileLifetime;
        Damage = weaponDataData.Damage;
        StartDirection = player.GetAimDirection();
        Target = player.GetTarget();
        IsInitialized = true;
        
        // Apply custom behaviors on spawn
        WeaponDataData?.OnProjectileSpawn(this, player , Target);
    }

    #endregion Base -------------------------------------------------------------------------
    

    
    #region Custom Behivors -----------------------------------------------------------------------------

    protected virtual void MoveProjectile()
    {
        // Apply custom behaviors on movement
        WeaponDataData?.OnProjectileMovement(this, Owner, Target);
    }

    
    protected virtual void DestroyProjectile()
    {
        // Apply custom behaviors on destroy
        WeaponDataData?.OnProjectileDestroy(this, Owner, Target);
        
        // Destroy the projectile object
        Destroy(gameObject);
    }


    protected virtual void ProjectileHit(ChickenEnemy collision)
    {
        
        // Apply custom behaviors on collision
        WeaponDataData?.OnProjectileCollision(this, Owner, Target, collision);
        
        // Play impact effect
        WeaponDataData?.PlayImpactEffect(transform.position, Quaternion.identity);
        
        // Apply damage to the enemy object
        collision.TakeDamage(Damage);
        
        
        // Destroy the projectile on impact
        DestroyProjectile();
    }

    #endregion Custom Behivors -----------------------------------------------------------------------------



#if UNITY_EDITOR
    #region Editor -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        WeaponDataData?.OnProjectileDrawGizmos(this, Owner, Target);
    }

    #endregion Editor -------------------------------------------------------------------------
#endif

    
}
