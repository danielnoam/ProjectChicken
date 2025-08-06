using UnityEngine;

public class SpaceItemBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    [HideInInspector] public float moveSpeed = 5f; // Set by spawner
    
    [Header("Rotation Settings")]
    public bool enableRotation = true; // Toggle rotation on/off
    public Vector3 rotationSpeed = new Vector3(43f, 67f, 91f); // Degrees per second for X, Y, Z axes (non-repeating pattern)
    
    [Header("Scaling Settings")]
    public float initialScale = 0.1f;
    public float minSize = 0.8f;
    public float maxSize = 1.5f;
    [HideInInspector] public float scaleDuration = 2f; // Set by spawner
    
    [Header("Lifecycle Settings")]
    public float fadeOutDuration = 1f;
    public float destroyDelay = 2f;
    
    private Vector3 initialPosition;
    private float currentScale;
    private float targetScale;
    private float scaleTimer = 0f;
    private bool isFadingOut = false;
    private float fadeTimer = 0f;
    private Renderer starRenderer;
    private Material starMaterial;
    private Color originalColor;
    private Vector3 movementDirection; // World space movement direction
    
    void Start()
    {
        // Store initial position and setup
        initialPosition = transform.position;
        currentScale = initialScale;
        
        // Set movement direction in world space (toward player/camera)
        movementDirection = Vector3.back; // Always move toward positive Z (toward camera)
        
        // Set random target scale within the specified range
        targetScale = Random.Range(minSize, maxSize);
        
        transform.localScale = Vector3.one * currentScale;
        
        // Get renderer and material for fading
        starRenderer = GetComponent<Renderer>();
        if (starRenderer != null)
        {
            starMaterial = starRenderer.material;
            originalColor = starMaterial.color;
        }
    }
    
    void Update()
    {
        MoveItem();
        RotateItem();
        ScaleItem();
        HandleFading();
    }
    
    void MoveItem()
    {
        // Move the item in world space (always toward positive Z regardless of rotation)
        transform.position += movementDirection * moveSpeed * Time.deltaTime;
    }
    
    void RotateItem()
    {
        // Only rotate if rotation is enabled
        if (enableRotation)
        {
            // Rotate the item around all axes using the rotation speed
            Vector3 rotation = rotationSpeed * Time.deltaTime;
            transform.Rotate(rotation, Space.Self);
        }
    }
    
    void ScaleItem()
    {
        if (!isFadingOut && currentScale < targetScale)
        {
            // Update scale timer
            scaleTimer += Time.deltaTime;
            
            // Calculate progress (0 to 1) based on duration
            float progress = scaleTimer / scaleDuration;
            progress = Mathf.Clamp01(progress); // Ensure it doesn't go over 1
            
            // Interpolate between initial and target scale
            currentScale = Mathf.Lerp(initialScale, targetScale, progress);
            transform.localScale = Vector3.one * currentScale;
        }
    }
    
    void HandleFading()
    {
        if (isFadingOut)
        {
            fadeTimer += Time.deltaTime;
            
            // Calculate fade alpha
            float fadeProgress = fadeTimer / fadeOutDuration;
            float alpha = Mathf.Lerp(originalColor.a, 0f, fadeProgress);
            
            // Apply fade to material
            if (starMaterial != null)
            {
                Color newColor = originalColor;
                newColor.a = alpha;
                starMaterial.color = newColor;
            }
            
            // Destroy after fade is complete
            if (fadeTimer >= fadeOutDuration)
            {
                Destroy(gameObject);
            }
        }
    }
    
    // Called externally to start fade out (only called by ItemDestroyer now)
    public void StartFadeOut()
    {
        if (!isFadingOut)
        {
            isFadingOut = true;
            fadeTimer = 0f;
            
            // Start destruction timer as backup
            Destroy(gameObject, destroyDelay);
        }
    }
    
    // Alternative method to destroy immediately if needed
    public void DestroyImmediately()
    {
        Destroy(gameObject);
    }
}