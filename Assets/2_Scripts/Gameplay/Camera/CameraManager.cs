
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
    [SerializeField] private float fovGainOnDodge = 10f;
    [Space(5)]
    [SerializeField] private bool reticleInfluencePosition = true;
    [SerializeField, ShowIf("reticleInfluencePosition")] private CameraOffsetSettings reticlePositionOffset = new CameraOffsetSettings();[EndIf]
    [Space(5)]
    [SerializeField] private bool reticleInfluenceRotation = true;
    [SerializeField, ShowIf("reticleInfluenceRotation")] private CameraOffsetSettings reticleRotationOffset = new CameraOffsetSettings();[EndIf]
    [Space(5)]
    [SerializeField] private bool playerInfluencePosition = true;
    [SerializeField, ShowIf("playerInfluencePosition")] private CameraOffsetSettings playerPositionOffset = new CameraOffsetSettings();[EndIf]
    [Space(5)]
    [SerializeField] private bool playerInfluencesRotation;
    [SerializeField, ShowIf("playerInfluencesRotation")] private CameraOffsetSettings playerRotationOffset = new CameraOffsetSettings();[EndIf]
    [EndFoldout]
    
    
    [Foldout("Intro Camera Settings")]
    [SerializeField] private bool changePositions = true;
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
    private Vector3 _targetFollowOffset;
    private Vector3 _currentFollowOffset;
    private Vector2 _currentRotationOffset;
    private Coroutine  _changePositionCoroutine;
    private CinemachineCamera _activeCamera;
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
        
        reticlePositionOffset.Validate();
        reticleRotationOffset.Validate();
        playerPositionOffset.Validate();
        playerRotationOffset.Validate();

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
        }

        if (player)
        {
            player.Movement.OnDodge += Dodge;
            player.Health.OnDeath += Death;
            followCamera.Target.TrackingTarget = player.GetFollowCameraTarget();
            storeCamera.Target.TrackingTarget = player.GetStoreCameraTarget();
            storeCamera.Target.LookAtTarget = player.GetStoreCameraLookAtTarget();
            introCamera.Target.TrackingTarget = player.GetRandomCameraPosition(); 
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
            player.Movement.OnDodge -= Dodge;
            player.Health.OnDeath -= Death;
            followCamera.Target.TrackingTarget = null;
            introCamera.Target.TrackingTarget = null;
            introCamera.Target.LookAtTarget = null;
            outroCamera.Target.LookAtTarget = null;

        }
    }
    
    private void Update()
    {
        UpdateDynamicCameraOffset();
        UpdateDynamicRotationOffset();
    }
    
    

    #region Camera Control -----------------------------------------------------------------------------------------------

    private void SetActiveCamera(CinemachineCamera cam)
    {
        if (!cam || cam == _activeCamera) return;

        _activeCamera = cam;
        
        if (_activeCamera == followCamera && followCameraRotateExtenstion)
        {
            followCameraRotateExtenstion.SetRotationOffset(Vector3.zero);
            _currentRotationOffset = Vector2.zero;
        }

        if (_changePositionCoroutine != null) StopCoroutine(_changePositionCoroutine);
        
        followCamera.Priority = 0;
        introCamera.Priority = 0;
        outroCamera.Priority = 0;
        storeCamera.Priority = 0;

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
            if (newTarget == cam.Target.TrackingTarget) newTarget = player.GetRandomCameraPosition();
            cam.Target.TrackingTarget = newTarget;
            
            yield return new WaitForSeconds(changePositionEvery);
        }
    }
    


    #endregion Camera Control -----------------------------------------------------------------------------------------------

    
    
    #region Camera Effects ------------------------------------------------------------------------------------------------
    
    
    private void UpdateDynamicCameraOffset()
    {
        Vector3 combinedOffset = Vector3.zero;
        
        // Calculate aim offset
        if (reticleInfluencePosition)
        {
            Vector2 normalizedAimPosition = GetNormalizedAimPosition();
            Vector3 aimOffset = CalculateDynamicPositionOffset(normalizedAimPosition, reticlePositionOffset);
            combinedOffset += aimOffset;
        }
        
        // Calculate movement offset
        if (playerInfluencePosition)
        {
            Vector2 normalizedMovementPosition = GetNormalizedMovementPosition();
            Vector3 movementOffset = CalculateDynamicPositionOffset(normalizedMovementPosition, playerPositionOffset);
            combinedOffset += movementOffset;
        }
        
        // Use the higher smoothness value for interpolation
        float smoothness = Mathf.Max(
            reticleInfluencePosition ? reticlePositionOffset.smoothness : 0f,
            playerInfluencePosition ? playerPositionOffset.smoothness : 0f
        );
        
        // Smooth the offset change
        _currentFollowOffset = Vector3.Lerp(_currentFollowOffset, combinedOffset, smoothness * Time.deltaTime);
        
        // Apply the offset to the follow camera
        followCameraFollow.FollowOffset = _currentFollowOffset;
    }

    private void UpdateDynamicRotationOffset()
    {
        if (!followCameraRotate)
        {
            _currentRotationOffset = Vector2.zero;
            return;
        }

        Vector2 combinedRotationOffset = Vector2.zero;
        
        // Calculate aim rotation offset
        if (reticleInfluenceRotation)
        {
            Vector2 normalizedAimPosition = GetNormalizedAimPosition();
            Vector2 aimRotationOffset = CalculateDynamicRotationOffset(normalizedAimPosition, reticleRotationOffset);
            combinedRotationOffset += aimRotationOffset;
        }
        
        // Calculate movement rotation offset
        if (playerInfluencesRotation)
        {
            Vector2 normalizedMovementPosition = GetNormalizedMovementPosition();
            Vector2 movementRotationOffset = CalculateDynamicRotationOffset(normalizedMovementPosition, playerRotationOffset);
            combinedRotationOffset += movementRotationOffset;
        }
        
        // Use the higher smoothness value for interpolation
        float smoothness = Mathf.Max(
            reticleInfluenceRotation ? reticleRotationOffset.smoothness : 0f,
            playerInfluencesRotation ? playerRotationOffset.smoothness : 0f
        );
    
        _currentRotationOffset = Vector2.Lerp(_currentRotationOffset, combinedRotationOffset, smoothness * Time.deltaTime);
        
        Vector3 eulerOffset = new Vector3(_currentRotationOffset.y, _currentRotationOffset.x, 0);
        followCameraRotateExtenstion.SetRotationOffset(eulerOffset);
    }
    
    #endregion Camera Effects ------------------------------------------------------------------------------------------------
    
    
    
    #region Events ---------------------------------------------------------------------------------------------------------
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        switch (stage.StageType)
        {
            case StageType.Delay:
                SetActiveCamera(followCamera);
                break;
            case StageType.Store:
                SetActiveCamera(storeCamera);
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
    
    private void Dodge()
    {
        if (_fovSequence.isAlive) _fovSequence.Stop();

        float duration = 1f;
        float upDuration = duration * 0.3f;
        float downDuration = duration * 0.7f;
        
        _fovSequence = Sequence.Create()
                .Group(Tween.Custom(startValue: followCamera.Lens.FieldOfView, endValue: _defaultFov + fovGainOnDodge, duration: upDuration, (value) => { followCamera.Lens.FieldOfView = value; }, Ease.InSine))
                .Chain(Tween.Custom(startValue: _defaultFov + fovGainOnDodge, endValue: _defaultFov, duration: downDuration, (value) => { followCamera.Lens.FieldOfView = value; }, Ease.OutBack))
            ;
    }
    
    private void Death()
    {
        SetActiveCamera(introCamera);
    }
    

    #endregion Events ---------------------------------------------------------------------------------------------------------


    
    #region Helpers ---------------------------------------------------------------------------------------------------------

    private Vector2 GetNormalizedAimPosition()
    {
        if (!player) return Vector2.zero;

        return player.Aiming.NormalizedAimPosition;
    }

    private Vector2 GetNormalizedMovementPosition()
    {
        if (!player) return Vector2.zero;

        return player.Movement.NormalizedMovementPosition;
    }
    
    private Vector3 CalculateDynamicPositionOffset(Vector2 normalizedPosition, CameraOffsetSettings settings)
    {
        // Apply minimum range threshold - only calculate offset if input exceeds minimum
        float xInput = ApplyMinRange(normalizedPosition.x, settings.threshold.x);
        float yInput = ApplyMinRange(normalizedPosition.y, settings.threshold.y);
    
        // Convert processed input to offset
        float xOffset = xInput * settings.range.x;
        float yOffset = yInput * settings.range.y;

        // Apply inversions
        if (settings.invertX) xOffset = -xOffset;
        if (settings.invertY) yOffset = -yOffset;

        // Calculate Z offset based on the maximum of normalized X and Y inputs
        // This ensures Z reaches full when either X or Y reaches full
        float normalizedX = Mathf.Abs(xInput);
        float normalizedY = Mathf.Abs(yInput);
        float normalizedMagnitude = Mathf.Max(normalizedX, normalizedY);
    
        // Apply a threshold to the normalized magnitude using existing positionThreshold.z
        float zInput = ApplyMinRange(normalizedMagnitude, settings.threshold.z);
    
        // Calculate final Z offset
        float zOffset = zInput * settings.range.z;

        // Apply to dynamic offset
        Vector3 dynamicOffset = new Vector3(xOffset, yOffset, zOffset);

        return dynamicOffset;
    }

    private Vector2 CalculateDynamicRotationOffset(Vector2 normalizedPosition, CameraOffsetSettings settings)
    {
        // Apply minimum range threshold - only calculate offset if input exceeds minimum
        float xInput = ApplyMinRange(normalizedPosition.x, settings.threshold.x);
        float yInput = ApplyMinRange(normalizedPosition.y, settings.threshold.y);
        
        // Convert processed input to rotation offset
        float xRotationOffset = xInput * settings.range.x;
        float yRotationOffset = yInput * settings.range.y;
        
        // Apply inversions
        if (settings.invertX) xRotationOffset = -xRotationOffset;
        if (settings.invertY) yRotationOffset = -yRotationOffset;
        
        // Add to default rotation offset
        Vector2 dynamicRotationOffset = new Vector2(xRotationOffset, yRotationOffset);
        
        return dynamicRotationOffset;
    }
    
    private float ApplyMinRange(float input, float minRange)
    {
        // Clamp minRange to prevent division by zero and invalid values
        minRange = Mathf.Clamp(minRange, 0f, 0.99f);
        
        float absInput = Mathf.Abs(input);
        
        // If input is below the minimum threshold, return 0
        if (absInput < minRange)
        {
            return 0f;
        }
        
        // Prevent division by zero when minRange approaches 1
        float denominator = 1f - minRange;
        if (denominator <= 0.01f)
        {
            // If minRange is very close to 1, return the sign
            return Mathf.Sign(input);
        }
        
        // Remap the input from [minRange, 1] to [0, 1] to maintain smooth scaling
        float remappedInput = (absInput - minRange) / denominator;
        
        // Clamp the result to prevent any overflow issues
        remappedInput = Mathf.Clamp01(remappedInput);
        
        // Restore the original sign
        return remappedInput * Mathf.Sign(input);
    }
    
    public Transform OutroCameraPosition()
    {
        return !outroCamera ? transform : outroCamera.transform;
    }

    #endregion Helpers ---------------------------------------------------------------------------------------------------------


}
