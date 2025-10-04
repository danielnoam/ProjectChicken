using System;
using System.Collections;
using KBCore.Refs;
using PrimeTween;
using TMPro;
using UnityEngine;

public class StageTaskVisualizer : MonoBehaviour
{
    
    [Header("Task Info")]
    [SerializeField] private bool showEnemyInfo = true;
    [SerializeField] private bool showObstacleInfo = true;
    [SerializeField] private float punchDuration = 0.3f;
    [SerializeField] private float punchScale = 0.2f;
    
    
    [Header("Task Text Visualization")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 2, 0);
    [SerializeField] private float targetFollowSpeed = 35f;
    [SerializeField] private float cameraFollowSpeed = 15f;
    [SerializeField] private float fadeDuration = 0.5f;
    
    
    [Header("References")]
    [SerializeField] private CanvasGroup taskTextGroup;
    [SerializeField] private TextMeshProUGUI taskText;
    [SerializeField] private ReplaceTextWithBinding textBinding;
    [SerializeField] private CanvasGroup enemiesRemainingCanvasGroup;
    [SerializeField] private TextMeshProUGUI enemiesRemainingText;
    [SerializeField] private CanvasGroup obstaclesBrokeCanvasGroup;
    [SerializeField] private TextMeshProUGUI obstaclesBrokeText;
    [SerializeField] private CanvasGroup obstaclesPassedThroughCanvasGroup;
    [SerializeField] private TextMeshProUGUI obstaclesPassedThroughText;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;
    
    private Camera _camera;
    private Transform _followTarget;
    private Sequence _fadeSequence;
    private Coroutine _hideCoroutine;
    private int _obstacleToBreak;
    private int _obstacleToPassThrough;

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
        _camera = Camera.main;
    }

    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
            levelManager.ObstacleManager.OnObstacleBroke += OnObstacleBroke;
            levelManager.ObstacleManager.OnPlayerPassedThroughObstacle += PlayerPassedThroughObstacle;
            levelManager.EnemySpawner.OnEnemyDeath += OnEnemyDeath;
        }
        
        if (player)
        {
            _followTarget = player.GetTextFollowPosition();
        }
    }
    
    private void OnDisable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
            levelManager.ObstacleManager.OnObstacleBroke -= OnObstacleBroke;
            levelManager.ObstacleManager.OnPlayerPassedThroughObstacle -= PlayerPassedThroughObstacle;
            levelManager.EnemySpawner.OnEnemyDeath -= OnEnemyDeath;
        }
        
        _followTarget = null;
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
    }
    


    private void Update()
    {
        if (levelManager)
        {
            if (showEnemyInfo) enemiesRemainingText.text = $"{levelManager.EnemiesLeft}";
            if (showObstacleInfo)
            {
                obstaclesBrokeText.text = $"{Mathf.Min(levelManager.ObstaclesBroke, _obstacleToBreak)} / {_obstacleToBreak}";
                obstaclesPassedThroughText.text = $"{Mathf.Min(levelManager.ObstaclesPassedThrough, _obstacleToPassThrough)} / {_obstacleToPassThrough}";
            }
        }
        
        if (taskTextGroup)
        {
            if (_followTarget) 
            {
                Vector3 targetPosition = _followTarget.position + targetOffset;
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, targetFollowSpeed * Time.deltaTime);
                taskTextGroup.transform.position = smoothedPosition;
            }

            if (_camera)
            {
                Vector3 direction = transform.position - _camera.transform.position;
                direction.y = 0; // Keep only the horizontal direction
                if (direction.sqrMagnitude > 0.001f) // Avoid zero-length direction
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    taskTextGroup.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, cameraFollowSpeed * Time.deltaTime);
                }
            }
        }

    }
    
    private void OnEnemyDeath(ChickenStateController enemy)
    {
        Tween.PunchScale(enemiesRemainingCanvasGroup.transform, Vector3.one * punchScale, punchDuration, 1);
    }
    
    private void PlayerPassedThroughObstacle(PassthroughObstacle passthroughObstacle)
    {
        if (!passthroughObstacle) return;
        Tween.PunchScale(obstaclesPassedThroughCanvasGroup.transform, Vector3.one * punchScale, punchDuration, 1);
    }

    private void OnObstacleBroke(NormalObstacle normalObstacle)
    {
        if (!normalObstacle) return;
        Tween.PunchScale(obstaclesBrokeCanvasGroup.transform, Vector3.one * punchScale, punchDuration, 1);
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        enemiesRemainingCanvasGroup.gameObject.SetActive(stage.StageType == StageType.EnemyWave && showEnemyInfo);
        obstaclesBrokeCanvasGroup.gameObject.SetActive(false);
        obstaclesPassedThroughCanvasGroup.gameObject.SetActive(false);

        if (stage.StageType == StageType.Task)
        {
            string taskString = $"";

            foreach (var stageTask in stage.Tasks)
            {
                if (stageTask is null) continue;

                if (stageTask is PassThroughObstaclesTask passThroughObstaclesTask)
                {
                    obstaclesPassedThroughCanvasGroup.gameObject.SetActive(showObstacleInfo);
                    _obstacleToPassThrough = passThroughObstaclesTask.TargetAmount;
                }
                else if (stageTask is BreakObstaclesTask breakObstaclesTask)
                {
                    obstaclesBrokeCanvasGroup.gameObject.SetActive(showObstacleInfo);
                    _obstacleToBreak = breakObstaclesTask.TargetAmount;
                }
                
                taskString += $"{stageTask.Description}\n";
            }
            
            ShowTaskText(taskString);
        }
        else
        {
            HideTaskText();
        }
    }

    private void ShowTaskText(string taskDescription)
    {
        if (!taskTextGroup || !taskText) return;
        taskText.text = taskDescription;
        textBinding?.UpdateBindingText();
        
        if (_fadeSequence.isAlive) _fadeSequence.Stop();
        _fadeSequence = Sequence.Create()
            .Group(Tween.Alpha(taskTextGroup, 1, fadeDuration));
    }
    
    private void ShowTaskForDuration(string taskDescription, float duration)
    {
        ShowTaskText(taskDescription);
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideTaskIn(duration));
    }
    
    private void HideTaskText()
    {
        if (!taskTextGroup) return;
        
        if (_fadeSequence.isAlive) _fadeSequence.Stop();
        _fadeSequence = Sequence.Create()
            .Group(Tween.Alpha(taskTextGroup, 0, fadeDuration));
    }

    private IEnumerator HideTaskIn(float duration)
    {
        yield return new WaitForSeconds(duration);
        HideTaskText();
    }
}
