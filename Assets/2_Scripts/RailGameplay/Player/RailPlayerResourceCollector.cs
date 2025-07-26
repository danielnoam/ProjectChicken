using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;

public class RailPlayerResourceCollector : MonoBehaviour
{
    [Header("Resource Collection")]
    [SerializeField, Min(0)] private float baseMagnetRadius = 14f;
    [SerializeField] private SOResourceMagnetUpgrade[] resourceMagnetUpgrades = Array.Empty<SOResourceMagnetUpgrade>();
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    
    private float _currentMagnetRadius;
    private readonly List<Resource> _resourcesInRange = new List<Resource>();
    private readonly Dictionary<ResourceType, Action<Resource>> _collectionActions = new Dictionary<ResourceType, Action<Resource>>();
    
    public event Action<Resource> OnResourceCollected;

    private void OnValidate()
    {
        this.ValidateRefs();
    }

    private void Awake()
    {
        // Initialize collection actions
        _collectionActions.Add(ResourceType.Currency, (resource) => player.UpdateCurrency(resource.CurrencyWorth));
        _collectionActions.Add(ResourceType.HealthPack, (resource) => player.HealHealth(resource.HealthWorth));
        _collectionActions.Add(ResourceType.ShieldPack, (resource) => player.HealShield(resource.ShieldWorth));
        _collectionActions.Add(ResourceType.SpecialWeapon, (resource) => player.PlayerWeapon.SetSpecialWeapon(resource.WeaponData));
        
        // Calculate magnet radius with upgrades
        _currentMagnetRadius = baseMagnetRadius + TotalResourceMagnetUpgrades();
    }
    
    private void Update()
    {
        CheckResourcesInRange();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!player.IsAlive()) return;

        if (other.TryGetComponent(out Resource resource))
        {
            CollectResource(resource);
        }
    }
    
    private void CheckResourcesInRange()
    {
        if (!player.IsAlive()) return;
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, _currentMagnetRadius);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out Resource resource))
            {
                if (!resource || _resourcesInRange.Contains(resource)) continue;
                _resourcesInRange.Add(resource);
                resource.SetMagnetized(transform);
            }
        }
        
        for (int i = _resourcesInRange.Count - 1; i >= 0; i--)
        {
            var resource = _resourcesInRange[i];
            
            if (!resource)
            {
                _resourcesInRange.RemoveAt(i);
                continue;
            }
    
            var distance = Vector3.Distance(transform.position, resource.transform.position);
            if (distance > _currentMagnetRadius)
            {
                resource.ReleaseFromMagnetization();
                _resourcesInRange.RemoveAt(i);
            }
        }
    }
    
    private void CollectResource(Resource resource)
    {
        if (!resource) return;
        
        if (_collectionActions.TryGetValue(resource.ResourceType, out var action))
        {
            action(resource);
        }
        
        _resourcesInRange.Remove(resource);
        resource.ResourceCollected();
        OnResourceCollected?.Invoke(resource);
    }
    
    private float TotalResourceMagnetUpgrades()
    {
        var magnet = 0f;
        
        foreach (var upgrade in resourceMagnetUpgrades)
        {
            if (SaveManager.HasStoreItem(upgrade.ItemID))
            {
                magnet += upgrade.MagnetUpgradeAmount;
            }
        }

        return magnet;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawWireSphere(transform.position, _currentMagnetRadius);
        UnityEditor.Handles.Label(transform.position + (Vector3.up * _currentMagnetRadius), "Magnet Radius");
    }
#endif
}