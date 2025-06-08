using System.Collections;
using System.Linq;
using UnityEngine;
using VInspector;

// ChickenFollowFormation: Makes chickens move to formation slots while looking at the player
// Now includes support for spawn point and automatic reassignment on formation changes

[RequireComponent(typeof(Rigidbody))]
public class ChickenFollowFormation : MonoBehaviour
{
    // Movement Settings
    [Header("Movement Settings")]
    [SerializeField] private float initialSpeed = 2f; // Base time in seconds to reach slot
    [SerializeField] private float arrivalTimeVariance = 0.5f; // Random time added (0 to this value)
    [SerializeField] private float concussTime = 1.5f; // Recovery time after being pushed
    [SerializeField] private AnimationCurve movementCurve = null; // Optional: custom movement curve
    [SerializeField] private float spawnPointSpeed = 3f; // Time to reach spawn point
    
    [Header("Formation Settings")]
    [SerializeField] private float arrivalThreshold = 0.5f; // Distance to consider "arrived" at slot
    [SerializeField] private float followStrength = 10f; // How strongly to follow slot when in formation
    [SerializeField] private float damping = 5f; // Damping for smooth movement
    [SerializeField] private float maxWaitTime = 5f; // Max time to wait for formation
    [SerializeField] private float slotCheckInterval = 2f; // Check for open slots every X seconds when idle
    [SerializeField] private float followLerpSpeed = 2f; // Lerp speed when following formation (lower = smoother)
    [SerializeField] private float positionDeadZone = 0.1f; // Stop micro-adjusting when this close to slot
    
    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint; // Reference to spawn point GameObject
    [SerializeField] private float spawnPointRadius = 3f; // Radius around spawn point for idle chickens
    [SerializeField] private bool randomizeSpawnOffset = true; // Add random offset at spawn point
    
    [Header("Rotation Settings")]
    [SerializeField] private bool lookAtPlayer = true; // Enable looking at player
    [SerializeField] private float rotationSpeed = 5f; // Speed of rotation towards player
    [SerializeField] private bool instantFirstRotation = true; // Instantly face player when first spawning
    [SerializeField] private bool lockYRotationOnly = true; // Only rotate on Y axis (typical for top-down games)
    [SerializeField] private string playerTag = "Player"; // Tag to find player
    [SerializeField] private Transform playerOverride = null; // Optional: manually assign player transform
    
    [Header("Rotation Debug")]
    [SerializeField, ReadOnly] private bool playerFound = false;
    [SerializeField, ReadOnly] private string currentPlayerName = "None";
    
    [Header("Idle Behavior")]
    [SerializeField] private bool enableIdleMovement = true; // Enable subtle movement while idle
    [SerializeField] private float idleWobbleSpeed = 1f; // Speed of idle wobble
    [SerializeField] private float idleWobbleAmount = 0.5f; // Amount of wobble movement
    
    // Internal References
    private FormationManager formationManager;
    private FormationManager.FormationSlot assignedSlot;
    private Rigidbody rb;
    private Transform playerTransform;
    
    // State Management
    private bool isMovingToSlot = false;
    private bool isMovingToSpawnPoint = false;
    private bool isAtSpawnPoint = false;
    private bool isConcussed = false;
    private bool hasArrivedAtSlot = false;
    private bool isWaitingForFormation = true;
    private bool isIdling = false;
    private float concussTimer = 0f;
    private float waitTimer = 0f;
    private float nextSlotCheckTime = 0f;
    
    // Movement Variables
    private Vector3 initialPosition;
    private Vector3 spawnPointTargetPosition;
    private float moveTimer = 0f;
    private float actualArrivalTime = 0f; // Actual time to arrive (with variance)
    private Vector3 idleStartPosition;
    private float idleTime = 0f;
    private bool hasPerformedFirstRotation = false;
    
