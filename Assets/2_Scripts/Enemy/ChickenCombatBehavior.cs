using KBCore.Refs;
using UnityEngine;
using VInspector;

// Handles combat state, concussion mechanics, damage, and hovering
[RequireComponent(typeof(ChickenController), typeof(ChickenFormationBehavior))]
public class ChickenCombatBehavior : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverHeight = 2f; // Height above formation slot to hover
    [SerializeField] private float hoverForce = 10f; // Force applied to maintain hover
    [SerializeField] private float wiggleAmount = 1f; // How much the chicken wiggles while hovering
    [SerializeField] private float wiggleSpeed = 2f; // Speed of the wiggle movement
    [SerializeField] private float hoverDamping = 5f; // Damping for smooth hovering
    
    [Header("Concussion Settings")]
    [SerializeField] private float concussRange = 5f; // Distance from slot to trigger concuss
    [SerializeField] private float concussFloatDrag = 2f; // Drag while floating
    [SerializeField] private bool enableConcussRotation = true; // Spin while concussed
    [SerializeField] private float concussRotationSpeed = 50f; // Spin speed
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private float currentConcussTimer = 0f;
    [SerializeField, ReadOnly] private bool canBeConcussed = false;
    [SerializeField, ReadOnly] private bool isHovering = false;
    
    // References
    [SerializeField, Self] private ChickenController chickenController;
    [SerializeField, Self] private ChickenFormationBehavior formationBehavior;
    [SerializeField, Self] private Rigidbody rb;
    
    // Concussion state
    private float concussTimer = 0f;
    private Vector3 concussVelocity;
    
    // Hover state
    private Vector3 hoverTarget;
    private Vector3 noiseOffset;
    private float hoverStartTime;
    
    // Events
    public event System.Action OnDamaged;
    public event System.Action OnConcussionStart;
    public event System.Action OnConcussionEnd;
    public event System.Action OnHoverStart;
    public event System.Action OnHoverEnd;
    
    private void OnValidate()
    {
        this.ValidateRefs();
    }
    private void Awake()
    {
        // Initialize random noise offset for unique wiggle pattern
        noiseOffset = new Vector3(
            Random.Range(0f, 100f),
            Random.Range(0f, 100f),
            Random.Range(0f, 100f)
        );
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
        
        // Handle hovering state
        if (newState == ChickenController.ChickenState.InCombat && oldState != ChickenController.ChickenState.InCombat)
        {
            StartHovering();
        }
        else if (oldState == ChickenController.ChickenState.InCombat && newState != ChickenController.ChickenState.InCombat)
        {
            StopHovering();
        }
        
        if (newState == ChickenController.ChickenState.Concussed)
        {
            StopHovering();
            OnConcussionStart?.Invoke();
        }
        else if (oldState == ChickenController.ChickenState.Concussed)
        {
            OnConcussionEnd?.Invoke();
            if (newState == ChickenController.ChickenState.InCombat)
            {
                StartHovering();
            }
        }
    }
    
    private void OnArrivedAtFormation()
    {
        canBeConcussed = true;
    }
    
    private void StartHovering()
    {
        isHovering = true;
        hoverStartTime = Time.time;
        UpdateHoverTarget();
        OnHoverStart?.Invoke();
    }
    
    private void StopHovering()
    {
        isHovering = false;
        OnHoverEnd?.Invoke();
    }
    
    private void UpdateHoverTarget()
    {
        if (formationBehavior != null)
        {
            Vector3 slotPosition = formationBehavior.GetTargetSlotPosition;
            hoverTarget = slotPosition + Vector3.up * hoverHeight;
        }
    }
    
    private void Update()
    {
        // Handle concussed rotation
        if (chickenController.IsConcussed && enableConcussRotation)
        {
            transform.Rotate(Vector3.up, concussRotationSpeed * Time.deltaTime);
        }
        
        // Update hover target position
        if (isHovering)
        {
            UpdateHoverTarget();
        }
        
        currentConcussTimer = concussTimer;
    }
    
    private void FixedUpdate()
    {
        switch (chickenController.CurrentState)
        {
            case ChickenController.ChickenState.InCombat:
                if (isHovering)
                {
                    HandleHoverPhysics();
                }
                break;
                
            case ChickenController.ChickenState.Concussed:
                HandleConcussedPhysics();
                break;
        }
    }
    
    // Handle hovering physics with wiggle
    private void HandleHoverPhysics()
    {
        float time = Time.time - hoverStartTime;
        
        // Generate random wiggle using Perlin noise
        Vector3 wiggle = new Vector3(
            (Mathf.PerlinNoise((time + noiseOffset.x) * wiggleSpeed, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise((time + noiseOffset.y) * wiggleSpeed, 10f) - 0.5f) * 2f,
            (Mathf.PerlinNoise((time + noiseOffset.z) * wiggleSpeed, 20f) - 0.5f) * 2f
        ) * wiggleAmount;
        
        Vector3 targetPosition = hoverTarget + wiggle;
        Vector3 direction = targetPosition - transform.position;
        
        // Apply hover force
        Vector3 force = direction * hoverForce;
        
        // Add damping to reduce oscillation
        Vector3 dampingForce = -rb.linearVelocity * hoverDamping;
        
        rb.AddForce(force + dampingForce);
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
    private void EnterConcussState(float concussTime)
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
    public void ApplyConcussion(float concussDuration)
    {
        if (!canBeConcussed || chickenController.CurrentState != ChickenController.ChickenState.InCombat)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot apply concussion - not in combat mode!");
            return;
        }
        
        EnterConcussState(concussDuration);
        
    }
    
    public void ApplyForce(Vector3 direction, float force)
    {
        if (chickenController.CurrentState == ChickenController.ChickenState.Concussed)
        {
            // Apply force while concussed
            concussVelocity += direction * force;
        }
        else if (chickenController.CurrentState == ChickenController.ChickenState.InCombat)
        {
            // Apply force normally
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
    }
    
    public void ApplyTorque(Vector3 torque, float force)
    {
        if (chickenController.CurrentState == ChickenController.ChickenState.Concussed)
        {
            // Apply torque while concussed
            rb.AddTorque(torque * force, ForceMode.Impulse);
        }
        else if (chickenController.CurrentState == ChickenController.ChickenState.InCombat)
        {
            // Apply torque normally
            rb.AddTorque(torque * force, ForceMode.VelocityChange);
        }
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
                
                // Draw hover target when hovering
                if (isHovering)
                {
                    Gizmos.color = new Color(0, 1, 0, 0.3f);
                    Gizmos.DrawWireSphere(hoverTarget, 0.5f);
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(slotPos, hoverTarget);
                }
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
            
            // Show hovering state
            if (isHovering)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
                
                #if UNITY_EDITOR
                Vector3 labelPos = transform.position + Vector3.up * 2f;
                UnityEditor.Handles.Label(labelPos, "Hovering");
                #endif
            }
        }
    }
}