using System;
using KBCore.Refs;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;

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
    [SerializeField, Self, HideInInspector] private CinemachineImpulseSource impulseSource;
    
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
            player.OnHealthChanged += OnHealthChanged;
            player.OnShieldChanged += OnShieldChanged;
            player.OnWeaponUsed += OnWeaponUsed;
            introCamera.Target.TrackingTarget = player.GetIntroCameraTarget(); 
            introCamera.Target.LookAtTarget = player.transform;
            outroCamera.Target.LookAtTarget = player.transform;
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
            player.OnHealthChanged -= OnHealthChanged;
            player.OnShieldChanged -= OnShieldChanged;
            player.OnWeaponUsed -= OnWeaponUsed;
            introCamera.Target.TrackingTarget = null;
            introCamera.Target.LookAtTarget = null;
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

    [Button]
    private void ShakeCamera(CinemachineImpulseDefinition.ImpulseShapes impulseShape, float intensity = 3f, float duration = 0.5f)
    {
        if (!impulseSource) return;
        
        impulseSource.ImpulseDefinition.ImpulseShape = impulseShape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = new Vector3(Random.Range(-1f,1f),Random.Range(-1f,1f),Random.Range(-1f,1f));
        impulseSource.GenerateImpulseWithForce(intensity);
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
    private void OnPlayerDodge()
    {
        if (_fovSequence.isAlive) _fovSequence.Stop();
        
        _fovSequence = Sequence.Create()
            .Group(Tween.Custom(startValue: followCamera.Lens.FieldOfView, endValue: _defaultFov + fovGainOnDodge, duration: 0.5f, (value) => { followCamera.Lens.FieldOfView = value; }))
            .Chain(Tween.Custom(startValue: _defaultFov + fovGainOnDodge, endValue: _defaultFov, duration: 0.5f, (value) => { followCamera.Lens.FieldOfView = value; }))
            ;
    }
    
    private void OnHealthChanged(int health)
    {
        if (health <= 0)
        {
            ShakeCamera(CinemachineImpulseDefinition.ImpulseShapes.Rumble,5, 1f);
        }
    }
    
    private void OnShieldChanged(float shield)
    {
        if (shield <= 0)
        {
            ShakeCamera(CinemachineImpulseDefinition.ImpulseShapes.Rumble,3, 0.5f);
        }
    }
    
    private void OnWeaponUsed(SOWeapon weapon)
    {
        if (!weapon) return;
        
        ShakeCamera(CinemachineImpulseDefinition.ImpulseShapes.Recoil, 0.3f, 0.1f);
    }
}
