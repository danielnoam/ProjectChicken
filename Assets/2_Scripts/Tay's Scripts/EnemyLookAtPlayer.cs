using UnityEngine;

public class EnemyLookAtPlayer : MonoBehaviour
{
    private Transform player;
    private float rotationSpeed = 5f;
    
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Calculate direction to player
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.Normalize();
        
        // Create rotation that looks at player
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        
        // Smoothly rotate towards player
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}