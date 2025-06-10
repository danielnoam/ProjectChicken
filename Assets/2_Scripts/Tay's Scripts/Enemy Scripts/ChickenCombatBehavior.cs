using UnityEngine;
using VInspector;

// Handles combat state, concussion mechanics, and damage
[RequireComponent(typeof(ChickenController), typeof(ChickenFormationBehavior))]
public class ChickenCombatBehavior : MonoBehaviour
{
    [Header("Concussion Settings")]
    [SerializeField] private float concussTime = 1.5f; // Recovery time
    [SerializeField] private float concussRange = 5f; // Distance from slot to trigger concuss
    [SerializeField] private float concussFloatDrag = 2f; // Drag while floating
    [SerializeField] private bool enableConcussRotation = true; // Spin while concussed
    [SerializeField] private float concussRotationSpeed = 50f; // Spin speed
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private float currentConcussTimer = 0f;
    [SerializeField, ReadOnly] private bool canBeConcussed = false;
    
    // References
    private ChickenController chickenController;
    private ChickenFormationBehavior formationBehavior;
    private Rigidbody rb;
    
    // Concussion state
    private float concussTimer = 0f;
    private Vector3 concussVelocity;
    
    // Events
    public event System.Action OnDamaged;
    public event System.Action OnConcussionStart;
    public event System.Action OnConcussionEnd;
    
    private void Awake()
    {
        chickenController = GetComponent<ChickenController>();
        formationBehavior = GetComponent<ChickenFormationBehavior>();
        rb = GetComponent<Rigidbody>();
    }
    
    private void OnEnable()
    {
        // Subscribe to state changes
        chickenController.OnStateChanged += OnStateChanged;
        formationBehavior.OnArrivedAtSlot += OnArrivedAtFormation;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        chickenController.OnStateChanged -= OnStateChanged;
        formationBehavior.OnArrivedAtSlot -= OnArrivedAtFormation;
    }
    
    private void OnStateChanged(ChickenController.ChickenState oldState, ChickenController.ChickenState newState)
    {
        // Update if we can be concussed
        canBeConcussed = newState == ChickenController.ChickenState.InCombat;
        
        if (newState == ChickenController.ChickenState.Concussed)
        {
            OnConcussionStart?.Invoke();
        }
        else if (oldState == ChickenController.ChickenState.Concussed)
        {
            OnConcussionEnd?.Invoke();
        }
    }
    
    private void OnArrivedAtFormation()
    {
        canBeConcussed = true;
    }
    
    private void Update()
    {
        // Handle concussed rotation
        if (chickenController.IsConcussed && enableConcussRotation)
        {
            transform.Rotate(Vector3.up, concussRotationSpeed * Time.deltaTime);
        }
        
        currentConcussTimer = concussTimer;
    }
    
    private void FixedUpdate()
    {
        switch (chickenController.CurrentState)
        {
            case ChickenController.ChickenState.InCombat:
                CheckConcussionRange();
                break;
                
            case ChickenController.ChickenState.Concussed:
                HandleConcussedPhysics();
                break;
        }
    }
    
    // Check if knocked too far from slot
    private void CheckConcussionRange()
    {
        if (!formationBehavior.HasAssignedSlot) return;
        
        float distanceToSlot = formationBehavior.GetDistanceToSlot();
        if (distanceToSlot > concussRange)
        {
            EnterConcussState();
        }
    }
    
    // Handle physics while concussed
    private void HandleConcussedPhysics()
    {
        concussTimer -= Time.fixedDeltaTime;
        
        // Apply floating physics
        rb.linearVelocity = concussVelocity;
        concussVelocity *= (1f - concussFloatDrag * Time.fixedDeltaTime);
        
        if (concussTimer <= 0f)
        {
            ExitConcussState();
        }
    }
    
    // Enter concussion state
    private void EnterConcussState()
    {
        if (!canBeConcussed || chickenController.CurrentState != ChickenController.ChickenState.InCombat)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot enter concuss state - not in combat!");
            return;
        }
        
        chickenController.SetState(ChickenController.ChickenState.Concussed);
        concussTimer = concussTime;
        concussVelocity = rb.linearVelocity; // Preserve current velocity
    }
    
    // Exit concussion and return to slot
    private void ExitConcussState()
    {
        chickenController.SetState(ChickenController.ChickenState.ReturningToSlot);
    }
    
    // Apply concussive force (called by weapons)
    public void ApplyConcussion(Vector3 force)
    {
        if (!canBeConcussed || chickenController.CurrentState != ChickenController.ChickenState.InCombat)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot apply concussion - not in combat mode!");
            return;
        }
        
        // Apply the force
        rb.AddForce(force, ForceMode.Impulse);
        
        // Distance check will trigger concussion in next FixedUpdate
    }
    
    // Take damage (for other systems to hook into)
    public void TakeDamage(float damage)
    {
        OnDamaged?.Invoke();
        // Add damage handling logic here if needed
    }
    
    private void OnDrawGizmos()
    {
        // Draw concuss range when in combat
        if (chickenController != null && formationBehavior != null)
        {
            if (chickenController.IsInCombatMode || chickenController.IsConcussed)
            {
                Vector3 slotPos = formationBehavior.GetTargetSlotPosition;
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                Gizmos.DrawWireSphere(slotPos, concussRange);
            }
            
            // Show concussion state
            if (chickenController.IsConcussed)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 1f);
                
                #if UNITY_EDITOR
                Vector3 labelPos = transform.position + Vector3.up * 1.5f;
                UnityEditor.Handles.Label(labelPos, $"Concussed: {concussTimer:F1}s");
                #endif
            }
        }
    }
}