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
    [Foldout("Position Offset Settings")]
    [SerializeField] private bool useDynamicPositionOffset = true;
    [SerializeField] private Vector3 positionOffsetRange = new Vector3(10f, 5f, 1f);
    [SerializeField] private Vector3 positionThreshold = new Vector3(0.2f, 0.2f, 0.2f);
    [SerializeField] private float positionOffsetSmoothness = 5f;
    [SerializeField] private bool invertPositionX = true;
    [SerializeField] private bool invertPositionY = true;
    [EndFoldout]
    [Foldout("Rotation Offset Settings")]
    [SerializeField] private bool useDynamicRotationOffset = true;
    [SerializeField] private Vector2 rotationOffsetRange = new Vector2(15f, 10f);
    [SerializeField] private Vector2 rotationThreshold = new Vector2(0.15f, 0.15f);
    [SerializeField] private float rotationSmoothness = 3f;
    [SerializeField] private bool invertRotationX = false;
    [SerializeField] private bool invertRotationY = false;
    [EndFoldout]

    [Header("References")]
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera followCamera;
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera introCamera;
    [SerializeField, Child(Flag.Editable)] private CinemachineCamera outroCamera;
    [SerializeField, Child(Flag.Editable)] private CinemachineFollow followCameraFollow;
    [SerializeField, Child(Flag.Editable)] private CinemachineRotateWithFollowTarget followCameraRotate;
    [SerializeField, Child(Flag.Editable)] private CinemachineRotationOffsetExtension followCameraRotateExtenstion;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private CinemachineImpulseSource impulseSource;
    
    private Sequence _fovSequence;
    private float _defaultFov;
    private Vector3 _targetFollowOffset;
    private Vector3 _currentFollowOffset;
    private Vector2 _currentRotationOffset;

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
        
        // Clamp min ranges to valid values
        positionThreshold.x = Mathf.Clamp(positionThreshold.x, 0f, 0.99f);
        positionThreshold.y = Mathf.Clamp(positionThreshold.y, 0f, 0.99f);
        positionThreshold.z = Mathf.Clamp(positionThreshold.z, 0f, 0.99f);
        rotationThreshold.x = Mathf.Clamp(rotationThreshold.x, 0f, 0.99f);
        rotationThreshold.y = Mathf.Clamp(rotationThreshold.y, 0f, 0.99f);
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
            followCamera.Target.TrackingTarget = player.GetFollowCameraTarget();
            followCamera.Target.LookAtTarget = player.GetReticleTarget();
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
    
    private void Update()
    {
        UpdateDynamicCameraOffset();
        UpdateDynamicRotationOffset();
    }

    #region Camera Control -----------------------------------------------------------------------------------------------

    private void SetActiveCamera(CinemachineCamera cam)
    {
        if (!cam) return;
        
        followCamera.Priority = 0;
        introCamera.Priority = 0;
        outroCamera.Priority = 0;

        cam.Priority = 10;
    }
    


    #endregion Camera Control -----------------------------------------------------------------------------------------------

    #region Camera Effects ------------------------------------------------------------------------------------------------
    
    [Button]
    private void ShakeCamera(CinemachineImpulseDefinition.ImpulseShapes impulseShape, float intensity = 3f, float duration = 0.5f)
    {
        if (!impulseSource) return;
        
        impulseSource.ImpulseDefinition.ImpulseShape = impulseShape;
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.DefaultVelocity = new Vector3(Random.Range(-1f,1f),Random.Range(-1f,1f),Random.Range(-1f,1f));
        impulseSource.GenerateImpulseWithForce(intensity);
    }
    
    private void UpdateDynamicCameraOffset()
    {
        if (!useDynamicPositionOffset)
        {
            _currentFollowOffset = Vector3.zero;
            return;
        }
        
        // Get the normalized aim position from the aiming component
        Vector2 normalizedAimPosition = GetNormalizedAimPosition();
        
        // Calculate offset based on aim position
        Vector3 dynamicOffset = CalculateDynamicOffset(normalizedAimPosition);
        
        // Smooth the offset change
        _currentFollowOffset = Vector3.Lerp(_currentFollowOffset, dynamicOffset, positionOffsetSmoothness * Time.deltaTime);
        
        // Apply the offset to the follow camera
        followCameraFollow.FollowOffset = _currentFollowOffset;
    }

    private void UpdateDynamicRotationOffset()
    {
        if (!useDynamicRotationOffset || !followCameraRotate)
        {
            _currentRotationOffset = Vector2.zero;
            return;
        }

        Vector2 normalizedAimPosition = GetNormalizedAimPosition();
        Vector2 dynamicRotationOffset = CalculateDynamicRotationOffset(normalizedAimPosition);
    
        _currentRotationOffset = Vector2.Lerp(_currentRotationOffset, dynamicRotationOffset, rotationSmoothness * Time.deltaTime);
    
        // Convert to Vector3 and set it on the component
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

        float duration = 1f;
        float upDuration = duration * 0.3f;
        float downDuration = duration * 0.7f;
        
        _fovSequence = Sequence.Create()
                .Group(Tween.Custom(startValue: followCamera.Lens.FieldOfView, endValue: _defaultFov + fovGainOnDodge, duration: upDuration, (value) => { followCamera.Lens.FieldOfView = value; }))
                .Chain(Tween.Custom(startValue: _defaultFov + fovGainOnDodge, endValue: _defaultFov, duration: downDuration, (value) => { followCamera.Lens.FieldOfView = value; }, Ease.OutBack))
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

    #endregion Events ---------------------------------------------------------------------------------------------------------


    #region Helpers ---------------------------------------------------------------------------------------------------------

    private Vector2 GetNormalizedAimPosition()
    {
        if (!player) return Vector2.zero;

        return player.GetNormalizedReticlePosition();
    }
    
    private Vector3 CalculateDynamicOffset(Vector2 normalizedAimPosition)
    {
        // Apply minimum range threshold - only calculate offset if input exceeds minimum
        float xInput = ApplyMinRange(normalizedAimPosition.x, positionThreshold.x);
        float yInput = ApplyMinRange(normalizedAimPosition.y, positionThreshold.y);
    
        // Convert processed input to offset
        float xOffset = xInput * positionOffsetRange.x;
        float yOffset = yInput * positionOffsetRange.y;

        // Apply inversions
        if (invertPositionX) xOffset = -xOffset;
        if (invertPositionY) yOffset = -yOffset;

        // Calculate Z offset based on the maximum of normalized X and Y inputs
        // This ensures Z reaches full when either X or Y reaches full
        float normalizedX = Mathf.Abs(xInput);
        float normalizedY = Mathf.Abs(yInput);
        float normalizedMagnitude = Mathf.Max(normalizedX, normalizedY);
    
        // Apply threshold to the normalized magnitude using existing positionThreshold.z
        float zInput = ApplyMinRange(normalizedMagnitude, positionThreshold.z);
    
        // Calculate final Z offset
        float zOffset = zInput * positionOffsetRange.z;

        // Apply to dynamic offset
        Vector3 dynamicOffset = new Vector3(xOffset, yOffset, zOffset);

        return dynamicOffset;
    }

    private Vector2 CalculateDynamicRotationOffset(Vector2 normalizedAimPosition)
    {
        // Apply minimum range threshold - only calculate offset if input exceeds minimum
        float xInput = ApplyMinRange(normalizedAimPosition.x, rotationThreshold.x);
        float yInput = ApplyMinRange(normalizedAimPosition.y, rotationThreshold.y);
        
        // Convert processed input to rotation offset
        float xRotationOffset = xInput * rotationOffsetRange.x;
        float yRotationOffset = yInput * rotationOffsetRange.y;
        
        // Apply inversions
        if (invertRotationX) xRotationOffset = -xRotationOffset;
        if (invertRotationY) yRotationOffset = -yRotationOffset;
        
        // Add to default rotation offset
        Vector2 dynamicRotationOffset = new Vector2(xRotationOffset, yRotationOffset);
        
        return dynamicRotationOffset;
    }
    
    private float ApplyMinRange(float input, float minRange)
    {
        // Clamp minRange to prevent division by zero and invalid values
        minRange = Mathf.Clamp(minRange, 0f, 0.99f);
        
        float absInput = Mathf.Abs(input);
        
        // If input is below minimum threshold, return 0
        if (absInput < minRange)
        {
            return 0f;
        }
        
        // Prevent division by zero when minRange approaches 1
        float denominator = 1f - minRange;
        if (denominator <= 0.01f)
        {
            // If minRange is very close to 1, just return the sign
            return Mathf.Sign(input);
        }
        
        // Remap the input from [minRange, 1] to [0, 1] to maintain smooth scaling
        float remappedInput = (absInput - minRange) / denominator;
        
        // Clamp the result to prevent any overflow issues
        remappedInput = Mathf.Clamp01(remappedInput);
        
        // Restore the original sign
        return remappedInput * Mathf.Sign(input);
    }

    #endregion Helpers ---------------------------------------------------------------------------------------------------------
}