using System;
using KBCore.Refs;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(RailPlayer))]
public class RailPlayerCameraInterface : MonoBehaviour
{
    
    [Header("Follow Camera")]
    [SerializeField] private float offsetChangeDuration = 1f;
    [SerializeField] private Ease offsetChangeEase = Ease.InOutSine;
    
    [Header("References")]
    [SerializeField] private Transform cameraPositions;
    [SerializeField] private Transform followCameraTarget;
    [SerializeField] private Transform outroCameraTarget;
    [SerializeField] private Transform storeCameraTarget;
    [SerializeField] private Transform storeCameraLookAtTarget;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    
    
    private Tween _followCameraTargetOffsetTween;
    private Vector3 _followCameraTargetBasePosition;
    private Vector3 _followCameraTargetCurrentOffset;
    
    
    
    private void OnValidate() { this.ValidateRefs(); }

    private void Awake()
    {
        if (followCameraTarget) _followCameraTargetBasePosition = followCameraTarget.localPosition;
    }

    private void OnEnable()
    {
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged += OnStageChanged;
            player.LevelManager.OnLevelSet += OnLevelSet;
        }
    }
    
    
    private void OnDisable()
    {
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged -= OnStageChanged;
            player.LevelManager.OnLevelSet -= OnLevelSet;
        }
    }

    private void OnLevelSet(SOLevel level)
    {
        if (!level) return;

        _followCameraTargetOffsetTween.Stop();
        _followCameraTargetOffsetTween = Tween.LocalPosition(
            followCameraTarget,
            _followCameraTargetBasePosition,
            offsetChangeDuration,
            offsetChangeEase
        );
        
    }

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        _followCameraTargetOffsetTween.Stop();
        _followCameraTargetOffsetTween = Tween.LocalPosition(
            followCameraTarget,
            _followCameraTargetBasePosition + stage.FollowCameraOffset,
            offsetChangeDuration,
            offsetChangeEase
        );
    }


    public Transform GetFollowCameraTarget()
    {
        return followCameraTarget ? followCameraTarget : transform;
    }
    
    public Transform GetOutroCameraTarget()
    {
        return outroCameraTarget ? outroCameraTarget : transform;
    }
    
    public Transform GetStoreCameraTarget()
    {
        return storeCameraTarget ? storeCameraTarget : transform;
    }
    
    public Transform GetStoreCameraLookAtTarget()
    {
        return storeCameraLookAtTarget ? storeCameraLookAtTarget : transform;
    }
    
    
    public Transform GetRandomCameraPosition()
    {
        if (!cameraPositions) return transform;
        
        int randomIndex = Random.Range(0, cameraPositions.childCount);
        return cameraPositions.GetChild(randomIndex);
    }
}