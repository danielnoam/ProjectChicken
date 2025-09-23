using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DNExtensions.MenuSystem;
using KBCore.Refs;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;
using Random = UnityEngine.Random;

public class UpgradeStore : MonoBehaviour
{
    public static UpgradeStore Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private bool closeStoreOnPurchase;
    [SerializeField, Min(1)] private int availableUpgrades = 3;
    [Foldout("Rarity Costs")]
    [SerializeField] private int commonCost = 25;
    [SerializeField] private int uncommonCost = 50;
    [SerializeField] private int rareCost = 100;
    [SerializeField] private int epicCost = 150;
    [EndFoldout]
    
    [Header("Gfx")]
    [SerializeField] private float storeAnimationDuration = 1f;
    [SerializeField] private Vector3 offsetBetweenUpgrades = new Vector3(25,0,0);
    
    [Header("Pay Animation")]
    [SerializeField] private float payAnimationDuration = 3f;
    [SerializeField] private Ease payAnimationMoveEase = Ease.InOutSine;
    [SerializeField] private Vector3 captainOffset;
    [SerializeField] private Ease payAnimationScaleEase = Ease.OutBounce;
    
    [Header("References")]
    [SerializeField] private SOPlayerStats playerStats;
    [SerializeField] private Transform storeGfx;
    [SerializeField] private Transform eggHolder;
    [SerializeField] private CaptainCock captain;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeStoreButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private PlayerUpgradesDisplay playerUpgradesDisplay;
    [SerializeField] private UpgradeEgg upgradeEggPrefab;
    [SerializeField] private GameObject chickenLegPrefab;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;
    
    private readonly List<SOUpgradeBase> _storeUpgradesPool = new List<SOUpgradeBase>();
    private readonly List<UpgradeEgg> _upgradeEggs = new List<UpgradeEgg>();
    private bool _isOpen;
    private bool _hasRerolled;
    private bool _hasPurchasedItem;
    private Sequence _storeSequence;
    private Sequence _paySequence;

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