    private void OnEnable()
    {
        // Subscribe to formation change event
        FormationManager.OnFormationChanged += HandleFormationChanged;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from formation change event
        FormationManager.OnFormationChanged -= HandleFormationChanged;
    }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configure rigidbody for space movement
        rb.useGravity = false;
        rb.linearDamping = 1f;
        rb.angularDamping = 2f;
        
        // IMPORTANT: Freeze rotation on rigidbody to prevent physics from interfering
        rb.freezeRotation = true;
        
        // Find player transform
        FindPlayer();
        
        // Find spawn point if not assigned
        if (spawnPoint == null)
        {
            GameObject spawnPointObject = GameObject.Find("Spawn Point");
            if (spawnPointObject != null)
            {
                spawnPoint = spawnPointObject.transform;
                Debug.Log($"{gameObject.name}: Found spawn point - {spawnPointObject.name}");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: No spawn point assigned and couldn't find 'Spawn Point' GameObject!");
            }
        }
    }
    
    private void FindPlayer()
    {
        // First check if player override is set
        if (playerOverride != null)
        {
            playerTransform = playerOverride;
            playerFound = true;
            currentPlayerName = playerOverride.name;
            Debug.Log($"{gameObject.name}: Using player override - {playerOverride.name}");
            return;
        }
        
        // Try to find player by tag
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerFound = true;
            currentPlayerName = playerObject.name;
            Debug.Log($"{gameObject.name}: Found player by tag '{playerTag}' - {playerObject.name}");
        }
        else
        {
            // Try alternative common player tags
            string[] alternativeTags = { "Player", "player", "MainPlayer", "LocalPlayer" };
            foreach (string tag in alternativeTags)
            {
                if (tag != playerTag) // Don't check the same tag twice
                {
                    try
                    {
                        playerObject = GameObject.FindGameObjectWithTag(tag);
                        if (playerObject != null)
                        {
                            playerTransform = playerObject.transform;
                            playerFound = true;
                            currentPlayerName = playerObject.name;
                            Debug.LogWarning($"{gameObject.name}: Player not found with tag '{playerTag}', but found with tag '{tag}'. Consider updating playerTag setting.");
                            return;
                        }
                    }
                    catch (UnityException) 
                    {
                        // Tag doesn't exist, continue
                    }
                }
            }
            
            playerFound = false;
            currentPlayerName = "None";
            Debug.LogError($"{gameObject.name}: Player with tag '{playerTag}' not found! Chicken won't be able to look at player. Make sure player GameObject has the correct tag or assign playerOverride manually.");
        }
    }
    
    private void Start()
    {
        // Validate player setup
        if (lookAtPlayer && playerTransform == null)
        {
            Debug.LogError($"{gameObject.name}: lookAtPlayer is enabled but no player found! Check player tag or assign playerOverride.");
        }
        
        // Find FormationManager
        formationManager = FindObjectOfType<FormationManager>();
        
        if (formationManager == null)
        {
            Debug.LogError("FormationManager not found! Chicken will not be able to join formation.");
            // Move to spawn point if available
            if (spawnPoint != null)
            {
                MoveToSpawnPoint();
            }
            else
            {
                enabled = false;
            }
            return;
        }
        
        // Double-check rigidbody rotation is frozen
        if (rb != null && !rb.freezeRotation)
        {
            Debug.LogWarning($"{gameObject.name}: Rigidbody rotation was not frozen. Freezing it now to prevent physics interference.");
            rb.freezeRotation = true;
        }
        
        // Check for Animator that might override rotation
        Animator animator = GetComponent<Animator>();
        if (animator != null && animator.applyRootMotion)
        {
            Debug.LogWarning($"{gameObject.name}: Animator has 'Apply Root Motion' enabled which may interfere with rotation! Consider disabling it.");
        }
        
        // Start coroutine to wait for formation generation
        StartCoroutine(WaitForFormationAndAssign());
    }
    
    private IEnumerator WaitForFormationAndAssign()
    {
        // Wait for formation to be generated
        while (isWaitingForFormation && waitTimer < maxWaitTime)
        {
            // Check if formations are available
            if (formationManager.FormationSlots != null && formationManager.FormationSlots.Count > 0)
            {
                Debug.Log($"Formations ready! Total slots: {formationManager.FormationSlots.Count}");
                
                // Try to assign to slot
                if (TryAssignToSlot())
                {
                    isWaitingForFormation = false;
                    isIdling = false;
                    isMovingToSlot = true;
                    isMovingToSpawnPoint = false;
                    isAtSpawnPoint = false;
                    initialPosition = transform.position;
                    moveTimer = 0f; // Reset timer
                    
                    // Calculate actual arrival time with random variance
                    actualArrivalTime = initialSpeed + Random.Range(0f, arrivalTimeVariance);
                    
                    Debug.Log($"{gameObject.name} successfully assigned to formation! Arrival time: {actualArrivalTime:F2}s");
                    yield break;
                }
                else
                {
                    Debug.Log($"All {formationManager.FormationSlots.Count} slots are occupied. {gameObject.name} will move to spawn point.");
                    isWaitingForFormation = false;
                    MoveToSpawnPoint();
                    yield break;
                }
            }
            
            waitTimer += Time.deltaTime;
            yield return null;
        }
        
        // If we're here and still waiting, formation generation timed out
        if (isWaitingForFormation)
        {
            Debug.LogWarning($"{gameObject.name} timed out waiting for formation generation! Moving to spawn point.");
            isWaitingForFormation = false;
            MoveToSpawnPoint();
        }
    }
    
    // Handle formation change notification
    private void HandleFormationChanged()
    {
        Debug.Log($"{gameObject.name} received formation change notification!");
        OnFormationChangedNotification();
    }
    
    // Called when formation changes
    public void OnFormationChangedNotification()
    {
        // Release current slot if any
        ReleaseSlot();
        
        // Reset all states
        isMovingToSlot = false;
        isMovingToSpawnPoint = false;
        isAtSpawnPoint = false;
        hasArrivedAtSlot = false;
        isIdling = false;
        isWaitingForFormation = true;
        moveTimer = 0f;
        actualArrivalTime = 0f;
        waitTimer = 0f;
        
        // Stop all coroutines
        StopAllCoroutines();
        
        // Try to get a new slot
        StartCoroutine(WaitForFormationAndAssign());
    }
    
    private void MoveToSpawnPoint()
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning($"{gameObject.name} can't move to spawn point - no spawn point assigned!");
            // Just idle in place
            isIdling = true;
            idleStartPosition = transform.position;
            idleTime = 0f;
            nextSlotCheckTime = Time.time + slotCheckInterval;
            StartCoroutine(IdleAndCheckForSlots());
            return;
        }
        
        isMovingToSpawnPoint = true;
        isMovingToSlot = false;
        isIdling = false;
        isAtSpawnPoint = false;
        initialPosition = transform.position;
        moveTimer = 0f;
        
        // Calculate target position at spawn point
        if (randomizeSpawnOffset)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnPointRadius;
            spawnPointTargetPosition = spawnPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        else
        {
            spawnPointTargetPosition = spawnPoint.position;
        }
        
        Debug.Log($"{gameObject.name} moving to spawn point at {spawnPointTargetPosition}");
    }
    
    private IEnumerator IdleAndCheckForSlots()
    {
        Debug.Log($"{gameObject.name} entering idle state, will check for slots every {slotCheckInterval} seconds.");
        
        while (isIdling || isAtSpawnPoint)
        {
            // Wait until next check time
            yield return new WaitForSeconds(slotCheckInterval);
            
            // Check if formations exist and have available slots
            if (formationManager != null && formationManager.FormationSlots != null && formationManager.FormationSlots.Count > 0)
            {
                var availableSlots = formationManager.GetAvailableSlots();
                Debug.Log($"{gameObject.name} checking for slots... Available: {availableSlots.Count}");
                
                if (availableSlots.Count > 0 && TryAssignToSlot())
                {
                    isIdling = false;
                    isAtSpawnPoint = false;
                    isMovingToSpawnPoint = false;
                    isMovingToSlot = true;
                    initialPosition = transform.position;
                    moveTimer = 0f; // Reset timer
                    
                    // Calculate actual arrival time with random variance
                    actualArrivalTime = initialSpeed + Random.Range(0f, arrivalTimeVariance);
                    
                    Debug.Log($"{gameObject.name} found an open slot! Moving to formation in {actualArrivalTime:F2}s.");
                    yield break;
                }
            }
        }
    }
    
    private bool TryAssignToSlot()
    {
        // First, log current formation state
        var availableSlots = formationManager.GetAvailableSlots();
        Debug.Log($"Attempting to assign {gameObject.name}. Available slots: {availableSlots.Count} / {formationManager.FormationSlots.Count}");
        
        // Method 1: Try to get any available slot
        assignedSlot = formationManager.TryOccupySlot(gameObject);
        
        if (assignedSlot != null)
        {
            Debug.Log($"{gameObject.name} assigned to slot at position {assignedSlot.localPosition} (index: {assignedSlot.formationIndex})");
            return true;
        }
        
        // Method 2: Find nearest available slot and occupy it
        var nearestSlot = formationManager.GetNearestAvailableSlot(transform.position);
        if (nearestSlot != null && formationManager.OccupySpecificSlot(nearestSlot, gameObject))
        {
            assignedSlot = nearestSlot;
            Debug.Log($"{gameObject.name} assigned to nearest slot at position {nearestSlot.localPosition} (index: {nearestSlot.formationIndex})");
            return true;
        }
        
        return false;
    }
    
    private void Update()
    {
        // Handle rotation to face player (separate from physics movement)
        if (lookAtPlayer && playerTransform != null && !isConcussed)
        {
            HandleLookAtPlayer();
        }
        else if (lookAtPlayer && playerTransform == null)
        {
            // Try to find player again if it's missing
            FindPlayer();
            if (playerTransform == null && Time.frameCount % 300 == 0) // Log every 5 seconds
            {
                Debug.LogWarning($"{gameObject.name}: lookAtPlayer is enabled but no player found!");
            }
        }
    }
    
    private void LateUpdate()
    {
        // Double-check rotation in LateUpdate to override any other scripts that might be changing rotation
        if (lookAtPlayer && playerTransform != null && !isConcussed)
        {
            // Only re-apply if rotation has changed significantly (indicating another script modified it)
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            if (lockYRotationOnly)
            {
                directionToPlayer.y = 0;
            }
            
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion expectedRotation = Quaternion.LookRotation(directionToPlayer);
                float angleDifference = Quaternion.Angle(transform.rotation, expectedRotation);
                
                // If rotation is way off what we expect, something else changed it
                if (angleDifference > 45f && hasPerformedFirstRotation)
                {
                    Debug.LogWarning($"{gameObject.name}: Rotation was changed by another script! Expected angle difference: {angleDifference:F1}°. Re-applying player look rotation.");
                    HandleLookAtPlayer();
                }
            }
        }
    }
    
    private void HandleLookAtPlayer()
    {
        // Calculate world space direction to player (ignoring any parent transforms)
        Vector3 worldPlayerPos = playerTransform.position;
        Vector3 worldChickenPos = transform.position;
        Vector3 directionToPlayer = worldPlayerPos - worldChickenPos;
        
        if (lockYRotationOnly)
        {
            // Only rotate on Y axis (typical for top-down or side-view games)
            directionToPlayer.y = 0;
        }
        
        if (directionToPlayer.sqrMagnitude > 0.001f) // Avoid zero vector
        {
            // Calculate rotation in world space
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            
            // Check if this is the first rotation and instant rotation is enabled
            if (!hasPerformedFirstRotation && instantFirstRotation)
            {
                transform.rotation = targetRotation;
                hasPerformedFirstRotation = true;
                Debug.Log($"{gameObject.name} - Instant first rotation to face player at {worldPlayerPos}");
            }
            else
            {
                // Smooth rotation in world space
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // Debug logging
            if (Time.frameCount % 60 == 0) // Log every second
            {
                float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
                Debug.Log($"{gameObject.name} - Player at: {worldPlayerPos}, Direction: {directionToPlayer.normalized}, Current Rotation: {transform.rotation.eulerAngles}, Angle to player: {angleToPlayer:F1}°");
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} - Direction to player is too small! Player might be at same position as chicken.");
        }
    }
    
    private void FixedUpdate()
    {
        // Handle concussion recovery
        if (isConcussed)
        {
            concussTimer -= Time.fixedDeltaTime;
            if (concussTimer <= 0f)
            {
                isConcussed = false;
                Debug.Log($"{gameObject.name} recovered from concussion");
            }
            return; // Don't control movement while concussed
        }
        
        // Handle movement to spawn point
        if (isMovingToSpawnPoint)
        {
            MoveTowardsPosition(spawnPointTargetPosition, spawnPointSpeed, ref isMovingToSpawnPoint, () => {
                isAtSpawnPoint = true;
                isIdling = true;
                idleStartPosition = transform.position;
                idleTime = 0f;
                nextSlotCheckTime = Time.time + slotCheckInterval;
                Debug.Log($"{gameObject.name} arrived at spawn point");
                StartCoroutine(IdleAndCheckForSlots());
            });
            return;
        }
        
        // Handle idle state
        if ((isIdling || isAtSpawnPoint) && !isMovingToSlot)
        {
            // Simple idle behavior with optional wobble
            if (enableIdleMovement)
            {
                idleTime += Time.fixedDeltaTime;
                
                // Create a subtle wobble effect
                float wobbleX = Mathf.Sin(idleTime * idleWobbleSpeed) * idleWobbleAmount;
                float wobbleY = Mathf.Cos(idleTime * idleWobbleSpeed * 0.7f) * idleWobbleAmount * 0.5f;
                
                Vector3 wobbleOffset = new Vector3(wobbleX, wobbleY, 0);
                Vector3 targetIdlePosition = idleStartPosition + wobbleOffset;
                
                // Smoothly move to wobble position
                rb.linearVelocity = (targetIdlePosition - transform.position) * 2f;
            }
            else
            {
                // Just slow down if idle movement is disabled
                rb.linearVelocity *= 0.95f;
            }
            return;
        }
        
        // Don't do anything while waiting for formation or if no slot assigned
        if (isWaitingForFormation || assignedSlot == null) return;
        
        // Get target position from formation
        Vector3 targetPosition = formationManager.GetSlotWorldPosition(assignedSlot);
        
        if (isMovingToSlot && !hasArrivedAtSlot)
        {
            // Initial movement to slot
            MoveTowardsSlot(targetPosition);
        }
        else if (hasArrivedAtSlot)
        {
            // Follow slot position with smooth lerping
            FollowSlotPosition(targetPosition);
        }
    }
    
    private void MoveTowardsPosition(Vector3 targetPosition, float speed, ref bool isMoving, System.Action onArrival)
    {
        moveTimer += Time.fixedDeltaTime;
        float t = moveTimer / speed;
        
        if (t >= 1f)
        {
            t = 1f;
            isMoving = false;
            onArrival?.Invoke();
        }
        
        // Apply ease-out curve
        float easedT = movementCurve != null && movementCurve.length > 0 ? 
            movementCurve.Evaluate(t) : EaseOutCubic(t);
        
        // Smooth interpolation to target position
        Vector3 desiredPosition = Vector3.Lerp(initialPosition, targetPosition, easedT);
        Vector3 velocity = (desiredPosition - transform.position) / Time.fixedDeltaTime;
        
        // Apply velocity through rigidbody
        rb.linearVelocity = velocity;
        
        // Check if close enough to consider arrived
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < arrivalThreshold && isMoving)
        {
            isMoving = false;
            onArrival?.Invoke();
        }
    }
    
    private void MoveTowardsSlot(Vector3 targetPosition)
    {
        moveTimer += Time.fixedDeltaTime;
        float t = moveTimer / actualArrivalTime;
        
        if (t >= 1f)
        {
            // Arrived at slot
            t = 1f;
            hasArrivedAtSlot = true;
            isMovingToSlot = false;
            Debug.Log($"{gameObject.name} arrived at formation slot");
        }
        
        // Apply ease-out curve (starts fast, slows down at the end)
        float easedT;
        if (movementCurve != null && movementCurve.length > 0)
        {
            // Use custom animation curve if provided
            easedT = movementCurve.Evaluate(t);
        }
        else
        {
            // Use default ease-out cubic
            easedT = EaseOutCubic(t);
        }
        
        // Smooth interpolation to slot position with easing
        Vector3 desiredPosition = Vector3.Lerp(initialPosition, targetPosition, easedT);
        Vector3 velocity = (desiredPosition - transform.position) / Time.fixedDeltaTime;
        
        // Apply velocity through rigidbody
        rb.linearVelocity = velocity;
        
        // Check if close enough to consider arrived (even if timer hasn't finished)
        float distanceToSlot = Vector3.Distance(transform.position, targetPosition);
        if (distanceToSlot < arrivalThreshold && !hasArrivedAtSlot)
        {
            hasArrivedAtSlot = true;
            isMovingToSlot = false;
            Debug.Log($"{gameObject.name} arrived at formation slot (early arrival)");
        }
    }
    
    // Easing function: starts fast, ends slow
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    
    // Alternative easing functions you can try:
    private float EaseOutQuad(float t)
    {
        return t * (2f - t);
    }
    
    private float EaseOutExpo(float t)
    {
        return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
    }
    
    private void FollowSlotPosition(Vector3 targetPosition)
    {
        // Calculate distance to slot
        float distanceToSlot = Vector3.Distance(transform.position, targetPosition);
        
        // If we're within the dead zone, reduce or stop movement to prevent jittering
        if (distanceToSlot < positionDeadZone)
        {
            // Greatly reduce velocity when very close to prevent micro-adjustments
            rb.linearVelocity *= 0.5f;
            return;
        }
        
        // Smooth lerp to target position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, followLerpSpeed * Time.fixedDeltaTime);
        
        // Calculate velocity needed to reach the smoothed position
        Vector3 desiredVelocity = (smoothedPosition - transform.position) / Time.fixedDeltaTime;
        
        // Apply damping for extra smoothness
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, damping * Time.fixedDeltaTime);
    }
    
    // Call this when the chicken gets hit by a concussive weapon
    public void ApplyConcussion(Vector3 force)
    {
        isConcussed = true;
        concussTimer = concussTime;
        
        // Apply the force
        rb.AddForce(force, ForceMode.Impulse);
        
        Debug.Log($"{gameObject.name} was concussed! Recovering in {concussTime} seconds");
    }
    
    // Call this when chicken takes damage (for visual feedback or other systems)
    public void OnDamaged()
    {
        // You can add visual feedback here
        // For example: flash red, play hurt animation, etc.
    }
    
    // Call this when the chicken dies
    private void OnDestroy()
    {
        ReleaseSlot();
    }
    
    // Alternative death method if you're pooling objects
    public void OnDeath()
    {
        ReleaseSlot();
        // Add death effects, score, etc.
    }
    
    private void ReleaseSlot()
    {
        if (assignedSlot != null && formationManager != null)
        {
            formationManager.ReleaseSlot(assignedSlot);
            Debug.Log($"{gameObject.name} released its formation slot");
            assignedSlot = null;
        }
        
        // Stop any ongoing coroutines
        StopAllCoroutines();
    }
    
    // Force reassignment (useful if formation changes)
    public void ForceReassignSlot()
    {
        ReleaseSlot();
        isWaitingForFormation = true;
        isMovingToSlot = false;
        isMovingToSpawnPoint = false;
        isAtSpawnPoint = false;
        hasArrivedAtSlot = false;
        isIdling = false;
        moveTimer = 0f;
        actualArrivalTime = 0f;
        waitTimer = 0f;
        StartCoroutine(WaitForFormationAndAssign());
    }
    
    // Call this to notify when a slot becomes available (optional optimization)
    public void NotifySlotAvailable()
    {
        if (isIdling || isAtSpawnPoint)
        {
            Debug.Log($"{gameObject.name} notified of available slot, attempting assignment...");
            StopAllCoroutines();
            isIdling = false;
            isAtSpawnPoint = false;
            isWaitingForFormation = true;
            moveTimer = 0f;
            actualArrivalTime = 0f;
            StartCoroutine(WaitForFormationAndAssign());
        }
    }
    
    // Optional: Add this method to your FormationManager to notify idle chickens
    // when a slot becomes available (more efficient than constant checking)
    public static void NotifyAllIdleChickens()
    {
        var idleChickens = FindObjectsOfType<ChickenFollowFormation>();
        foreach (var chicken in idleChickens)
        {
            if (chicken.IsIdle || chicken.IsAtSpawnPoint)
            {
                chicken.NotifySlotAvailable();
            }
        }
    }
    
    // Public method to update player reference at runtime
    public void SetPlayerTransform(Transform newPlayer)
    {
        playerTransform = newPlayer;
        if (newPlayer == null)
        {
            playerFound = false;
            currentPlayerName = "None";
            Debug.LogWarning($"{gameObject.name}: Player transform set to null!");
        }
        else
        {
            playerFound = true;
            currentPlayerName = newPlayer.name;
            Debug.Log($"{gameObject.name}: Player transform updated to {newPlayer.name}");
        }
    }
    
    // Debug visualization
    private void OnValidate()
    {
        // Update player reference if override changed in inspector
        if (Application.isPlaying && playerOverride != null)
        {
            SetPlayerTransform(playerOverride);
        }
    }
    
    private void OnDrawGizmos()
    {
        // Draw spawn point reference
        if (spawnPoint != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan
            Gizmos.DrawWireSphere(spawnPoint.position, spawnPointRadius);
            
            if (isMovingToSpawnPoint || isAtSpawnPoint)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, spawnPointTargetPosition);
                Gizmos.DrawSphere(spawnPointTargetPosition, 0.3f);
            }
        }
        
        if (assignedSlot != null && formationManager != null)
        {
            Vector3 slotPosition = formationManager.GetSlotWorldPosition(assignedSlot);
            
            // Draw line to target slot
            Gizmos.color = hasArrivedAtSlot ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, slotPosition);
            
            // Draw target position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(slotPosition, 0.5f);
            
            // Draw dead zone
            if (hasArrivedAtSlot)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(slotPosition, positionDeadZone);
            }
            
            // Show movement progress
            if (isMovingToSlot && actualArrivalTime > 0)
            {
                #if UNITY_EDITOR
                float progress = moveTimer / actualArrivalTime;
                Vector3 labelPos = transform.position + Vector3.up * 2f;
                UnityEditor.Handles.Label(labelPos, $"Progress: {(progress * 100f):F0}%");
                #endif
            }
            
            // Show concussion state
            if (isConcussed)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }
        else if (isIdling || isAtSpawnPoint)
        {
            // Show idle state
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawWireSphere(transform.position, 0.8f);
            
            #if UNITY_EDITOR
            Vector3 labelPos = transform.position + Vector3.up * 1.5f;
            string stateText = isAtSpawnPoint ? "AT SPAWN POINT" : "IDLE";
            UnityEditor.Handles.Label(labelPos, $"{stateText} (next check: {(nextSlotCheckTime - Time.time):F1}s)");
            #endif
        }
        else if (isWaitingForFormation)
        {
            // Show waiting state
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.8f);
        }
        
        // Draw line to player if looking at player
        if (lookAtPlayer && playerTransform != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Magenta with transparency
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
    
    // Debug method to check rotation status
    [Button]
    private void DebugRotationStatus()
    {
        Debug.Log($"=== {gameObject.name} Rotation Debug ===");
        Debug.Log($"Look At Player Enabled: {lookAtPlayer}");
        Debug.Log($"Player Transform: {(playerTransform != null ? playerTransform.name : "NULL")}");
        if (playerTransform != null)
        {
            Debug.Log($"Player Position: {playerTransform.position}");
            Debug.Log($"Chicken Position: {transform.position}");
            Debug.Log($"Direction to Player: {(playerTransform.position - transform.position).normalized}");
        }
        Debug.Log($"Current Rotation: {transform.rotation.eulerAngles}");
        Debug.Log($"Rigidbody Freeze Rotation: {rb.freezeRotation}");
        Debug.Log($"Is Concussed: {isConcussed}");
        
        // Check for animator
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            Debug.Log($"Animator found - Apply Root Motion: {animator.applyRootMotion}");
        }
        
        // Check for other components that might affect rotation
        var otherRotators = GetComponents<MonoBehaviour>().Where(m => m != this && m.enabled).ToArray();
        if (otherRotators.Length > 0)
        {
            Debug.LogWarning($"Other active components on GameObject that might affect rotation: {string.Join(", ", otherRotators.Select(r => r.GetType().Name))}");
        }
    }
    
    // Force look at player (for testing)
    [Button]
    private void ForceImmediateLookAtPlayer()
    {
        if (playerTransform == null)
        {
            Debug.LogError("No player transform set!");
            return;
        }
        
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        if (lockYRotationOnly)
        {
            directionToPlayer.y = 0;
        }
        
        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion previousRotation = transform.rotation;
            transform.rotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            hasPerformedFirstRotation = true; // Mark as performed
            Debug.Log($"Forced rotation to face player. Previous: {previousRotation.eulerAngles}, New: {transform.rotation.eulerAngles}");
            
            // Also test if rotation "sticks"
            StartCoroutine(CheckRotationAfterDelay());
        }
    }
    
    private IEnumerator CheckRotationAfterDelay()
    {
        Quaternion expectedRotation = transform.rotation;
        yield return new WaitForSeconds(0.1f);
        
        float angleDiff = Quaternion.Angle(expectedRotation, transform.rotation);
        if (angleDiff > 1f)
        {
            Debug.LogError($"Rotation changed after 0.1 seconds! Something is overriding rotation. Angle difference: {angleDiff:F1}°");
        }
        else
        {
            Debug.Log($"Rotation remained stable after 0.1 seconds. Good!");
        }
    }
    
    [Button]
    private void DebugCurrentState()
    {
        Debug.Log($"=== {gameObject.name} State Debug ===");
        Debug.Log($"Is Waiting For Formation: {isWaitingForFormation}");
        Debug.Log($"Is Moving To Slot: {isMovingToSlot}");
        Debug.Log($"Is Moving To Spawn Point: {isMovingToSpawnPoint}");
        Debug.Log($"Is At Spawn Point: {isAtSpawnPoint}");
        Debug.Log($"Is Idling: {isIdling}");
        Debug.Log($"Has Arrived At Slot: {hasArrivedAtSlot}");
        Debug.Log($"Assigned Slot: {(assignedSlot != null ? "Yes" : "No")}");
        Debug.Log($"Spawn Point: {(spawnPoint != null ? spawnPoint.name : "NULL")}");
    }
    
    // Public properties
    public bool IsInFormation => hasArrivedAtSlot && !isConcussed;
    public bool HasAssignedSlot => assignedSlot != null;
    public bool IsWaitingForSlot => isWaitingForFormation;
    public bool IsIdle => isIdling;
    public bool IsAtSpawnPoint => isAtSpawnPoint;
    public Vector3 GetTargetSlotPosition => assignedSlot != null ? formationManager.GetSlotWorldPosition(assignedSlot) : transform.position;
    public Transform CurrentPlayerTarget => playerTransform;
}