using System.Collections;
using KBCore.Refs;
using UnityEngine;
using VInspector;

// Handles all formation-related movement and slot management
[RequireComponent(typeof(ChickenController))]
public class ChickenFormationBehavior : MonoBehaviour
{
    [Header("Formation Settings")]
    [SerializeField] private float initialSpeed = 2f; // Base time in seconds to reach slot
    [SerializeField] private float arrivalTimeVariance = 0.5f; // Random time added
    [SerializeField] private AnimationCurve movementCurve = null; // Optional: custom movement curve
    [SerializeField] private float arrivalThreshold = 0.5f; // Distance to consider "arrived"
    [SerializeField] private float maxWaitTime = 5f; // Max time to wait for formation
    
    [Header("Combat Movement")]
    [SerializeField] private float damping = 5f; // Damping for smooth movement
    [SerializeField] private float followLerpSpeed = 2f; // Lerp speed when following
    [SerializeField] private float positionDeadZone = 0.1f; // Stop micro-adjusting when this close
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool hasAssignedSlot = false;
    [SerializeField, ReadOnly] private string assignedSlotInfo = "None";
    
    // References
    [SerializeField,Self] private ChickenController chickenController; 
    private FormationManager _formationManager;
    private FormationManager.FormationSlot _assignedSlot;
   
    [SerializeField, Self] private Rigidbody rb;
    
    // Movement tracking
    private Vector3 _initialPosition;
    private float _moveTimer = 0f;
    private float _actualArrivalTime = 0f;
    private bool _hasArrivedAtSlotOnce = false;
    private float _waitTimer = 0f;
    
    // Events
    public event System.Action OnArrivedAtSlot;
    public event System.Action OnSlotReleased;
    
    // Properties
    public bool HasAssignedSlot => _assignedSlot != null;
    public Vector3 GetTargetSlotPosition => _assignedSlot != null && _formationManager != null ? 
        _formationManager.GetSlotWorldPosition(_assignedSlot) : transform.position;
    
