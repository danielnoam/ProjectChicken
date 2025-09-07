using UnityEngine;
using UnityEngine.UI;
using DNExtensions;

public class EggWarningCircle : MonoBehaviour, IPooledObject
{
    [Header("Visual Components")]
    [SerializeField] private Image circleImage;
    [SerializeField] private RectTransform rectTransform;
    
    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinScale = 0.8f;
    [SerializeField] private float pulseMaxScale = 1.2f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Runtime variables
    private Vector3 targetPosition;
    private float originalSize;
    private Color originalColor;
    private float fadeDistance;
    private bool isFadingOut = false;
    private float fadeTimer = 0f;
    private bool isInitialized = false;
    
    // Animation variables
    private float pulseTimer = 0f;
    
    void Awake()
    {
        // Auto-assign components if not set
        if (circleImage == null)
            circleImage = GetComponent<Image>();
        
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        
        if (circleImage == null)
        {
            Debug.LogError("EggWarningCircle: No Image component found!");
        }
        
        if (rectTransform == null)
        {
            Debug.LogError("EggWarningCircle: No RectTransform component found!");
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        UpdatePulseAnimation();
        UpdateFadeOut();
    }
    
    public void Initialize(Vector3 position, float size, Color color, float fadeDistanceValue)
    {
        targetPosition = position;
        originalSize = size;
        originalColor = color;
        fadeDistance = fadeDistanceValue;
        
        // Set position
        transform.position = position;
        
        // Set size
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(size, size);
        }
        
        // Set color
        if (circleImage != null)
        {
            circleImage.color = color;
        }
        
        // Reset state
        isFadingOut = false;
        fadeTimer = 0f;
        pulseTimer = 0f;
        isInitialized = true;
        
        if (showDebugLogs)
            Debug.Log($"EggWarningCircle: Initialized at {position} with size {size}");
    }
    
    public void UpdateWithEggPosition(Vector3 eggPosition)
    {
        if (!isInitialized || isFadingOut) return;
        
        // Calculate distance from egg to target
        float distanceToTarget = Vector3.Distance(eggPosition, targetPosition);
        
        // Start fading when egg gets close
        if (distanceToTarget <= fadeDistance)
        {
            StartFadeOut();
        }
        
        // Check if egg has passed through the target (simple check)
        // You might want to make this more sophisticated based on your needs
        Vector3 eggToTarget = targetPosition - eggPosition;
        if (eggToTarget.magnitude < 0.5f) // Very close to target
        {
            StartFadeOut();
        }
    }
    
    public void StartFadeOut()
    {
        if (isFadingOut) return;
        
        isFadingOut = true;
        fadeTimer = 0f;
        
        if (showDebugLogs)
            Debug.Log("EggWarningCircle: Starting fade out");
    }
    
    private void UpdatePulseAnimation()
    {
        if (isFadingOut) return;
        
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulseValue = (Mathf.Sin(pulseTimer) + 1f) * 0.5f; // 0 to 1
        float currentScale = Mathf.Lerp(pulseMinScale, pulseMaxScale, pulseValue);
        
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one * currentScale;
        }
    }
    
    private void UpdateFadeOut()
    {
        if (!isFadingOut) return;
        
        fadeTimer += Time.deltaTime;
        float fadeProgress = fadeTimer / fadeOutDuration;
        
        if (circleImage != null)
        {
            Color currentColor = originalColor;
            currentColor.a = Mathf.Lerp(originalColor.a, 0f, fadeProgress);
            circleImage.color = currentColor;
        }
        
        // Scale down during fade
        if (rectTransform != null)
        {
            float scaleProgress = Mathf.Lerp(1f, 0f, fadeProgress);
            rectTransform.localScale = Vector3.one * scaleProgress;
        }
        
        // Remove when fully faded
        if (fadeProgress >= 1f)
        {
            ReturnToPool();
        }
    }
    
    // Set custom fade distance for dynamic adjustment
    public void SetFadeDistance(float newFadeDistance)
    {
        fadeDistance = newFadeDistance;
    }
    
    // Force immediate cleanup
    public void ForceCleanup()
    {
        if (showDebugLogs)
            Debug.Log("EggWarningCircle: Force cleanup");
        
        ReturnToPool();
    }
    
    #region Pool Object -------------------------------------------------------------------------
    
    public void ReturnToPool()
    {
        ObjectPooler.ReturnObjectToPool(gameObject);
    }
    
    public void OnPoolGet()
    {
        // Reset state when retrieved from pool
        isInitialized = false;
        isFadingOut = false;
        fadeTimer = 0f;
        pulseTimer = 0f;
        
        // Reset visual state
        if (circleImage != null)
        {
            Color resetColor = originalColor;
            resetColor.a = 1f;
            circleImage.color = resetColor;
        }
        
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
        
        gameObject.SetActive(true);
    }
    
    public void OnPoolReturn()
    {
        // Clean up when returned to pool
        isInitialized = false;
        isFadingOut = false;
        
        // Reset transform parent (will be set again when used)
        transform.SetParent(null);
    }
    
    public void OnPoolRecycle()
    {
        OnPoolReturn();
    }
    
    #endregion Pool Object -------------------------------------------------------------------------
}