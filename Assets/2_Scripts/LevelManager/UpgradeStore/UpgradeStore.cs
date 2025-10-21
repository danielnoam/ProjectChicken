using System;
using System.Collections.Generic;
using System.Linq;
using DNExtensions.MenuSystem;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using VInspector;
using Random = UnityEngine.Random;


[Serializable]
public class RarityCosts
{
    [SerializeField] private int commonCost = 25;
    [SerializeField] private int uncommonCost = 50;
    [SerializeField] private int rareCost = 100;
    
    public void SetCosts(int common, int uncommon, int rare)
    {
        commonCost = common;
        uncommonCost = uncommon;
        rareCost = rare;
    }
    
    public void SetCosts(RarityCosts costs)
    {
        commonCost = costs.commonCost;
        uncommonCost = costs.uncommonCost;
        rareCost = costs.rareCost;
    }
    
    
    public int GetCostByRarity(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => commonCost,
            UpgradeRarity.Uncommon => uncommonCost,
            UpgradeRarity.Rare => rareCost,
            _ => uncommonCost
        };
    }
}

[Serializable]
public class RarityWeights
{
    [SerializeField] private float commonWeight = 100f;
    [SerializeField] private float uncommonWeight = 60f;
    [SerializeField] private float rareWeight = 30f;
    
    public void SetWeights(float common, float uncommon, float rare)
    {
        commonWeight = common;
        uncommonWeight = uncommon;
        rareWeight = rare;
    }
    
    public void SetWeights(RarityWeights weights)
    {
        commonWeight = weights.commonWeight;
        uncommonWeight = weights.uncommonWeight;
        rareWeight = weights.rareWeight;
    }
    
    public float GetWeightByRarity(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => commonWeight,
            UpgradeRarity.Uncommon => uncommonWeight,
            UpgradeRarity.Rare => rareWeight,
            _ => commonWeight
        };
    }
}



public class UpgradeStore : NavigatableUIScreen
{
    public static UpgradeStore Instance { get; private set; }
    
    [SerializeField] private SOPlayerStats playerStats;
    [SerializeField] private Transform storeGfx;
    [SerializeField] private Transform eggHolder;
    [SerializeField] private CaptainCock captain;
    [SerializeField] private Button closeStoreButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private PlayerUpgradesDisplay playerUpgradesDisplay;
    [SerializeField] private UpgradeEgg upgradeEggPrefab;
    [SerializeField] private GameObject chickenLegPrefab;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;
    
    [Header("Store Settings")]
    [SerializeField] private bool closeStoreOnPurchase;
    [SerializeField] private Vector3 offsetBetweenUpgrades = new Vector3(25,0,0);
    [SerializeField] private bool allowRerollAfterPurchase;
    [SerializeField, Min(0)] private int rerollCostAfterPurchase = 25;
    
    [Header("Animations")]
    [SerializeField] private float uiAnimationDuration = 1f;
    [SerializeField] private float payAnimationDuration = 3f;
    [SerializeField] private Ease payAnimationMoveEase = Ease.InOutSine;
    [SerializeField] private Ease payAnimationScaleEase = Ease.OutBounce;
    [SerializeField] private Vector3 captainOffset;
    
    
    private const int availableUpgrades = 3;
    
    private readonly List<SOUpgradeBase> _storeUpgradesPool = new List<SOUpgradeBase>();
    private readonly RarityWeights _storeRarityWeights = new RarityWeights();
    private readonly RarityCosts _storeRarityCosts = new RarityCosts();
    private readonly List<UpgradeEgg> _upgradeEggs = new List<UpgradeEgg>();
    private Sequence _storeSequence;
    private Sequence _paySequence;
    private bool _hasRerolled;
    private bool _hasPurchasedItem;
    
    public RailPlayer Player => player;
    public LevelManager LevelManager => levelManager;

    public event Action OnStoreOpened;
    public event Action OnStoreClosed;
    public event Action OnStoreRerolled;
    public event Action<UpgradeEgg> OnUpgradeSelected;
    
    
    
