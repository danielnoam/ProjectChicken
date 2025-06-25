using System;
using KBCore.Refs;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Follow Camera Settings")]
    [SerializeField] private float fovGainOnDodge = 5f;

    [Header("References")]
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera followCamera;
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera introCamera;
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera outroCamera;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;

    
    private Sequence _fovSequence;
    private float _defaultFov;

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
        _defaultFov = followCamera.Lens.FieldOfView;
    }

    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
        }

        if (player)
        {
            player.OnDodge += OnPlayerDodge;
        }
    }

    private void OnDisable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }
        
        if (player)
        {
            player.OnDodge -= OnPlayerDodge;
        }
    }
    
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        switch (stage.StageType)
        {
            case StageType.Checkpoint:
                SetActiveCamera(followCamera);
                break;
            case StageType.EnemyWave:
                SetActiveCamera(followCamera);
                break;
            case StageType.Intro:
                SetActiveCamera(introCamera);
                break;
            case StageType.Outro:
                SetActiveCamera(outroCamera);
                break;
            default:
                SetActiveCamera(followCamera);
                break;
        }
    }
    
    
    private void SetActiveCamera(CinemachineCamera cam)
    {
        if (!cam) return;
        
        followCamera.Priority = 0;
        introCamera.Priority = 0;
        outroCamera.Priority = 0;

        cam.Priority = 10;
    }
    
    
    private void OnPlayerDodge()
    {
        if (_fovSequence.isAlive) _fovSequence.Stop();
        
        _fovSequence = Sequence.Create()
            .Group(Tween.Custom(startValue: followCamera.Lens.FieldOfView, endValue: _defaultFov + fovGainOnDodge, duration: 0.5f, (value) => { followCamera.Lens.FieldOfView = value; }))
            .Chain(Tween.Custom(startValue: _defaultFov + fovGainOnDodge, endValue: _defaultFov, duration: 0.5f, (value) => { followCamera.Lens.FieldOfView = value; }))
            ;
    }
}
