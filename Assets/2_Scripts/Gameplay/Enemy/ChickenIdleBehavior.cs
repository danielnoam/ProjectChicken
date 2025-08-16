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
    
    [Header("Outer Waiting Area")]
    [SerializeField] private bool useOuterWaitingArea = true; // Use outer area instead of spawn point
    [SerializeField] private float outerAreaMargin = 5f; // Distance outside the big spawn collider
    [SerializeField] private string spawnAreaBigTag = "SpawnAreaBig"; // Tag for big spawn collider
    [SerializeField] private string spawnAreaBlockerTag = "SpawnAreaBlocker"; // Tag for blocker collider
    [SerializeField] private LayerMask obstacleLayerMask = -1; // Layers to avoid when positioning
    
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
    [SerializeField, ReadOnly] private bool usingOuterArea = false;
    
    // References
    [SerializeField, Self]private ChickenController chickenController;
    [SerializeField, Self] private ChickenFormationBehavior formationBehavior;
    [SerializeField, Self]private Rigidbody rb;
    
    // Movement state
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private Vector3 idleStartPosition;
    private float moveTimer = 0f;
    private float idleTime = 0f;
    private Coroutine currentIdleCoroutine;
    private EnemySpawner enemySpawner;
    private BoxCollider spawnAreaBig;
    private BoxCollider spawnAreaBlocker;
    
    private void OnValidate()
    {
        this.ValidateRefs();
    }
    
    private void Awake()
    {
        // Find enemy spawner for spawn area info
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        
        // Find the spawn area colliders
        FindSpawnAreaColliders();
        
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
    
    private void FindSpawnAreaColliders()
    {
        // Try to get colliders from the spawner first (cleanest approach)
        if (enemySpawner != null)
        {
            spawnAreaBig = enemySpawner.SpawnAreaBig;
            spawnAreaBlocker = enemySpawner.SpawnAreaBlocker;
        }
        
        // Fallback: find by tags
        if (spawnAreaBig == null && !string.IsNullOrEmpty(spawnAreaBigTag))
        {
            GameObject bigObj = GameObject.FindGameObjectWithTag(spawnAreaBigTag);
            if (bigObj != null) spawnAreaBig = bigObj.GetComponent<BoxCollider>();
        }
        
        if (spawnAreaBlocker == null && !string.IsNullOrEmpty(spawnAreaBlockerTag))
        {
            GameObject blockerObj = GameObject.FindGameObjectWithTag(spawnAreaBlockerTag);
            if (blockerObj != null) spawnAreaBlocker = blockerObj.GetComponent<BoxCollider>();
        }
        
        // Final fallback: search in spawner's children
        if ((spawnAreaBig == null || spawnAreaBlocker == null) && enemySpawner != null)
        {
            BoxCollider[] colliders = enemySpawner.GetComponentsInChildren<BoxCollider>();
            if (colliders.Length >= 2)
            {
                // Find the largest as big area
                BoxCollider largest = colliders[0];
                foreach (var collider in colliders)
                {
                    if (collider.size.magnitude > largest.size.magnitude)
                        largest = collider;
                }
                spawnAreaBig = largest;
                
                // Find the other one as blocker
                foreach (var collider in colliders)
                {
                    if (collider != spawnAreaBig)
                    {
                        spawnAreaBlocker = collider;
                        break;
                    }
                }
            }
        }
        
        if (spawnAreaBig == null)
        {
            Debug.LogWarning($"{gameObject.name}: Could not find spawn area colliders! Make sure to tag them or place them in EnemySpawner.");
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
    
    private void OnStateChanged(ChickenState oldState, ChickenState newState)
    {
        switch (newState)
        {
            case ChickenState.MovingToSpawnPoint:
                StartMovingToWaitingArea();
                break;
                
            case ChickenState.AtSpawnPoint:
            case ChickenState.Idle:
                StartIdleBehavior();
                break;
        }
    }

    public void SetSpawnPoint(Transform spawnPoint)
    {
        if (!spawnPoint) return;
        
        this.spawnPoint = spawnPoint;
    }
    
    // Called by FormationBehavior when no slots are available
    public void MoveToSpawnPoint()
    {
        chickenController.SetState(ChickenState.MovingToSpawnPoint);
    }
    
    private void StartMovingToWaitingArea()
    {
        initialPosition = transform.position;
        moveTimer = 0f;
        
        // Choose between outer area or traditional spawn point
        if (useOuterWaitingArea && spawnAreaBig != null)
        {
            targetPosition = GetOuterWaitingPosition();
            usingOuterArea = true;
        }
        else if (spawnPoint != null)
        {
            // Traditional spawn point behavior
            if (randomizeSpawnOffset)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnPointRadius;
                targetPosition = spawnPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            }
            else
            {
                targetPosition = spawnPoint.position;
            }
            usingOuterArea = false;
        }
        else
        {
            // No spawn point - just go idle where they are
            chickenController.SetState(ChickenState.Idle);
            return;
        }
        
        currentTargetPosition = targetPosition;
    }
    
    // Get a position in the outer waiting area (outside the spawn area colliders)
    private Vector3 GetOuterWaitingPosition()
    {
        if (spawnAreaBig == null)
        {
            // Fallback to spawn point if no colliders found
            return spawnPoint != null ? spawnPoint.position : transform.position;
        }
        
        Bounds bigBounds = spawnAreaBig.bounds;
        
        // Try to find a good position outside the big spawn area
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 candidatePosition = GetRandomOuterPosition(bigBounds);
            
            // Check if position is valid (outside spawn areas, no obstacles)
            if (IsOuterPositionValid(candidatePosition))
            {
                return candidatePosition;
            }
        }
        
        // Fallback: position at edge of big area
        return GetFallbackOuterPosition(bigBounds);
    }
    
    // Generate random position outside the big spawn area bounds
    private Vector3 GetRandomOuterPosition(Bounds bigBounds)
    {
        // Expand the bounds by the margin to create outer area
        Bounds outerBounds = bigBounds;
        outerBounds.Expand(outerAreaMargin * 2f);
        
        // Generate random position in the outer area
        Vector3 randomPosition = new Vector3(
            Random.Range(outerBounds.min.x, outerBounds.max.x),
            Random.Range(outerBounds.min.y, outerBounds.max.y),
            Random.Range(outerBounds.min.z, outerBounds.max.z)
        );
        
        // If the position is inside the big bounds, push it outside
        if (bigBounds.Contains(randomPosition))
        {
            Vector3 center = bigBounds.center;
            Vector3 direction = (randomPosition - center).normalized;
            
            // Find the closest point on the big bounds surface
            Vector3 surfacePoint = bigBounds.ClosestPoint(randomPosition);
            
            // Push it outside by the margin
            randomPosition = surfacePoint + direction * outerAreaMargin;
        }
        
        return randomPosition;
    }
    
    // Check if outer position is valid (outside spawn areas, no obstacles)
    private bool IsOuterPositionValid(Vector3 position)
    {
        if (spawnAreaBig == null) return true;
        
        // Must be OUTSIDE the big spawn area (opposite of spawn logic)
        if (spawnAreaBig.bounds.Contains(position))
            return false;
        
        // Should not be inside the blocker area either (though this is less critical for outer positions)
        if (spawnAreaBlocker != null && spawnAreaBlocker.bounds.Contains(position))
            return false;
        
        // Check for obstacles if layer mask is set
        if (obstacleLayerMask != -1)
        {
            Collider[] obstacles = Physics.OverlapSphere(position, 1f, obstacleLayerMask);
            if (obstacles.Length > 0)
                return false;
        }
        
        return true;
    }
    
    // Get fallback outer position when no valid position found
    private Vector3 GetFallbackOuterPosition(Bounds bigBounds)
    {
        // Position at the edge + margin in a random direction
        Vector3 center = bigBounds.center;
        Vector3 randomDirection = Random.onUnitSphere;
        randomDirection.y = Mathf.Abs(randomDirection.y) * 0.3f; // Prefer horizontal positioning
        
        Vector3 surfacePoint = bigBounds.ClosestPoint(center + randomDirection * 100f);
        return surfacePoint + randomDirection.normalized * outerAreaMargin;
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
            case ChickenState.MovingToSpawnPoint:
                MoveTowardsPosition(targetPosition, spawnPointSpeed, OnArrivedAtWaitingArea);
                break;
                
            case ChickenState.AtSpawnPoint:
            case ChickenState.Idle:
                HandleIdleMovement();
                break;
        }
    }
    
    private void MoveTowardsPosition(Vector3 targetPos, float speed, System.Action onArrival)
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
        Vector3 desiredPosition = Vector3.Lerp(initialPosition, targetPos, easedT);
        Vector3 velocity = (desiredPosition - transform.position) / Time.fixedDeltaTime;
        rb.linearVelocity = velocity;
        
        // Check early arrival
        if (Vector3.Distance(transform.position, targetPos) < arrivalThreshold && t < 1f)
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
    
    private void OnArrivedAtWaitingArea()
    {
        chickenController.SetState(ChickenState.AtSpawnPoint);
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
    
    // Public method to get outer waiting positions (for external systems)
    public Vector3 GetRandomOuterPosition()
    {
        return GetOuterWaitingPosition();
    }
    
    private void OnDrawGizmos()
    {
        // Draw spawn point
        if (spawnPoint != null && !useOuterWaitingArea)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan
            Gizmos.DrawWireSphere(spawnPoint.position, spawnPointRadius);
            
            if (chickenController != null && 
                (chickenController.CurrentState == ChickenState.MovingToSpawnPoint || 
                 chickenController.CurrentState == ChickenState.AtSpawnPoint))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, currentTargetPosition);
                Gizmos.DrawSphere(currentTargetPosition, 0.3f);
            }
        }
        // Show idle state
        if (chickenController != null && (chickenController.IsIdle || chickenController.IsAtSpawnPoint))
        {
            Color stateColor = usingOuterArea ? Color.yellow : new Color(1f, 0.5f, 0f); // Orange
            Gizmos.color = stateColor;
            Gizmos.DrawWireSphere(transform.position, 0.8f);
            
            #if UNITY_EDITOR
            Vector3 labelPos = transform.position + Vector3.up * 1.5f;
            string stateText = chickenController.IsAtSpawnPoint ? "WAITING" : "IDLE";
            string areaText = usingOuterArea ? " (Outer)" : " (Spawn)";
            string colliderStatus = spawnAreaBig != null ? "" : " [NO COLLIDERS]";
            UnityEditor.Handles.Label(labelPos, $"{stateText}{areaText}{colliderStatus}\nNext check: {(nextSlotCheckTime - Time.time):F1}s");
            #endif
        }
    }
}