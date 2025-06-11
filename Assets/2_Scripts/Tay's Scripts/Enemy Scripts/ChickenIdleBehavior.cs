using System.Collections;
using KBCore.Refs;
using UnityEngine;
using VInspector;

// Handles idle behavior, spawn point movement, and periodic slot checking
[RequireComponent(typeof(ChickenController), typeof(ChickenFormationBehavior))]
public class ChickenIdleBehavior : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint; // Reference to spawn point
    [SerializeField] private float spawnPointRadius = 3f; // Radius around spawn point
    [SerializeField] private bool randomizeSpawnOffset = true; // Random offset at spawn
    [SerializeField] private float spawnPointSpeed = 3f; // Time to reach spawn point
    
    [Header("Idle Behavior")]
    [SerializeField] private bool enableIdleMovement = true; // Subtle movement while idle
    [SerializeField] private float idleWobbleSpeed = 1f; // Speed of wobble
    [SerializeField] private float idleWobbleAmount = 0.5f; // Amount of wobble
    [SerializeField] private float slotCheckInterval = 2f; // Check for slots every X seconds
    
    [Header("Movement")]
    [SerializeField] private AnimationCurve movementCurve = null; // Movement easing
    [SerializeField] private float arrivalThreshold = 0.5f; // Distance to consider arrived
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private float nextSlotCheckTime = 0f;
    [SerializeField, ReadOnly] private Vector3 currentTargetPosition;
    
    // References
    [SerializeField, Self]private ChickenController chickenController;
    [SerializeField, Self] private ChickenFormationBehavior formationBehavior;
    [SerializeField, Self]private Rigidbody rb;
    
    // Movement state
    private Vector3 initialPosition;
    private Vector3 spawnPointTargetPosition;
    private Vector3 idleStartPosition;
    private float moveTimer = 0f;
    private float idleTime = 0f;
    private Coroutine currentIdleCoroutine;
    private void OnValidate()
    {
        this.ValidateRefs();
    }
    private void Awake()
    {
        // Find spawn point if not assigned
        if (spawnPoint == null)
        {
            GameObject spawnPointObject = GameObject.Find("Spawn Point");
            if (spawnPointObject != null)
            {
                spawnPoint = spawnPointObject.transform;
            }
        }
    }
    
    private void OnEnable()
    {
        chickenController.OnStateChanged += OnStateChanged;
    }
    
    private void OnDisable()
    {
        chickenController.OnStateChanged -= OnStateChanged;
        
        if (currentIdleCoroutine != null)
        {
            StopCoroutine(currentIdleCoroutine);
        }
    }
    
    private void OnStateChanged(ChickenController.ChickenState oldState, ChickenController.ChickenState newState)
    {
        switch (newState)
        {
            case ChickenController.ChickenState.MovingToSpawnPoint:
                StartMovingToSpawnPoint();
                break;
                
            case ChickenController.ChickenState.AtSpawnPoint:
            case ChickenController.ChickenState.Idle:
                StartIdleBehavior();
                break;
        }
    }
    
    // Called by FormationBehavior when no slots are available
    public void MoveToSpawnPoint()
    {
        if (spawnPoint == null)
        {
            // No spawn point - just go idle
            chickenController.SetState(ChickenController.ChickenState.Idle);
            return;
        }
        
        chickenController.SetState(ChickenController.ChickenState.MovingToSpawnPoint);
    }
    
    private void StartMovingToSpawnPoint()
    {
        if (spawnPoint == null)
        {
            chickenController.SetState(ChickenController.ChickenState.Idle);
            return;
        }
        
        initialPosition = transform.position;
        moveTimer = 0f;
        
        // Calculate target position
        if (randomizeSpawnOffset)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnPointRadius;
            spawnPointTargetPosition = spawnPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        else
        {
            spawnPointTargetPosition = spawnPoint.position;
        }
        
        currentTargetPosition = spawnPointTargetPosition;
    }
    
    private void StartIdleBehavior()
    {
        idleStartPosition = transform.position;
        idleTime = 0f;
        nextSlotCheckTime = Time.time + slotCheckInterval;
        
        // Start checking for available slots
        if (currentIdleCoroutine != null)
        {
            StopCoroutine(currentIdleCoroutine);
        }
        currentIdleCoroutine = StartCoroutine(IdleAndCheckForSlots());
    }
    
    private IEnumerator IdleAndCheckForSlots()
    {
        while (chickenController.IsIdle || chickenController.IsAtSpawnPoint)
        {
            yield return new WaitForSeconds(slotCheckInterval);
            
            // Ask formation behavior to check for slots
            formationBehavior.CheckForAvailableSlot();
            nextSlotCheckTime = Time.time + slotCheckInterval;
        }
    }
    
    private void FixedUpdate()
    {
        switch (chickenController.CurrentState)
        {
            case ChickenController.ChickenState.MovingToSpawnPoint:
                MoveTowardsPosition(spawnPointTargetPosition, spawnPointSpeed, OnArrivedAtSpawnPoint);
                break;
                
            case ChickenController.ChickenState.AtSpawnPoint:
            case ChickenController.ChickenState.Idle:
                HandleIdleMovement();
                break;
        }
    }
    
    private void MoveTowardsPosition(Vector3 targetPosition, float speed, System.Action onArrival)
    {
        moveTimer += Time.fixedDeltaTime;
        float t = moveTimer / speed;
        
        if (t >= 1f)
        {
            t = 1f;
            onArrival?.Invoke();
        }
        
        // Apply easing
        float easedT = movementCurve != null && movementCurve.length > 0 ? 
            movementCurve.Evaluate(t) : EaseOutCubic(t);
        
        // Interpolate position
        Vector3 desiredPosition = Vector3.Lerp(initialPosition, targetPosition, easedT);
        Vector3 velocity = (desiredPosition - transform.position) / Time.fixedDeltaTime;
        rb.linearVelocity = velocity;
        
        // Check early arrival
        if (Vector3.Distance(transform.position, targetPosition) < arrivalThreshold && t < 1f)
        {
            onArrival?.Invoke();
        }
    }
    
    private void HandleIdleMovement()
    {
        if (enableIdleMovement)
        {
            idleTime += Time.fixedDeltaTime;
            
            // Create wobble effect
            float wobbleX = Mathf.Sin(idleTime * idleWobbleSpeed) * idleWobbleAmount;
            float wobbleY = Mathf.Cos(idleTime * idleWobbleSpeed * 0.7f) * idleWobbleAmount * 0.5f;
            
            Vector3 wobbleOffset = new Vector3(wobbleX, wobbleY, 0);
            Vector3 targetIdlePosition = idleStartPosition + wobbleOffset;
            
            // Apply smooth movement
            rb.linearVelocity = (targetIdlePosition - transform.position) * 2f;
        }
        else
        {
            // Just apply drag
            rb.linearVelocity *= 0.95f;
        }
    }
    
    private void OnArrivedAtSpawnPoint()
    {
        chickenController.SetState(ChickenController.ChickenState.AtSpawnPoint);
    }
    
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    
    // Force check for available slots
    [Button]
    public void ForceCheckSlots()
    {
        if (chickenController.IsIdle || chickenController.IsAtSpawnPoint)
        {
            formationBehavior.CheckForAvailableSlot();
        }
    }
    
    private void OnDrawGizmos()
    {
        // Draw spawn point
        if (spawnPoint != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan
            Gizmos.DrawWireSphere(spawnPoint.position, spawnPointRadius);
            
            if (chickenController != null && 
                (chickenController.CurrentState == ChickenController.ChickenState.MovingToSpawnPoint || 
                 chickenController.CurrentState == ChickenController.ChickenState.AtSpawnPoint))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, currentTargetPosition);
                Gizmos.DrawSphere(currentTargetPosition, 0.3f);
            }
        }
        
        // Show idle state
        if (chickenController != null && (chickenController.IsIdle || chickenController.IsAtSpawnPoint))
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawWireSphere(transform.position, 0.8f);
            
            #if UNITY_EDITOR
            Vector3 labelPos = transform.position + Vector3.up * 1.5f;
            string stateText = chickenController.IsAtSpawnPoint ? "AT SPAWN POINT" : "IDLE";
            UnityEditor.Handles.Label(labelPos, $"{stateText}\nNext check: {(nextSlotCheckTime - Time.time):F1}s");
            #endif
        }
    }
}