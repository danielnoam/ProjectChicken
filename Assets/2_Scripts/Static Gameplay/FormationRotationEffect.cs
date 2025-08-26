using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FormationRotationEffect : MonoBehaviour
{
    [Header("Rotation Effect Settings")]
    [Tooltip("Toggle the rotation effect on/off")]
    public bool enableRotation = true;

    [Header("Rotation Speed")]
    [Tooltip("Base rotation speed in degrees per second")]
    [Range(-360f, 360f)]
    public float baseRotationSpeed = 45f;

    [Header("Rotation Variation Settings")]
    [Tooltip("Enable individual rotation speed variation per formation")]
    public bool enableSpeedVariation = true;
    [Tooltip("Maximum variation to add/subtract from base rotation speed (in degrees/second)")]
    [Range(0f, 180f)]
    public float speedVariation = 20f;
    [Tooltip("Add random starting rotation offset to each formation")]
    public bool useRandomStartingRotation = true;

    [Header("Rotation Direction")]
    [Tooltip("Allow formations to rotate in different directions")]
    public bool allowRandomDirection = true;

    [Header("Advanced Settings")]
    [Tooltip("Use smooth rotation interpolation")]
    public bool useSmoothRotation = true;
    [Tooltip("Start the rotation effect immediately on Start()")]
    public bool startImmediately = true;
    [Tooltip("Show debug logs for rotation effect")]
    public bool showDebugLogs = false;

    [Header("Effect Interaction")]
    [Tooltip("How to handle interaction with other effects like breathing")]
    public EffectInteractionMode interactionMode = EffectInteractionMode.ReadCurrentState;

    public enum EffectInteractionMode
    {
        ReadOriginalPositions,  // Always use original formation positions (effects overwrite each other)
        ReadCurrentState       // Read current FormationSlots state (effects stack)
    }

    // Components
    private FormationCreator formationCreator;

    // Animation state
    private bool isInitialized = false;
    private bool previousEnableRotation = false;
    private float rotationStartTime;
    private FormationCreator.FormationType currentFormationType;
    private float blendInDuration = 1f;

    // Formation tracking
    [System.Serializable]
    public class RotationData
    {
        public List<Vector3> baseSlots;           // Base positions to rotate from (original or current)
        public Vector3 centerPosition;           // Center point for rotation
        public float effectiveRotationSpeed;     // Degrees per second
        public float startingRotationOffset;     // Starting angle offset in degrees
        public float currentRotationAngle;       // Current rotation angle in degrees
        public int rotationDirection;            // 1 for clockwise, -1 for counter-clockwise
        public int startSlotIndex;               // Index of first slot in global array
        public int slotCount;                    // Number of slots in this formation

        public RotationData()
        {
            baseSlots = new List<Vector3>();
            centerPosition = Vector3.zero;
            effectiveRotationSpeed = 45f;
            startingRotationOffset = 0f;
            currentRotationAngle = 0f;
            rotationDirection = 1;
            startSlotIndex = 0;
            slotCount = 0;
        }
    }

    private List<RotationData> formations = new List<RotationData>();
    private List<Vector3> capturedBaseSlots = new List<Vector3>();
    private bool needsFormationDataUpdate = true;

    void Awake()
    {
        formationCreator = GetComponent<FormationCreator>();
        if (formationCreator == null)
        {
            Debug.LogError("FormationRotationEffect: No FormationCreator component found on this GameObject!");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        previousEnableRotation = enableRotation;
        currentFormationType = formationCreator.currentFormation;
        isInitialized = true;

        // Start rotation effect if enabled
        if (enableRotation && startImmediately)
        {
            StartRotation();
        }

        if (showDebugLogs)
        {
            Debug.Log($"FormationRotationEffect: Initialized with {currentFormationType} formation");
        }
    }

    void Update()
    {
        if (!isInitialized || formationCreator == null)
            return;

        // Check if formation type changed
        if (currentFormationType != formationCreator.currentFormation)
        {
            HandleFormationTypeChange();
        }

        // Check if we need to update formation data
        bool formationCountChanged = formationCreator.FormationSlots != null &&
                                   formationCreator.FormationSlots.Count != capturedBaseSlots.Count;

        if (needsFormationDataUpdate || formationCountChanged)
        {
            UpdateFormationData();
            needsFormationDataUpdate = false;
        }

        // Check if enableRotation state changed
        if (enableRotation != previousEnableRotation)
        {
            if (enableRotation && !previousEnableRotation)
            {
                StartRotation();
                if (showDebugLogs)
                {
                    Debug.Log("FormationRotationEffect: Rotation toggled ON");
                }
            }
            else if (!enableRotation && previousEnableRotation)
            {
                StopRotation();
                if (showDebugLogs)
                {
                    Debug.Log("FormationRotationEffect: Rotation toggled OFF");
                }
            }
            previousEnableRotation = enableRotation;
        }

        // Only continue if rotation is enabled
        if (!enableRotation)
            return;

        // Update the rotation animation
        UpdateRotationAnimation();
    }

    void HandleFormationTypeChange()
    {
        FormationCreator.FormationType oldType = currentFormationType;
        currentFormationType = formationCreator.currentFormation;

        if (showDebugLogs)
        {
            Debug.Log($"FormationRotationEffect: Formation type changed from {oldType} to {currentFormationType} - rebuilding data");
        }

        // Force complete rebuild of formation data
        formations.Clear();
        capturedBaseSlots.Clear();
        needsFormationDataUpdate = true;

        // If rotation was active, restart it properly
        if (enableRotation)
        {
            // Wait for formation to be regenerated, then restart rotation
            Invoke("RestartRotationAfterFormationChange", 0.1f);
        }
    }

    void RestartRotationAfterFormationChange()
    {
        if (formationCreator.FormationSlots == null || formationCreator.FormationSlots.Count == 0)
        {
            Invoke("RestartRotationAfterFormationChange", 0.1f); // Try again next frame
            return;
        }

        UpdateFormationData();
        rotationStartTime = Time.time;

        // Initialize starting angles for all formations
        for (int i = 0; i < formations.Count; i++)
        {
            formations[i].currentRotationAngle = formations[i].startingRotationOffset;
        }

        if (showDebugLogs)
        {
            Debug.Log($"FormationRotationEffect: Restarted rotation for {currentFormationType} with {formations.Count} formations");
        }
    }

    void UpdateFormationData()
    {
        // Capture current formation slots as base for rotation
        if (formationCreator.FormationSlots != null && formationCreator.FormationSlots.Count > 0)
        {
            capturedBaseSlots.Clear();
            capturedBaseSlots.AddRange(formationCreator.FormationSlots);
        }
        else
        {
            return; // No formation slots to work with yet
        }

        formations.Clear();

        // Generate base formation to understand slot distribution
        List<Vector3> baseFormation = formationCreator.Generator.GenerateFormation(formationCreator.currentFormation);
        int slotsPerFormation = baseFormation.Count;
        int formationCount = formationCreator.formationCount;

        if (showDebugLogs)
        {
            Debug.Log($"FormationRotationEffect: Updating data - FormationType: {formationCreator.currentFormation}, SlotsPerFormation: {slotsPerFormation}, FormationCount: {formationCount}, TotalSlots: {capturedBaseSlots.Count}");
        }

        // Create rotation data for each formation
        for (int i = 0; i < formationCount; i++)
        {
            RotationData rotData = new RotationData();

            // Calculate speed variation
            float variation = enableSpeedVariation ?
                Random.Range(-speedVariation, speedVariation) : 0f;
            rotData.effectiveRotationSpeed = baseRotationSpeed + variation;

            // Calculate starting rotation offset
            rotData.startingRotationOffset = useRandomStartingRotation ?
                Random.Range(0f, 360f) : 0f;
            rotData.currentRotationAngle = rotData.startingRotationOffset;

            // Calculate rotation direction
            rotData.rotationDirection = allowRandomDirection && Random.value > 0.5f ? -1 : 1;

            // Calculate slot indices for this formation
            rotData.startSlotIndex = i * slotsPerFormation;
            rotData.slotCount = slotsPerFormation;

            // Extract base slots for this formation and calculate center
            Vector3 centerSum = Vector3.zero;
            int actualSlotCount = 0;

            for (int j = 0; j < slotsPerFormation && (rotData.startSlotIndex + j) < capturedBaseSlots.Count; j++)
            {
                Vector3 slot = capturedBaseSlots[rotData.startSlotIndex + j];
                rotData.baseSlots.Add(slot);
                centerSum += slot;
                actualSlotCount++;
            }

            // Calculate formation center
            if (actualSlotCount > 0)
            {
                rotData.centerPosition = centerSum / actualSlotCount;
                rotData.slotCount = actualSlotCount; // Update with actual count
            }

            formations.Add(rotData);

            if (showDebugLogs)
            {
                Debug.Log($"FormationRotationEffect: Formation {i} - Speed: {rotData.effectiveRotationSpeed:F2}°/s, Direction: {(rotData.rotationDirection > 0 ? "CW" : "CCW")}, StartAngle: {rotData.startingRotationOffset:F1}°, Slots: {rotData.slotCount}, Center: {rotData.centerPosition}");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"FormationRotationEffect: Completed data update with {formations.Count} formations");
        }
    }

    void UpdateRotationAnimation()
    {
        if (formations.Count == 0 || capturedBaseSlots.Count == 0)
            return;

        float elapsedTime = Time.time - rotationStartTime;
        float blendFactor = Mathf.Clamp01(elapsedTime / blendInDuration);

        // Process each formation individually to calculate rotation angles
        for (int formationIndex = 0; formationIndex < formations.Count; formationIndex++)
        {
            RotationData rotData = formations[formationIndex];

            // Calculate current rotation angle for this formation
            float angleIncrement = rotData.effectiveRotationSpeed * rotData.rotationDirection * elapsedTime;
            float targetAngle = rotData.startingRotationOffset + angleIncrement;

            // Blend from starting angle to rotating pattern
            float blendedAngle = Mathf.Lerp(rotData.startingRotationOffset, targetAngle, blendFactor);
            rotData.currentRotationAngle = blendedAngle;
        }

        // Apply rotation transformations to FormationSlots
        ApplyRotationTransforms();

        // Debug info (only occasionally to avoid spam)
        if (showDebugLogs && Time.frameCount % 120 == 0) // Every 2 seconds at 60fps
        {
            Debug.Log($"Rotation: Elapsed={elapsedTime:F1}s, BlendFactor={blendFactor:F2}, Formations={formations.Count}");
            for (int i = 0; i < Mathf.Min(3, formations.Count); i++) // Show first 3 formations
            {
                Debug.Log($"  Formation {i}: Speed={formations[i].effectiveRotationSpeed:F1}°/s, Angle={formations[i].currentRotationAngle:F1}°, Dir={formations[i].rotationDirection}");
            }
        }
    }

    void ApplyRotationTransforms()
    {
        if (formations.Count == 0 || capturedBaseSlots.Count == 0)
            return;

        // Apply rotation to each formation independently
        for (int formationIndex = 0; formationIndex < formations.Count; formationIndex++)
        {
            RotationData rotData = formations[formationIndex];

            // Convert angle to radians
            float angleInRadians = rotData.currentRotationAngle * Mathf.Deg2Rad;

            // Apply rotation to this formation's slots
            for (int slotIndex = 0; slotIndex < rotData.baseSlots.Count; slotIndex++)
            {
                int globalSlotIndex = rotData.startSlotIndex + slotIndex;
                if (globalSlotIndex < formationCreator.FormationSlots.Count)
                {
                    Vector3 baseSlot = rotData.baseSlots[slotIndex];

                    // Calculate offset from formation center
                    Vector3 offset = baseSlot - rotData.centerPosition;

                    // Apply 2D rotation around formation center (rotate in XY plane)
                    float cos = Mathf.Cos(angleInRadians);
                    float sin = Mathf.Sin(angleInRadians);

                    Vector3 rotatedOffset = new Vector3(
                        offset.x * cos - offset.y * sin,
                        offset.x * sin + offset.y * cos,
                        offset.z  // Keep Z unchanged
                    );

                    Vector3 rotatedSlot = rotData.centerPosition + rotatedOffset;
                    formationCreator.FormationSlots[globalSlotIndex] = rotatedSlot;
                }
            }
        }
    }

    // Public methods for external control
    [ContextMenu("Start Rotation")]
    public void StartRotation()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("FormationRotationEffect: Component not initialized yet.");
            return;
        }

        enableRotation = true;
        previousEnableRotation = true;
        rotationStartTime = Time.time;

        // Capture current state based on interaction mode
        CaptureCurrentFormationState();

        // Force update formation data
        UpdateFormationData();

        Debug.Log($"FormationRotationEffect: Rotation started with {formations.Count} formations");
    }

    [ContextMenu("Stop Rotation")]
    public void StopRotation()
    {
        enableRotation = false;
        previousEnableRotation = false;

        // Restore base formation slots if we're in original position mode
        if (interactionMode == EffectInteractionMode.ReadOriginalPositions &&
            capturedBaseSlots.Count > 0 &&
            formationCreator.FormationSlots.Count == capturedBaseSlots.Count)
        {
            for (int i = 0; i < capturedBaseSlots.Count; i++)
            {
                formationCreator.FormationSlots[i] = capturedBaseSlots[i];
            }
        }

        Debug.Log($"FormationRotationEffect: Rotation stopped");
    }

    [ContextMenu("Reset Formation")]
    public void ResetFormation()
    {
        if (formationCreator != null && isInitialized)
        {
            // Force regeneration of formation
            formationCreator.GenerateFormation();
            needsFormationDataUpdate = true;

            // If rotation is active, restart with new formation data
            if (enableRotation)
            {
                StartRotation();
            }

            Debug.Log($"FormationRotationEffect: Formation reset");
        }
    }

    [ContextMenu("Regenerate Rotation Variations")]
    public void RegenerateRotationVariations()
    {
        needsFormationDataUpdate = true;

        if (enableRotation)
        {
            StartRotation(); // This will regenerate the data
        }
        else
        {
            UpdateFormationData();
        }

        Debug.Log($"FormationRotationEffect: Regenerated variations for {formations.Count} formations");
    }

    [ContextMenu("Print Current Status")]
    public void PrintCurrentStatus()
    {
        if (!isInitialized)
        {
            Debug.Log("FormationRotationEffect: Not initialized yet");
            return;
        }

        Debug.Log($"=== ROTATION EFFECT STATUS ===");
        Debug.Log($"Rotation Enabled: {enableRotation}");
        Debug.Log($"Formation Type: {currentFormationType}");
        Debug.Log($"Formation Count: {formations.Count}");
        Debug.Log($"Base Rotation Speed: {baseRotationSpeed:F1}°/s");
        Debug.Log($"Speed Variation: {(enableSpeedVariation ? "Enabled" : "Disabled")} (±{speedVariation:F1}°/s)");
        Debug.Log($"Interaction Mode: {interactionMode}");

        // Show individual formation data
        if (enableRotation && formations.Count > 0)
        {
            Debug.Log("=== INDIVIDUAL FORMATION DATA ===");
            for (int i = 0; i < formations.Count; i++)
            {
                RotationData data = formations[i];
                string direction = data.rotationDirection > 0 ? "Clockwise" : "Counter-Clockwise";
                Debug.Log($"Formation {i}: Speed={data.effectiveRotationSpeed:F1}°/s, Angle={data.currentRotationAngle:F1}°, Direction={direction}, Slots={data.slotCount}");
            }
        }
        else
        {
            Debug.Log("Status: Stopped");
        }
    }

    [ContextMenu("Toggle Debug Logs")]
    public void ToggleDebugLogs()
    {
        showDebugLogs = !showDebugLogs;
        Debug.Log($"FormationRotationEffect: Debug logs {(showDebugLogs ? "enabled" : "disabled")}");
    }

    // Capture current formation state based on interaction mode
    void CaptureCurrentFormationState()
    {
        if (interactionMode == EffectInteractionMode.ReadCurrentState)
        {
            // Read whatever is currently in FormationSlots (might be modified by other effects)
            if (formationCreator.FormationSlots != null && formationCreator.FormationSlots.Count > 0)
            {
                capturedBaseSlots.Clear();
                capturedBaseSlots.AddRange(formationCreator.FormationSlots);
            }
        }
        // For ReadOriginalPositions mode, we'll capture in UpdateFormationData when needed
    }

    // Toggle rotation effect
    public void ToggleRotation()
    {
        if (enableRotation)
        {
            StopRotation();
        }
        else
        {
            StartRotation();
        }
    }

    // Set new rotation speed
    public void SetRotationSpeed(float newSpeed)
    {
        baseRotationSpeed = Mathf.Clamp(newSpeed, -360f, 360f);

        // Update all formation data proportionally
        for (int i = 0; i < formations.Count; i++)
        {
            float currentVariation = formations[i].effectiveRotationSpeed - baseRotationSpeed;
            formations[i].effectiveRotationSpeed = baseRotationSpeed + currentVariation;
        }

        Debug.Log($"FormationRotationEffect: Base rotation speed set to {baseRotationSpeed} degrees/second");
    }

    // Set speed variation settings
    public void SetSpeedVariation(bool enabled, float variation)
    {
        enableSpeedVariation = enabled;
        speedVariation = Mathf.Max(0f, variation);

        RegenerateRotationVariations();

        Debug.Log($"FormationRotationEffect: Speed variation {(enabled ? "enabled" : "disabled")} with ±{speedVariation:F1}°/s variation");
    }

    // Set interaction mode
    public void SetInteractionMode(EffectInteractionMode mode)
    {
        interactionMode = mode;
        if (enableRotation)
        {
            StartRotation(); // Restart to apply new interaction mode
        }

        Debug.Log($"FormationRotationEffect: Interaction mode set to {mode}");
    }

    // Properties for external access
    public bool IsRotating => enableRotation;
    public bool SpeedVariationEnabled => enableSpeedVariation;
    public float SpeedVariation => speedVariation;
    public int FormationCount => formations.Count;
    public float BaseRotationSpeed => baseRotationSpeed;
    public EffectInteractionMode CurrentInteractionMode => interactionMode;

    // Get the current rotation angle for a specific formation
    public float GetFormationRotationAngle(int formationIndex)
    {
        if (!enableRotation || !isInitialized || formationIndex >= formations.Count || formationIndex < 0)
            return 0f;

        return formations[formationIndex].currentRotationAngle;
    }

    // Get the effective rotation speed for a specific formation
    public float GetFormationRotationSpeed(int formationIndex)
    {
        if (formationIndex >= formations.Count || formationIndex < 0)
            return baseRotationSpeed;

        return formations[formationIndex].effectiveRotationSpeed;
    }

    // Get rotation direction for a specific formation
    public int GetFormationRotationDirection(int formationIndex)
    {
        if (formationIndex >= formations.Count || formationIndex < 0)
            return 1;

        return formations[formationIndex].rotationDirection;
    }

    // Get current rotation status as a string for debugging
    public string GetCurrentRotationStatus()
    {
        if (!enableRotation || !isInitialized) return "Stopped";

        float elapsedTime = Time.time - rotationStartTime;
        float blendFactor = Mathf.Clamp01(elapsedTime / blendInDuration);

        if (blendFactor < 1f)
        {
            return $"Blending In ({blendFactor * 100:F0}%)";
        }

        if (enableSpeedVariation && formations.Count > 1)
        {
            return "Multi-Formation Rotation";
        }

        string direction = allowRandomDirection ? "Mixed Directions" : (baseRotationSpeed >= 0 ? "Clockwise" : "Counter-Clockwise");
        return $"Rotating {direction}";
    }

    // Get individual formation status
    public string GetFormationRotationStatus(int formationIndex)
    {
        if (!enableRotation || !isInitialized || formationIndex >= formations.Count || formationIndex < 0)
            return "Stopped";

        float elapsedTime = Time.time - rotationStartTime;
        float blendFactor = Mathf.Clamp01(elapsedTime / blendInDuration);

        if (blendFactor < 1f)
        {
            return $"Blending ({blendFactor * 100:F0}%)";
        }

        RotationData data = formations[formationIndex];
        string direction = data.rotationDirection > 0 ? "CW" : "CCW";
        return $"Rotating {direction} ({data.currentRotationAngle:F1}°)";
    }

    // Handle inspector changes
    void OnValidate()
    {
        // Clamp rotation speed
        baseRotationSpeed = Mathf.Clamp(baseRotationSpeed, -360f, 360f);
        speedVariation = Mathf.Max(0f, speedVariation);

        if (Application.isPlaying && isInitialized)
        {
            // Check if enableRotation state changed
            if (enableRotation != previousEnableRotation)
            {
                if (enableRotation && !previousEnableRotation)
                {
                    StartRotation();
                }
                else if (!enableRotation && previousEnableRotation)
                {
                    StopRotation();
                }
                previousEnableRotation = enableRotation;
            }

            // Mark that we need to update formation data on next frame
            needsFormationDataUpdate = true;
        }
    }

    void OnDisable()
    {
        if (showDebugLogs && isInitialized)
        {
            Debug.Log($"FormationRotationEffect: Disabled");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FormationRotationEffect))]