        if (levelManager && transform.transform.position != levelManager.EnemyPosition)
        {
            transform.transform.position = levelManager.EnemyPosition;
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
        rerollButton.onClick.AddListener(RerollEggs);
        rerollButton.GetComponentInChildren<TextMeshProUGUI>().text = "Reroll";

        // Set the upgrades position with offset
        var startPosition = Vector3.zero - offsetBetweenUpgrades;
        for (int i = 0; i < availableUpgrades; i++)
        {
            var position = startPosition + offsetBetweenUpgrades * i;
            var egg = Instantiate(upgradeEggPrefab, eggHolder);
            egg.transform.localPosition = position;
            egg.SetPlayer(player);
            _upgradeEggs.Add(egg);
            egg.OnUpgradeBought += OnUpgradeBought;
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
        if (!stage || stage.StageType == StageType.Store && _isOpen) return;
        
        if (stage.StageType != StageType.Store && _isOpen)
        {
            CloseStore();
            return;
        } 

        SetStoreUpgradesPool(stage.UpgradesPool.ToList());
        if (_storeUpgradesPool is { Count: > 0 })
        {
            OpenStore();
            closeStoreButton.interactable = stage.AllowToCloseStore;
        }
        else
        {
            CloseStore();
        }
    }
    
    private void OnUpgradeBought(SOUpgradeBase upgrade)
    {
        int upgradeCost = GetCostByRarity(upgrade.ItemRarity);
        player.ResourceCollector.SpendCurrency(upgradeCost); 
        upgrade.ApplyUpgrade(player);
        _hasPurchasedItem = true;
        UpdateRerollButtonState();
        playerUpgradesDisplay.UpdatePlayerUpgrades(player);
        

        if (chickenLegPrefab)
        {
            if (_paySequence.isAlive) _paySequence.Complete();
            _paySequence = Sequence.Create();
            List<GameObject> chickenLegs = new List<GameObject>();
            for (int i = 0; i < Mathf.Clamp(upgradeCost,1 ,50); i++)
            {
                var chickenLeg = Instantiate(chickenLegPrefab, transform);
                chickenLegs.Add(chickenLeg);
                
                var chickenStartSize = chickenLeg.transform.localScale;
                chickenLeg.transform.localScale = Vector3.zero;
                
                _paySequence.Group(Tween.Position(chickenLeg.transform, 
                    player.transform.position,
                    captain.transform.position + captainOffset, 
                    duration: payAnimationDuration,
                    ease: payAnimationMoveEase,
                    startDelay: i * 0.05f));
                
                _paySequence.Group(Tween.Rotation(chickenLeg.transform, 
                    Random.rotation, 
                    duration: Random.Range(payAnimationDuration/2,payAnimationDuration),
                    ease: payAnimationMoveEase,
                    startDelay: i * 0.05f));
                
                _paySequence.Group(Tween.Scale(chickenLeg.transform, 
                    Vector3.zero, 
                    chickenStartSize * Random.Range(0.5f, 1.25f), 
                    duration: payAnimationDuration * Random.Range(payAnimationDuration/5,payAnimationDuration/3),
                    ease: payAnimationScaleEase,
                    startDelay: i * 0.05f));
            }
            
            _paySequence.OnComplete(() =>
            {
                foreach (var chickenLeg in chickenLegs.ToList())
                {
                    chickenLegs.Remove(chickenLeg);
                    Destroy(chickenLeg);
                }
            });
        }
        
        if (closeStoreOnPurchase)
        {
            if (_storeSequence.isAlive) _storeSequence.Stop();
            _storeSequence = Sequence.Create()
                .ChainDelay(1.3f)
                .OnComplete(CloseStore);
        }

    }

    private void RerollEggs()
    {
        if (_hasRerolled || _hasPurchasedItem) return;
        
        _hasRerolled = true;
        UpdateRerollButtonState();
        
        var tempPool = new List<SOUpgradeBase>(_storeUpgradesPool);
        var validEggCount = Mathf.Min(_upgradeEggs.Count, tempPool.Count);

        for (var index = 0; index < validEggCount; index++)
        {
            var egg = _upgradeEggs[index];
            var upgrade = GetUpgradeFromPool(tempPool);

            if (!upgrade) continue;
            tempPool.Remove(upgrade);
            var index1 = index;
            egg.RerollUpgrade(upgrade, GetCostByRarity(upgrade.ItemRarity), index1 * 0.25f);
        }
        
    }
    
    private void UpdateRerollButtonState()
    {
        if (_hasRerolled || _hasPurchasedItem)
        {
            rerollButton.GetComponentInChildren<TextMeshProUGUI>().text = "Used";
        }
        else
        {
            rerollButton.GetComponentInChildren<TextMeshProUGUI>().text = "Reroll";
        }
        
        
        rerollButton.interactable = !_hasRerolled && !_hasPurchasedItem;
        if (!rerollButton.interactable) rerollButton.GetComponentInChildren<MenuSelectableAnimator>().Deselect();

    }
    
    private void OpenStore()
    {
        if (_isOpen) return;
        _isOpen = true;
        
        _hasRerolled = false;
        _hasPurchasedItem = false;

        transform.transform.position = levelManager.EnemyPosition;
        storeGfx.gameObject.SetActive(true);
        UpdateRerollButtonState();
        captain.OnStoreOpen();
        playerUpgradesDisplay.UpdatePlayerUpgrades(player);
        
        if (_storeSequence.isAlive) _storeSequence.Stop();
        _storeSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1, storeAnimationDuration));
        
        SetEggsUpgrades();
        
        _storeSequence.OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true; 
            OnStoreOpened?.Invoke();
        });
    }

    private void CloseStore()
    {
        if (!_isOpen) return;
        
        _isOpen = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        playerUpgradesDisplay.ClearUpgrades();
        
        if (_paySequence.isAlive) _paySequence.Complete();
        if (_storeSequence.isAlive) _storeSequence.Stop();
        _storeSequence = Sequence.Create()
            .ChainCallback(() =>
            {
                foreach (var egg in _upgradeEggs)
                {
                    egg.ResetUpgrade(true);
                }
                captain.OnStoreClose();
            })
            .Group(Tween.Alpha(canvasGroup, 0, storeAnimationDuration))    
            .OnComplete(() =>
            {
                storeGfx.gameObject.SetActive(false);
                OnStoreClosed?.Invoke();
            });
    }
    

    private void SetStoreUpgradesPool(List<SOUpgradeBase> newPool)
    {
        if (newPool is not { Count: > 0 }) return;

        _storeUpgradesPool.Clear();

        foreach (var upgrade in newPool)
        {
            if (!upgrade) continue;
        
            if (upgrade.CanBeOfferedToPlayer(player))
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
            _storeSequence.ChainCallback(() => egg.SetUpgrade(true, upgrade,GetCostByRarity(upgrade.ItemRarity), index1 * 0.25f));
        }
        
        for (var index = validEggCount; index < _upgradeEggs.Count; index++)
        {
            var egg = _upgradeEggs[index];
            _storeSequence.ChainCallback(() => egg.ResetUpgrade(true));
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

    private int GetCostByRarity(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => commonCost,
            UpgradeRarity.Uncommon => uncommonCost,
            UpgradeRarity.Rare => rareCost,
            UpgradeRarity.Epic => epicCost,
            _ => uncommonCost
        };
    }
}