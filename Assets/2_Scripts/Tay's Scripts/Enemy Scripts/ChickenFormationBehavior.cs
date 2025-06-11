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
    [SerializeField] private float slotCheckInterval = 2f; // Check for open slots every X seconds
    
    [Header("Combat Movement")]
    [SerializeField] private float followStrength = 10f; // How strongly to follow slot in combat
    [SerializeField] private float damping = 5f; // Damping for smooth movement
    [SerializeField] private float followLerpSpeed = 2f; // Lerp speed when following
    [SerializeField] private float positionDeadZone = 0.1f; // Stop micro-adjusting when this close
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool hasAssignedSlot = false;
    [SerializeField, ReadOnly] private string assignedSlotInfo = "None";
    
    // References
    [SerializeField,Self] private ChickenController chickenController; 
    private FormationManager formationManager;
    private FormationManager.FormationSlot assignedSlot;
   
    [SerializeField, Self] private Rigidbody rb;
    
    // Movement tracking
    private Vector3 initialPosition;
    private float moveTimer = 0f;
    private float actualArrivalTime = 0f;
    private bool hasArrivedAtSlotOnce = false;
    private float waitTimer = 0f;
    
    // Events
    public event System.Action OnArrivedAtSlot;
    public event System.Action OnSlotReleased;
    
    // Properties
    public bool HasAssignedSlot => assignedSlot != null;
    public Vector3 GetTargetSlotPosition => assignedSlot != null && formationManager != null ? 
        formationManager.GetSlotWorldPosition(assignedSlot) : transform.position;
    
    private void OnValidate()
    {
        this.ValidateRefs();
    }
    private void Awake()
    {
        // Find FormationManager
        formationManager = FindObjectOfType<FormationManager>();
        if (formationManager == null)
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
        if (formationManager != null)
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
                waitTimer = 0f;
                StartCoroutine(WaitForFormationAndAssign());
                break;
        }
    }
    
    // Wait for formation to be ready and try to assign
    private IEnumerator WaitForFormationAndAssign()
    {
        while (chickenController.CurrentState == ChickenController.ChickenState.WaitingForFormation && 
               waitTimer < maxWaitTime)
        {
            if (formationManager.FormationSlots != null && formationManager.FormationSlots.Count > 0)
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
            
            waitTimer += Time.deltaTime;
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
        assignedSlot = formationManager.TryOccupySlot(gameObject);
        
        if (assignedSlot == null)
        {
            // Try nearest slot
            var nearestSlot = formationManager.GetNearestAvailableSlot(transform.position);
            if (nearestSlot != null && formationManager.OccupySpecificSlot(nearestSlot, gameObject))
            {
                assignedSlot = nearestSlot;
            }
        }
        
        hasAssignedSlot = assignedSlot != null;
        assignedSlotInfo = assignedSlot != null ? $"Slot {assignedSlot.formationIndex}" : "None";
        
        return assignedSlot != null;
    }
    
    // Start moving to assigned slot
    private void StartMovingToSlot()
    {
        if (assignedSlot == null) return;
        
        initialPosition = transform.position;
        moveTimer = 0f;
        actualArrivalTime = initialSpeed + Random.Range(0f, arrivalTimeVariance);
    }
    
    // Start returning to slot after concussion
    private void StartReturningToSlot()
    {
        if (assignedSlot == null) return;
        
        initialPosition = transform.position;
        moveTimer = 0f;
        actualArrivalTime = initialSpeed / 2f; // Return faster
    }
    
    // Release current slot
    public void ReleaseSlot()
    {
        // Don't release if concussed
        if (chickenController.IsConcussed) return;
        
        if (assignedSlot != null && formationManager != null)
        {
            formationManager.ReleaseSlot(assignedSlot);
            assignedSlot = null;
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
        hasArrivedAtSlotOnce = false;
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
            formationManager != null)
        {
            var availableSlots = formationManager.GetAvailableSlots();
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
        if (assignedSlot == null) return;
        
        Vector3 targetPosition = formationManager.GetSlotWorldPosition(assignedSlot);
        moveTimer += Time.fixedDeltaTime;
        float t = moveTimer / actualArrivalTime;
        
        if (t >= 1f)
        {
            t = 1f;
            if (!hasArrivedAtSlotOnce)
            {
                hasArrivedAtSlotOnce = true;
                chickenController.SetState(ChickenController.ChickenState.InCombat);
                OnArrivedAtSlot?.Invoke();
            }
        }
        
        // Apply movement curve
        float easedT = movementCurve != null && movementCurve.length > 0 ? 
            movementCurve.Evaluate(t) : EaseOutCubic(t);
        
        Vector3 desiredPosition = Vector3.Lerp(initialPosition, targetPosition, easedT);
        Vector3 velocity = (desiredPosition - transform.position) / Time.fixedDeltaTime;
        rb.linearVelocity = velocity;
        
        // Check early arrival
        if (Vector3.Distance(transform.position, targetPosition) < arrivalThreshold && !hasArrivedAtSlotOnce)
        {
            hasArrivedAtSlotOnce = true;
            chickenController.SetState(ChickenController.ChickenState.InCombat);
            OnArrivedAtSlot?.Invoke();
        }
    }
    
    // Follow slot position during combat
    private void FollowSlotInCombat()
    {
        if (assignedSlot == null) return;
        
        Vector3 targetPosition = formationManager.GetSlotWorldPosition(assignedSlot);
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
        if (assignedSlot == null) return;
        
        Vector3 targetPosition = formationManager.GetSlotWorldPosition(assignedSlot);
        moveTimer += Time.fixedDeltaTime;
        float t = moveTimer / actualArrivalTime;
        
        if (t >= 1f || Vector3.Distance(transform.position, targetPosition) < arrivalThreshold)
        {
            chickenController.SetState(ChickenController.ChickenState.InCombat);
        }
        
        float easedT = movementCurve != null && movementCurve.length > 0 ? 
            movementCurve.Evaluate(t) : EaseOutCubic(t);
        
        Vector3 desiredPosition = Vector3.Lerp(initialPosition, targetPosition, easedT);
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
        if (assignedSlot == null || formationManager == null) return float.MaxValue;
        return Vector3.Distance(transform.position, formationManager.GetSlotWorldPosition(assignedSlot));
    }
    
    private void OnDrawGizmos()
    {
        if (assignedSlot != null && formationManager != null)
        {
            Vector3 slotPosition = formationManager.GetSlotWorldPosition(assignedSlot);
            
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