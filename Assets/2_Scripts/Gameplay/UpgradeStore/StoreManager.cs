using System;
using System.Collections.Generic;
using System.Linq;
using KBCore.Refs;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField, Min(1)] private int availableUpgrades = 3;
    [SerializeField] private int baseRerollCost = 5;
    [SerializeField] private int rerollCostIncrease = 15;
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
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;


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
        
        this.ValidateRefs();
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
        rerollButton.onClick.AddListener(RerollEggs);
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
        if (!stage || stage.StageType != StageType.Store && _isOpen == false) return;

        if (stage.StageType != StageType.Store && _isOpen)
        {
            CloseStore();
            return;
        } 
        

        if (_storeUpgradesPool is { Count: > 0 })
        {
            store.transform.position = levelManager.EnemyPosition;
            SetStoreUpgradesPool(stage.UpgradesPool.ToList());
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

    private void RerollEggs()
    {
        if (player.ResourceCollector.CurrentCurrency < _currentRerollCost) return;
        
        player.ResourceCollector.UpdateCurrency(-_currentRerollCost);
        _currentRerollCost += rerollCostIncrease;
        _currentRerollCost = Mathf.Clamp(_currentRerollCost, baseRerollCost, maxRerollCost);
        rerollButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Reroll ({_currentRerollCost})";
        rerollButton.interactable = player.ResourceCollector.CurrentCurrency < _currentRerollCost;
        
        if (_storeSequence.isAlive) _storeSequence.Stop();
        _storeSequence = Sequence.Create();
        
        foreach (var egg in _upgradeEggs)
        {
            egg.Reset(true);
        }
        
        SetEggsUpgrades();
    }
    
    
    private void OpenStore()
    {
        if (_isOpen) return;
        _isOpen = true;
        


        storeGfx.gameObject.SetActive(true);
        rerollButton.interactable = player.ResourceCollector.CurrentCurrency < _currentRerollCost;
        captain.OnStoreOpen();
        
        if (_storeSequence.isAlive) _storeSequence.Stop();
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
        if (_isOpen == false) return;
        _isOpen = true;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
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
            if (player.HasUpgrade(upgrade.ItemID)) continue;

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
        var validEggCount = Mathf.Min(_upgradeEggs.Count, tempPool.Count);

        for (var index = 0; index < validEggCount; index++)
        {
            var egg = _upgradeEggs[index];
            var upgrade = GetUpgradeFromPool(tempPool);

            if (!upgrade) continue;
            tempPool.Remove(upgrade);
            var index1 = index;
            _storeSequence.ChainCallback(() => egg.SetUpgrade(upgrade, index1 * 0.5f));
        }
    
        // Hide remaining eggs that don't have upgrades
        for (var index = validEggCount; index < _upgradeEggs.Count; index++)
        {
            var egg = _upgradeEggs[index];
            _storeSequence.ChainCallback(() => egg.Reset(true));
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
