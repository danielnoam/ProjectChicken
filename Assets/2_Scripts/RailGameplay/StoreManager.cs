using System;
using System.Collections.Generic;
using AYellowpaper;
using DNExtensions;
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
    
    [Header("Animation")]
    [SerializeField] private float animationDuration = 2f;
    
    [Header("References")]
    [SerializeField] private Transform store;
    [SerializeField] private Transform storeGfx;
    [SerializeField] private Transform eggHolder;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeStoreButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private UpgradeEgg upgradeEggPrefab;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;


    private ChanceList<InterfaceReference<IStoreItem, ScriptableObject>> _currentUpgradesPool = new ChanceList<InterfaceReference<IStoreItem, ScriptableObject>>();
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

        var offset = new Vector3(25, 0, 0);
        var startPosition = Vector3.zero - offset;
        for (int i = 0; i < availableUpgrades; i++)
        {
            var position = startPosition + offset * i;
            var egg = Instantiate(upgradeEggPrefab, position, Quaternion.identity, eggHolder);
            _upgradeEggs.Add(egg);
            egg.onUpgradeSelected += OnUpgradeSelected;

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

    private void Update()
    {
        if (!_isOpen) return;
        
        store.transform.position = levelManager.EnemyPosition;
        store.transform.rotation = player.SplineRotation;
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        if (stage.StageType == StageType.Store)
        {
            if (stage.UpgradesPool != null)
            {
                _currentUpgradesPool = stage.UpgradesPool;
                OpenStore();
            }
            else
            {
                CloseStore();
            }

        }
    }
    
    private void OnUpgradeSelected(IStoreItem upgrade)
    {
        Debug.Log($"Selected {upgrade.ItemName}");
        CloseStore();
    }

    private void RerollItems()
    {
        if (player.CurrentCurrency < _currentRerollCost) return;
        

        foreach (var egg in _upgradeEggs)
        {
            var upgrade = _currentUpgradesPool.GetRandomItem();
            egg.SetUpgrade(upgrade.Value);
            Debug.Log(upgrade.Value.ItemName);
        }
        player.UpdateCurrency(-_currentRerollCost);
        _currentRerollCost += baseRerollCost;
        _currentRerollCost = Mathf.Clamp(_currentRerollCost, baseRerollCost, maxRerollCost);
        rerollButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Reroll ({_currentRerollCost})";
    }
    
    
    private void OpenStore()
    {
        if (_storeSequence.isAlive) _storeSequence.Stop();

        _isOpen = true;
        
        _storeSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1, animationDuration))
            .OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true; 
                storeGfx.gameObject.SetActive(true);
                _currentRerollCost = baseRerollCost;
                foreach (var egg in _upgradeEggs)
                {
                    var upgrade = _currentUpgradesPool.GetRandomItem();
                    egg.SetUpgrade(upgrade.Value);
                    Debug.Log(upgrade.Value.ItemName);
                }
                OnStoreOpened?.Invoke();
            });
    }

    private void CloseStore()
    {
        if (_storeSequence.isAlive) _storeSequence.Stop();
        
        
        storeGfx.gameObject.SetActive(false);
        _storeSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 0, animationDuration))            .OnComplete(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                _isOpen = false;
                OnStoreClosed?.Invoke();
            });
    }
}
