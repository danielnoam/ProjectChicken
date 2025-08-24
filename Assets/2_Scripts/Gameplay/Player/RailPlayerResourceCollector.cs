using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;
using VInspector;

public class RailPlayerResourceCollector : MonoBehaviour
{

    [SerializeField, Self, HideInInspector] private RailPlayer player;
    private readonly List<Resource> _resourcesInRange = new List<Resource>();
    private readonly Dictionary<ResourceType, Action<Resource>> _collectionActions = new Dictionary<ResourceType, Action<Resource>>();
    private float _currentMagnetRadius;
    
    public int CurrentCurrency { get; private set; }
    
    public event Action<Resource> OnResourceCollected;
    public event Action<int> OnCurrencyChanged;

    private void OnValidate()
    {
        this.ValidateRefs();
    }
    
    private void OnEnable()
    {
        player.LevelManager.OnStageChanged += OnStageChanged;
    }
    
    private void OnDisable()
    {
        player.LevelManager.OnStageChanged -= OnStageChanged;
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
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        if (stage.StageType == StageType.Outro)
        {
            SaveManager.UpdatePlayerCurrency(CurrentCurrency);
        }
    }
    

    public void SetUp()
    {
        _collectionActions.Clear();
        
        _collectionActions.Add(ResourceType.Currency, (resource) => UpdateCurrency(resource.CurrencyWorth));
        _collectionActions.Add(ResourceType.HealthPack, (resource) => player.Health.HealHealth(resource.HealthWorth));
        _collectionActions.Add(ResourceType.ShieldPack, (resource) => player.Health.HealShield(resource.ShieldWorth));
        _collectionActions.Add(ResourceType.SpecialWeapon, (resource) => player.WeaponSystem.SetSpecialWeapon(resource.WeaponData));
        
        _currentMagnetRadius = 0;
        CurrentCurrency = 0;
        OnCurrencyChanged?.Invoke(CurrentCurrency);
    }

    
    private void CheckResourcesInRange()
    {
        if (!player.Health.IsAlive()) return;
        
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
    

    public void UpgradeMagnetSizeBy(float amount)
    {
        _currentMagnetRadius += amount;
        if (_currentMagnetRadius > player.GameSettings.MaxMagnetSize)
        {
            _currentMagnetRadius = player.GameSettings.MaxMagnetSize;
        }
    }
    
    
    [Button]
    public void UpdateCurrency(int amount)
    {
        CurrentCurrency += amount;
        OnCurrencyChanged?.Invoke(CurrentCurrency);
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