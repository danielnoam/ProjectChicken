using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FormationBreathingEffect : MonoBehaviour
{
    [Header("Breathing Effect Settings")]
    [Tooltip("Toggle the breathing effect on/off")]
    public bool enableBreathing = true;
    
    [Header("Breathing Range")]
    [Tooltip("Minimum scale factor (0.5 = 50% of original size)")]
    [Range(0.1f, 1.5f)]
    public float minScale = 0.7f;
    [Tooltip("Maximum scale factor (1.5 = 150% of original size)")]
    [Range(0.5f, 3f)]
    public float maxScale = 1.3f;
    
    [Header("Animation Settings")]
    [Tooltip("Base time in seconds for one complete cycle (min to max to min)")]
    [Range(1f, 20f)]
    public float baseCycleTime = 4f;
    
    [Header("Cycle Variation Settings")]
    [Tooltip("Enable individual cycle time variation per formation")]
    public bool enableCycleVariation = true;
    [Tooltip("Maximum variation to add/subtract from base cycle time (in seconds)")]
    [Range(0f, 10f)]
    public float cycleTimeVariation = 2f;
    [Tooltip("Add random phase offset to each formation")]
    public bool useRandomPhaseOffset = true;
    
    [Header("Advanced Settings")]
    [Tooltip("Use smooth sine wave instead of linear transitions")]
    public bool useSmoothCurve = true;
    [Tooltip("Start the breathing effect immediately on Start()")]
    public bool startImmediately = true;
    [Tooltip("Show debug logs for breathing effect")]
    public bool showDebugLogs = false;
    
    // Components
    private FormationCreator formationCreator;
    
    // Animation state
    private bool isInitialized = false;
    private bool previousEnableBreathing = false;
    private float breathingStartTime;
    private FormationCreator.FormationType currentFormationType;
    private float blendInDuration = 1f;
    
    // Formation tracking
    [System.Serializable]
    public class FormationData
    {
        public List<Vector3> originalSlots;
        public Vector3 centerPosition;
        public float effectiveCycleTime;
        public float phaseOffset;
        public float currentScale;
        public float startingScale;
        public int startSlotIndex;
        public int slotCount;
        
        public FormationData()
        {
            originalSlots = new List<Vector3>();
            centerPosition = Vector3.zero;
            currentScale = 1f;
            startingScale = 1f;
            startSlotIndex = 0;
            slotCount = 0;
        }
    }
    
    private List<FormationData> formations = new List<FormationData>();
    private List<Vector3> originalGlobalSlots = new List<Vector3>();
    private bool needsFormationDataUpdate = true;
    
    void Awake()
    {
        formationCreator = GetComponent<FormationCreator>();
        if (formationCreator == null)
        {
            Debug.LogError("FormationBreathingEffect: No FormationCreator component found on this GameObject!");
            enabled = false;
            return;
        }
    }
    
    void Start()
    {
        previousEnableBreathing = enableBreathing;
        currentFormationType = formationCreator.currentFormation;
        ValidateRanges();
        isInitialized = true;
        
        // Start breathing effect if enabled
        if (enableBreathing && startImmediately)
        {
            StartBreathing();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"FormationBreathingEffect: Initialized with {currentFormationType} formation");
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
        if (needsFormationDataUpdate || (formationCreator.FormationSlots != null && formationCreator.FormationSlots.Count != originalGlobalSlots.Count))
        {
            UpdateFormationData();
            needsFormationDataUpdate = false;
        }
        
        // Check if enableBreathing state changed
        if (enableBreathing != previousEnableBreathing)
        {
            if (enableBreathing && !previousEnableBreathing)
            {
                StartBreathing();
                if (showDebugLogs)
                {
                    Debug.Log("FormationBreathingEffect: Breathing toggled ON");
                }
            }
            else if (!enableBreathing && previousEnableBreathing)
            {
                StopBreathing();
                if (showDebugLogs)
                {
                    Debug.Log("FormationBreathingEffect: Breathing toggled OFF");
                }
            }
            previousEnableBreathing = enableBreathing;
        }
        
        // Only continue if breathing is enabled
        if (!enableBreathing)
            return;
            
        // Update the breathing animation
        UpdateBreathingAnimation();
    }
    
    void HandleFormationTypeChange()
    {
        FormationCreator.FormationType oldType = currentFormationType;
        currentFormationType = formationCreator.currentFormation;
        
        if (showDebugLogs)
        {
            Debug.Log($"FormationBreathingEffect: Formation type changed from {oldType} to {currentFormationType} - rebuilding data");
        }
        
        // Force complete rebuild of formation data
        formations.Clear();
        originalGlobalSlots.Clear();
        needsFormationDataUpdate = true;
        
        // If breathing was active, restart it properly
        if (enableBreathing)
        {
            // Wait for formation to be regenerated, then restart breathing
            Invoke("RestartBreathingAfterFormationChange", 0.1f);
        }
    }
    
    void RestartBreathingAfterFormationChange()
    {
        if (formationCreator.FormationSlots == null || formationCreator.FormationSlots.Count == 0)
        {
            Invoke("RestartBreathingAfterFormationChange", 0.1f); // Try again next frame
            return;
        }
        
        UpdateFormationData();
        breathingStartTime = Time.time;
        
        // Initialize starting scales for all formations
        for (int i = 0; i < formations.Count; i++)
        {
            formations[i].startingScale = 1f; // Start from normal scale
            formations[i].currentScale = 1f;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"FormationBreathingEffect: Restarted breathing for {currentFormationType} with {formations.Count} formations");
        }
    }
    
    void UpdateFormationData()
    {
        // Store original formation slots
        if (formationCreator.FormationSlots != null && formationCreator.FormationSlots.Count > 0)
        {
            originalGlobalSlots.Clear();
            originalGlobalSlots.AddRange(formationCreator.FormationSlots);
        }
        else
        {
            return; // No formation slots to work with yet
        }
        
        // Calculate current scales before clearing formation data
        List<float> previousScales = new List<float>();
        for (int i = 0; i < formations.Count; i++)
        {
            previousScales.Add(formations[i].currentScale);
        }
        
        formations.Clear();
        
        // Generate base formation to understand slot distribution
        List<Vector3> baseFormation = formationCreator.Generator.GenerateFormation(formationCreator.currentFormation);
        int slotsPerFormation = baseFormation.Count;
        int formationCount = formationCreator.formationCount;
        
        if (showDebugLogs)
        {
            Debug.Log($"FormationBreathingEffect: Updating data - FormationType: {formationCreator.currentFormation}, SlotsPerFormation: {slotsPerFormation}, FormationCount: {formationCount}, TotalSlots: {originalGlobalSlots.Count}");
        }
        
        // Create formation data for each formation
        for (int i = 0; i < formationCount; i++)
        {
            FormationData formData = new FormationData();
            
            // Calculate cycle time variation
            float variation = enableCycleVariation ? 
                Random.Range(-cycleTimeVariation, cycleTimeVariation) : 0f;
            formData.effectiveCycleTime = Mathf.Max(0.5f, baseCycleTime + variation);
            
            // Calculate phase offset
            formData.phaseOffset = useRandomPhaseOffset ? 
                Random.Range(0f, 2f * Mathf.PI) : 0f;
            
            // Initialize scales - preserve previous scale if available
            formData.currentScale = (i < previousScales.Count) ? previousScales[i] : 1f;
            formData.startingScale = formData.currentScale;
            
            // Calculate slot indices for this formation
            formData.startSlotIndex = i * slotsPerFormation;
            formData.slotCount = slotsPerFormation;
            
            // Extract original slots for this formation and calculate center
            Vector3 centerSum = Vector3.zero;
            int actualSlotCount = 0;
            
            for (int j = 0; j < slotsPerFormation && (formData.startSlotIndex + j) < originalGlobalSlots.Count; j++)
            {
                Vector3 slot = originalGlobalSlots[formData.startSlotIndex + j];
                formData.originalSlots.Add(slot);
                centerSum += slot;
                actualSlotCount++;
            }
            
            // Calculate formation center
            if (actualSlotCount > 0)
            {
                formData.centerPosition = centerSum / actualSlotCount;
                formData.slotCount = actualSlotCount; // Update with actual count
            }
            
            formations.Add(formData);
            
            if (showDebugLogs)
            {
                Debug.Log($"FormationBreathingEffect: Formation {i} - Cycle: {formData.effectiveCycleTime:F2}s, Phase: {formData.phaseOffset:F2}, StartingScale: {formData.startingScale:F2}, Slots: {formData.slotCount}, Center: {formData.centerPosition}");
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"FormationBreathingEffect: Completed data update with {formations.Count} formations");
        }
    }
    
    void UpdateBreathingAnimation()
    {
        if (formations.Count == 0 || originalGlobalSlots.Count == 0)
            return;
            
        float elapsedTime = Time.time - breathingStartTime;
        float blendFactor = Mathf.Clamp01(elapsedTime / blendInDuration);
        
        // Process each formation individually to calculate scales
        for (int formationIndex = 0; formationIndex < formations.Count; formationIndex++)
        {
            FormationData formData = formations[formationIndex];
            
            // Calculate target breathing scale for this specific formation
            float adjustedTime = elapsedTime + (formData.phaseOffset / (2f * Mathf.PI)) * formData.effectiveCycleTime;
            float cycleProgress = (adjustedTime % formData.effectiveCycleTime) / formData.effectiveCycleTime;
            
            float targetBreathingScale = CalculateBreathingScale(cycleProgress);
            
            // Blend from starting scale to breathing pattern
            float blendedScale = Mathf.Lerp(formData.startingScale, targetBreathingScale, blendFactor);
            formData.currentScale = blendedScale;
        }
        
        // Apply breathing transformations directly to FormationSlots
        ApplyBreathingTransforms();
        
        // Debug info (only occasionally to avoid spam)
        if (showDebugLogs && Time.frameCount % 120 == 0) // Every 2 seconds at 60fps
        {
            Debug.Log($"Breathing: Elapsed={elapsedTime:F1}s, BlendFactor={blendFactor:F2}, Formations={formations.Count}");
            for (int i = 0; i < Mathf.Min(3, formations.Count); i++) // Show first 3 formations
            {
                Debug.Log($"  Formation {i}: StartScale={formations[i].startingScale:F2}, CurrentScale={formations[i].currentScale:F2}");
            }
        }
    }
    
    void ApplyBreathingTransforms()
    {
        if (formations.Count == 0 || originalGlobalSlots.Count == 0)
            return;
            
        // Apply breathing scale to each formation independently
        for (int formationIndex = 0; formationIndex < formations.Count; formationIndex++)
        {
            FormationData formData = formations[formationIndex];
            
            // Apply breathing scale to this formation's slots
            for (int slotIndex = 0; slotIndex < formData.originalSlots.Count; slotIndex++)
            {
                int globalSlotIndex = formData.startSlotIndex + slotIndex;
                if (globalSlotIndex < formationCreator.FormationSlots.Count)
                {
                    Vector3 originalSlot = formData.originalSlots[slotIndex];
                    
                    // Scale relative to formation center
                    Vector3 offset = originalSlot - formData.centerPosition;
                    Vector3 scaledOffset = offset * formData.currentScale;
                    Vector3 scaledSlot = formData.centerPosition + scaledOffset;
                    
                    formationCreator.FormationSlots[globalSlotIndex] = scaledSlot;
                }
            }
        }
    }
    
    float CalculateBreathingScale(float cycleProgress)
    {
        float normalizedValue;
        
        if (useSmoothCurve)
        {
            // Simple sine wave
            float sineWave = Mathf.Sin(cycleProgress * 2f * Mathf.PI);
            normalizedValue = (sineWave + 1f) * 0.5f; // Convert from [-1,1] to [0,1]
        }
        else
        {
            // Linear breathing: 0 -> 1 -> 0
            if (cycleProgress <= 0.5f)
            {
                normalizedValue = cycleProgress * 2f; // 0 to 1
            }
            else
            {
                normalizedValue = 2f - (cycleProgress * 2f); // 1 to 0
            }
        }
        
        // Map normalized value to scale range
        return Mathf.Lerp(minScale, maxScale, normalizedValue);
    }
    
    void ValidateRanges()
    {
        // Validate scale range
        if (minScale >= maxScale)
        {
            Debug.LogWarning("FormationBreathingEffect: minScale should be less than maxScale. Swapping values.");
            float temp = minScale;
            minScale = maxScale;
            maxScale = temp;
        }
        
        if (minScale <= 0f)
        {
            Debug.LogWarning("FormationBreathingEffect: minScale must be positive. Setting to 0.1f");
            minScale = 0.1f;
        }
        
        // Validate cycle time variation
        if (cycleTimeVariation < 0f)
        {
            cycleTimeVariation = 0f;
        }
        
        if (baseCycleTime - cycleTimeVariation <= 0.5f)
        {
            cycleTimeVariation = baseCycleTime - 0.6f;
        }
    }
    
    // Public methods for external control
    [ContextMenu("Start Breathing")]
    public void StartBreathing()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("FormationBreathingEffect: Component not initialized yet.");
            return;
        }
        
        enableBreathing = true;
        previousEnableBreathing = true;
        breathingStartTime = Time.time;
        
        // Force update formation data
        UpdateFormationData();
        
        // Store current scale for each formation (for smooth blending)
        for (int i = 0; i < formations.Count; i++)
        {
            formations[i].startingScale = formations[i].currentScale; // Start from current scale
        }
        
        Debug.Log($"FormationBreathingEffect: Breathing started with {formations.Count} formations");
    }
    
    [ContextMenu("Stop Breathing")]
    public void StopBreathing()
    {
        enableBreathing = false;
        previousEnableBreathing = false;
        
        // Restore original formation slots
        if (originalGlobalSlots.Count > 0 && formationCreator.FormationSlots.Count == originalGlobalSlots.Count)
        {
            for (int i = 0; i < originalGlobalSlots.Count; i++)
            {
                formationCreator.FormationSlots[i] = originalGlobalSlots[i];
            }
        }
        
        Debug.Log($"FormationBreathingEffect: Breathing stopped - restored original formation");
    }
    
    [ContextMenu("Reset Formation")]
    public void ResetFormation()
    {
        if (formationCreator != null && isInitialized)
        {
            // Force regeneration of formation
            formationCreator.GenerateFormation();
            needsFormationDataUpdate = true;
            
            // If breathing is active, restart with new formation data
            if (enableBreathing)
            {
                StartBreathing();
            }
            
            Debug.Log($"FormationBreathingEffect: Formation reset");
        }
    }

    [ContextMenu("Regenerate Breathing Variations")]
    public void RegenerateBreathingVariations()
    {
        needsFormationDataUpdate = true;
        
        if (enableBreathing)
        {
            StartBreathing(); // This will regenerate the data
        }
        else
        {
            UpdateFormationData();
        }
        
        Debug.Log($"FormationBreathingEffect: Regenerated variations for {formations.Count} formations");
    }

    [ContextMenu("Print Current Status")]
    public void PrintCurrentStatus()
    {
        if (!isInitialized)
        {
            Debug.Log("FormationBreathingEffect: Not initialized yet");
            return;
        }
        
        Debug.Log($"=== BREATHING EFFECT STATUS ===");
        Debug.Log($"Breathing Enabled: {enableBreathing}");
        Debug.Log($"Formation Type: {currentFormationType}");
        Debug.Log($"Formation Count: {formations.Count}");
        Debug.Log($"Base Cycle Time: {baseCycleTime:F1}s");
        Debug.Log($"Cycle Variation: {(enableCycleVariation ? "Enabled" : "Disabled")} (±{cycleTimeVariation:F1}s)");
        Debug.Log($"Scale Range: {minScale:F2} to {maxScale:F2}");
        
        // Show individual formation data
        if (enableBreathing && formations.Count > 0)
        {
            Debug.Log("=== INDIVIDUAL FORMATION DATA ===");
            for (int i = 0; i < formations.Count; i++)
            {
                FormationData data = formations[i];
                float progress = GetFormationCycleProgress(i);
                string phase = GetFormationPhase(i);
                Debug.Log($"Formation {i}: Cycle={data.effectiveCycleTime:F1}s, Scale={data.currentScale:F2}, Progress={progress * 100:F1}%, Phase={phase}, Slots={data.slotCount}");
            }
        }
    }

    [ContextMenu("Toggle Debug Logs")]
    public void ToggleDebugLogs()
    {
        showDebugLogs = !showDebugLogs;
        Debug.Log($"FormationBreathingEffect: Debug logs {(showDebugLogs ? "enabled" : "disabled")}");
    }
    
    // Toggle breathing effect
    public void ToggleBreathing()
    {
        if (enableBreathing)
        {
            StopBreathing();
        }
        else
        {
            StartBreathing();
        }
    }
    
    // Set new scale range
    public void SetScaleRange(float newMinScale, float newMaxScale)
    {
        minScale = newMinScale;
        maxScale = newMaxScale;
        ValidateRanges();
        
        Debug.Log($"FormationBreathingEffect: Scale range set to {minScale:F2} - {maxScale:F2}");
    }
    
    // Set cycle time
    public void SetCycleTime(float newCycleTime)
    {
        float oldCycleTime = baseCycleTime;
        baseCycleTime = Mathf.Clamp(newCycleTime, 1f, 20f);
        ValidateRanges();
        
        // Update all formation data proportionally
        float timeScale = baseCycleTime / oldCycleTime;
        for (int i = 0; i < formations.Count; i++)
        {
            formations[i].effectiveCycleTime *= timeScale;
        }
        
        Debug.Log($"FormationBreathingEffect: Base cycle time set to {baseCycleTime} seconds");
    }
    
    // Set cycle variation settings
    public void SetCycleVariation(bool enabled, float variation)
    {
        enableCycleVariation = enabled;
        cycleTimeVariation = Mathf.Max(0f, variation);
        ValidateRanges();
        
        RegenerateBreathingVariations();
        
        Debug.Log($"FormationBreathingEffect: Cycle variation {(enabled ? "enabled" : "disabled")} with ±{cycleTimeVariation:F1}s variation");
    }
    
    // Properties for external access
    public bool IsBreathing => enableBreathing;
    public bool CycleVariationEnabled => enableCycleVariation;
    public float CycleTimeVariation => cycleTimeVariation;
    public int FormationCount => formations.Count;
    public float MinScale => minScale;
    public float MaxScale => maxScale;
    
    // Get the current progress through the base breathing cycle (0 to 1)
    public float CycleProgress 
    { 
        get 
        {
            if (!enableBreathing || !isInitialized) return 0f;
            float elapsedTime = Time.time - breathingStartTime;
            return (elapsedTime % baseCycleTime) / baseCycleTime;
        }
    }
    
    // Get individual formation progress
    public float GetFormationCycleProgress(int formationIndex)
    {
        if (!enableBreathing || !isInitialized || formationIndex >= formations.Count || formationIndex < 0)
            return 0f;
            
        FormationData data = formations[formationIndex];
        float elapsedTime = Time.time - breathingStartTime;
        float adjustedTime = elapsedTime + (data.phaseOffset / (2f * Mathf.PI)) * data.effectiveCycleTime;
        return (adjustedTime % data.effectiveCycleTime) / data.effectiveCycleTime;
    }
    
    // Get blend factor (how much we've transitioned from starting scale to breathing pattern)
    public float BlendFactor
    {
        get
        {
            if (!enableBreathing || !isInitialized) return 0f;
            float elapsedTime = Time.time - breathingStartTime;
            return Mathf.Clamp01(elapsedTime / blendInDuration);
        }
    }
    
    // Get individual formation scale
    public float GetFormationScale(int formationIndex)
    {
        if (!enableBreathing || !isInitialized || formationIndex >= formations.Count || formationIndex < 0)
            return 1f;
            
        return formations[formationIndex].currentScale;
    }
    
    // Get individual formation effective cycle time
    public float GetFormationCycleTime(int formationIndex)
    {
        if (formationIndex >= formations.Count || formationIndex < 0)
            return baseCycleTime;
            
        return formations[formationIndex].effectiveCycleTime;
    }
    
    // Get current breathing phase as a string for debugging
    public string GetCurrentPhase()
    {
        if (!enableBreathing || !isInitialized) return "Stopped";
        
        float blendFactor = BlendFactor;
        if (blendFactor < 1f)
        {
            return $"Blending In ({blendFactor * 100:F0}%)";
        }
        
        if (enableCycleVariation && formations.Count > 1)
        {
            return "Multi-Formation Breathing";
        }
        
        float progress = CycleProgress;
        
        if (useSmoothCurve)
        {
            float cosWave = Mathf.Cos(progress * 2f * Mathf.PI);
            if (cosWave > 0)
            {
                return "Expanding (Smooth)";
            }
            else
            {
                return "Contracting (Smooth)";
            }
        }
        else
        {
            if (progress <= 0.5f)
                return "Expanding (Linear)";
            else
                return "Contracting (Linear)";
        }
    }
    
    // Get individual formation phase
    public string GetFormationPhase(int formationIndex)
    {
        if (!enableBreathing || !isInitialized || formationIndex >= formations.Count || formationIndex < 0)
            return "Stopped";
        
        float blendFactor = BlendFactor;
        if (blendFactor < 1f)
        {
            return $"Blending ({blendFactor * 100:F0}%)";
        }
            
        float progress = GetFormationCycleProgress(formationIndex);
        
        if (useSmoothCurve)
        {
            float cosWave = Mathf.Cos(progress * 2f * Mathf.PI);
            if (cosWave > 0)
            {
                return "Expanding";
            }
            else
            {
                return "Contracting";
            }
        }
        else
        {
            if (progress <= 0.5f)
                return "Expanding";
            else
                return "Contracting";
        }
    }
    
    // Handle inspector changes
    void OnValidate()
    {
        ValidateRanges();
        
        if (Application.isPlaying && isInitialized)
        {
            // Check if enableBreathing state changed
            if (enableBreathing != previousEnableBreathing)
            {
                if (enableBreathing && !previousEnableBreathing)
                {
                    StartBreathing();
                }
                else if (!enableBreathing && previousEnableBreathing)
                {
                    StopBreathing();
                }
                previousEnableBreathing = enableBreathing;
            }
            
            // Mark that we need to update formation data on next frame
            needsFormationDataUpdate = true;
        }
    }
    
    void OnDisable()
    {
        if (showDebugLogs && isInitialized)
        {
            Debug.Log($"FormationBreathingEffect: Disabled");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FormationBreathingEffect))]
