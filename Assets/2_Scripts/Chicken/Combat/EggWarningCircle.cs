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

    [Header("Lifetime Settings")]
    [SerializeField] private float maxLifetime = 10f;

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

    // Lifetime variables
    private float lifetimeTimer = 0f;

    // Pass-through detection
    private Vector3 lastEggPosition;
    private bool hasTrackedPosition = false;

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

        UpdateLifetime();
        UpdatePulseAnimation();
        UpdateFadeOut();
    }

    public void Initialize(Vector3 position, float size , float fadeDistanceValue)
    {
        targetPosition = position;
        originalSize = size;
        fadeDistance = fadeDistanceValue;

        // IMPORTANT: Ensure pivot is centered for proper visual alignment
        if (rectTransform != null)
        {
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(size, size);
        }

        // Set position AFTER setting pivot to ensure correct placement
        transform.position = position;

        // Store original color
        if (circleImage != null)
        {
            originalColor = circleImage.color;
        }

        // Reset state
        isFadingOut = false;
        fadeTimer = 0f;
        pulseTimer = 0f;
        lifetimeTimer = 0f;
        hasTrackedPosition = false;
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
            return;
        }

        // Check if egg has passed the target
        if (hasTrackedPosition)
        {
            // Calculate vectors from last position and current position to target
            Vector3 lastToTarget = targetPosition - lastEggPosition;
            Vector3 currentToTarget = targetPosition - eggPosition;

            // Calculate egg's movement direction
            Vector3 eggMovement = eggPosition - lastEggPosition;

            // If the egg has moved and is going away from target, it has passed
            if (eggMovement.sqrMagnitude > 0.0001f)
            {
                // Dot product of movement and direction to target
                // If negative, egg is moving away from target (has passed it)
                float dotProduct = Vector3.Dot(eggMovement.normalized, currentToTarget.normalized);
                
                if (dotProduct < -0.1f) // Threshold to avoid false positives
                {
                    if (showDebugLogs)
                        Debug.Log($"EggWarningCircle: Egg passed target! Dot: {dotProduct}");
                    StartFadeOut();
                    return;
                }
            }

            // Alternative check: if egg crossed the target plane
            // Check if the egg was on one side and now is on the other
            if (lastToTarget.sqrMagnitude > 0.0001f && currentToTarget.sqrMagnitude > 0.0001f)
            {
                // If the direction to target flipped significantly, we passed it
                float directionDot = Vector3.Dot(lastToTarget.normalized, currentToTarget.normalized);
                if (directionDot < 0.5f && currentToTarget.sqrMagnitude > lastToTarget.sqrMagnitude)
                {
                    if (showDebugLogs)
                        Debug.Log($"EggWarningCircle: Egg crossed target plane! Direction dot: {directionDot}");
                    StartFadeOut();
                    return;
                }
            }
        }

        // Update last position for next frame
        lastEggPosition = eggPosition;
        hasTrackedPosition = true;

        // Fallback: Check if egg is very close to target
        if (distanceToTarget < 0.5f)
        {
            if (showDebugLogs)
                Debug.Log("EggWarningCircle: Egg very close to target");
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

    private void UpdateLifetime()
    {
        lifetimeTimer += Time.deltaTime;

        if (lifetimeTimer >= maxLifetime)
        {
            if (showDebugLogs)
                Debug.Log("EggWarningCircle: Lifetime expired, starting fade out");

            StartFadeOut();
        }
    }

    private void UpdatePulseAnimation()
    {
        if (isFadingOut) return;

        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulseValue = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
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

    public void SetFadeDistance(float newFadeDistance)
    {
        fadeDistance = newFadeDistance;
    }

    public void SetMaxLifetime(float newLifetime)
    {
        maxLifetime = newLifetime;
    }

    public float GetRemainingLifetime()
    {
        return Mathf.Max(0f, maxLifetime - lifetimeTimer);
    }

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
        lifetimeTimer = 0f;
        hasTrackedPosition = false;

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
        lifetimeTimer = 0f;

        // Reset transform parent
        transform.SetParent(null);
    }

    public void OnPoolRecycle()
    {
        OnPoolReturn();
    }

    #endregion Pool Object -------------------------------------------------------------------------
}