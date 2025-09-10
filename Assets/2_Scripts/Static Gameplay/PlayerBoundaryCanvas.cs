using KBCore.Refs;
using UnityEngine;

public class PlayerBoundaryCanvas : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool updateContinuously = true;
    [SerializeField] private bool matchPosition = true;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private float canvasSizeScaler = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLog;

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;





    private void OnValidate()
    {
        // Auto-assign references in editor
        if (!targetCanvas)
        {
            targetCanvas = GetComponent<Canvas>();
        }

        if (!canvasRectTransform && targetCanvas)
        {
            canvasRectTransform = targetCanvas.GetComponent<RectTransform>();
        }

        if (!levelManager) levelManager = FindFirstObjectByType<LevelManager>();

        this.ValidateRefs();
    }

    private void Awake()
    {
        // Get canvas component if not assigned
        if (!targetCanvas)
        {
            targetCanvas = GetComponent<Canvas>();
        }

        // Get RectTransform if not assigned
        if (!canvasRectTransform && targetCanvas)
        {
            canvasRectTransform = targetCanvas.GetComponent<RectTransform>();
        }

        // Ensure canvas is set to World Space
        if (targetCanvas && targetCanvas.renderMode != RenderMode.WorldSpace)
        {
            targetCanvas.renderMode = RenderMode.WorldSpace;
            if (debugLog) Debug.Log("Canvas render mode changed to World Space");
        }
    }

    private void Start()
    {
        // Get LevelManager instance
        levelManager = LevelManager.Instance;

        if (!levelManager)
        {
            if (debugLog) Debug.LogError("LevelManager instance not found!");
            return;
        }

        UpdateCanvasBoundaries();
    }

    private void Update()
    {
        if (updateContinuously && levelManager)
        {
            UpdateCanvasBoundaries();
        }
    }

    // Public method to manually update the canvas boundaries
    public void UpdateCanvasBoundaries()
    {
        if (!levelManager || !canvasRectTransform)
        {
            if (debugLog) Debug.LogWarning("Missing references for updating canvas boundaries");
            return;
        }

        // Get the player boundaries from LevelManager
        Vector2 playerBoundary = GetPlayerBoundarySize();
        Vector3 playerPos = levelManager.PlayerPosition;

        if (playerBoundary == Vector2.zero)
        {
            if (debugLog) Debug.LogWarning("Player boundary size is zero");
            return;
        }

        // Update canvas size with scaler applied
        // Convert from world units to canvas units (assuming 1:1 ratio)
        Vector2 scaledSize = new Vector2(playerBoundary.x * 2f * canvasSizeScaler, playerBoundary.y * 2f * canvasSizeScaler);
        canvasRectTransform.sizeDelta = scaledSize;

        // Update canvas position if enabled
        if (matchPosition)
        {
            transform.position = playerPos + positionOffset;
        }

        if (debugLog)
        {
            Debug.Log($"Canvas updated - Size: {canvasRectTransform.sizeDelta}, Position: {transform.position}, Scaler: {canvasSizeScaler}");
        }
    }

    // Get the player boundary size from LevelManager
    private Vector2 GetPlayerBoundarySize()
    {
        return levelManager.PlayerBoundarySize;
    }

    // Method to set a custom boundary size (useful for testing)
    public void SetCustomBoundarySize(Vector2 size)
    {
        if (!canvasRectTransform) return;

        Vector2 scaledSize = new Vector2(size.x * canvasSizeScaler, size.y * canvasSizeScaler);
        canvasRectTransform.sizeDelta = scaledSize;

        if (debugLog)
        {
            Debug.Log($"Canvas size set to custom value: {scaledSize} (Original: {size}, Scaler: {canvasSizeScaler})");
        }
    }

    // Method to fit canvas to specific boundaries
    public void FitToCustomBoundaries(Vector2 boundarySize, Vector3 position)
    {
        if (!canvasRectTransform) return;

        Vector2 scaledSize = new Vector2(boundarySize.x * 2f * canvasSizeScaler, boundarySize.y * 2f * canvasSizeScaler);
        canvasRectTransform.sizeDelta = scaledSize;
        transform.position = position + positionOffset;

        if (debugLog)
        {
            Debug.Log($"Canvas fitted to custom boundaries - Size: {scaledSize}, Position: {position + positionOffset}, Scaler: {canvasSizeScaler}");
        }
    }

    // Method to update the canvas size scaler at runtime
    public void SetCanvasSizeScaler(float scaler)
    {
        canvasSizeScaler = Mathf.Max(0.1f, scaler); // Prevent negative or zero values
        UpdateCanvasBoundaries(); // Immediately apply the new scaler

        if (debugLog)
        {
            Debug.Log($"Canvas size scaler updated to: {canvasSizeScaler}");
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!canvasRectTransform) return;

        // Draw canvas boundaries
        Gizmos.color = Color.blue;
        Vector3 size = new Vector3(canvasRectTransform.sizeDelta.x, canvasRectTransform.sizeDelta.y, 0.1f);
        Gizmos.DrawWireCube(transform.position, size);

        // Draw center point
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }
#endif
}