public class FormationBreathingEffectEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        FormationBreathingEffect breathingEffect = (FormationBreathingEffect)target;
        
        GUILayout.Space(10);
        
        // Runtime controls
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(breathingEffect.IsBreathing ? "Stop Breathing" : "Start Breathing", GUILayout.Height(30)))
            {
                breathingEffect.ToggleBreathing();
            }
            
            if (GUILayout.Button("Reset Formation", GUILayout.Height(30)))
            {
                breathingEffect.ResetFormation();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Regenerate Variations", GUILayout.Height(25)))
            {
                breathingEffect.RegenerateBreathingVariations();
            }
            
            if (GUILayout.Button("Print Status", GUILayout.Height(25)))
            {
                breathingEffect.PrintCurrentStatus();
            }
            EditorGUILayout.EndHorizontal();
            
            // Show current values
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Current Info", EditorStyles.boldLabel);
            
            FormationCreator fc = breathingEffect.GetComponent<FormationCreator>();
            if (fc != null)
            {
                EditorGUILayout.LabelField($"Formation Type: {fc.currentFormation}");
                EditorGUILayout.LabelField($"Formation Count: {fc.formationCount}");
                
                if (breathingEffect.CycleVariationEnabled)
                {
                    EditorGUILayout.LabelField($"Cycle Variation: ±{breathingEffect.CycleTimeVariation:F1}s");
                }
                
                EditorGUILayout.LabelField($"Scale Range: {breathingEffect.MinScale:F2} - {breathingEffect.MaxScale:F2}");
                
                if (breathingEffect.IsBreathing)
                {
                    EditorGUILayout.LabelField($"Phase: {breathingEffect.GetCurrentPhase()}");
                    EditorGUILayout.LabelField($"Cycle Progress: {breathingEffect.CycleProgress * 100:F1}%");
                    EditorGUILayout.LabelField($"Blend Factor: {breathingEffect.BlendFactor * 100:F1}%");
                    
                    // Show progress bars
                    Rect progressRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(progressRect, breathingEffect.CycleProgress, "Breathing Cycle");
                    
                    Rect blendRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(blendRect, breathingEffect.BlendFactor, "Blend Factor");
                    
                    // Show individual formation progress
                    if (breathingEffect.CycleVariationEnabled && breathingEffect.FormationCount > 1)
                    {
                        GUILayout.Space(5);
                        EditorGUILayout.LabelField("Individual Formations", EditorStyles.boldLabel);
                        
                        for (int i = 0; i < Mathf.Min(6, breathingEffect.FormationCount); i++)
                        {
                            float progress = breathingEffect.GetFormationCycleProgress(i);
                            float scale = breathingEffect.GetFormationScale(i);
                            string phase = breathingEffect.GetFormationPhase(i);
                            
                            Rect formationRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
                            EditorGUI.ProgressBar(formationRect, progress, $"F{i}: {phase} (Scale: {scale:F2})");
                        }
                        
                        if (breathingEffect.FormationCount > 6)
                        {
                            EditorGUILayout.LabelField($"... and {breathingEffect.FormationCount - 6} more formations");
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Status: Stopped");
                }
            }
        }
        
        // Validation
        GUILayout.Space(5);
        FormationCreator formationCreator = breathingEffect.GetComponent<FormationCreator>();
        if (formationCreator == null)
        {
            EditorGUILayout.HelpBox("⚠️ No FormationCreator found!", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("✓ Breathing effect ready - works independently", MessageType.Info);
            
            if (breathingEffect.CycleVariationEnabled)
            {
                EditorGUILayout.HelpBox($"✓ Each formation breathes independently with ±{breathingEffect.CycleTimeVariation:F1}s variation", MessageType.Info);
            }
        }
    }
}
#endif