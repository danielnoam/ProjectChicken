using System;
using System.Collections;
using KBCore.Refs;
using PrimeTween;
using TMPro;
using UnityEngine;

public class TaskVisualizer : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 2, 0);
    [SerializeField] private float targetFollowSpeed = 5f;
    [SerializeField] private float cameraFollowSpeed = 5f;
    
    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.5f;
    
    
    [Header("References")]
    [SerializeField] private CanvasGroup taskGroup;
    [SerializeField] private TextMeshProUGUI taskText;
    [SerializeField] private ReplaceTextWithBinding textBinding;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;
    
    private Camera _camera;
    private Transform _followTarget;
    private Sequence _fadeSequence;
    private Coroutine _hideCoroutine;
    

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
        }
        
        _followTarget = null;
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
    }

    private void Update()
    {

        if (_followTarget) 
        {
            Vector3 targetPosition = _followTarget.position + targetOffset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, targetFollowSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }

        if (_camera)
        {
            Vector3 direction = transform.position - _camera.transform.position;
            direction.y = 0; // Keep only the horizontal direction
            if (direction.sqrMagnitude > 0.001f) // Avoid zero-length direction
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, cameraFollowSpeed * Time.deltaTime);
            }
        }
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        if (stage.StageType == StageType.Task)
        {
            string taskString = $"";

            foreach (var stageTask in stage.Tasks)
            {
                if (stageTask is null) continue;
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
        taskText.text = taskDescription;
        textBinding?.UpdateBindingText();
        
        if (_fadeSequence.isAlive) _fadeSequence.Stop();
        _fadeSequence = Sequence.Create()
            .Group(Tween.Alpha(taskGroup, 1, fadeDuration));
    }
    
    private void ShowTaskForDuration(string taskDescription, float duration)
    {
        ShowTaskText(taskDescription);
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideTaskIn(duration));
    }
    
    private void HideTaskText()
    {
        if (_fadeSequence.isAlive) _fadeSequence.Stop();
        _fadeSequence = Sequence.Create()
            .Group(Tween.Alpha(taskGroup, 0, fadeDuration));
    }

    private IEnumerator HideTaskIn(float duration)
    {
        yield return new WaitForSeconds(duration);
        HideTaskText();
    }
}
