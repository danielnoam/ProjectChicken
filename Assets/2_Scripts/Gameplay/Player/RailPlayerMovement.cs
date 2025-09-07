
using System;
using System.Collections;
using DNExtensions;
using KBCore.Refs;
using UnityEngine;
using PrimeTween;
using UnityEngine.InputSystem;
using VInspector;

[RequireComponent(typeof(RailPlayer))]
[RequireComponent(typeof(Rigidbody))]
public class RailPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0)] private float maxMoveSpeed = 35f;
    [SerializeField, Min(0.1f)] private float acceleration = 5f;
    [SerializeField, Min(0.1f)] private float deceleration = 3f;
    [SerializeField, Min(0)] private float pathFollowSpeed = 1000f;
    
    [Header("Rotation Settings")]
    [SerializeField, Min(0)] private float rollSpeed = 5f;
    [SerializeField, Min(0)] private float maxRollAmount = 30f;
    [SerializeField, Min(0)] private float pitchYawSpeed = 22f;
    [SerializeField, Min(0)] private float maxPitchAngle = 30f;
    [SerializeField, Min(0)] private float maxYawAngle = 45f;

    
    [Header("Dodge Settings")]
    [SerializeField, Min(0)] private float dodgeAccumulationRate = 2;
    [SerializeField, Min(0)] private float dodgeMoveSpeed = 65f;
    [SerializeField, Min(0)] private float dodgeDuration = 0.4f;
    [SerializeField, Min(0)] private float dodgeCooldown = 0.45f;
    [SerializeField, Min(0)] private float dodgeRollAmount = 360f;
    [SerializeField, Min(0)] private TweenSettings dodgeTweenSettings = new TweenSettings(1.2f, Ease.Custom);
    
    [Header("References")] 
    [SerializeField, Child(Flag.Editable)] private AudioSource audioSource;
    [SerializeField] private Transform shipModel;
    [SerializeField] private SOAudioEvent dodgeSfx;
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private RailPlayerAiming playerAiming;
    [SerializeField, Self, HideInInspector] private RailPlayerInput playerInput;
    [SerializeField, Self, HideInInspector] private Rigidbody playerRigidbody;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;


    private bool _allowMovement;
    private float _horizontalInput;
    private float _verticalInput;
    private Quaternion _velocityRotation = Quaternion.identity;
    private Quaternion _aimRotation = Quaternion.identity;
    private Vector3 _targetOffsetFromSpline = Vector3.zero;
    private Vector3 _currentOffsetFromSpline = Vector3.zero;
    private bool _isDodging;
    private int _currentDodgeRemining;
    private int _maxDodgeAccumulation;
    private float _dodgeAccumulationRateTimer;
    private float _dodgeCooldownTimer;
    private float _dodgeTimeCounter;
    private float _currentDodgeRoll;
    private Vector3 _dodgeDirection;

    private Tween _dodgeTween;
    private Vector2 _normalizedMovementPosition;
    private Coroutine _autoCenterRoutine;
    private float MovementBoundaryX => player.LevelManager ? player.LevelManager.PlayerBoundary.x : 10f;
    private float MovementBoundaryY => player.LevelManager ? player.LevelManager.PlayerBoundary.y : 6f;
    
    
    public Vector3 InputDirection { get; private set; }
    public bool IsDodging => _isDodging;
    public Vector2 NormalizedMovementPosition => _normalizedMovementPosition;

    
    public event Action OnDodge;
    public event Action<float> OnDodgeCooldownUpdated;
    public event Action<int> OnDodgeCountChanged;

    private void OnValidate()
    {
        this.ValidateRefs();

        if (player.LevelManager && !Application.isPlaying)
        {
            transform.position = player.LevelManager.PlayerPosition;
        }
    }

    
    

    private void OnEnable()
    {
        player.Health.OnDeath += OnDeath;
        playerInput.OnMoveEvent += OnMove;
        playerInput.OnDodgeLeftEvent += OnDodgeLeft;
        playerInput.OnDodgeRightEvent += OnDodgeRight;
        playerInput.OnDodgeFreeformEvent += OnDodgeFreeform;
        
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged += OnStageChanged;
        }
        
    }
    
    private void OnDisable()
    {
        player.Health.OnDeath -= OnDeath;
        playerInput.OnMoveEvent -= OnMove;
        playerInput.OnDodgeLeftEvent -= OnDodgeLeft;
        playerInput.OnDodgeRightEvent -= OnDodgeRight;
        playerInput.OnDodgeFreeformEvent -= OnDodgeFreeform;
        
        if (player.LevelManager)
        {
            player.LevelManager.OnStageChanged -= OnStageChanged;
        }
    }


    public void SetUp()
    {
        _allowMovement = true;
        _maxDodgeAccumulation = player.PlayerStats.BaseDodgeAccumulation;
        _currentDodgeRemining = _maxDodgeAccumulation;
        
        OnDodgeCooldownUpdated?.Invoke(_dodgeCooldownTimer/dodgeCooldown);
        OnDodgeCountChanged?.Invoke(_currentDodgeRemining);
    }

    private void Update()
    {
        HandleDodging();
        HandleShipModelAndRotation();
    }

    private void FixedUpdate()
    {
        HandlePosition();
    }
    
    private void OnDeath()
    {
        _allowMovement = false;
        _autoCenterRoutine = StartCoroutine(ReturnToCenter());
    }
    
    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;

        if (_autoCenterRoutine != null) StopCoroutine(_autoCenterRoutine);

        if (_allowMovement != stage.AllowPlayerMovement)
        {
            _allowMovement = stage.AllowPlayerMovement;

            if (!stage.AllowPlayerMovement)
            {
                _autoCenterRoutine = StartCoroutine(ReturnToCenter());
            }
        }

    }
    

    #region Movement --------------------------------------------------------------------------------------
    

    private void HandlePosition()
    {
        Vector3 playerSplinePosition = player.LevelManager.PlayerPosition;
        
        // Calculate current offset from spline in local space
        Vector3 worldOffset = transform.position - playerSplinePosition;
        _currentOffsetFromSpline = worldOffset;
        

        if (!_isDodging)
        {
            InputDirection = new Vector3(_horizontalInput, _verticalInput, 0);

            if (InputDirection != Vector3.zero)
            {
                _targetOffsetFromSpline += InputDirection * (maxMoveSpeed * Time.fixedDeltaTime);
                _targetOffsetFromSpline.x = Mathf.Clamp(_targetOffsetFromSpline.x, -MovementBoundaryX, MovementBoundaryX);
                _targetOffsetFromSpline.y = Mathf.Clamp(_targetOffsetFromSpline.y, -MovementBoundaryY, MovementBoundaryY);
                _targetOffsetFromSpline.z = 0; 
            }
            

            float lerpSpeed = InputDirection != Vector3.zero ? acceleration : deceleration;
            _currentOffsetFromSpline = Vector3.Lerp(_currentOffsetFromSpline, _targetOffsetFromSpline, lerpSpeed * Time.fixedDeltaTime);
        }
        else
        {

            Vector3 dodgeMovement = _dodgeDirection * (dodgeMoveSpeed * Time.fixedDeltaTime);
            _targetOffsetFromSpline += dodgeMovement;
            _currentOffsetFromSpline += dodgeMovement;
            
            _targetOffsetFromSpline.x = Mathf.Clamp(_targetOffsetFromSpline.x, -MovementBoundaryX, MovementBoundaryX);
            _targetOffsetFromSpline.y = Mathf.Clamp(_targetOffsetFromSpline.y, -MovementBoundaryY, MovementBoundaryY);
            _currentOffsetFromSpline.x = Mathf.Clamp(_currentOffsetFromSpline.x, -MovementBoundaryX, MovementBoundaryX);
            _currentOffsetFromSpline.y = Mathf.Clamp(_currentOffsetFromSpline.y, -MovementBoundaryY, MovementBoundaryY);
        }
        

        _normalizedMovementPosition = new Vector2(
            MovementBoundaryX > 0 ? _currentOffsetFromSpline.x / MovementBoundaryX : 0f,
            MovementBoundaryY > 0 ? _currentOffsetFromSpline.y / MovementBoundaryY : 0f
        );
        

        Vector3 desiredWorldPosition = playerSplinePosition + _currentOffsetFromSpline;
        Vector3 positionDifference = desiredWorldPosition - transform.position;
        float distanceToDesired = positionDifference.magnitude;
        float effectiveFollowSpeed = pathFollowSpeed * (1f + distanceToDesired);
        
        playerRigidbody.linearVelocity = positionDifference.normalized * Mathf.Min(effectiveFollowSpeed, distanceToDesired / Time.fixedDeltaTime);
    }
    
    
    
    private void HandleShipModelAndRotation()
    {
        if (!shipModel) return;
    
        // Movement rotation based on movement (only roll)
        float inputRoll = -_horizontalInput * maxRollAmount;
        Quaternion targetVelocityRotation = Quaternion.Euler(0f, 0f, inputRoll);
    
        _velocityRotation = _horizontalInput != 0f 
            ? Quaternion.Slerp(_velocityRotation, targetVelocityRotation, rollSpeed * Time.deltaTime) 
            : Quaternion.Slerp(_velocityRotation, targetVelocityRotation, rollSpeed / 2 * Time.deltaTime);

        // Aim rotation from aiming (only pitch and yaw)
        if (playerAiming)
        {
            Vector3 aimDirection = playerAiming.AimDirection;
        
            float yawAngle = Mathf.Atan2(aimDirection.x, aimDirection.z) * Mathf.Rad2Deg;
            float pitchAngle = -Mathf.Asin(Mathf.Clamp(aimDirection.y, -1f, 1f)) * Mathf.Rad2Deg;
        
            yawAngle = Mathf.Clamp(yawAngle, -maxYawAngle, maxYawAngle);
            pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
        
            Quaternion targetAimRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            _aimRotation = Quaternion.Slerp(_aimRotation, targetAimRotation, pitchYawSpeed * Time.deltaTime);
        }
        else
        {
            _aimRotation = Quaternion.identity;
        }

        // Combine all rotations: aim + input roll + dodge roll
        Vector3 finalEuler = _aimRotation.eulerAngles;
        finalEuler.z = _velocityRotation.eulerAngles.z + _currentDodgeRoll;
    
        shipModel.localRotation = Quaternion.Euler(finalEuler);
    }
    
    
    private IEnumerator ReturnToCenter()
    {
        while (_targetOffsetFromSpline.magnitude > 0.01f)
        {
            _targetOffsetFromSpline = Vector3.Lerp(_targetOffsetFromSpline, Vector3.zero, 1f * Time.deltaTime);
            yield return null;
        }
        
        _targetOffsetFromSpline = Vector3.zero;
    }

    
    
    #endregion Movement  --------------------------------------------------------------------------------------

    

    #region Dodge --------------------------------------------------------------------------------------

    private void HandleDodging()
    {
        // Check if we are currently dodging
        if (_isDodging && _dodgeTimeCounter <= dodgeDuration)
        {
            _dodgeTimeCounter += Time.deltaTime;
        
            // Reset dodge if we exceed the dodge time
            if (_dodgeTimeCounter >= dodgeDuration)
            {
                _isDodging = false;
                _dodgeCooldownTimer = dodgeCooldown;
                _currentDodgeRoll = 0f;
            }
        }
    
        // Check cooldown
        if (!_isDodging && _dodgeCooldownTimer > 0f)
        {
            _dodgeCooldownTimer -= Time.deltaTime;
            if (_dodgeCooldownTimer < 0f) _dodgeCooldownTimer = 0f;
            OnDodgeCooldownUpdated?.Invoke(_dodgeCooldownTimer/dodgeCooldown);
        }
        
        // Accumulate dodges
        if (!_isDodging && _dodgeAccumulationRateTimer > 0f && _currentDodgeRemining < _maxDodgeAccumulation)
        {
            _dodgeAccumulationRateTimer -= Time.deltaTime;
            
            if (_dodgeAccumulationRateTimer < 0f)
            {
                _dodgeAccumulationRateTimer = dodgeAccumulationRate;
                _currentDodgeRemining += 1;
                OnDodgeCountChanged?.Invoke(_currentDodgeRemining);
            }

        }
    }
    
    private void Dodge(Vector3 direction)
    {
        if (!(_dodgeCooldownTimer <= 0f) || _isDodging || _currentDodgeRemining <= 0) return;
        
        OnDodge?.Invoke();
        _dodgeDirection = direction;
        _dodgeTimeCounter = 0f;
        _isDodging = true;
        _currentDodgeRemining -= 1;
        _dodgeAccumulationRateTimer = dodgeAccumulationRate;
        OnDodgeCountChanged?.Invoke(_currentDodgeRemining);
        
        dodgeSfx?.Play(audioSource);
        controllerVibrationSource.VibrateFadeIn(0.05f, 0f, dodgeTweenSettings.duration/2);
        
        if (_dodgeTween.isAlive) _dodgeTween.Stop();
        float startRoll = 0;
        float targetRoll = startRoll + (-_dodgeDirection.x * dodgeRollAmount);
        
        _dodgeTween = Tween.Custom(
            onValueChange: rollAngle => _currentDodgeRoll = rollAngle,
            startValue: startRoll,
            endValue: targetRoll,
            settings: dodgeTweenSettings
        );
    }
    
    
    public void UpgradeDodgeAccumulationBy(int amount)
    {
        _maxDodgeAccumulation += amount;
        if (_maxDodgeAccumulation > player.PlayerStats.MaxDodgeAccumulation)
        {
            _maxDodgeAccumulation = player.PlayerStats.MaxDodgeAccumulation;
        }
        
        _dodgeAccumulationRateTimer = dodgeAccumulationRate;
        _currentDodgeRemining = _maxDodgeAccumulation;
        OnDodgeCountChanged?.Invoke(_currentDodgeRemining);
    }
    

    #endregion Dodge --------------------------------------------------------------------------------------
    
    
    
    #region Input Handling --------------------------------------------------------------------------------------

    private void OnMove(InputAction.CallbackContext context)
    {
        if (!_allowMovement || !player.Health.IsAlive())
        {
            _horizontalInput = 0f;
            _verticalInput = 0f;
            return;
        }
        
        if (context.started || context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            _horizontalInput = input.x;
            _verticalInput = input.y;
        } 
        else if (context.canceled)
        {
            _horizontalInput = 0f;
            _verticalInput = 0f;
        }
    }
    
    private void OnDodgeLeft(InputAction.CallbackContext context)
    {
        if (!_allowMovement || !player.Health.IsAlive()) return;

        Dodge(Vector3.left);
    }



    
    private void OnDodgeRight(InputAction.CallbackContext context)
    {
        if (!_allowMovement || !player.Health.IsAlive()) return;
        
        Dodge(Vector3.right);
    }
    
    private void OnDodgeFreeform(InputAction.CallbackContext context)
    {
        if (!_allowMovement || !player.Health.IsAlive()) return;


        switch (_horizontalInput)
        {
            case < 0:
                Dodge(Vector3.left);
                break;
            case > 0:
                Dodge(Vector3.right);
                break;
        }
    }

    #endregion Input Handling --------------------------------------------------------------------------------------
    
    
    #if UNITY_EDITOR
    #region Editor -----------------------------------------------------------------------------------------------


    

    private void OnDrawGizmos()
    {
        // Draw player position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(player.LevelManager.PlayerPosition, 0.5f);
        Vector3 playerSplinePosition = player.LevelManager.PlayerPosition;
        Vector3[] localCorners = new Vector3[]
        {
            new Vector3(-MovementBoundaryX, -MovementBoundaryY, 0),
            new Vector3(MovementBoundaryX, -MovementBoundaryY, 0),  
            new Vector3(MovementBoundaryX, MovementBoundaryY, 0),   
            new Vector3(-MovementBoundaryX, MovementBoundaryY, 0)  
        };
        Vector3[] worldCorners = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            worldCorners[i] = playerSplinePosition + localCorners[i];
        }
        for (int i = 0; i < 4; i++)
        {
            int nextIndex = (i + 1) % 4;
            Gizmos.DrawLine(worldCorners[i], worldCorners[nextIndex]);
        }
        UnityEditor.Handles.Label(playerSplinePosition + Vector3.up * (MovementBoundaryY + 1f), "Player Boundaries");
        
    }

    #endregion Editor -----------------------------------------------------------------------------------------------

#endif
    
}