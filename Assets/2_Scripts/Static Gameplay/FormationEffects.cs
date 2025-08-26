using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FormationEffects : MonoBehaviour
{
    [Header("Effect Toggles")]
    [Tooltip("Enable/disable breathing effect")]
    public bool enableBreathing = true;
    [Tooltip("Enable/disable rotation effect")]
    public bool enableRotation = true;

    [Header("Breathing Settings")]
    [Tooltip("Minimum scale factor for breathing")]
    [Range(0.1f, 1.5f)]
    public float breathingMinScale = 0.7f;
    [Tooltip("Maximum scale factor for breathing")]
    [Range(0.5f, 3f)]
    public float breathingMaxScale = 1.3f;
    [Tooltip("Time for one complete breathing cycle (seconds)")]
    [Range(1f, 20f)]
    public float breathingCycleTime = 4f;
    [Tooltip("Use smooth sine wave for breathing")]
    public bool breathingUseSmoothCurve = true;

    [Header("Breathing Variation")]
    [Tooltip("Enable individual cycle time variation per formation")]
    public bool breathingEnableCycleVariation = true;
    [Tooltip("Maximum variation for breathing cycle times")]
    [Range(0f, 10f)]
    public float breathingCycleVariation = 2f;
    [Tooltip("Random phase offset for breathing")]
    public bool breathingUseRandomPhase = true;

    [Header("Rotation Settings")]
    [Tooltip("Base rotation speed in degrees per second")]
    [Range(-360f, 360f)]
    public float rotationSpeed = 45f;

    [Header("Rotation Variation")]
    [Tooltip("Enable individual rotation speed variation per formation")]
    public bool rotationEnableSpeedVariation = true;
    [Tooltip("Maximum variation for rotation speeds")]
    [Range(0f, 180f)]
    public float rotationSpeedVariation = 20f;
    [Tooltip("Allow formations to rotate in different directions")]
    public bool rotationAllowRandomDirection = true;
    [Tooltip("Random starting rotation offset")]
    public bool rotationUseRandomStartingAngle = true;

    [Header("Spin Burst Settings")]
    [Tooltip("Enable random spin burst effects")]
    public bool enableSpinBurst = true;
    [Tooltip("Minimum seconds between spin bursts")]
    [Range(1f, 30f)]
    public float spinBurstMinInterval = 3f;
    [Tooltip("Maximum seconds between spin bursts")]
    [Range(1f, 30f)]
    public float spinBurstMaxInterval = 6f;
    [Tooltip("Duration of each spin burst in seconds")]
    [Range(0.5f, 5f)]
    public float spinBurstDuration = 1f;
    [Tooltip("Speed multiplier during spin burst")]
    [Range(1f, 10f)]
    public float spinBurstSpeedMultiplier = 3f;

    [Header("General Settings")]
    [Tooltip("Start effects immediately when enabled")]
    public bool startImmediately = true;
    [Tooltip("Show debug logs")]
    public bool showDebugLogs = false;

    // Components
    private FormationCreator formationCreator;

    // State tracking
    private bool isInitialized = false;
    private bool previousEnableBreathing = false;
    private bool previousEnableRotation = false;
    private float effectStartTime;
    private FormationCreator.FormationType currentFormationType;
    private float blendInDuration = 1f;

    // Original values storage
    private float originalSpacing;
    private float originalCircleRadius;
    private bool hasStoredOriginalValues = false;

    // Formation effect data
    [System.Serializable]
    public class FormationEffectData
    {
        // Breathing data
        public float breathingCycleTime;
        public float breathingPhaseOffset;
        public float currentBreathingScale;

        // Rotation data
        public float rotationSpeed;
        public float startingRotationAngle;
        public float currentRotationAngle;
        public int rotationDirection; // 1 for clockwise, -1 for counter-clockwise

        // Spin burst data
        public float spinBurstTimer;        // Time until next burst
        public float spinBurstTimeLeft;     // Time remaining in current burst
        public bool isInSpinBurst;          // Currently in a spin burst
        public float nextBurstInterval;     // How long until next burst (randomized)

        // Base formation data for rotation
        public List<Vector3> baseSlots;
        public Vector3 centerPosition;
        public int startSlotIndex;
        public int slotCount;

        public FormationEffectData()
        {
            breathingCycleTime = 4f;
            breathingPhaseOffset = 0f;
            currentBreathingScale = 1f;
            rotationSpeed = 45f;
            startingRotationAngle = 0f;
            currentRotationAngle = 0f;
            rotationDirection = 1;
            spinBurstTimer = 0f;
            spinBurstTimeLeft = 0f;
            isInSpinBurst = false;
            nextBurstInterval = 5f;
            baseSlots = new List<Vector3>();
            centerPosition = Vector3.zero;
            startSlotIndex = 0;
            slotCount = 0;
        }
    }

    private List<FormationEffectData> formationEffects = new List<FormationEffectData>();
    private bool needsFormationDataUpdate = true;

    void Awake()
    {
        formationCreator = GetComponent<FormationCreator>();
        if (formationCreator == null)
        {
            Debug.LogError("FormationEffects: No FormationCreator component found on this GameObject!");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        previousEnableBreathing = enableBreathing;
        previousEnableRotation = enableRotation;
        currentFormationType = formationCreator.currentFormation;

        ValidateSettings();

        isInitialized = true;

        // Start effects if enabled
        if ((enableBreathing || enableRotation) && startImmediately)
        {
            StartEffects();
        }

        if (showDebugLogs)
        {
            Debug.Log($"FormationEffects: Initialized with {currentFormationType} formation - Breathing: {enableBreathing}, Rotation: {enableRotation}");
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
        if (needsFormationDataUpdate)
        {
            UpdateFormationEffectData();
            needsFormationDataUpdate = false;
        }

        // Check if effect states changed
        bool breathingStateChanged = enableBreathing != previousEnableBreathing;
        bool rotationStateChanged = enableRotation != previousEnableRotation;

        if (breathingStateChanged || rotationStateChanged)
        {
            HandleEffectStateChanges();
            previousEnableBreathing = enableBreathing;
            previousEnableRotation = enableRotation;
        }

        // Only continue if at least one effect is enabled
        if (!enableBreathing && !enableRotation)
            return;

        // Update effects
        UpdateEffects();
    }

    void StoreOriginalValues()
    {
        if (!hasStoredOriginalValues)
        {
            originalSpacing = formationCreator.spacing;
            originalCircleRadius = formationCreator.circleRadius;
            hasStoredOriginalValues = true;

            if (showDebugLogs)
            {
                Debug.Log($"FormationEffects: Stored original values - Spacing: {originalSpacing}, Radius: {originalCircleRadius}");
            }
        }
    }

    void HandleFormationTypeChange()
    {
        FormationCreator.FormationType oldType = currentFormationType;
        currentFormationType = formationCreator.currentFormation;

        if (showDebugLogs)
        {
            Debug.Log($"FormationEffects: Formation type changed from {oldType} to {currentFormationType}");
        }

        // Rebuild effect data
        formationEffects.Clear();
        needsFormationDataUpdate = true;

        // Restart effects if any are active
        if (enableBreathing || enableRotation)
        {
            Invoke("RestartEffectsAfterFormationChange", 0.1f);
        }
    }

    void RestartEffectsAfterFormationChange()
    {
        if (formationCreator.FormationSlots == null || formationCreator.FormationSlots.Count == 0)
        {
            Invoke("RestartEffectsAfterFormationChange", 0.1f);
            return;
        }

        UpdateFormationEffectData();
        effectStartTime = Time.time;

        if (showDebugLogs)
        {
            Debug.Log($"FormationEffects: Restarted effects for {currentFormationType} with {formationEffects.Count} formations");
        }
    }

    void UpdateFormationEffectData()
    {
        formationEffects.Clear();

        int formationCount = formationCreator.formationCount;

        // Create effect data for each formation
        for (int i = 0; i < formationCount; i++)
        {
            FormationEffectData effectData = new FormationEffectData();

            // Breathing variation
            if (breathingEnableCycleVariation)
            {
                float variation = Random.Range(-breathingCycleVariation, breathingCycleVariation);
                effectData.breathingCycleTime = Mathf.Max(0.5f, breathingCycleTime + variation);
            }
            else
            {
                effectData.breathingCycleTime = breathingCycleTime;
            }

            effectData.breathingPhaseOffset = breathingUseRandomPhase ?
                Random.Range(0f, 2f * Mathf.PI) : 0f;
            effectData.currentBreathingScale = 1f;

            // Rotation variation
            if (rotationEnableSpeedVariation)
            {
                float variation = Random.Range(-rotationSpeedVariation, rotationSpeedVariation);
                effectData.rotationSpeed = rotationSpeed + variation;
            }
            else
            {
                effectData.rotationSpeed = rotationSpeed;
            }

            effectData.startingRotationAngle = rotationUseRandomStartingAngle ?
                Random.Range(0f, 360f) : 0f;
            effectData.currentRotationAngle = effectData.startingRotationAngle;
            effectData.rotationDirection = (rotationAllowRandomDirection && Random.value > 0.5f) ? -1 : 1;

            // Spin burst initialization
            if (enableSpinBurst)
            {
                // Random initial timer for first burst
                effectData.nextBurstInterval = Random.Range(spinBurstMinInterval, spinBurstMaxInterval);
                effectData.spinBurstTimer = effectData.nextBurstInterval;
                effectData.spinBurstTimeLeft = 0f;
                effectData.isInSpinBurst = false;
            }
            else
            {
                effectData.spinBurstTimer = 0f;
                effectData.spinBurstTimeLeft = 0f;
                effectData.isInSpinBurst = false;
            }

            formationEffects.Add(effectData);

            if (showDebugLogs)
            {
                string burstInfo = enableSpinBurst ? $", NextBurst: {effectData.nextBurstInterval:F1}s" : "";
                Debug.Log($"FormationEffects: Formation {i} - Breathing: {effectData.breathingCycleTime:F1}s cycle, Rotation: {effectData.rotationSpeed:F1}°/s {(effectData.rotationDirection > 0 ? "CW" : "CCW")}{burstInfo}");
            }
        }
    }

    void HandleEffectStateChanges()
    {
        bool anyEffectActive = enableBreathing || enableRotation;
        bool wasAnyEffectActive = previousEnableBreathing || previousEnableRotation;

        if (anyEffectActive && !wasAnyEffectActive)
        {
            // Starting effects
            StartEffects();
        }
        else if (!anyEffectActive && wasAnyEffectActive)
        {
            // Stopping all effects
            StopAllEffects();
        }
        else if (anyEffectActive)
        {
            // Some effects still active, just log the change
            if (showDebugLogs)
            {
                Debug.Log($"FormationEffects: Effect states changed - Breathing: {enableBreathing}, Rotation: {enableRotation}");
            }
        }
    }

    void UpdateEffects()
    {
        if (formationEffects.Count == 0)
            return;

        float elapsedTime = Time.time - effectStartTime;
        float blendFactor = Mathf.Clamp01(elapsedTime / blendInDuration);

        // Step 1: Always generate fresh base formation (with original parameters)
        formationCreator.GenerateFormation();

        // Step 2: Apply breathing effect by scaling individual formations
        if (enableBreathing)
        {
            ApplyBreathingEffect(elapsedTime, blendFactor);
            ApplyBreathingToPositions(blendFactor);
        }

        // Step 3: Update rotation base data from current positions (after breathing)
        if (enableRotation)
        {
            UpdateRotationBaseData();
        }

        // Step 4: Apply rotation effect to current positions
        if (enableRotation)
        {
            ApplyRotationEffect(elapsedTime, blendFactor);
        }

        // Debug info (only occasionally to avoid spam)
        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"FormationEffects: Elapsed={elapsedTime:F1}s, Blend={blendFactor:F2}, Breathing={enableBreathing}, Rotation={enableRotation}");
        }
    }

    void ApplyBreathingToPositions(float blendFactor)
    {
        // Apply individual breathing scales to formation positions
        List<Vector3> baseFormation = formationCreator.Generator.GenerateFormation(formationCreator.currentFormation);
        int slotsPerFormation = baseFormation.Count;

        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];

            // Calculate formation center from current positions
            Vector3 centerSum = Vector3.zero;
            int actualSlotCount = 0;
            int startIndex = i * slotsPerFormation;

            for (int j = 0; j < slotsPerFormation && (startIndex + j) < formationCreator.FormationSlots.Count; j++)
            {
                centerSum += formationCreator.FormationSlots[startIndex + j];
                actualSlotCount++;
            }

            if (actualSlotCount == 0) continue;
            Vector3 formationCenter = centerSum / actualSlotCount;

            // Apply breathing scale to this formation's slots
            for (int j = 0; j < slotsPerFormation && (startIndex + j) < formationCreator.FormationSlots.Count; j++)
            {
                int globalSlotIndex = startIndex + j;
                Vector3 originalSlot = formationCreator.FormationSlots[globalSlotIndex];

                // Scale around formation center
                Vector3 offset = originalSlot - formationCenter;
                Vector3 scaledOffset = offset * data.currentBreathingScale;
                Vector3 scaledSlot = formationCenter + scaledOffset;

                formationCreator.FormationSlots[globalSlotIndex] = scaledSlot;
            }
        }
    }

    void ApplyBreathingEffect(float elapsedTime, float blendFactor)
    {
        // Calculate individual breathing scales for each formation
        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];

            // Calculate breathing cycle progress for this formation
            float adjustedTime = elapsedTime + (data.breathingPhaseOffset / (2f * Mathf.PI)) * data.breathingCycleTime;
            float cycleProgress = (adjustedTime % data.breathingCycleTime) / data.breathingCycleTime;

            // Calculate target breathing scale
            float targetScale = CalculateBreathingScale(cycleProgress);

            // Blend from 1.0 to breathing pattern
            float blendedScale = Mathf.Lerp(1f, targetScale, blendFactor);
            data.currentBreathingScale = blendedScale;
        }

        // Don't modify formation parameters - we'll scale positions directly after generation
    }

    void ApplyScaleToFormationParameters(float scale)
    {
        switch (currentFormationType)
        {
            case FormationCreator.FormationType.Square:
            case FormationCreator.FormationType.Triangle:
            case FormationCreator.FormationType.VShape:
                formationCreator.spacing = originalSpacing * scale;
                break;

            case FormationCreator.FormationType.Circle:
                formationCreator.circleRadius = originalCircleRadius * scale;
                break;
        }

        // Force boundary manager to recalculate effective values
        if (formationCreator.BoundaryManager != null)
        {
            formationCreator.BoundaryManager.CalculateEffectiveValues();
        }
    }

    void UpdateRotationBaseData()
    {
        // Update base slots and centers for rotation from newly generated formation
        List<Vector3> baseFormation = formationCreator.Generator.GenerateFormation(formationCreator.currentFormation);
        int slotsPerFormation = baseFormation.Count;

        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];

            // Calculate slot indices for this formation
            data.startSlotIndex = i * slotsPerFormation;
            data.slotCount = slotsPerFormation;

            // Extract current slots for this formation and calculate center
            data.baseSlots.Clear();
            Vector3 centerSum = Vector3.zero;
            int actualSlotCount = 0;

            for (int j = 0; j < slotsPerFormation && (data.startSlotIndex + j) < formationCreator.FormationSlots.Count; j++)
            {
                Vector3 slot = formationCreator.FormationSlots[data.startSlotIndex + j];
                data.baseSlots.Add(slot);
                centerSum += slot;
                actualSlotCount++;
            }

            // Update formation center and slot count
            if (actualSlotCount > 0)
            {
                data.centerPosition = centerSum / actualSlotCount;
                data.slotCount = actualSlotCount;
            }
        }
    }

    void ApplyRotationEffect(float elapsedTime, float blendFactor)
    {
        float deltaTime = Time.deltaTime;

        // Update rotation angles and spin burst timers for each formation
        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];

            // Update spin burst timing if enabled
            if (enableSpinBurst && enableRotation)
            {
                UpdateSpinBurstTiming(data, deltaTime);
            }

            // Calculate effective rotation speed (with burst multiplier if active)
            float effectiveSpeed = data.rotationSpeed;
            if (data.isInSpinBurst)
            {
                effectiveSpeed *= spinBurstSpeedMultiplier;
            }

            // Calculate target rotation angle for this formation
            float angleIncrement = effectiveSpeed * data.rotationDirection * deltaTime;
            data.currentRotationAngle += angleIncrement;

            // Keep angle in 0-360 range for cleaner debugging
            while (data.currentRotationAngle >= 360f)
                data.currentRotationAngle -= 360f;
            while (data.currentRotationAngle < 0f)
                data.currentRotationAngle += 360f;
        }

        // Apply rotation transformations to FormationSlots
        ApplyRotationTransforms();
    }

    void UpdateSpinBurstTiming(FormationEffectData data, float deltaTime)
    {
        if (data.isInSpinBurst)
        {
            // Currently in a spin burst - count down the burst duration
            data.spinBurstTimeLeft -= deltaTime;

            if (data.spinBurstTimeLeft <= 0f)
            {
                // End spin burst and schedule next one
                data.isInSpinBurst = false;
                data.spinBurstTimeLeft = 0f;

                // Reroll random interval for next burst
                data.nextBurstInterval = Random.Range(spinBurstMinInterval, spinBurstMaxInterval);
                data.spinBurstTimer = data.nextBurstInterval;

                if (showDebugLogs)
                {
                    Debug.Log($"FormationEffects: Formation spin burst ended, next in {data.nextBurstInterval:F1}s");
                }
            }
        }
        else
        {
            // Not in burst - count down until next burst
            data.spinBurstTimer -= deltaTime;

            if (data.spinBurstTimer <= 0f)
            {
                // Start spin burst
                data.isInSpinBurst = true;
                data.spinBurstTimeLeft = spinBurstDuration;
                data.spinBurstTimer = 0f;

                if (showDebugLogs)
                {
                    Debug.Log($"FormationEffects: Formation spin burst started! Duration: {spinBurstDuration:F1}s at {spinBurstSpeedMultiplier}x speed");
                }
            }
        }
    }

    void ApplyRotationTransforms()
    {
        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];

            // Convert angle to radians
            float angleInRadians = data.currentRotationAngle * Mathf.Deg2Rad;

            // Apply rotation to this formation's slots
            for (int slotIndex = 0; slotIndex < data.baseSlots.Count; slotIndex++)
            {
                int globalSlotIndex = data.startSlotIndex + slotIndex;
                if (globalSlotIndex < formationCreator.FormationSlots.Count)
                {
                    Vector3 baseSlot = data.baseSlots[slotIndex];

                    // Calculate offset from formation center
                    Vector3 offset = baseSlot - data.centerPosition;

                    // Apply 2D rotation around formation center
                    float cos = Mathf.Cos(angleInRadians);
                    float sin = Mathf.Sin(angleInRadians);

                    Vector3 rotatedOffset = new Vector3(
                        offset.x * cos - offset.y * sin,
                        offset.x * sin + offset.y * cos,
                        offset.z
                    );

                    Vector3 rotatedSlot = data.centerPosition + rotatedOffset;
                    formationCreator.FormationSlots[globalSlotIndex] = rotatedSlot;
                }
            }
        }
    }

    float CalculateBreathingScale(float cycleProgress)
    {
        float normalizedValue;

        if (breathingUseSmoothCurve)
        {
            // Sine wave
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

        return Mathf.Lerp(breathingMinScale, breathingMaxScale, normalizedValue);
    }

    void RestoreOriginalParameters()
    {
        if (hasStoredOriginalValues)
        {
            formationCreator.spacing = originalSpacing;
            formationCreator.circleRadius = originalCircleRadius;

            if (formationCreator.BoundaryManager != null)
            {
                formationCreator.BoundaryManager.CalculateEffectiveValues();
            }
        }
    }

    void ValidateSettings()
    {
        // Validate breathing settings
        if (breathingMinScale >= breathingMaxScale)
        {
            Debug.LogWarning("FormationEffects: breathingMinScale should be less than breathingMaxScale. Swapping values.");
            float temp = breathingMinScale;
            breathingMinScale = breathingMaxScale;
            breathingMaxScale = temp;
        }

        if (breathingMinScale <= 0f)
        {
            breathingMinScale = 0.1f;
        }

        // Validate cycle variation
        if (breathingCycleVariation < 0f)
        {
            breathingCycleVariation = 0f;
        }

        if (breathingCycleTime - breathingCycleVariation <= 0.5f)
        {
            breathingCycleVariation = breathingCycleTime - 0.6f;
        }

        // Validate rotation settings
        rotationSpeed = Mathf.Clamp(rotationSpeed, -360f, 360f);
        rotationSpeedVariation = Mathf.Max(0f, rotationSpeedVariation);

        // Validate spin burst settings
        if (spinBurstMinInterval > spinBurstMaxInterval)
        {
            Debug.LogWarning("FormationEffects: spinBurstMinInterval should be less than spinBurstMaxInterval. Swapping values.");
            float temp = spinBurstMinInterval;
            spinBurstMinInterval = spinBurstMaxInterval;
            spinBurstMaxInterval = temp;
        }

        spinBurstMinInterval = Mathf.Max(1f, spinBurstMinInterval);
        spinBurstMaxInterval = Mathf.Max(spinBurstMinInterval, spinBurstMaxInterval);
        spinBurstDuration = Mathf.Clamp(spinBurstDuration, 0.5f, 5f);
        spinBurstSpeedMultiplier = Mathf.Clamp(spinBurstSpeedMultiplier, 1f, 10f);
    }

    // Public control methods
    [ContextMenu("Start Effects")]
    public void StartEffects()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("FormationEffects: Component not initialized yet.");
            return;
        }

        effectStartTime = Time.time;

        // Update formation data
        UpdateFormationEffectData();

        Debug.Log($"FormationEffects: Started - Breathing: {enableBreathing}, Rotation: {enableRotation}");
    }

    [ContextMenu("Stop All Effects")]
    public void StopAllEffects()
    {
        enableBreathing = false;
        enableRotation = false;
        previousEnableBreathing = false;
        previousEnableRotation = false;

        // Regenerate formation with original positions (no effects applied)
        formationCreator.GenerateFormation();

        Debug.Log($"FormationEffects: All effects stopped - restored original formation");
    }

    [ContextMenu("Reset Formation")]
    public void ResetFormation()
    {
        if (formationCreator != null && isInitialized)
        {
            // Store original values from current state
            StoreOriginalValues();

            // Force regeneration
            formationCreator.GenerateFormation();
            needsFormationDataUpdate = true;

            // Restart effects if any are active
            if (enableBreathing || enableRotation)
            {
                StartEffects();
            }

            Debug.Log($"FormationEffects: Formation reset");
        }
    }

    [ContextMenu("Regenerate Effect Variations")]
    public void RegenerateEffectVariations()
    {
        needsFormationDataUpdate = true;

        if (enableBreathing || enableRotation)
        {
            StartEffects();
        }
        else
        {
            UpdateFormationEffectData();
        }

        Debug.Log($"FormationEffects: Regenerated variations for {formationEffects.Count} formations");
    }

    [ContextMenu("Print Current Status")]
    public void PrintCurrentStatus()
    {
        if (!isInitialized)
        {
            Debug.Log("FormationEffects: Not initialized yet");
            return;
        }

        Debug.Log($"=== FORMATION EFFECTS STATUS ===");
        Debug.Log($"Breathing Enabled: {enableBreathing}");
        Debug.Log($"Rotation Enabled: {enableRotation}");
        Debug.Log($"Spin Burst Enabled: {enableSpinBurst}");
        Debug.Log($"Formation Type: {currentFormationType}");
        Debug.Log($"Formation Count: {formationEffects.Count}");

        if (enableBreathing)
        {
            Debug.Log($"Breathing - Cycle: {breathingCycleTime:F1}s, Scale: {breathingMinScale:F2}-{breathingMaxScale:F2}");
        }

        if (enableRotation)
        {
            Debug.Log($"Rotation - Speed: {rotationSpeed:F1}°/s, Variation: ±{rotationSpeedVariation:F1}°/s");

            if (enableSpinBurst)
            {
                Debug.Log($"Spin Burst - Interval: {spinBurstMinInterval:F1}-{spinBurstMaxInterval:F1}s, Duration: {spinBurstDuration:F1}s, Multiplier: {spinBurstSpeedMultiplier}x");
            }
        }

        // Show individual formation data
        if ((enableBreathing || enableRotation) && formationEffects.Count > 0)
        {
            Debug.Log("=== INDIVIDUAL FORMATION DATA ===");
            for (int i = 0; i < formationEffects.Count; i++)
            {
                FormationEffectData data = formationEffects[i];
                string status = "";

                if (enableBreathing)
                {
                    status += $"Breathing: {data.currentBreathingScale:F2} ";
                }

                if (enableRotation)
                {
                    string burstStatus = data.isInSpinBurst ? $" [BURST {data.spinBurstTimeLeft:F1}s]" : $" (Next: {data.spinBurstTimer:F1}s)";
                    status += $"Rotation: {data.currentRotationAngle:F1}°{burstStatus} ";
                }

                Debug.Log($"Formation {i}: {status}");
            }
        }
    }

    [ContextMenu("Trigger All Spin Bursts")]
    public void TriggerAllSpinBursts()
    {
        if (!enableSpinBurst || !enableRotation)
        {
            Debug.Log("FormationEffects: Spin burst or rotation not enabled");
            return;
        }

        foreach (var data in formationEffects)
        {
            data.isInSpinBurst = true;
            data.spinBurstTimeLeft = spinBurstDuration;
            data.spinBurstTimer = 0f;
        }

        Debug.Log($"FormationEffects: Triggered spin burst on all {formationEffects.Count} formations");
    }

    // Toggle methods
    public void ToggleBreathing()
    {
        enableBreathing = !enableBreathing;
        Debug.Log($"FormationEffects: Breathing {(enableBreathing ? "enabled" : "disabled")}");
    }

    public void ToggleRotation()
    {
        enableRotation = !enableRotation;
        Debug.Log($"FormationEffects: Rotation {(enableRotation ? "enabled" : "disabled")}");
    }

    // Set breathing parameters
    public void SetBreathingScale(float minScale, float maxScale)
    {
        breathingMinScale = minScale;
        breathingMaxScale = maxScale;
        ValidateSettings();
        Debug.Log($"FormationEffects: Breathing scale set to {breathingMinScale:F2} - {breathingMaxScale:F2}");
    }

    public void SetBreathingCycleTime(float cycleTime)
    {
        breathingCycleTime = Mathf.Clamp(cycleTime, 1f, 20f);
        ValidateSettings();
        Debug.Log($"FormationEffects: Breathing cycle time set to {breathingCycleTime:F1}s");
    }

    // Set rotation parameters
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Clamp(speed, -360f, 360f);
        Debug.Log($"FormationEffects: Rotation speed set to {rotationSpeed:F1}°/s");
    }

    // Properties for external access
    public bool IsBreathingActive => enableBreathing;
    public bool IsRotationActive => enableRotation;
    public bool AnyEffectActive => enableBreathing || enableRotation;
    public int FormationCount => formationEffects.Count;

    // Get current breathing scale (average across formations)
    public float CurrentBreathingScale
    {
        get
        {
            if (!enableBreathing || formationEffects.Count == 0) return 1f;

            float totalScale = 0f;
            foreach (var data in formationEffects)
            {
                totalScale += data.currentBreathingScale;
            }
            return totalScale / formationEffects.Count;
        }
    }

    // Get formation-specific values
    public float GetFormationBreathingScale(int formationIndex)
    {
        if (!enableBreathing || formationIndex >= formationEffects.Count || formationIndex < 0)
            return 1f;
        return formationEffects[formationIndex].currentBreathingScale;
    }

    public float GetFormationRotationAngle(int formationIndex)
    {
        if (!enableRotation || formationIndex >= formationEffects.Count || formationIndex < 0)
            return 0f;
        return formationEffects[formationIndex].currentRotationAngle;
    }

    public string GetEffectStatus()
    {
        if (!enableBreathing && !enableRotation) return "All Effects Stopped";
        if (enableBreathing && enableRotation) return "Breathing + Rotation Active";
        if (enableBreathing) return "Breathing Active";
        if (enableRotation) return "Rotation Active";
        return "Unknown";
    }

    // Handle inspector changes
    void OnValidate()
    {
        ValidateSettings();

        if (Application.isPlaying && isInitialized)
        {
            needsFormationDataUpdate = true;
        }
    }

    void OnDisable()
    {
        // Regenerate formation without effects when disabled
        if (formationCreator != null)
        {
            formationCreator.GenerateFormation();
        }

        if (showDebugLogs && isInitialized)
        {
            Debug.Log($"FormationEffects: Disabled and restored original formation");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FormationEffects))]
