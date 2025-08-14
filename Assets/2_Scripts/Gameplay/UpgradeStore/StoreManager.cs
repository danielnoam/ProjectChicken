using System;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField, Min(1)] private int availableUpgrades = 3;
    [SerializeField] private int baseRerollCost = 50;
    [SerializeField] private int maxRerollCost = 300;
    
    [Header("Gfx")]
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private Vector3 offsetBetweenUpgrades = new Vector3(25,0,0);
    
    [Header("References")]
    [SerializeField] private SOGameSettings gameSettings;
    [SerializeField] private Transform store;
    [SerializeField] private Transform storeGfx;
    [SerializeField] private Transform eggHolder;
    [SerializeField] private CaptainCock captain;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeStoreButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private UpgradeEgg upgradeEggPrefab;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;


    private readonly List<SOUpgradeBase> _storeUpgradesPool = new List<SOUpgradeBase>();
    private readonly List<UpgradeEgg> _upgradeEggs = new List<UpgradeEgg>();
    private bool _isOpen;
    private int _currentRerollCost;
    private Sequence _storeSequence;

    public event Action OnStoreOpened;
    public event Action OnStoreClosed;
    
    private void OnValidate()
    {
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        
        if (!player)
        {
            player = FindFirstObjectByType<RailPlayer>();
        }
    }

    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _isOpen = false;
        storeGfx.gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        closeStoreButton.onClick.AddListener(CloseStore);
        rerollButton.onClick.AddListener(RerollItems);
        rerollButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Reroll ({baseRerollCost})";


        // Set the upgrades position with offset
        var startPosition = Vector3.zero - offsetBetweenUpgrades;
        for (int i = 0; i < availableUpgrades; i++)
        {
            var position = startPosition + offsetBetweenUpgrades * i;
            var egg = Instantiate(upgradeEggPrefab, position, Quaternion.identity, eggHolder);
            _upgradeEggs.Add(egg);
            egg.OnUpgradeSelected += OnUpgradeSelected;
        }
        
        // Update the available pool of upgrades

    }



    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
        }
    }

    private void OnDisable()
    {
        
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }
        
    }
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage || stage.StageType != StageType.Store) return;
        
        store.transform.position = levelManager.EnemyPosition;
        
        SetStoreUpgradesPool(stage.UpgradesPool.ToList());
        
        if (_storeUpgradesPool is { Count: > 0 })
        {
            OpenStore();
        }
        else
        {
            CloseStore();
        }
    }
    
    private void OnUpgradeSelected(SOUpgradeBase upgrade)
    {
        upgrade.ApplyUpgrade(player);
        CloseStore();
    }

    private void RerollItems()
    {
        if (player.CurrentCurrency < _currentRerollCost) return;
        
        player.UpdateCurrency(-_currentRerollCost);
        _currentRerollCost += baseRerollCost;
        _currentRerollCost = Mathf.Clamp(_currentRerollCost, baseRerollCost, maxRerollCost);
        rerollButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Reroll ({_currentRerollCost})";
        foreach (var egg in _upgradeEggs)
        {
            egg.Reset(true);
        }
        
        if (_storeSequence.isAlive) _storeSequence.Stop();
        _storeSequence = Sequence.Create();

        SetEggsUpgrades();
    }
    
    
    private void OpenStore()
    {
        if (_storeSequence.isAlive) _storeSequence.Stop();

        storeGfx.gameObject.SetActive(true);
        _isOpen = true;
        captain.OnStoreOpen();
        
        _storeSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1, animationDuration));
        
        SetEggsUpgrades();
        
        _storeSequence.OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true; 
            _currentRerollCost = baseRerollCost;
            OnStoreOpened?.Invoke();
        });
    }



    private void CloseStore()
    {
        if (_storeSequence.isAlive) _storeSequence.Stop();
        
        _storeSequence = Sequence.Create()
            .ChainCallback(() =>
            {
                foreach (var egg in _upgradeEggs)
                {
                    egg.Reset(true);
                }
                captain.OnStoreClose();
            })
            .Group(Tween.Alpha(canvasGroup, 0, animationDuration))    
            .OnComplete(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                _isOpen = false;
                storeGfx.gameObject.SetActive(false);
                OnStoreClosed?.Invoke();
            });
    }
    

    private float GetWeightByRarity(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => 100f,
            UpgradeRarity.Uncommon => 60f,
            UpgradeRarity.Rare => 30f,
            UpgradeRarity.Epic => 10f,
            _ => 50f
        };
    }


    private void SetStoreUpgradesPool(List<SOUpgradeBase> newPool)
    {
        if (newPool is not { Count: > 0 }) return;
    
        _storeUpgradesPool.Clear();

        foreach (var upgrade in newPool)
        {
            if (!upgrade) continue;
        
  
            bool hasRequiredItems = true;
        
            if (upgrade.ItemNeededToUnlock is { Length: > 0 })
            {
                foreach (var requiredItem in upgrade.ItemNeededToUnlock)
                {
                    if (requiredItem && !player.HasUpgrade(requiredItem.ItemID))
                    {
                        hasRequiredItems = false;
                        break;
                    }
                }
            }
            
            if (hasRequiredItems)
            {
                _storeUpgradesPool.Add(upgrade);
            }
        }
    }
    
    private void SetEggsUpgrades()
    {
        var tempPool = new List<SOUpgradeBase>(_storeUpgradesPool);
    
        for (var index = 0; index < _upgradeEggs.Count; index++)
        {
            var egg = _upgradeEggs[index];
            var upgrade = GetUpgradeFromPool(tempPool);
        
            if (upgrade)
            {
                tempPool.Remove(upgrade);
            }
        
            var index1 = index;
            _storeSequence.ChainCallback(() => egg.SetUpgrade(upgrade, index1 * 0.5f));
        }
    }
    
    private SOUpgradeBase GetUpgradeFromPool(List<SOUpgradeBase> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var upgrade in pool)
        {
            totalWeight += GetWeightByRarity(upgrade.ItemRarity);
        }
    
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;
    
        foreach (var upgrade in pool)
        {
            currentWeight += GetWeightByRarity(upgrade.ItemRarity);
            if (randomValue <= currentWeight)
            {
                return upgrade;
            }
        }
    
        return null;
    }
}
