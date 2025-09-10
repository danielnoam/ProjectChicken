using UnityEngine;
using System.Collections.Generic;
using DNExtensions;

public class EggWarningSystem : MonoBehaviour
{
    public static EggWarningSystem Instance { get; private set; }
    
    
    [Header("Warning Settings")]
    [SerializeField] private GameObject warningCirclePrefab;
    [SerializeField] private Transform warningParent; // Parent transform for warning circles (should be the canvas)

    [Header("Direct Canvas Reference")]
    [SerializeField] private Transform targetCanvasTransform; // Direct reference to the warning area canvas
    [SerializeField] private bool autoFindCanvas = true; // Automatically find PlayerBoundaryCanvas

    [Header("Circle Settings")]
    [SerializeField] private float circleSize = 2f;
    [SerializeField] private float fadeDistance = 1f; // Distance before impact to start fading

    [Header("Canvas Boundary Settings")]
    [SerializeField] private bool clampWarningsToCanvas = true; // Always clamp warnings to canvas bounds
    [SerializeField] private float canvasPadding = 0.5f; // Padding from canvas edges

    [Header("Math-Based Calculation")]
    [SerializeField] private float maxProjectionDistance = 100f; // Maximum distance to project

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showDebugGizmos = false;
    [SerializeField] private bool showDetailedDebug = false; // Extra detailed debug info

    // Singleton pattern
  

    // Active warnings tracking
    private Dictionary<ChickenEggV2, EggWarningCircle> activeWarnings = new Dictionary<ChickenEggV2, EggWarningCircle>();

    // Cached canvas info
    private Vector2 cachedCanvasBounds = Vector2.zero;
    private Vector3 cachedCanvasPosition = Vector3.zero;
    private Vector3 cachedCanvasNormal = Vector3.back; // Canvas faces towards camera (negative Z)
    private float lastBoundsCheckTime = 0f;
    private float boundsCheckInterval = 1f;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Auto-find canvas if enabled
        if (autoFindCanvas && targetCanvasTransform == null)
        {
            PlayerBoundaryCanvas boundaryCanvas = FindFirstObjectByType<PlayerBoundaryCanvas>();
            if (boundaryCanvas != null)
            {
                targetCanvasTransform = boundaryCanvas.transform;
                if (showDebugLogs)
                    Debug.Log($"EggWarningSystem: Auto-found PlayerBoundaryCanvas: {targetCanvasTransform.name}");
            }
        }

        // Set warning parent to canvas if not set
        if (warningParent == null && targetCanvasTransform != null)
        {
            warningParent = targetCanvasTransform;
            if (showDebugLogs)
                Debug.Log($"EggWarningSystem: Set warning parent to canvas: {warningParent.name}");
        }

        if (targetCanvasTransform == null)
        {
            Debug.LogError("EggWarningSystem: No target canvas found! Please assign targetCanvasTransform or enable autoFindCanvas.");
        }

        if (warningCirclePrefab == null)
        {
            Debug.LogError("EggWarningSystem: No warning circle prefab assigned!");
        }

        // Cache initial canvas info
        UpdateCanvasCache();