public class FormationEffectsEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FormationEffects effects = (FormationEffects)target;

        GUILayout.Space(10);

        // Runtime controls
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            // Effect toggle buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(effects.IsBreathingActive ? "Stop Breathing" : "Start Breathing", GUILayout.Height(30)))
            {
                effects.ToggleBreathing();
            }

            if (GUILayout.Button(effects.IsRotationActive ? "Stop Rotation" : "Start Rotation", GUILayout.Height(30)))
            {
                effects.ToggleRotation();
            }
            EditorGUILayout.EndHorizontal();

            // Control buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop All Effects", GUILayout.Height(25)))
            {
                effects.StopAllEffects();
            }

            if (GUILayout.Button("Reset Formation", GUILayout.Height(25)))
            {
                effects.ResetFormation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Regenerate Variations", GUILayout.Height(25)))
            {
                effects.RegenerateEffectVariations();
            }

            if (GUILayout.Button("Print Status", GUILayout.Height(25)))
            {
                effects.PrintCurrentStatus();
            }
            EditorGUILayout.EndHorizontal();

            // Spin burst controls
            if (effects.enableSpinBurst && effects.IsRotationActive)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Trigger All Spin Bursts", GUILayout.Height(25)))
                {
                    effects.TriggerAllSpinBursts();
                }
                EditorGUILayout.EndHorizontal();
            }

            // Show current status
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);

            FormationCreator fc = effects.GetComponent<FormationCreator>();
            if (fc != null)
            {
                EditorGUILayout.LabelField($"Formation Type: {fc.currentFormation}");
                EditorGUILayout.LabelField($"Formation Count: {fc.formationCount}");
                EditorGUILayout.LabelField($"Effect Status: {effects.GetEffectStatus()}");

                if (effects.IsBreathingActive)
                {
                    EditorGUILayout.LabelField($"Current Breathing Scale: {effects.CurrentBreathingScale:F2}");
                }

                // Show individual formation data
                if (effects.AnyEffectActive && effects.FormationCount > 1)
                {
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Individual Formations", EditorStyles.boldLabel);

                    for (int i = 0; i < Mathf.Min(4, effects.FormationCount); i++)
                    {
                        string formationStatus = $"F{i}: ";

                        if (effects.IsBreathingActive)
                        {
                            formationStatus += $"Scale {effects.GetFormationBreathingScale(i):F2}";
                        }

                        if (effects.IsRotationActive)
                        {
                            if (effects.IsBreathingActive) formationStatus += ", ";
                            formationStatus += $"Angle {effects.GetFormationRotationAngle(i):F1}°";
                        }

                        EditorGUILayout.LabelField(formationStatus);
                    }

                    if (effects.FormationCount > 4)
                    {
                        EditorGUILayout.LabelField($"... and {effects.FormationCount - 4} more formations");
                    }
                }
            }
        }

        // Validation and info
        GUILayout.Space(5);
        FormationCreator formationCreator = effects.GetComponent<FormationCreator>();
        if (formationCreator == null)
        {
            EditorGUILayout.HelpBox("⚠️ No FormationCreator found!", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("✓ Formation Effects ready - unified breathing and rotation system", MessageType.Info);

            string effectInfo = "";
            if (effects.IsBreathingActive && effects.IsRotationActive)
            {
                effectInfo = "Both effects active: formations will breathe and rotate simultaneously";
            }
            else if (effects.IsBreathingActive)
            {
                effectInfo = "Breathing active: formations change size based on parameters";
            }
            else if (effects.IsRotationActive)
            {
                effectInfo = "Rotation active: formations spin around their centers";
            }
            else
            {
                effectInfo = "No effects active";
            }

            if (!string.IsNullOrEmpty(effectInfo))
            {
                EditorGUILayout.HelpBox(effectInfo, MessageType.None);
            }
        }

        // Usage instructions
        if (Application.isPlaying)
        {
            GUILayout.Space(5);
            EditorGUILayout.HelpBox("Runtime Controls:\n• Toggle individual effects with buttons above\n• Modify settings in real-time in the inspector\n• Use context menu for additional controls", MessageType.None);
        }
    }
}
#endif