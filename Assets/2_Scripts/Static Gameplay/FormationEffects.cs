/*using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FormationEffects : MonoBehaviour
{
    [Header("Effect Toggles")]
    public bool enableBreathing = true;
    public bool enableRotation = true;

    [Header("Breathing Settings")]
    [Range(0.1f, 1.5f)]
    public float breathingMinScale = 0.7f;
    [Range(0.5f, 3f)]
    public float breathingMaxScale = 1.3f;
    [Range(1f, 20f)]
    public float breathingCycleTime = 4f;
    public bool breathingUseSmoothCurve = true;

    [Header("Breathing Variation")]
    public bool breathingEnableCycleVariation = true;
    [Range(0f, 10f)]
    public float breathingCycleVariation = 2f;
    public bool breathingUseRandomPhase = true;

    [Header("Rotation Settings")]
    [Range(-360f, 360f)]
    public float rotationSpeed = 45f;

    [Header("Rotation Variation")]
    public bool rotationEnableSpeedVariation = true;
    [Range(0f, 180f)]
    public float rotationSpeedVariation = 20f;
    public bool rotationAllowRandomDirection = true;
    public bool rotationUseRandomStartingAngle = true;

    [Header("Spin Burst Settings")]
    public bool enableSpinBurst = true;
    [Range(1f, 30f)]
    public float spinBurstMinInterval = 3f;
    [Range(1f, 30f)]
    public float spinBurstMaxInterval = 6f;
    [Range(0.5f, 5f)]
    public float spinBurstDuration = 1f;
    [Range(1f, 10f)]
    public float spinBurstSpeedMultiplier = 3f;

    // Components
    private FormationCreator formationCreator;

    // State
    private bool isInitialized = false;
    private bool previousEnableBreathing = false;
    private bool previousEnableRotation = false;
    private float effectStartTime;
    private FormationCreator.FormationType currentFormationType;

    // Formation effect data
    [System.Serializable]
    public class FormationEffectData
    {
        // Breathing
        public float breathingCycleTime;
        public float breathingPhaseOffset;
        public float currentBreathingScale;

        // Rotation
        public float rotationSpeed;
        public float startingRotationAngle;
        public float currentRotationAngle;
        public int rotationDirection;

        // Spin burst
        public float spinBurstTimer;
        public float spinBurstTimeLeft;
        public bool isInSpinBurst;
        public float nextBurstInterval;

        // Formation data
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
            Debug.LogError("FormationEffects: No FormationCreator component found!");
            enabled = false;
        }
    }

    void Start()
    {
        previousEnableBreathing = enableBreathing;
        previousEnableRotation = enableRotation;
        currentFormationType = formationCreator.currentFormation;
        ValidateSettings();
        isInitialized = true;

        if ((enableBreathing || enableRotation))
        {
            StartEffects();
        }
    }

    void Update()
    {
        if (!isInitialized || formationCreator == null)
            return;

        // Handle formation type changes
        if (currentFormationType != formationCreator.currentFormation)
        {
            currentFormationType = formationCreator.currentFormation;
            formationEffects.Clear();
            needsFormationDataUpdate = true;

            if (enableBreathing || enableRotation)
            {
                Invoke("RestartEffectsAfterFormationChange", 0.1f);
            }
        }

        // Handle effect data updates
        if (needsFormationDataUpdate)
        {
            UpdateFormationEffectData();
            needsFormationDataUpdate = false;
        }

        // Handle effect state changes
        if (enableBreathing != previousEnableBreathing || enableRotation != previousEnableRotation)
        {
            previousEnableBreathing = enableBreathing;
            previousEnableRotation = enableRotation;
        }

        // Update effects
        if (enableBreathing || enableRotation)
        {
            UpdateEffects();
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
    }

    void UpdateFormationEffectData()
    {
        formationEffects.Clear();
        int formationCount = formationCreator.formationCount;

        for (int i = 0; i < formationCount; i++)
        {
            FormationEffectData effectData = new FormationEffectData();

            // Breathing setup
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

            // Rotation setup
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

            // Spin burst setup
            if (enableSpinBurst)
            {
                effectData.nextBurstInterval = Random.Range(spinBurstMinInterval, spinBurstMaxInterval);
                effectData.spinBurstTimer = effectData.nextBurstInterval;
            }

            formationEffects.Add(effectData);
        }
    }

    void UpdateEffects()
    {
        if (formationEffects.Count == 0)
            return;

        float elapsedTime = Time.time - effectStartTime;
        float blendFactor = Mathf.Clamp01(elapsedTime);

        // Generate base formation
        formationCreator.GenerateFormation();

        // Apply breathing to individual formations
        if (enableBreathing)
        {
            ApplyBreathingEffect(elapsedTime, blendFactor);
        }

        // Update rotation base data and apply rotation
        if (enableRotation)
        {
            UpdateRotationBaseData();
            ApplyRotationEffect(elapsedTime, blendFactor);
        }
    }

    void ApplyBreathingEffect(float elapsedTime, float blendFactor)
    {
        // Calculate individual breathing scales
        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];

            float adjustedTime = elapsedTime + (data.breathingPhaseOffset / (2f * Mathf.PI)) * data.breathingCycleTime;
            float cycleProgress = (adjustedTime % data.breathingCycleTime) / data.breathingCycleTime;
            float targetScale = CalculateBreathingScale(cycleProgress);

            data.currentBreathingScale = Mathf.Lerp(1f, targetScale, blendFactor);
        }

        // Apply breathing scaling to positions
        ApplyBreathingToPositions();
    }

    void ApplyBreathingToPositions()
    {
        List<Vector3> baseFormation = formationCreator.Generator.GenerateFormation(formationCreator.currentFormation);
        int slotsPerFormation = baseFormation.Count;

        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];

            // Calculate formation center
            Vector3 centerSum = Vector3.zero;
            int startIndex = i * slotsPerFormation;
            int actualSlotCount = 0;

            for (int j = 0; j < slotsPerFormation && (startIndex + j) < formationCreator.FormationSlots.Count; j++)
            {
                centerSum += formationCreator.FormationSlots[startIndex + j];
                actualSlotCount++;
            }

            if (actualSlotCount == 0) continue;
            Vector3 formationCenter = centerSum / actualSlotCount;

            // Apply breathing scale
            for (int j = 0; j < slotsPerFormation && (startIndex + j) < formationCreator.FormationSlots.Count; j++)
            {
                int globalSlotIndex = startIndex + j;
                Vector3 originalSlot = formationCreator.FormationSlots[globalSlotIndex];
                Vector3 offset = originalSlot - formationCenter;
                Vector3 scaledSlot = formationCenter + (offset * data.currentBreathingScale);
                formationCreator.FormationSlots[globalSlotIndex] = scaledSlot;
            }
        }
    }

    void UpdateRotationBaseData()
    {
        List<Vector3> baseFormation = formationCreator.Generator.GenerateFormation(formationCreator.currentFormation);
        int slotsPerFormation = baseFormation.Count;

        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];
            data.startSlotIndex = i * slotsPerFormation;
            data.slotCount = slotsPerFormation;
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

        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];

            // Update spin burst timing
            if (enableSpinBurst)
            {
                UpdateSpinBurstTiming(data, deltaTime);
            }

            // Calculate effective rotation speed
            float effectiveSpeed = data.rotationSpeed;
            if (data.isInSpinBurst)
            {
                effectiveSpeed *= spinBurstSpeedMultiplier;
            }

            // Update rotation angle
            float angleIncrement = effectiveSpeed * data.rotationDirection * deltaTime;
            data.currentRotationAngle += angleIncrement;

            // Normalize angle
            data.currentRotationAngle = data.currentRotationAngle % 360f;
            if (data.currentRotationAngle < 0f)
                data.currentRotationAngle += 360f;
        }

        ApplyRotationTransforms();
    }

    void UpdateSpinBurstTiming(FormationEffectData data, float deltaTime)
    {
        if (data.isInSpinBurst)
        {
            data.spinBurstTimeLeft -= deltaTime;
            if (data.spinBurstTimeLeft <= 0f)
            {
                // End spin burst
                data.isInSpinBurst = false;

                // Reroll new rotation speed with variation
                if (rotationEnableSpeedVariation)
                {
                    float variation = Random.Range(-rotationSpeedVariation, rotationSpeedVariation);
                    data.rotationSpeed = rotationSpeed + variation;
                }
                else
                {
                    data.rotationSpeed = rotationSpeed;
                }

                // 50% chance to switch direction
                if (Random.value < 0.5f)
                {
                    data.rotationDirection *= -1;
                }

                // Schedule next burst
                data.nextBurstInterval = Random.Range(spinBurstMinInterval, spinBurstMaxInterval);
                data.spinBurstTimer = data.nextBurstInterval;
            }
        }
        else
        {
            data.spinBurstTimer -= deltaTime;
            if (data.spinBurstTimer <= 0f)
            {
                data.isInSpinBurst = true;
                data.spinBurstTimeLeft = spinBurstDuration;
            }
        }
    }

    void ApplyRotationTransforms()
    {
        for (int i = 0; i < formationEffects.Count; i++)
        {
            FormationEffectData data = formationEffects[i];
            float angleInRadians = data.currentRotationAngle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleInRadians);
            float sin = Mathf.Sin(angleInRadians);

            for (int slotIndex = 0; slotIndex < data.baseSlots.Count; slotIndex++)
            {
                int globalSlotIndex = data.startSlotIndex + slotIndex;
                if (globalSlotIndex < formationCreator.FormationSlots.Count)
                {
                    Vector3 baseSlot = data.baseSlots[slotIndex];
                    Vector3 offset = baseSlot - data.centerPosition;

                    Vector3 rotatedOffset = new Vector3(
                        offset.x * cos - offset.y * sin,
                        offset.x * sin + offset.y * cos,
                        offset.z
                    );

                    formationCreator.FormationSlots[globalSlotIndex] = data.centerPosition + rotatedOffset;
                }
            }
        }
    }

    float CalculateBreathingScale(float cycleProgress)
    {
        float normalizedValue;

        if (breathingUseSmoothCurve)
        {
            float sineWave = Mathf.Sin(cycleProgress * 2f * Mathf.PI);
            normalizedValue = (sineWave + 1f) * 0.5f;
        }
        else
        {
            normalizedValue = cycleProgress <= 0.5f ?
                cycleProgress * 2f : 2f - (cycleProgress * 2f);
        }

        return Mathf.Lerp(breathingMinScale, breathingMaxScale, normalizedValue);
    }

    void ValidateSettings()
    {
        // Breathing validation
        if (breathingMinScale >= breathingMaxScale)
        {
            float temp = breathingMinScale;
            breathingMinScale = breathingMaxScale;
            breathingMaxScale = temp;
        }

        breathingMinScale = Mathf.Max(0.1f, breathingMinScale);
        breathingCycleVariation = Mathf.Max(0f, breathingCycleVariation);

        if (breathingCycleTime - breathingCycleVariation <= 0.5f)
        {
            breathingCycleVariation = breathingCycleTime - 0.6f;
        }

        // Rotation validation
        rotationSpeed = Mathf.Clamp(rotationSpeed, -360f, 360f);
        rotationSpeedVariation = Mathf.Max(0f, rotationSpeedVariation);

        // Spin burst validation
        if (spinBurstMinInterval > spinBurstMaxInterval)
        {
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
        if (!isInitialized) return;

        effectStartTime = Time.time;
        UpdateFormationEffectData();
    }

    [ContextMenu("Stop All Effects")]
    public void StopAllEffects()
    {
        enableBreathing = false;
        enableRotation = false;
        previousEnableBreathing = false;
        previousEnableRotation = false;
        formationCreator.GenerateFormation();
    }

    [ContextMenu("Regenerate Variations")]
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
    }

    [ContextMenu("Trigger All Spin Bursts")]
    public void TriggerAllSpinBursts()
    {
        if (!enableSpinBurst || !enableRotation) return;

        foreach (var data in formationEffects)
        {
            data.isInSpinBurst = true;
            data.spinBurstTimeLeft = spinBurstDuration;
            data.spinBurstTimer = 0f;
        }
    }

    // Toggle methods
    public void ToggleBreathing() => enableBreathing = !enableBreathing;
    public void ToggleRotation() => enableRotation = !enableRotation;

    // Properties
    public bool IsBreathingActive => enableBreathing;
    public bool IsRotationActive => enableRotation;
    public bool AnyEffectActive => enableBreathing || enableRotation;
    public int FormationCount => formationEffects.Count;

    public float GetFormationBreathingScale(int index)
    {
        return (enableBreathing && index >= 0 && index < formationEffects.Count) ?
            formationEffects[index].currentBreathingScale : 1f;
    }

    public float GetFormationRotationAngle(int index)
    {
        return (enableRotation && index >= 0 && index < formationEffects.Count) ?
            formationEffects[index].currentRotationAngle : 0f;
    }

    public bool IsFormationInSpinBurst(int index)
    {
        return enableSpinBurst && enableRotation && index >= 0 && index < formationEffects.Count &&
            formationEffects[index].isInSpinBurst;
    }

    public float GetFormationTimeUntilNextBurst(int index)
    {
        return (enableSpinBurst && enableRotation && index >= 0 && index < formationEffects.Count) ?
            formationEffects[index].spinBurstTimer : 0f;
    }

    public float GetFormationSpinBurstTimeLeft(int index)
    {
        return (enableSpinBurst && enableRotation && index >= 0 && index < formationEffects.Count) ?
            formationEffects[index].spinBurstTimeLeft : 0f;
    }

    public string GetEffectStatus()
    {
        if (!enableBreathing && !enableRotation) return "All Effects Stopped";
        if (enableBreathing && enableRotation) return "Breathing + Rotation Active";
        return enableBreathing ? "Breathing Active" : "Rotation Active";
    }

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
        if (formationCreator != null)
        {
            formationCreator.GenerateFormation();
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

        if (!Application.isPlaying) return;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

        // Effect toggles
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

        if (GUILayout.Button("Regenerate Variations", GUILayout.Height(25)))
        {
            effects.RegenerateEffectVariations();
        }
        EditorGUILayout.EndHorizontal();

        // Spin burst control
        if (effects.enableSpinBurst && effects.IsRotationActive)
        {
            if (GUILayout.Button("Trigger All Spin Bursts", GUILayout.Height(25)))
            {
                effects.TriggerAllSpinBursts();
            }
        }

        // Status display
        GUILayout.Space(5);
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Effects: {effects.GetEffectStatus()}");

        if (effects.AnyEffectActive)
        {
            FormationCreator fc = effects.GetComponent<FormationCreator>();
            EditorGUILayout.LabelField($"Formation Type: {fc.currentFormation}");
            EditorGUILayout.LabelField($"Formation Count: {fc.formationCount}");

            // Show spin burst status
            if (effects.enableSpinBurst && effects.IsRotationActive)
            {
                int burstsActive = 0;
                for (int i = 0; i < effects.FormationCount; i++)
                {
                    if (effects.IsFormationInSpinBurst(i)) burstsActive++;
                }
                EditorGUILayout.LabelField($"Spin Bursts Active: {burstsActive}/{effects.FormationCount}");
            }

            // Individual formation status
            if (effects.FormationCount > 1)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Individual Formations", EditorStyles.boldLabel);

                for (int i = 0; i < Mathf.Min(4, effects.FormationCount); i++)
                {
                    string status = $"F{i}: ";

                    if (effects.IsBreathingActive)
                    {
                        status += $"Scale {effects.GetFormationBreathingScale(i):F2}";
                    }

                    if (effects.IsRotationActive)
                    {
                        if (effects.IsBreathingActive) status += ", ";
                        status += $"Angle {effects.GetFormationRotationAngle(i):F1}°";

                        if (effects.enableSpinBurst)
                        {
                            status += effects.IsFormationInSpinBurst(i) ?
                                $" [BURST {effects.GetFormationSpinBurstTimeLeft(i):F1}s]" :
                                $" ({effects.GetFormationTimeUntilNextBurst(i):F1}s)";
                        }
                    }

                    EditorGUILayout.LabelField(status);
                }

                if (effects.FormationCount > 4)
                {
                    EditorGUILayout.LabelField($"... and {effects.FormationCount - 4} more");
                }
            }
        }
    }
}
#endif*/