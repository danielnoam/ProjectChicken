using PrimeTween;
using UnityEngine;

public class SpaceItemBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    [HideInInspector] public float moveSpeed = 5f; // Set by spawner
    
    [Header("Spawn Rotation")]
    public bool useRandomYRotation = true; // Toggle random Y rotation on spawn
    public float manualYRotation = 90f; // Manual Y rotation value when random is disabled (in degrees)
    
    [Header("Rotation Settings")]
    public bool enableRotation = true; // Toggle rotation on/off
    public Vector3 rotationSpeed = new Vector3(43f, 67f, 91f); // Degrees per second for X, Y, Z axes (non-repeating pattern)
    
    [Header("Scaling Settings")]

    public float minSize = 0.8f;
    public float maxSize = 1.5f;
    [HideInInspector] public float scaleDuration = 2f; // Set by spawner
    
    [Header("Lifecycle Settings")]
    public float fadeOutDuration = 1f;
    public float destroyDelay = 2f;
    
    private float initialScale = 0f;
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
    private bool isPooled = false; // Track if this object is from pool



    void Start()
    {
        InitializeItem();
    }
    
    void InitializeItem()
    {
        // Apply spawn rotation first
        ApplySpawnRotation();
        
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
    
    void ApplySpawnRotation()
    {
        if (useRandomYRotation)
        {
            // Generate a random Y rotation between 0 and 360 degrees
            float randomYRotation = Random.Range(0f, 360f);
            transform.rotation = Quaternion.Euler(0f, randomYRotation, 0f);
        }
        else
        {
            // Use the manually set Y rotation value
            transform.rotation = Quaternion.Euler(0f, manualYRotation, 0f);
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
        transform.position += movementDirection * (moveSpeed * LevelManager.WorldSpeed * Time.deltaTime);
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
            
            // Return to pool or destroy after fade is complete
            if (fadeTimer >= fadeOutDuration)
            {
                ReturnToPoolOrDestroy();
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
            Invoke(nameof(ReturnToPoolOrDestroy), destroyDelay);
        }
    }
    
    // Alternative method to return to pool immediately if needed
    public void ReturnToPoolOrDestroyImmediately()
    {
        CancelInvoke(); // Cancel any pending destruction
        ReturnToPoolOrDestroy();
    }
    
    void ReturnToPoolOrDestroy()
    {
        // Try to find the appropriate pool for this object
        SpaceItemPool targetPool = null;
        
        // First, try to find a pool that contains this prefab type
        targetPool = SpaceItemPool.FindPoolWithPrefab(FindOriginalPrefab());
        
        if (targetPool != null)
        {
            targetPool.ReturnToPool(gameObject);
        }
        else
        {
            // Fallback to destroying if no appropriate pool found
            Destroy(gameObject);
        }
    }
    
    GameObject FindOriginalPrefab()
    {
        // Simple approach to find original prefab based on name
        string itemName = gameObject.name.Replace("(Clone)", "").Trim();
        
        // Search through all pools to find matching prefab
        SpaceItemPool[] allPools = FindObjectsByType<SpaceItemPool>(FindObjectsSortMode.None);
        foreach (SpaceItemPool pool in allPools)
        {
            foreach (GameObject prefab in pool.itemPrefabs)
            {
                if (prefab != null && prefab.name == itemName)
                {
                    return prefab;
                }
            }
        }
        
        return null;
    }
    
    // Reset method for object pooling
    public void ResetForPool()
    {
        // Cancel any pending invokes
        CancelInvoke();
        
        // Apply spawn rotation for pooled objects too
        ApplySpawnRotation();
        
        // Reset all state variables
        scaleTimer = 0f;
        isFadingOut = false;
        fadeTimer = 0f;
        currentScale = initialScale;
        
        // Reset target scale to a new random value
        targetScale = Random.Range(minSize, maxSize);
        
        // Reset transform scale
        transform.localScale = Vector3.one * currentScale;
        
        // Reset material color if available
        if (starMaterial != null && originalColor != Color.clear)
        {
            starMaterial.color = originalColor;
        }
        else if (starRenderer != null)
        {
            // Re-get material and original color (in case material changed)
            starMaterial = starRenderer.material;
            if (starMaterial != null)
            {
                originalColor = starMaterial.color;
            }
        }
        
        // Set initial position to current position (will be updated by spawner)
        initialPosition = transform.position;
        
        // Movement direction stays the same
        movementDirection = Vector3.back;
        
        isPooled = true;
    }
    
    void OnEnable()
    {
        // If this object was just enabled from pool, initialize it
        if (isPooled)
        {
            // Don't call full InitializeItem() as ResetForPool() already handled most of it
            // Just ensure we have the renderer and material references
            if (starRenderer == null)
            {
                starRenderer = GetComponent<Renderer>();
                if (starRenderer != null && starMaterial == null)
                {
                    starMaterial = starRenderer.material;
                    originalColor = starMaterial.color;
                }
            }
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
    
}