    protected override void OnValidate()
    {
        base.OnValidate();
        
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

        isVisible = false;
        storeGfx.gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        SetupUpgradeEggs();
        SetupButtons();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }
    }
    
    private void SetupUpgradeEggs()
    {
        if (upgradeEggPrefab)
        {
            var startPosition = Vector3.zero - offsetBetweenUpgrades;
            for (int i = 0; i < availableUpgrades; i++)
            {
                var position = startPosition + offsetBetweenUpgrades * i;
                var egg = Instantiate(upgradeEggPrefab, eggHolder);
                egg.transform.localPosition = position;
                egg.Setup(this);
                _upgradeEggs.Add(egg);
                egg.OnUpgradeBought += OnUpgradeBought;
                egg.OnEggSelected += OnEggSelected;
                AddSelectable(egg.Button);
            }
        }
    }

    private void OnEggSelected(UpgradeEgg upgradeEgg)
    {
        OnUpgradeSelected?.Invoke(upgradeEgg);
        
    }

    private void SetupButtons()
    {
        if (closeStoreButton)
        {
            AddSelectable(closeStoreButton);
            closeStoreButton.onClick.AddListener(CloseStore);
        }

        if (rerollButton)
        {
            AddSelectable(rerollButton);
            rerollButton.onClick.AddListener(RerollEggs);
        }
    }
    
    protected override void OnStageChanged(SOLevelStage stage)
    {
        base.OnStageChanged(stage);
        
        if (stage.StageType == StageType.Store && isVisible)
        {
            // Store is already open
            return;
        }
        if (stage.StageType != StageType.Store && isVisible)
        {
            // Store should not be open
            CloseStore();
            return;
        } 

        
        
        SetStoreUpgradesPool(stage.UpgradesPool.ToList(), stage.PoolRarityWeights, stage.PoolRarityCosts);
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
        int upgradeCost = _storeRarityCosts.GetCostByRarity(upgrade.ItemRarity);
        player.ResourceCollector.SpendCurrency(upgradeCost); 
        upgrade.ApplyUpgrade(player);
        _hasPurchasedItem = true;
        UpdateRerollButtonState();
        playerUpgradesDisplay.UpdatePlayerUpgrades(player);
        
        PlayPayAnimation(upgradeCost);
        
        if (closeStoreOnPurchase)
        {
            if (_storeSequence.isAlive) _storeSequence.Stop();
            _storeSequence = Sequence.Create()
                .ChainDelay(1.3f)
                .OnComplete(CloseStore);
        }
    }

    private void PlayPayAnimation(int upgradeCost)
    {
        if (!chickenLegPrefab) return;
        
        if (_paySequence.isAlive) _paySequence.Complete();
        _paySequence = Sequence.Create();
        List<GameObject> chickenLegs = new List<GameObject>();
        
        for (int i = 0; i < Mathf.Clamp(upgradeCost, 1, 50); i++)
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
                duration: Random.Range(payAnimationDuration/2, payAnimationDuration),
                ease: payAnimationMoveEase,
                startDelay: i * 0.05f));
            
            _paySequence.Group(Tween.Scale(chickenLeg.transform, 
                Vector3.zero, 
                chickenStartSize * Random.Range(0.5f, 1.25f), 
                duration: payAnimationDuration * Random.Range(payAnimationDuration/5, payAnimationDuration/3),
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

    private void RerollEggs()
    {
        if (_hasRerolled) return;
        if (_hasPurchasedItem && !allowRerollAfterPurchase) return;
        
        if (_hasPurchasedItem && allowRerollAfterPurchase && rerollCostAfterPurchase > 0)
        {
            if (player.ResourceCollector.CurrentCurrency < rerollCostAfterPurchase)
            {
                // Add feedback that player can't afford reroll
                return;
            }
            player.ResourceCollector.SpendCurrency(rerollCostAfterPurchase);
        }

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
            egg.RerollUpgrade(upgrade, _storeRarityCosts.GetCostByRarity(upgrade.ItemRarity), index1 * 0.25f);
        }
        
        OnStoreRerolled?.Invoke();
    }
    
    private void UpdateRerollButtonState()
    {
        bool canReroll = !_hasRerolled && (!_hasPurchasedItem || allowRerollAfterPurchase);
        
        if (canReroll && _hasPurchasedItem && allowRerollAfterPurchase && rerollCostAfterPurchase > 0)
        {
            if (player.ResourceCollector.CurrentCurrency >= rerollCostAfterPurchase)
            {
                canReroll = true;
            }
            else
            {
                canReroll = false;
            }
        }
    
        rerollButton.interactable = canReroll;
        if (!rerollButton.interactable) 
        {
            rerollButton.GetComponentInChildren<SelectableAnimator>().Deselect();
        }
    }
    
    private void OpenStore()
    {
        if (isVisible) return;
        isVisible = true;
        
        _hasRerolled = false;
        _hasPurchasedItem = false;

        transform.transform.position = levelManager.EnemyPosition;
        storeGfx.gameObject.SetActive(true);
        UpdateRerollButtonState();
        captain.OnStoreOpen();
        playerUpgradesDisplay.UpdatePlayerUpgrades(player);
        
        if (_storeSequence.isAlive) _storeSequence.Stop();
        _storeSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1, uiAnimationDuration));
        
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
        if (!isVisible) return;
        
        isVisible = false;
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
            .Group(Tween.Alpha(canvasGroup, 0, uiAnimationDuration))    
            .OnComplete(() =>
            {
                storeGfx.gameObject.SetActive(false);
                OnStoreClosed?.Invoke();
            });
    }

    private void SetStoreUpgradesPool(List<SOUpgradeBase> newPool, RarityWeights poolWeights, RarityCosts poolCosts)
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
        
        
        _storeRarityWeights.SetWeights(poolWeights);
        _storeRarityCosts.SetCosts(poolCosts);
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
            _storeSequence.ChainCallback(() => egg.SetUpgrade(true, upgrade, _storeRarityCosts.GetCostByRarity(upgrade.ItemRarity), index1 * 0.25f));
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
            totalWeight += _storeRarityWeights.GetWeightByRarity(upgrade.ItemRarity);
        }
    
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
    
        foreach (var upgrade in pool)
        {
            currentWeight += _storeRarityWeights.GetWeightByRarity(upgrade.ItemRarity);
            if (randomValue <= currentWeight)
            {
                return upgrade;
            }
        }
    
        return null;
    }
    



}