public class FormationRotationEffectEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FormationRotationEffect rotationEffect = (FormationRotationEffect)target;

        GUILayout.Space(10);

        // Runtime controls
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(rotationEffect.IsRotating ? "Stop Rotation" : "Start Rotation", GUILayout.Height(30)))
            {
                rotationEffect.ToggleRotation();
            }

            if (GUILayout.Button("Reset Formation", GUILayout.Height(30)))
            {
                rotationEffect.ResetFormation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Regenerate Variations", GUILayout.Height(25)))
            {
                rotationEffect.RegenerateRotationVariations();
            }

            if (GUILayout.Button("Print Status", GUILayout.Height(25)))
            {
                rotationEffect.PrintCurrentStatus();
            }
            EditorGUILayout.EndHorizontal();

            // Interaction mode controls
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Effect Interaction", EditorStyles.boldLabel);

            FormationRotationEffect.EffectInteractionMode newMode = (FormationRotationEffect.EffectInteractionMode)EditorGUILayout.EnumPopup(
                "Interaction Mode", rotationEffect.CurrentInteractionMode);

            if (newMode != rotationEffect.CurrentInteractionMode)
            {
                rotationEffect.SetInteractionMode(newMode);
            }

            // Show current values
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Current Info", EditorStyles.boldLabel);

            FormationCreator fc = rotationEffect.GetComponent<FormationCreator>();
            if (fc != null)
            {
                EditorGUILayout.LabelField($"Formation Type: {fc.currentFormation}");
                EditorGUILayout.LabelField($"Formation Count: {fc.formationCount}");

                if (rotationEffect.SpeedVariationEnabled)
                {
                    EditorGUILayout.LabelField($"Speed Variation: ±{rotationEffect.SpeedVariation:F1}°/s");
                }

                EditorGUILayout.LabelField($"Base Speed: {rotationEffect.BaseRotationSpeed:F1}°/s");

                if (rotationEffect.IsRotating)
                {
                    EditorGUILayout.LabelField($"Status: {rotationEffect.GetCurrentRotationStatus()}");

                    // Show individual formation rotation data
                    if (rotationEffect.SpeedVariationEnabled && rotationEffect.FormationCount > 1)
                    {
                        GUILayout.Space(5);
                        EditorGUILayout.LabelField("Individual Formations", EditorStyles.boldLabel);

                        for (int i = 0; i < Mathf.Min(6, rotationEffect.FormationCount); i++)
                        {
                            float angle = rotationEffect.GetFormationRotationAngle(i);
                            float speed = rotationEffect.GetFormationRotationSpeed(i);
                            int direction = rotationEffect.GetFormationRotationDirection(i);
                            string status = rotationEffect.GetFormationRotationStatus(i);

                            EditorGUILayout.LabelField($"F{i}: {status} @ {speed:F1}°/s");
                        }

                        if (rotationEffect.FormationCount > 6)
                        {
                            EditorGUILayout.LabelField($"... and {rotationEffect.FormationCount - 6} more formations");
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Status: Stopped");
                }
            }
        }

        // Effect stacking info
        GUILayout.Space(5);
        FormationBreathingEffect breathingEffect = rotationEffect.GetComponent<FormationBreathingEffect>();
        if (breathingEffect != null)
        {
            if (breathingEffect.IsBreathing && rotationEffect.IsRotating)
            {
                EditorGUILayout.HelpBox("✓ Both breathing and rotation effects are active!\n\nTip: Adjust Script Execution Order in Project Settings to control effect layering.", MessageType.Info);
            }
            else if (breathingEffect.IsBreathing || rotationEffect.IsRotating)
            {
                string activeEffect = breathingEffect.IsBreathing ? "breathing" : "rotation";
                EditorGUILayout.HelpBox($"Currently running: {activeEffect} effect only", MessageType.None);
            }
        }

        // Validation
        GUILayout.Space(5);
        FormationCreator formationCreator = rotationEffect.GetComponent<FormationCreator>();
        if (formationCreator == null)
        {
            EditorGUILayout.HelpBox("⚠️ No FormationCreator found!", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("✓ Rotation effect ready - works independently or with other effects", MessageType.Info);

            if (rotationEffect.SpeedVariationEnabled)
            {
                EditorGUILayout.HelpBox($"✓ Each formation rotates independently with ±{rotationEffect.SpeedVariation:F1}°/s variation", MessageType.Info);
            }

            // Show interaction mode explanation
            string modeExplanation = rotationEffect.CurrentInteractionMode == FormationRotationEffect.EffectInteractionMode.ReadOriginalPositions
                ? "Reads original formation positions (effects may overwrite each other)"
                : "Reads current formation state (effects stack on top of each other)";
            EditorGUILayout.HelpBox($"Interaction: {modeExplanation}", MessageType.None);
        }
    }
}
#endif