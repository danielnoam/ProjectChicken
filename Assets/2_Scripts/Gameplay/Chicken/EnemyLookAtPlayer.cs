using UnityEngine;

public class EnemyLookAtPlayer : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player; // Drag the player here, or leave null to find by tag
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 2f; // How fast the enemy turns
    public bool useSmootRotation = true; // Toggle between instant and smooth rotation
    public bool lockYAxis = true; // Keep enemy upright (only rotate on Y axis)
    
    void Start()
    {
        // If no player assigned, try to find one by tag
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("No player found! Make sure player has 'Player' tag or assign manually.");
            }
        }
    }
    
    void Update()
    {
        // Make sure we have a player to look at
        if (player == null) return;
        
        // Calculate direction to player
        Vector3 directionToPlayer = player.position - transform.position;
        
        // If we want to lock Y axis (keep enemy upright)
        if (lockYAxis)
        {
            directionToPlayer.y = 0;
        }
        
        // Skip if player is too close (prevents jittering)
        if (directionToPlayer.magnitude < 0.1f) return;
        
        // Calculate the rotation needed to look at player
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        
        // Apply rotation (smooth or instant)
        if (useSmootRotation)
        {
            // Smooth rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Instant rotation
            transform.rotation = targetRotation;
        }
    }
}