using System;
using System.Collections.Generic;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using VInspector;

public class RailPlayerResourceCollector : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private float specialWeaponMagnetRadius = 10f;
    [SerializeField, Self, HideInInspector] private RailPlayer player;

    
    [Separator]
    [SerializeField, VInspector.ReadOnly] private float currentMagnetRadius;
    [SerializeField, VInspector.ReadOnly] private int currentCurrency ;
    
    private readonly List<Resource> _resourcesInRange = new List<Resource>();
    private readonly Dictionary<ResourceType, Action<Resource>> _collectionActions = new Dictionary<ResourceType, Action<Resource>>();
    
    public int CurrentCurrency => currentCurrency;
    public event Action<Resource> OnResourceCollected;
    public event Action<int> OnCurrencyChanged;

    private void OnValidate()
    {
        this.ValidateRefs();
    }
    

    private void Update()
    {
        CheckResourcesInRange();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!player.Health.IsAlive()) return;

        if (other.TryGetComponent(out Resource resource))
        {
            CollectResource(resource);
        }
    }
    
    

    public void SetUp(int currency = 0)
    {
        _collectionActions.Clear();
        
        _collectionActions.Add(ResourceType.Currency, (resource) => AddCurrency(resource.CurrencyWorth));
        _collectionActions.Add(ResourceType.HealthPack, (resource) => player.Health.HealHealth(resource.HealthWorth));
        _collectionActions.Add(ResourceType.ShieldPack, (resource) => player.Health.HealShield(resource.ShieldWorth));
        _collectionActions.Add(ResourceType.SpecialWeapon, (resource) => player.WeaponSystem.SetActiveWeapon(resource.WeaponData));

        currentMagnetRadius = player.PlayerStats.BaseMagnetRadius;
        currentCurrency = currency;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    
    private void CheckResourcesInRange()
    {
        if (!player.Health.IsAlive()) return;
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, currentMagnetRadius);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out Resource resource))
            {
                if (!resource || _resourcesInRange.Contains(resource)) continue;
                
                float effectiveMagnetRadius = resource.ResourceType == ResourceType.SpecialWeapon 
                    ? specialWeaponMagnetRadius
                    : currentMagnetRadius;
                
                
                float distance = Vector3.Distance(transform.position, resource.transform.position);
                
            

                if (distance <= effectiveMagnetRadius)
                {
                    _resourcesInRange.Add(resource);
                    resource.SetMagnetized(transform);
                }
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
            
            float effectiveRadius = resource.ResourceType == ResourceType.SpecialWeapon 
                ? specialWeaponMagnetRadius
                : currentMagnetRadius;

            var distance = Vector3.Distance(transform.position, resource.transform.position);
            if (distance > effectiveRadius)
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
    

    public void UpgradeMagnetSizeBy(float amount)
    {
        currentMagnetRadius += amount;
        if (currentMagnetRadius > player.PlayerStats.MaxMagnetSize)
        {
            currentMagnetRadius = player.PlayerStats.MaxMagnetSize;
        }
    }
    
    
    [Button]
    public void AddCurrency(int amount)
    {
        currentCurrency += amount;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
    
    public void SpendCurrency(int amount)
    {
        if (currentCurrency < amount) return;
        
        currentCurrency -= amount;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawWireSphere(transform.position, currentMagnetRadius);
        UnityEditor.Handles.Label(transform.position + (Vector3.up * currentMagnetRadius), "Magnet Radius");
        
        Gizmos.color = Color.purple;
        Gizmos.DrawWireSphere(transform.position, specialWeaponMagnetRadius);
        UnityEditor.Handles.Label(transform.position + (Vector3.up * specialWeaponMagnetRadius), "Special Weapon Magnet Radius");
    }
#endif

}