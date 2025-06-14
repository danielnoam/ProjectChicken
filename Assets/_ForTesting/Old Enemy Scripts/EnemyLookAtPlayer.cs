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
        if (player == null)
        {
            Debug.LogWarning("Player not found!");
            return;
        }

        Debug.DrawLine(transform.position, player.position, Color.red); // Visual aid

        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0; // Ignore vertical difference
        directionToPlayer.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}