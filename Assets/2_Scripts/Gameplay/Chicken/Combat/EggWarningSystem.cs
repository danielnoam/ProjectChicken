using UnityEngine;
using System.Collections.Generic;
using DNExtensions;

public class EggWarningSystem : MonoBehaviour
{
    [Header("Warning Settings")]
    [SerializeField] private GameObject warningCirclePrefab;
    [SerializeField] private Transform warningParent; // Parent transform for warning circles (should be the canvas)
    [SerializeField] private float groundLevel = 0f; // Y level where we calculate impact
    [SerializeField] private LayerMask groundLayerMask = 1; // What counts as ground for raycasting
    
    [Header("Circle Settings")]
    [SerializeField] private float circleSize = 2f;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float fadeDistance = 1f; // Distance before impact to start fading
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showDebugGizmos = false;
    
    // Singleton pattern
    public static EggWarningSystem Instance { get; private set; }
    
    // Active warnings tracking
    private Dictionary<ChickenEggV2, EggWarningCircle> activeWarnings = new Dictionary<ChickenEggV2, EggWarningCircle>();
    
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
        // Auto-find warning parent if not assigned (look for world space canvas)
        if (warningParent == null)
        {
            PlayerBoundaryCanvas boundaryCanvas = FindObjectOfType<PlayerBoundaryCanvas>();
            if (boundaryCanvas != null)
            {
                warningParent = boundaryCanvas.transform;
                if (showDebugLogs)
                    Debug.Log("EggWarningSystem: Auto-assigned PlayerBoundaryCanvas as warning parent");
            }
            else
            {
                Debug.LogWarning("EggWarningSystem: No warning parent assigned and no PlayerBoundaryCanvas found!");
            }
        }
        
        if (warningCirclePrefab == null)
        {
            Debug.LogError("EggWarningSystem: No warning circle prefab assigned!");
        }
    }
    
    void Update()
    {
        UpdateActiveWarnings();
    }
    
    // Called when an egg is shot to create a warning
    public void CreateWarning(ChickenEggV2 egg, Vector3 startPosition, Vector3 direction, float speed)
    {
        if (egg == null || warningCirclePrefab == null || warningParent == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("EggWarningSystem: Cannot create warning - missing references");
            return;
        }
        
        // Calculate impact point
        Vector3 impactPoint = CalculateImpactPoint(startPosition, direction, speed);
        
        if (impactPoint == Vector3.zero)
        {
            if (showDebugLogs)
                Debug.LogWarning("EggWarningSystem: Could not calculate valid impact point");
            return;
        }
        
        // Create warning circle
        GameObject warningObj = ObjectPooler.GetObjectFromPool(warningCirclePrefab, impactPoint, Quaternion.identity);
        if (warningObj == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("EggWarningSystem: Failed to get warning circle from pool");
            return;
        }
        
        // Set parent to canvas
        warningObj.transform.SetParent(warningParent, true);
        warningObj.transform.position = impactPoint;
        
        // Get warning circle component
        EggWarningCircle warningCircle = warningObj.GetComponent<EggWarningCircle>();
        if (warningCircle == null)
        {
            Debug.LogError("EggWarningSystem: Warning prefab missing EggWarningCircle component!");
            ObjectPooler.ReturnObjectToPool(warningObj);
            return;
        }
        
        // Initialize warning circle
        warningCircle.Initialize(impactPoint, circleSize, warningColor, fadeDistance);
        
        // Track this warning
        activeWarnings[egg] = warningCircle;
        
        if (showDebugLogs)
            Debug.Log($"EggWarningSystem: Created warning at {impactPoint} for egg {egg.name}");
    }
    
    // Called when an egg is destroyed/deactivated
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
            Debug.Log($"EggWarningSystem: Removed warning for egg {egg.name}");
    }
    
    // Update all active warnings
    private void UpdateActiveWarnings()
    {
        List<ChickenEggV2> eggsToRemove = new List<ChickenEggV2>();
        
        foreach (var kvp in activeWarnings)
        {
            ChickenEggV2 egg = kvp.Key;
            EggWarningCircle warning = kvp.Value;
            
            // Check if egg still exists and is active
            if (egg == null || !egg.gameObject.activeInHierarchy)
            {
                eggsToRemove.Add(egg);
                continue;
            }
            
            // Check if warning still exists
            if (warning == null || !warning.gameObject.activeInHierarchy)
            {
                eggsToRemove.Add(egg);
                continue;
            }
            
            // Update warning with egg position
            warning.UpdateWithEggPosition(egg.transform.position);
        }
        
        // Clean up removed eggs
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
    
    // Calculate where the egg will hit
    private Vector3 CalculateImpactPoint(Vector3 startPosition, Vector3 direction, float speed)
    {
        // First try raycasting to find ground intersection
        Ray ray = new Ray(startPosition, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayerMask))
        {
            return hit.point;
        }
        
        // Fallback: calculate intersection with ground plane at groundLevel
        if (Mathf.Abs(direction.y) > 0.001f) // Avoid division by zero
        {
            float t = (groundLevel - startPosition.y) / direction.y;
            if (t > 0) // Only forward intersections
            {
                Vector3 impactPoint = startPosition + direction * t;
                return impactPoint;
            }
        }
        
        // If no valid intersection found, return zero vector
        return Vector3.zero;
    }
    
    // Public method to clear all warnings
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
        
        // Draw ground level
        Gizmos.color = Color.green;
        Vector3 center = transform.position;
        Gizmos.DrawWireCube(new Vector3(center.x, groundLevel, center.z), new Vector3(20f, 0.1f, 20f));
        
        // Draw active warning positions
        Gizmos.color = warningColor;
        foreach (var warning in activeWarnings.Values)
        {
            if (warning != null)
            {
                Gizmos.DrawWireSphere(warning.transform.position, circleSize * 0.5f);
            }
        }
    }
}