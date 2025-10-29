using System;
using DNExtensions;
using UnityEngine;
using PrimeTween;
using VInspector;


public class BackgroundObjectMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private Ease transitionEase = Ease.InOutSine;
    
    [Header("Stage Settings")]
    [SerializeField] private int startFromStage = 2;
    
    [Separator]
    [SerializeField, VInspector.ReadOnly] private bool isInitialized;
    [SerializeField, VInspector.ReadOnly] private int currentStageIndex;
    [SerializeField, VInspector.ReadOnly] private int totalStages;
    
    private LevelManager _levelManager;
    private Tween _currentTween;

    
    private void OnValidate()
    {
        totalStages = Mathf.Max(1, totalStages);
        transitionDuration = Mathf.Max(0.01f, transitionDuration);
        startFromStage = Mathf.Max(0, startFromStage);
        
        if (Application.isPlaying) return;
        
        if (startFromStage >= totalStages)
        {
            Debug.LogWarning($"[{gameObject.name}] Start From Stage ({startFromStage}) is >= Total Stages ({totalStages}). Object will never move!");
        }
    }


    private void Awake()
    {
        FindLevelManager();
    }
    

    private void OnEnable()
    {
        if (_levelManager)
        {
            _levelManager.OnStageChanged += OnStageChanged;
            _levelManager.OnLevelSet += OnLevelSet;
        }
    }

    private void OnDisable()
    {
        if (_levelManager)
        {
            _levelManager.OnStageChanged -= OnStageChanged;
            _levelManager.OnLevelSet -= OnLevelSet;
        }
        
        if (_currentTween.isAlive)
        {
            _currentTween.Stop();
        }
    }

    private void OnLevelSet(SOLevel level)
    {
        if (!level) return;
        
        Initialize();
        
    }

    private void FindLevelManager()
    {
        if (!_levelManager)
        {
            _levelManager = FindFirstObjectByType<LevelManager>();
        }
    }

    /// <summary>
    /// Initialize and set object to spawn position
    /// </summary>
    public void Initialize()
    {
        currentStageIndex = 0;
        transform.position = spawnPosition;
        isInitialized = true;
        
        if (_levelManager) totalStages = _levelManager.TotalStageCount;
    }

    private void OnStageChanged(SOLevelStage newStage)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        currentStageIndex++;

        // Skip if we haven't reached the start stage yet
        if (currentStageIndex < startFromStage)
        {
            return;
        }

        // Calculate adjusted stage progress
        int adjustedStageIndex = currentStageIndex - startFromStage;
        int adjustedTotalStages = totalStages - startFromStage;

        // Calculate progress (0 to 1)
        float progress = adjustedTotalStages <= 1 ? 1f : Mathf.Clamp01((float)adjustedStageIndex / (adjustedTotalStages - 1));
        
        // Calculate target position for this stage
        Vector3 stageTargetPosition = Vector3.Lerp(spawnPosition, targetPosition, progress);
        

        // Stop current tween if any
        if (_currentTween.isAlive)
        {
            _currentTween.Stop();
        }

        // Animate to new position
        _currentTween = Tween.Position(
            transform,
            stageTargetPosition,
            transitionDuration,
            transitionEase
        );
    }



    
    

#if UNITY_EDITOR
    
    
    /// <summary>
    /// Capture current position as spawn position
    /// </summary>
    [Button("Capture Current as Spawn Position")]
    public void CaptureCurrentAsSpawnPosition()
    {
        spawnPosition = transform.position;
        
    }

    /// <summary>
    /// Capture current position as target position
    /// </summary>
    [Button("Capture Current as Target Position")]
    public void CaptureCurrentAsTargetPosition()
    {
        targetPosition = transform.position;
        
    }
    
    


    private void OnDrawGizmosSelected()
    {

        // Draw spawn position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPosition, 0.5f);
        Gizmos.DrawLine(spawnPosition + Vector3.up * 0.5f, spawnPosition + Vector3.up * 1.5f);

        // Draw target position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
        Gizmos.DrawLine(targetPosition + Vector3.up * 0.5f, targetPosition + Vector3.up * 1.5f);

        // Draw path
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(spawnPosition, targetPosition);

        // Draw intermediate stage positions
        int previewStages = totalStages > 1 ? totalStages : 10;
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        for (int i = 1; i < previewStages; i++)
        {
            float progress = (float)i / (previewStages - 1);
            Vector3 intermediatePos = Vector3.Lerp(spawnPosition, targetPosition, progress);
            Gizmos.DrawWireSphere(intermediatePos, 0.2f);
        }
        
        
        // Draw current position with emphasis
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.7f);
        
        // Draw distance to spawn
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawLine(transform.position, spawnPosition);
        
        // Draw distance to target
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawLine(transform.position, targetPosition);

        // Draw stage labels
        DrawLabel(spawnPosition + Vector3.up * 2f, "SPAWN\n(Stage 0)", Color.green);
        DrawLabel(targetPosition + Vector3.up * 2f, "TARGET\n(Final Stage)", Color.red);
        
        // Draw current stage info
        if (Application.isPlaying && isInitialized)
        {
            float progress = totalStages <= 1 ? 1f : Mathf.Clamp01((float)currentStageIndex / (totalStages - 1));
            DrawLabel(transform.position + Vector3.up, $"Stage {currentStageIndex}/{totalStages}\n{progress:P0}", Color.cyan);
        }
    }

    private void DrawLabel(Vector3 position, string text, Color color)
    {

        GUIStyle style = new GUIStyle();
        style.normal.textColor = color;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        UnityEditor.Handles.Label(position, text, style);

    }









    
#endif
}