    private void OnValidate()
    {
        this.ValidateRefs();
    }
    private void Awake()
    {
        // Find FormationManager
        _formationManager = FindFirstObjectByType<FormationManager>();
        if (_formationManager == null)
        {
            Debug.LogError($"{gameObject.name}: FormationManager not found!");
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to formation change event
        FormationManager.OnFormationChanged += HandleFormationChanged;
        
        // Subscribe to state changes
        if (chickenController != null)
        {
            chickenController.OnStateChanged += OnStateChanged;
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        FormationManager.OnFormationChanged -= HandleFormationChanged;
        
        if (chickenController != null)
        {
            chickenController.OnStateChanged -= OnStateChanged;
        }
        
        ReleaseSlot();
    }
    
    private void Start()
    {
        if (_formationManager != null)
        {
            StartCoroutine(WaitForFormationAndAssign());
        }
    }
    
    // Handle state changes
    private void OnStateChanged(ChickenController.ChickenState oldState, ChickenController.ChickenState newState)
    {
        switch (newState)
        {
            case ChickenController.ChickenState.MovingToSlot:
                StartMovingToSlot();
                break;
                
            case ChickenController.ChickenState.ReturningToSlot:
                StartReturningToSlot();
                break;
                
            case ChickenController.ChickenState.WaitingForFormation:
                _waitTimer = 0f;
                StartCoroutine(WaitForFormationAndAssign());
                break;
        }
    }
    
    // Wait for formation to be ready and try to assign
    private IEnumerator WaitForFormationAndAssign()
    {
        while (chickenController.CurrentState == ChickenController.ChickenState.WaitingForFormation && 
               _waitTimer < maxWaitTime)
        {
            if (_formationManager.FormationSlots != null && _formationManager.FormationSlots.Count > 0)
            {
                if (TryAssignToSlot())
                {
                    chickenController.SetState(ChickenController.ChickenState.MovingToSlot);
                    yield break;
                }
                else
                {
                    // No slots available - let ChickenIdleBehavior handle spawn point
                    SendMessage("MoveToSpawnPoint", SendMessageOptions.DontRequireReceiver);
                    yield break;
                }
            }
            
            _waitTimer += Time.deltaTime;
            yield return null;
        }
        
        // Timed out - move to spawn point
        if (chickenController.CurrentState == ChickenController.ChickenState.WaitingForFormation)
        {
            SendMessage("MoveToSpawnPoint", SendMessageOptions.DontRequireReceiver);
        }
    }
    
    // Try to get a formation slot
    private bool TryAssignToSlot()
    {
        _assignedSlot = _formationManager.TryOccupySlot(gameObject);
        
        if (_assignedSlot == null)
        {
            // Try nearest slot
            var nearestSlot = _formationManager.GetNearestAvailableSlot(transform.position);
            if (nearestSlot != null && _formationManager.OccupySpecificSlot(nearestSlot, gameObject))
            {
                _assignedSlot = nearestSlot;
            }
        }
        
        hasAssignedSlot = _assignedSlot != null;
        assignedSlotInfo = _assignedSlot != null ? $"Slot {_assignedSlot.formationIndex}" : "None";
        
        return _assignedSlot != null;
    }
    
    // Start moving to assigned slot
    private void StartMovingToSlot()
    {
        if (_assignedSlot == null) return;
        
        _initialPosition = transform.position;
        _moveTimer = 0f;
        _actualArrivalTime = initialSpeed + Random.Range(0f, arrivalTimeVariance);
    }
    
    // Start returning to slot after concussion
    private void StartReturningToSlot()
    {
        if (_assignedSlot == null) return;
        
        _initialPosition = transform.position;
        _moveTimer = 0f;
        _actualArrivalTime = initialSpeed / 2f; // Return faster
    }
    
    // Release current slot
    public void ReleaseSlot()
    {
        // Don't release if concussed
        if (chickenController.IsConcussed) return;
        
        if (_assignedSlot != null && _formationManager != null)
        {
            _formationManager.ReleaseSlot(_assignedSlot);
            _assignedSlot = null;
            hasAssignedSlot = false;
            assignedSlotInfo = "None";
            OnSlotReleased?.Invoke();
        }
    }
    
    // Handle formation changes
    private void HandleFormationChanged()
    {
        // Don't reset if concussed
        if (chickenController.IsConcussed) return;
        
        ReleaseSlot();
        _hasArrivedAtSlotOnce = false;
        chickenController.SetState(ChickenController.ChickenState.WaitingForFormation);
    }
    
    // Called when a slot becomes available
    public void NotifySlotAvailable()
    {
        if (chickenController.IsIdle || chickenController.IsAtSpawnPoint)
        {
            StopAllCoroutines();
            chickenController.SetState(ChickenController.ChickenState.WaitingForFormation);
        }
    }
    
    // Check if we can be assigned to a slot
    public void CheckForAvailableSlot()
    {
        if (!HasAssignedSlot && 
            (chickenController.IsIdle || chickenController.IsAtSpawnPoint) && 
            _formationManager != null)
        {
            var availableSlots = _formationManager.GetAvailableSlots();
            if (availableSlots.Count > 0 && TryAssignToSlot())
            {
                chickenController.SetState(ChickenController.ChickenState.MovingToSlot);
            }
        }
    }
    
    private void FixedUpdate()
    {
        switch (chickenController.CurrentState)
        {
            case ChickenController.ChickenState.MovingToSlot:
                MoveTowardsSlot();
                break;
                
            case ChickenController.ChickenState.InCombat:
                FollowSlotInCombat();
                break;
                
            case ChickenController.ChickenState.ReturningToSlot:
                MoveTowardsSlotFast();
                break;
        }
    }
    
    // Move to slot for first time
    private void MoveTowardsSlot()
    {
        if (_assignedSlot == null) return;
        
        Vector3 targetPosition = _formationManager.GetSlotWorldPosition(_assignedSlot);
        _moveTimer += Time.fixedDeltaTime;
        float t = _moveTimer / _actualArrivalTime;
        
        if (t >= 1f)
        {
            t = 1f;
            if (!_hasArrivedAtSlotOnce)
            {
                _hasArrivedAtSlotOnce = true;
                chickenController.SetState(ChickenController.ChickenState.InCombat);
                OnArrivedAtSlot?.Invoke();
            }
        }
        
        // Apply movement curve
        float easedT = movementCurve != null && movementCurve.length > 0 ? 
            movementCurve.Evaluate(t) : EaseOutCubic(t);
        
        Vector3 desiredPosition = Vector3.Lerp(_initialPosition, targetPosition, easedT);
        Vector3 velocity = (desiredPosition - transform.position) / Time.fixedDeltaTime;
        rb.linearVelocity = velocity;
        
        // Check early arrival
        if (Vector3.Distance(transform.position, targetPosition) < arrivalThreshold && !_hasArrivedAtSlotOnce)
        {
            _hasArrivedAtSlotOnce = true;
            chickenController.SetState(ChickenController.ChickenState.InCombat);
            OnArrivedAtSlot?.Invoke();
        }
    }
    
    // Follow slot position during combat
    private void FollowSlotInCombat()
    {
        if (_assignedSlot == null) return;
        
        Vector3 targetPosition = _formationManager.GetSlotWorldPosition(_assignedSlot);
        float distanceToSlot = Vector3.Distance(transform.position, targetPosition);
        
        // Stop micro-adjustments
        if (distanceToSlot < positionDeadZone)
        {
            rb.linearVelocity *= 0.5f;
            return;
        }
        
        // Smooth follow
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, followLerpSpeed * Time.fixedDeltaTime);
        Vector3 desiredVelocity = (smoothedPosition - transform.position) / Time.fixedDeltaTime;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, damping * Time.fixedDeltaTime);
    }
    
