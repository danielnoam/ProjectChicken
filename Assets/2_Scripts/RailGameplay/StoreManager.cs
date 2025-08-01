using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private Transform storeGfx;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeStoreButton;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;


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
        
        closeStoreButton.onClick.AddListener(CloseStore);
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
        storeGfx.transform.position = levelManager.EnemyPosition;
        storeGfx.transform.rotation = player.SplineRotation;
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        if (stage.StageType == StageType.Store)
        {
            OpenStore();
        }
    }

    private void OpenStore()
    {
        if (_storeSequence.isAlive) _storeSequence.Stop();

        
        storeGfx.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        
        OnStoreOpened?.Invoke();
    }

    private void CloseStore()
    {
        if (_storeSequence.isAlive) _storeSequence.Stop();
        

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        OnStoreClosed?.Invoke();
        storeGfx.gameObject.SetActive(false);
    }
}