        if (showDetailedDebug)
        {
            Debug.Log($"EggWarningSystem: Setup complete");
            Debug.Log($"  Target Canvas: {targetCanvasTransform?.name ?? "null"}");
            Debug.Log($"  Warning Parent: {warningParent?.name ?? "null"}");
            Debug.Log($"  Canvas Position: {cachedCanvasPosition}");
            Debug.Log($"  Canvas Bounds: {cachedCanvasBounds}");
        }
    }

    void Update()
    {
        // Periodically update canvas cache
        if (Time.time - lastBoundsCheckTime > boundsCheckInterval)
        {
            UpdateCanvasCache();
            lastBoundsCheckTime = Time.time;
        }

        UpdateActiveWarnings();
    }

    // Called when an egg is shot to create a warning - DIRECT CALCULATION
    public void CreateWarning(ChickenEggV2 egg, Vector3 startPosition, Vector3 direction, float speed)
    {
        if (egg == null || warningCirclePrefab == null || warningParent == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("EggWarningSystem: Cannot create warning - missing references");
            return;
        }

        if (targetCanvasTransform == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("EggWarningSystem: No target canvas assigned!");
            return;
        }

        Vector3 impactPoint = CalculateDirectImpactPoint(startPosition, direction);

        if (showDetailedDebug)
        {
            Debug.Log($"EggWarningSystem: Creating warning for egg {egg.name}");
            Debug.Log($"  Start Position: {startPosition}");
            Debug.Log($"  Direction: {direction}");
            Debug.Log($"  Calculated Impact: {impactPoint}");
        }

        if (impactPoint == Vector3.zero)
        {
            if (showDebugLogs)
                Debug.LogWarning("EggWarningSystem: Could not calculate impact point");
            return;
        }

        // Clamp to canvas bounds if enabled
        if (clampWarningsToCanvas)
        {
            Vector3 originalImpact = impactPoint;
            impactPoint = ClampPositionToCanvas(impactPoint);

            if (showDetailedDebug && originalImpact != impactPoint)
            {
                Debug.Log($"EggWarningSystem: Clamped position from {originalImpact} to {impactPoint}");
            }
        }

        CreateWarningAtPosition(egg, impactPoint);
    }

    // Direct mathematical calculation of where egg hits canvas
    private Vector3 CalculateDirectImpactPoint(Vector3 startPosition, Vector3 direction)
    {
        if (targetCanvasTransform == null)
            return Vector3.zero;

        // Get canvas plane info
        Vector3 canvasPos = cachedCanvasPosition;
        Vector3 canvasNormal = cachedCanvasNormal;

        // Calculate intersection with canvas plane using math
        // Plane equation: dot(normal, point - planePoint) = 0
        // Ray equation: point = start + t * direction
        // Solving: dot(normal, start + t * direction - planePoint) = 0

        float denominator = Vector3.Dot(canvasNormal, direction);

        if (Mathf.Abs(denominator) < 0.0001f)
        {
            // Ray is parallel to plane, project forward on canvas plane
            if (showDetailedDebug)
                Debug.Log("EggWarningSystem: Ray parallel to canvas, projecting forward");

            Vector3 projectedPoint = startPosition + direction.normalized * 15f;
            // Project onto canvas plane
            Vector3 toPoint = projectedPoint - canvasPos;
            float distance = Vector3.Dot(toPoint, canvasNormal);
            projectedPoint = projectedPoint - canvasNormal * distance;
            return projectedPoint;
        }

        // Calculate intersection parameter t
        float t = Vector3.Dot(canvasNormal, canvasPos - startPosition) / denominator;

        if (t < 0 || t > maxProjectionDistance)
        {
            // Intersection is behind ray or too far
            if (showDetailedDebug)
                Debug.Log($"EggWarningSystem: Invalid intersection t={t}, using projection");

            Vector3 projectedPoint = startPosition + direction.normalized * 15f;
            // Project onto canvas plane
            Vector3 toPoint = projectedPoint - canvasPos;
            float distance = Vector3.Dot(toPoint, canvasNormal);
            projectedPoint = projectedPoint - canvasNormal * distance;
            return projectedPoint;
        }

        // Calculate intersection point
        Vector3 intersectionPoint = startPosition + direction * t;

        if (showDetailedDebug)
        {
            Debug.Log($"EggWarningSystem: Direct calculation successful");
            Debug.Log($"  Intersection parameter t: {t}");
            Debug.Log($"  Canvas position: {canvasPos}");
            Debug.Log($"  Canvas normal: {canvasNormal}");
            Debug.Log($"  Intersection point: {intersectionPoint}");
        }

        return intersectionPoint;
    }

    // Called when an egg is shot with a specific target position (for formations)
    public void CreateWarningAtTarget(ChickenEggV2 egg, Vector3 targetPosition)
    {
        if (egg == null || warningCirclePrefab == null || warningParent == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("EggWarningSystem: Cannot create warning - missing references");
            return;
        }

        // For formation attacks, project the target onto the canvas plane
        if (targetCanvasTransform != null)
        {
            Vector3 canvasPos = cachedCanvasPosition;
            Vector3 canvasNormal = cachedCanvasNormal;

            // Project target position onto canvas plane
            Vector3 toTarget = targetPosition - canvasPos;
            float distance = Vector3.Dot(toTarget, canvasNormal);
            targetPosition = targetPosition - canvasNormal * distance;
        }

        // Clamp to canvas bounds if enabled
        if (clampWarningsToCanvas)
        {
            targetPosition = ClampPositionToCanvas(targetPosition);
        }

        CreateWarningAtPosition(egg, targetPosition);

        if (showDebugLogs)
            Debug.Log($"EggWarningSystem: Created formation warning at {targetPosition} for egg {egg?.name ?? "null"}");
    }

    // Update canvas cache
    private void UpdateCanvasCache()
    {
        if (targetCanvasTransform == null)
            return;

        // Cache canvas position
        cachedCanvasPosition = targetCanvasTransform.position;

        // Cache canvas normal (assuming canvas faces camera)
        cachedCanvasNormal = -targetCanvasTransform.forward;

        // Cache canvas bounds
        Vector2 newBounds = GetCanvasBounds();
        if (newBounds != cachedCanvasBounds)
        {
            cachedCanvasBounds = newBounds;
            if (showDetailedDebug)
                Debug.Log($"EggWarningSystem: Updated canvas cache - Position: {cachedCanvasPosition}, Bounds: {cachedCanvasBounds}");
        }
    }

    // Get canvas bounds from various sources
    private Vector2 GetCanvasBounds()
    {
        // Method 1: LevelManager
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            Vector2 playerBounds = levelManager.PlayerBoundarySize;
            if (playerBounds != Vector2.zero)
            {
                return playerBounds * 2f;
            }
        }

        // Method 2: Canvas RectTransform
        if (targetCanvasTransform != null)
        {
            Canvas canvas = targetCanvasTransform.GetComponent<Canvas>();
            if (canvas != null)
            {
                RectTransform rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform != null && rectTransform.sizeDelta != Vector2.zero)
                {
                    return rectTransform.sizeDelta;
                }
            }
        }

        // Fallback
        return new Vector2(20f, 15f);
    }

    // Clamp position to canvas boundaries
    private Vector3 ClampPositionToCanvas(Vector3 position)
    {
        if (cachedCanvasBounds == Vector2.zero)
            return position;

        // Convert to local canvas space, clamp, then convert back
        Vector3 localPos = targetCanvasTransform.InverseTransformPoint(position);

        float canvasHalfWidth = (cachedCanvasBounds.x * 0.5f) - canvasPadding;
        float canvasHalfHeight = (cachedCanvasBounds.y * 0.5f) - canvasPadding;

        localPos.x = Mathf.Clamp(localPos.x, -canvasHalfWidth, canvasHalfWidth);
        localPos.y = Mathf.Clamp(localPos.y, -canvasHalfHeight, canvasHalfHeight);

        return targetCanvasTransform.TransformPoint(localPos);
    }

    // Create warning at position
    private void CreateWarningAtPosition(ChickenEggV2 egg, Vector3 impactPoint)
    {
        GameObject warningObj = ObjectPooler.GetObjectFromPool(warningCirclePrefab, impactPoint, Quaternion.identity);
        if (warningObj == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("EggWarningSystem: Failed to get warning from pool");
            return;
        }

        warningObj.transform.SetParent(warningParent, true);
        warningObj.transform.position = impactPoint;

        EggWarningCircle warningCircle = warningObj.GetComponent<EggWarningCircle>();
        if (warningCircle == null)
        {
            Debug.LogError("EggWarningSystem: Warning prefab missing EggWarningCircle component!");
            ObjectPooler.ReturnObjectToPool(warningObj);
            return;
        }

        warningCircle.Initialize(impactPoint, circleSize, fadeDistance);

        if (egg != null)
        {
            activeWarnings[egg] = warningCircle;
        }

        if (showDebugLogs)
            Debug.Log($"EggWarningSystem: Warning created at {impactPoint} for {egg?.name ?? "formation"}");
    }

    // Remove warning
    public void RemoveWarning(ChickenEggV2 egg)
    {
        if (egg == null || !activeWarnings.ContainsKey(egg))
            return;

        EggWarningCircle warning = activeWarnings[egg];
        if (warning != null)
        {
            warning.StartFadeOut();
        }

        activeWarnings.Remove(egg);

        if (showDebugLogs)
            Debug.Log($"EggWarningSystem: Removed warning for {egg.name}");
    }

    // Update active warnings
    private void UpdateActiveWarnings()
    {
        List<ChickenEggV2> eggsToRemove = new List<ChickenEggV2>();

        foreach (var kvp in activeWarnings)
        {
            ChickenEggV2 egg = kvp.Key;
            EggWarningCircle warning = kvp.Value;

            if (egg == null || !egg.gameObject.activeInHierarchy)
            {
                eggsToRemove.Add(egg);
                continue;
            }

            if (warning == null || !warning.gameObject.activeInHierarchy)
            {
                eggsToRemove.Add(egg);
                continue;
            }

            warning.UpdateWithEggPosition(egg.transform.position);
        }

        foreach (ChickenEggV2 egg in eggsToRemove)
        {
            if (activeWarnings.ContainsKey(egg))
            {
                EggWarningCircle warning = activeWarnings[egg];
                if (warning != null)
                {
                    warning.StartFadeOut();
                }
                activeWarnings.Remove(egg);
            }
        }
    }

    // Clear all warnings
    public void ClearAllWarnings()
    {
        foreach (var warning in activeWarnings.Values)
        {
            if (warning != null)
            {
                warning.StartFadeOut();
            }
        }
        activeWarnings.Clear();

        if (showDebugLogs)
            Debug.Log("EggWarningSystem: Cleared all warnings");
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw canvas plane
        if (targetCanvasTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = targetCanvasTransform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(cachedCanvasBounds.x, cachedCanvasBounds.y, 0.1f));
            Gizmos.matrix = Matrix4x4.identity;

            // Draw canvas normal
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(cachedCanvasPosition, cachedCanvasNormal * 2f);
        }

        // Draw active warnings
        Gizmos.color = Color.red;
        foreach (var warning in activeWarnings.Values)
        {
            if (warning != null)
            {
                Gizmos.DrawWireSphere(warning.transform.position, circleSize * 0.5f);
            }
        }
    }
}