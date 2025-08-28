using UnityEngine;

public class ChickenEggV2 : MonoBehaviour
{
    [Header("Egg Settings")]
    public float lifetime = 5f; // How long the egg exists before destroying itself
    public bool useGravity = false; // Whether egg should be affected by gravity

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Vector3 velocity;
    private float spawnTime;
    private bool isInitialized = false;

    void Start()
    {
        spawnTime = Time.time;

        // Set up rigidbody if present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = useGravity;
        }
    }

    void Update()
    {
        // Move the egg if initialized
        if (isInitialized)
        {
            transform.position += velocity * Time.deltaTime;
        }

        // Destroy after lifetime
        if (Time.time - spawnTime >= lifetime)
        {
            if (showDebugLogs)
                Debug.Log($"Egg {gameObject.name}: Destroyed after {lifetime} seconds");

            Destroy(gameObject);
        }
    }

    public void Initialize(Vector3 direction, float speed)
    {
        velocity = direction.normalized * speed;
        isInitialized = true;

        if (showDebugLogs)
            Debug.Log($"Egg {gameObject.name}: Initialized with velocity {velocity}");
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if we hit the player
        if (other.CompareTag("Player"))
        {
            Debug.LogError("Egg Hit Player");

            if (showDebugLogs)
                Debug.Log($"Egg {gameObject.name}: Hit player {other.gameObject.name}");

            // Destroy the egg
            Destroy(gameObject);
            return;
        }
    }

    void OnDrawGizmos()
    {
        if (isInitialized)
        {
            // Draw velocity direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + velocity.normalized * 2f);
        }
    }
}