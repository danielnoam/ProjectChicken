using UnityEngine;

public class TestMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool usePhysics = false; // Toggle between Transform and Rigidbody movement
    
    private Rigidbody rb;
    
    private void Awake()
    {
        // Get Rigidbody if using physics
        if (usePhysics)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("No Rigidbody found! Add one or disable 'Use Physics'");
            }
        }
    }
    
    private void Update()
    {
        // Get input using old Input System
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        float verticalInput = Input.GetAxis("Vertical");     // W/S or Up/Down arrows
        
        // Apply movement based on method
        if (usePhysics && rb != null)
        {
            MoveWithPhysics(horizontalInput, verticalInput);
        }
        else
        {
            MoveWithTransform(horizontalInput, verticalInput);
        }
    }
    
    private void MoveWithTransform(float horizontal, float vertical)
    {
        // Map input directly to X and Y axes (canvas-style)
        // Horizontal = X-axis (left/right)
        // Vertical = Y-axis (up/down)
        Vector3 movement = new Vector3(horizontal, vertical, 0f);
        
        // Apply movement to transform
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
    }
    
    private void MoveWithPhysics(float horizontal, float vertical)
    {
        // Map input directly to X and Y axes (canvas-style)
        Vector3 movement = new Vector3(horizontal, vertical, 0f);
        
        // Apply movement using Rigidbody
        Vector3 newPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }
}