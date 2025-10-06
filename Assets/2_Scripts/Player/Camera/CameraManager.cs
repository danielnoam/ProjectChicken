using System.Collections;
using DNExtensions;
using KBCore.Refs;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;
using VInspector;

[SelectionBase]
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    
    [Foldout("Follow Camera Settings")]
    [Header("FOV Effects")]
    [Tooltip("Additional field of view added to the camera during dodge maneuvers")]
    [SerializeField] private float fovGainOnDodge = 10f;
    [SerializeField] private float fovGainDurationForDodge = 1f;
    [SerializeField] private float fovGainOnPassthrough = -15f;
    [SerializeField] private float fovGainDurationForPassthrough = 0.5f;
    [Header("Offset Influence")]
    [Tooltip("Camera effects based on where the player is aiming")]
    [SerializeField] private CameraSettings reticleInfluenceSettings = new CameraSettings();
    [Tooltip("Camera effects based on player movement direction")]
    [SerializeField] private CameraSettings playerInfluenceSettings = new CameraSettings();
    [EndFoldout]
    
    [Foldout("Intro Camera Settings")]
    [Tooltip("Should the intro camera automatically change positions during intro sequences?")]
    [SerializeField] private bool changePositions = true;
    [Tooltip("Time in seconds between position changes during intro sequences")]
    [SerializeField, Min(1f)] private float changePositionEvery = 1.5f;
    [EndFoldout]

    [Header("References")]
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera followCamera;
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera introCamera;
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera outroCamera;
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera storeCamera;
    [SerializeField, Child(Flag.Editable)] private CinemachineFollow followCameraFollow;
    [SerializeField, Child(Flag.Editable)] private CinemachineRotateWithFollowTarget followCameraRotate;
    [SerializeField, Child(Flag.Editable)] private CinemachineRotationOffsetExtension followCameraRotateExtenstion;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;
    

    private Sequence _fovSequence;
    private Vector3 _currentFollowOffset;
    private Vector2 _currentRotationOffset;
    private Coroutine _changePositionCoroutine;
    private CinemachineCamera _activeCamera;
    private float _defaultFov;

    private void OnValidate()
    {
        if (!levelManager)
            levelManager = FindFirstObjectByType<LevelManager>();
        
        if (!player)
            player = FindFirstObjectByType<RailPlayer>();
        
        // Validate all settings
        reticleInfluenceSettings.Validate();
        playerInfluenceSettings.Validate();
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
        
        _defaultFov = followCamera.Lens.FieldOfView;
    }

    private void OnEnable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
            levelManager.ObstacleManager.OnPlayerEnteredPassThroughObstacle += OnPlayerEnteredPassThroughObstacle;
            levelManager.ObstacleManager.OnPlayerPassedThroughObstacle += OnPlayerPassedThroughObstacle;
        }


        if (player)
        {
            player.Movement.OnDodge += OnPlayerDodge;
            player.Health.OnDeath += OnPlayerDeath;
            SetupCameraTargets();
        }
    }

    private void OnDisable()
    {
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
            levelManager.ObstacleManager.OnPlayerEnteredPassThroughObstacle -= OnPlayerEnteredPassThroughObstacle;
            levelManager.ObstacleManager.OnPlayerPassedThroughObstacle -= OnPlayerPassedThroughObstacle;
        }
           
        
        if (player)
        {
            player.Movement.OnDodge -= OnPlayerDodge;
            player.Health.OnDeath -= OnPlayerDeath;
            ClearCameraTargets();
        }
    }
    


    private void Update()
    {
        UpdateDynamicCameraPosition();
        UpdateDynamicCameraRotation();
    }

    #region Camera Setup
    
    private void SetupCameraTargets()
    {
        followCamera.Target.TrackingTarget = player.GetFollowCameraTarget();
        storeCamera.Target.TrackingTarget = player.GetStoreCameraTarget();
        storeCamera.Target.LookAtTarget = player.GetStoreCameraLookAtTarget();
        introCamera.Target.TrackingTarget = player.GetRandomCameraPosition(); 
        introCamera.Target.LookAtTarget = player.transform;
        outroCamera.Target.LookAtTarget = player.transform;
    }

    private void ClearCameraTargets()
    {
        followCamera.Target.TrackingTarget = null;
        introCamera.Target.TrackingTarget = null;
        introCamera.Target.LookAtTarget = null;
        outroCamera.Target.LookAtTarget = null;
    }

    #endregion

    #region Camera Control

    private void SetActiveCamera(CinemachineCamera cam)
    {
        if (!cam || cam == _activeCamera) return;

        _activeCamera = cam;
        
        if (_activeCamera == followCamera && followCameraRotateExtenstion)
        {
            followCameraRotateExtenstion.SetRotationOffset(Vector3.zero);
            _currentRotationOffset = Vector2.zero;
        }

        if (_changePositionCoroutine != null) 
            StopCoroutine(_changePositionCoroutine);
        
        // Reset all camera priorities
        followCamera.Priority = 0;
        introCamera.Priority = 0;
        outroCamera.Priority = 0;
        storeCamera.Priority = 0;

        // Set active camera priority
        _activeCamera.Priority = 10;

        if (_activeCamera == introCamera && changePositions)
        {
            _changePositionCoroutine = StartCoroutine(ChangeCameraPosition(introCamera));
        }
    }
    
    private IEnumerator ChangeCameraPosition(CinemachineCamera cam)
    {
        if (!cam || !cam.isActiveAndEnabled) yield break;
        
        yield return new WaitForSeconds(changePositionEvery);
        
        while (true)
        {
            if (!cam.IsLive || !changePositions) yield break;

            var newTarget = player.GetRandomCameraPosition();
            if (newTarget == cam.Target.TrackingTarget) 
                newTarget = player.GetRandomCameraPosition();
            
            cam.Target.TrackingTarget = newTarget;
            
            yield return new WaitForSeconds(changePositionEvery);
        }
    }

    
    private void PunchFOV(float duration, float fovGain)
    {
        if (_fovSequence.isAlive) _fovSequence.Stop();
        
        float upDuration = duration * 0.3f;
        float downDuration = duration * 0.7f;
        
        _fovSequence = Sequence.Create()
            .Group(Tween.Custom(
                startValue: followCamera.Lens.FieldOfView, 
                endValue: _defaultFov + fovGain, 
                duration: upDuration, 
                onValueChange: value => followCamera.Lens.FieldOfView = value, 
                ease: Ease.InSine))
            .Chain(Tween.Custom(
                startValue: _defaultFov + fovGain, 
                endValue: _defaultFov, 
                duration: downDuration, 
                onValueChange: value => followCamera.Lens.FieldOfView = value, 
                ease: Ease.OutBack));
    }
    
    private void AddToFOV(float duration, float fovGain, Ease ease = Ease.InOutBack)
    {
        if (_fovSequence.isAlive) _fovSequence.Stop();

        _fovSequence = Sequence.Create()
            .Group(Tween.Custom(
                startValue: followCamera.Lens.FieldOfView,
                endValue: _defaultFov + fovGain,
                duration: duration,
                onValueChange: value => followCamera.Lens.FieldOfView = value,
                ease));
    }
    
    private void ResetFOV(float duration, Ease ease = Ease.InOutBack)
    {
        if (_fovSequence.isAlive) _fovSequence.Stop();

        _fovSequence = Sequence.Create()
            .Group(Tween.Custom(
                startValue: followCamera.Lens.FieldOfView,
                endValue: _defaultFov,
                duration: duration,
                onValueChange: value => followCamera.Lens.FieldOfView = value,
                ease));
    }
    
    #endregion

    
    #region Dynamic Camera Effects
    
    private void UpdateDynamicCameraPosition()
    {
        Vector3 combinedOffset = Vector3.zero;
        float maxSmoothness = 0f;
        
        // Calculate reticle position influence
        if (reticleInfluenceSettings.enablePosition)
        {
            Vector2 normalizedAimPosition = GetNormalizedAimPosition();
            Vector3 aimOffset = CalculateDynamicPositionOffset(normalizedAimPosition, reticleInfluenceSettings);
            combinedOffset += aimOffset;
            maxSmoothness = Mathf.Max(maxSmoothness, reticleInfluenceSettings.positionSmoothness);
        }
        
        // Calculate player movement position influence
        if (playerInfluenceSettings.enablePosition)
        {
            Vector2 normalizedMovementPosition = GetNormalizedMovementPosition();
            Vector3 movementOffset = CalculateDynamicPositionOffset(normalizedMovementPosition, playerInfluenceSettings);
            combinedOffset += movementOffset;
            maxSmoothness = Mathf.Max(maxSmoothness, playerInfluenceSettings.positionSmoothness);
        }
        
        // Smooth the offset change
        _currentFollowOffset = Vector3.Lerp(_currentFollowOffset, combinedOffset, maxSmoothness * Time.deltaTime);
        
        // Apply the offset to the follow camera
        if (followCameraFollow)
            followCameraFollow.FollowOffset = _currentFollowOffset;
    }

    private void UpdateDynamicCameraRotation()
    {
        if (!followCameraRotateExtenstion)
        {
            _currentRotationOffset = Vector2.zero;
            return;
        }

        Vector2 combinedRotationOffset = Vector2.zero;
        float maxSmoothness = 0f;
        
        // Calculate reticle rotation influence
        if (reticleInfluenceSettings.enableRotation)
        {
            Vector2 normalizedAimPosition = GetNormalizedAimPosition();
            Vector2 aimRotationOffset = CalculateDynamicRotationOffset(normalizedAimPosition, reticleInfluenceSettings);
            combinedRotationOffset += aimRotationOffset;
            maxSmoothness = Mathf.Max(maxSmoothness, reticleInfluenceSettings.rotationSmoothness);
        }
        
        // Calculate player movement rotation influence
        if (playerInfluenceSettings.enableRotation)
        {
            Vector2 normalizedMovementPosition = GetNormalizedMovementPosition();
            Vector2 movementRotationOffset = CalculateDynamicRotationOffset(normalizedMovementPosition, playerInfluenceSettings);
            combinedRotationOffset += movementRotationOffset;
            maxSmoothness = Mathf.Max(maxSmoothness, playerInfluenceSettings.rotationSmoothness);
        }
    
        // Smooth the rotation offset change
        _currentRotationOffset = Vector2.Lerp(_currentRotationOffset, combinedRotationOffset, maxSmoothness * Time.deltaTime);
        
        // Convert to Euler angles and apply
        Vector3 eulerOffset = new Vector3(_currentRotationOffset.y, _currentRotationOffset.x, 0);
        followCameraRotateExtenstion.SetRotationOffset(eulerOffset);
    }
    
    #endregion

    #region Offset Calculations
    
    private Vector3 CalculateDynamicPositionOffset(Vector2 normalizedPosition, CameraSettings settings)
    {
        // Apply minimum range thresholds for X/Y
        float xInput = ApplyMinRange(normalizedPosition.x, settings.positionThreshold.x);
        float yInput = ApplyMinRange(normalizedPosition.y, settings.positionThreshold.y);
    
        // Convert processed input to position offset
        float xOffset = xInput * settings.positionRange.x;
        float yOffset = yInput * settings.positionRange.y;

        // Apply inversions for X/Y
        if (settings.invertPositionX) xOffset = -xOffset;
        if (settings.invertPositionY) yOffset = -yOffset;

        // Calculate Z offset based on overall activity (max of X and Y)
        float normalizedMagnitude = Mathf.Max(Mathf.Abs(xInput), Mathf.Abs(yInput));
        float zInput = ApplyMinRange(normalizedMagnitude, settings.depthSettings.threshold);
        float zOffset = zInput * settings.depthSettings.range;
        if (settings.depthSettings.invert) zOffset = -zOffset;

        return new Vector3(xOffset, yOffset, zOffset);
    }

    private Vector2 CalculateDynamicRotationOffset(Vector2 normalizedPosition, CameraSettings settings)
    {
        // Apply minimum range thresholds
        float xInput = ApplyMinRange(normalizedPosition.x, settings.rotationThreshold.x);
        float yInput = ApplyMinRange(normalizedPosition.y, settings.rotationThreshold.y);
        
        // Convert processed input to rotation offset
        float xRotationOffset = xInput * settings.rotationRange.x;
        float yRotationOffset = yInput * settings.rotationRange.y;
        
        // Apply inversions
        if (settings.invertRotationX) xRotationOffset = -xRotationOffset;
        if (settings.invertRotationY) yRotationOffset = -yRotationOffset;
        
        return new Vector2(xRotationOffset, yRotationOffset);
    }
    
    private float ApplyMinRange(float input, float minRange)
    {
        // Clamp minRange to prevent division by zero and invalid values
        minRange = Mathf.Clamp(minRange, 0f, 0.99f);
        
        float absInput = Mathf.Abs(input);
        
        // If input is below the minimum threshold, return 0
        if (absInput < minRange)
            return 0f;
        
        // Prevent division by zero when minRange approaches 1
        float denominator = 1f - minRange;
        if (denominator <= 0.01f)
            return Mathf.Sign(input);
        
        // Remap the input from [minRange, 1] to [0, 1] to maintain smooth scaling
        float remappedInput = (absInput - minRange) / denominator;
        remappedInput = Mathf.Clamp01(remappedInput);
        
        // Restore the original sign
        return remappedInput * Mathf.Sign(input);
    }
    
    #endregion

    #region Events
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        switch (stage.StageType)
        {
            case StageType.Store:
                SetActiveCamera(storeCamera);
                break;
            case StageType.Intro:
                SetActiveCamera(introCamera);
                break;
            case StageType.Outro:
                SetActiveCamera(outroCamera);
                break;
            case StageType.Delay:
            case StageType.EnemyWave:
            case StageType.Task:
            default:
                SetActiveCamera(followCamera);
                break;
        }
    }
    
    private void OnPlayerEnteredPassThroughObstacle(PassthroughObstacle passthroughObstacle)
    {
        if (!passthroughObstacle.PassthroughCameraEffect) return;
        
        AddToFOV(fovGainDurationForPassthrough, fovGainOnPassthrough, Ease.InBack);
    }
    
    private void OnPlayerPassedThroughObstacle(PassthroughObstacle passthroughObstacle)
    {
        if (!passthroughObstacle.PassthroughCameraEffect) return;
        
        ResetFOV(fovGainDurationForPassthrough, Ease.OutSine);
    }
    
    private void OnPlayerDodge()
    {
        PunchFOV(fovGainDurationForDodge, fovGainOnDodge);
    }
    

    private void OnPlayerDeath()
    {
        SetActiveCamera(introCamera);
    }

    #endregion

    
    #region Helper Methods

    private Vector2 GetNormalizedAimPosition()
    {
        return player ? player.Aiming.NormalizedAimPosition : Vector2.zero;
    }

    private Vector2 GetNormalizedMovementPosition()
    {
        return player ? player.Movement.NormalizedMovementPosition : Vector2.zero;
    }
    
    #endregion
}