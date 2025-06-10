using UnityEngine;
using VInspector;

// Handles looking at the player
[RequireComponent(typeof(ChickenController))]
public class ChickenLookAtBehavior : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private bool lookAtPlayer = true; // Enable looking at player
    [SerializeField] private float rotationSpeed = 5f; // Rotation speed
    [SerializeField] private bool instantFirstRotation = true; // Instant face on spawn
    [SerializeField] private bool lockYRotationOnly = true; // Only Y axis rotation
    
    [Header("Player Reference")]
    [SerializeField] private string playerTag = "Player"; // Tag to find player
    [SerializeField] private Transform playerOverride = null; // Manual player assignment
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool playerFound = false;
    [SerializeField, ReadOnly] private string currentPlayerName = "None";
    
    // References
    private ChickenController chickenController;
    private Transform playerTransform;
    private bool hasPerformedFirstRotation = false;
    
    // Properties
    public Transform CurrentPlayerTarget => playerTransform;
    public bool IsLookingAtPlayer => lookAtPlayer && playerTransform != null;
    
    private void Awake()
    {
        chickenController = GetComponent<ChickenController>();
        
        // Ensure rigidbody rotation is frozen
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }
    
    private void Start()
    {
        FindPlayer();
        ValidateAnimatorSettings();
    }
    
    private void FindPlayer()
    {
        // Check override first
        if (playerOverride != null)
        {
            SetPlayerTransform(playerOverride);
            return;
        }
        
        // Try to find by tag
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            SetPlayerTransform(playerObject.transform);
        }
        else
        {
            // Try alternative tags
            string[] alternativeTags = { "Player", "player", "MainPlayer", "LocalPlayer" };
            foreach (string tag in alternativeTags)
            {
                if (tag != playerTag)
                {
                    try
                    {
                        playerObject = GameObject.FindGameObjectWithTag(tag);
                        if (playerObject != null)
                        {
                            SetPlayerTransform(playerObject.transform);
                            Debug.LogWarning($"{gameObject.name}: Found player with tag '{tag}' instead of '{playerTag}'");
                            return;
                        }
                    }
                    catch (UnityException) { }
                }
            }
            
            playerFound = false;
            currentPlayerName = "None";
            
            if (lookAtPlayer)
            {
                Debug.LogError($"{gameObject.name}: Player not found! Check tag or assign playerOverride.");
            }
        }
    }
    
    private void ValidateAnimatorSettings()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null && animator.applyRootMotion)
        {
            Debug.LogWarning($"{gameObject.name}: Animator 'Apply Root Motion' may interfere with rotation!");
        }
    }
    
    public void SetPlayerTransform(Transform newPlayer)
    {
        playerTransform = newPlayer;
        if (newPlayer == null)
        {
            playerFound = false;
            currentPlayerName = "None";
        }
        else
        {
            playerFound = true;
            currentPlayerName = newPlayer.name;
        }
    }
    
    private void Update()
    {
        // Don't rotate if concussed or if looking is disabled
        if (!lookAtPlayer || chickenController.IsConcussed || playerTransform == null)
        {
            // Try to find player periodically if missing
            if (lookAtPlayer && playerTransform == null && Time.frameCount % 300 == 0)
            {
                FindPlayer();
            }
            return;
        }
        
        HandleLookAtPlayer();
    }
    
    private void LateUpdate()
    {
        // Double-check rotation hasn't been overridden
        if (lookAtPlayer && !chickenController.IsConcussed && playerTransform != null && hasPerformedFirstRotation)
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            if (lockYRotationOnly)
            {
                directionToPlayer.y = 0;
            }
            
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion expectedRotation = Quaternion.LookRotation(directionToPlayer);
                float angleDifference = Quaternion.Angle(transform.rotation, expectedRotation);
                
                // Re-apply if significantly different
                if (angleDifference > 45f)
                {
                    Debug.LogWarning($"{gameObject.name}: Rotation overridden by another script!");
                    HandleLookAtPlayer();
                }
            }
        }
    }
    
    private void HandleLookAtPlayer()
    {
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        
        if (lockYRotationOnly)
        {
            directionToPlayer.y = 0;
        }
        
        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            
            if (!hasPerformedFirstRotation && instantFirstRotation)
            {
                transform.rotation = targetRotation;
                hasPerformedFirstRotation = true;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    // Debug methods
    [Button]
    private void DebugRotationStatus()
    {
        Debug.Log($"=== {gameObject.name} Rotation Debug ===");
        Debug.Log($"Look At Player: {lookAtPlayer}");
        Debug.Log($"Player Found: {playerFound}");
        Debug.Log($"Player Name: {currentPlayerName}");
        Debug.Log($"Current Rotation: {transform.rotation.eulerAngles}");
        Debug.Log($"Is Concussed: {chickenController.IsConcussed}");
        
        if (playerTransform != null)
        {
            Vector3 direction = playerTransform.position - transform.position;
            Debug.Log($"Direction to Player: {direction.normalized}");
        }
    }
    
    [Button]
    private void ForceImmediateLookAtPlayer()
    {
        if (playerTransform == null)
        {
            Debug.LogError("No player transform!");
            return;
        }
        
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        if (lockYRotationOnly)
        {
            directionToPlayer.y = 0;
        }
        
        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            hasPerformedFirstRotation = true;
            Debug.Log("Forced rotation to player");
        }
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying && playerOverride != null)
        {
            SetPlayerTransform(playerOverride);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (lookAtPlayer && playerTransform != null && !chickenController.IsConcussed)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Magenta
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}