    // Return to slot quickly after concussion
    private void MoveTowardsSlotFast()
    {
        if (_assignedSlot == null) return;
        
        Vector3 targetPosition = _formationManager.GetSlotWorldPosition(_assignedSlot);
        _moveTimer += Time.fixedDeltaTime;
        float t = _moveTimer / _actualArrivalTime;
        
        if (t >= 1f || Vector3.Distance(transform.position, targetPosition) < arrivalThreshold)
        {
            chickenController.SetState(ChickenController.ChickenState.InCombat);
        }
        
        float easedT = movementCurve != null && movementCurve.length > 0 ? 
            movementCurve.Evaluate(t) : EaseOutCubic(t);
        
        Vector3 desiredPosition = Vector3.Lerp(_initialPosition, targetPosition, easedT);
        Vector3 velocity = (desiredPosition - transform.position) / Time.fixedDeltaTime;
        rb.linearVelocity = velocity;
    }
    
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    
    // Get distance to assigned slot
    public float GetDistanceToSlot()
    {
        if (_assignedSlot == null || _formationManager == null) return float.MaxValue;
        return Vector3.Distance(transform.position, _formationManager.GetSlotWorldPosition(_assignedSlot));
    }
    
    private void OnDrawGizmos()
    {
        if (_assignedSlot != null && _formationManager != null)
        {
            Vector3 slotPosition = _formationManager.GetSlotWorldPosition(_assignedSlot);
            
            // Color based on state
            switch (chickenController.CurrentState)
            {
                case ChickenController.ChickenState.MovingToSlot:
                    Gizmos.color = Color.yellow;
                    break;
                case ChickenController.ChickenState.InCombat:
                    Gizmos.color = Color.green;
                    break;
                case ChickenController.ChickenState.ReturningToSlot:
                    Gizmos.color = Color.magenta;
                    break;
                default:
                    Gizmos.color = Color.gray;
                    break;
            }
            
            Gizmos.DrawLine(transform.position, slotPosition);
            Gizmos.DrawWireSphere(slotPosition, 0.5f);
            
            // Draw dead zone in combat
            if (chickenController.IsInCombatMode)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(slotPosition, positionDeadZone);
            }
        }
    }
}