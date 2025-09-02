using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FormationEffectManager : MonoBehaviour
{
    [Header("Effect Configurations")]
    public BreathingEffectConfig breathingConfig;
    public RotationEffectConfig rotationConfig;

    [Header("Effect Toggles")]
    public bool enableBreathing = true;
    public bool enableRotation = true;

    // Components
    private FormationCreator formationCreator;

    // Effects
    private List<IFormationEffect> effects = new List<IFormationEffect>();
    private BreathingEffect breathingEffect;
    private RotationEffect rotationEffect;

    // State
    private bool isInitialized = false;
    private float effectStartTime;
    private FormationCreator.FormationType currentFormationType;
    private bool needsEffectDataUpdate = true;

    // State tracking for toggles
    private bool previousEnableBreathing = false;
    private bool previousEnableRotation = false;

    void Awake()
    {
        formationCreator = GetComponent<FormationCreator>();
        if (formationCreator == null)
        {
            Debug.LogError("FormationEffectManager: No FormationCreator component found!");
            enabled = false;
            return;
        }

        InitializeEffects();
    }

    void Start()
    {
        if (formationCreator == null) return;

        currentFormationType = formationCreator.currentFormation;
        effectStartTime = Time.time;

        // Initialize all effects
        foreach (var effect in effects)
        {
            effect.Initialize(formationCreator.formationCount);
        }

        // Initialize toggle state tracking
        previousEnableBreathing = enableBreathing;
        previousEnableRotation = enableRotation;

        isInitialized = true;

        // Start effects if any are enabled
        if (AnyEffectActive)
        {
            StartEffects();
        }
    }

    void Update()
    {
        if (!isInitialized || formationCreator == null)
            return;

        // Handle toggle changes
        if (enableBreathing != previousEnableBreathing)
        {
            previousEnableBreathing = enableBreathing;
            if (enableBreathing)
                StartBreathing();
            else
                StopBreathing();
        }

        if (enableRotation != previousEnableRotation)
        {
            previousEnableRotation = enableRotation;
            if (enableRotation)
                StartRotation();
            else
                StopRotation();
        }

        // Handle formation type changes
        if (currentFormationType != formationCreator.currentFormation)
        {
            currentFormationType = formationCreator.currentFormation;
            needsEffectDataUpdate = true;
            Invoke("RestartEffectsAfterFormationChange", 0.1f);
        }

        // Handle effect data updates
        if (needsEffectDataUpdate)
        {
            UpdateAllEffectData();
            needsEffectDataUpdate = false;
        }

        // Update effect toggles
        UpdateEffectToggles();

        // Only update effects if any are active
        if (enableBreathing || enableRotation)
        {
            UpdateAndApplyEffects();
        }
    }

    private void InitializeEffects()
    {
        effects.Clear();

        // Create effects with their configurations
        if (breathingConfig != null)
        {
            breathingEffect = new BreathingEffect(breathingConfig);
            effects.Add(breathingEffect);
        }

        if (rotationConfig != null)
        {
            rotationEffect = new RotationEffect(rotationConfig);
            effects.Add(rotationEffect);
        }
    }

    private void UpdateEffectToggles()
    {
        if (breathingEffect != null)
            breathingEffect.IsEnabled = enableBreathing;

        if (rotationEffect != null)
            rotationEffect.IsEnabled = enableRotation;
    }

    private void UpdateAndApplyEffects()
    {
        float elapsedTime = Time.time;

        // Always generate base formation first
        formationCreator.GenerateFormation();

        // Update all effects
        foreach (var effect in effects)
        {
            effect.UpdateEffect(Time.deltaTime, elapsedTime);
        }

        // Apply effects to formations
        ApplyEffectsToFormations();
    }

    private void ApplyEffectsToFormations()
    {
        List<Vector3> baseFormation = formationCreator.Generator.GenerateFormation(formationCreator.currentFormation);
        int slotsPerFormation = baseFormation.Count;

        for (int formationIndex = 0; formationIndex < formationCreator.formationCount; formationIndex++)
        {
            // Apply each effect in sequence, recalculating center after each effect
            Vector3 centerPosition = CalculateFormationCenter(formationIndex, slotsPerFormation);

            foreach (var effect in effects)
            {
                if (effect.IsEnabled)
                {
                    effect.ApplyToFormation(formationCreator.FormationSlots, formationIndex, baseFormation, centerPosition);
                    // Recalculate center after each effect in case positions changed
                    centerPosition = CalculateFormationCenter(formationIndex, slotsPerFormation);
                }
            }
        }
    }

    private Vector3 CalculateFormationCenter(int formationIndex, int slotsPerFormation)
    {
        Vector3 centerSum = Vector3.zero;
        int startIndex = formationIndex * slotsPerFormation;
        int actualSlotCount = 0;

        for (int j = 0; j < slotsPerFormation && (startIndex + j) < formationCreator.FormationSlots.Count; j++)
        {
            centerSum += formationCreator.FormationSlots[startIndex + j];
            actualSlotCount++;
        }

        return actualSlotCount > 0 ? centerSum / actualSlotCount : Vector3.zero;
    }

    private void UpdateAllEffectData()
    {
        foreach (var effect in effects)
        {
            effect.OnFormationChanged(formationCreator.formationCount);
        }
    }

    private void RestartEffectsAfterFormationChange()
    {
        if (formationCreator.FormationSlots == null || formationCreator.FormationSlots.Count == 0)
        {
            Invoke("RestartEffectsAfterFormationChange", 0.1f);
            return;
        }

        UpdateAllEffectData();
        effectStartTime = Time.time;
    }

    // Individual effect control methods
    public void StopBreathing()
    {
        enableBreathing = false;
        if (breathingEffect != null)
            breathingEffect.IsEnabled = false;
        formationCreator.GenerateFormation(); // Reset to original formation
    }

    public void StartBreathing()
    {
        enableBreathing = true;
        if (breathingEffect != null)
            breathingEffect.IsEnabled = true;
    }

    public void StopRotation()
    {
        enableRotation = false;
        if (rotationEffect != null)
            rotationEffect.IsEnabled = false;
        formationCreator.GenerateFormation(); // Reset to original formation
    }

    public void StartRotation()
    {
        enableRotation = true;
        if (rotationEffect != null)
            rotationEffect.IsEnabled = true;
    }

    // Public control methods
    [ContextMenu("Start Effects")]
    public void StartEffects()
    {
        if (!isInitialized) return;

        effectStartTime = Time.time;
        UpdateAllEffectData();
    }

    [ContextMenu("Stop All Effects")]
    public void StopAllEffects()
    {
        enableBreathing = false;
        enableRotation = false;

        if (breathingEffect != null)
            breathingEffect.IsEnabled = false;
        if (rotationEffect != null)
            rotationEffect.IsEnabled = false;

        formationCreator.GenerateFormation();
    }

    [ContextMenu("Reset All Effects")]
    public void ResetAllEffects()
    {
        foreach (var effect in effects)
        {
            effect.Reset();
        }
        effectStartTime = Time.time;
    }

    [ContextMenu("Regenerate Effect Variations")]
    public void RegenerateEffectVariations()
    {
        needsEffectDataUpdate = true;
        if (AnyEffectActive)
        {
            StartEffects();
        }
        else
        {
            UpdateAllEffectData();
        }
    }

    [ContextMenu("Trigger Special Actions")]
    public void TriggerSpecialActions()
    {
        foreach (var effect in effects)
        {
            effect.TriggerSpecialAction();
        }
    }

    // Toggle methods
    public void ToggleBreathing()
    {
        if (enableBreathing)
            StopBreathing();
        else
            StartBreathing();
    }

    public void ToggleRotation()
    {
        if (enableRotation)
            StopRotation();
        else
            StartRotation();
    }

    // Properties
    public bool IsBreathingActive => enableBreathing && breathingEffect != null && breathingEffect.IsEnabled;
    public bool IsRotationActive => enableRotation && rotationEffect != null && rotationEffect.IsEnabled;
    public bool AnyEffectActive => IsBreathingActive || IsRotationActive;
    public int FormationCount => formationCreator?.formationCount ?? 0;

    // Effect-specific getters (delegated to individual effects)
    public float GetFormationBreathingScale(int index)
    {
        return breathingEffect?.GetFormationScale(index) ?? 1f;
    }

    public float GetFormationRotationAngle(int index)
    {
        return rotationEffect?.GetFormationRotationAngle(index) ?? 0f;
    }

    public bool IsFormationInSpinBurst(int index)
    {
        return rotationEffect?.IsFormationInSpinBurst(index) ?? false;
    }

    public float GetFormationTimeUntilNextBurst(int index)
    {
        return rotationEffect?.GetFormationTimeUntilNextBurst(index) ?? 0f;
    }

    public float GetFormationSpinBurstTimeLeft(int index)
    {
        return rotationEffect?.GetFormationSpinBurstTimeLeft(index) ?? 0f;
    }

    public string GetEffectStatus()
    {
        var activeEffects = new List<string>();

        if (enableBreathing && breathingEffect != null && breathingEffect.IsEnabled)
        {
            activeEffects.Add("Breathing");
        }

        if (enableRotation && rotationEffect != null && rotationEffect.IsEnabled)
        {
            activeEffects.Add("Rotation");
        }

        if (activeEffects.Count == 0) return "All Effects Stopped";
        if (activeEffects.Count == 1) return $"{activeEffects[0]} Active";
        return string.Join(" + ", activeEffects) + " Active";
    }

    // Add new effects at runtime
    public void AddEffect(IFormationEffect effect)
    {
        if (effect != null && !effects.Contains(effect))
        {
            effects.Add(effect);
            if (isInitialized)
            {
                effect.Initialize(formationCreator.formationCount);
            }
        }
    }

    public void RemoveEffect(IFormationEffect effect)
    {
        effects.Remove(effect);
    }

    public T GetEffect<T>() where T : class, IFormationEffect
    {
        return effects.OfType<T>().FirstOrDefault();
    }

    void OnValidate()
    {
        if (Application.isPlaying && isInitialized)
        {
            needsEffectDataUpdate = true;
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
[CustomEditor(typeof(FormationEffectManager))]
public class FormationEffectManagerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FormationEffectManager manager = (FormationEffectManager)target;

        if (!Application.isPlaying) return;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

        // Effect toggles
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(manager.IsBreathingActive ? "Stop Breathing" : "Start Breathing", GUILayout.Height(30)))
        {
            manager.ToggleBreathing();
        }

        if (GUILayout.Button(manager.IsRotationActive ? "Stop Rotation" : "Start Rotation", GUILayout.Height(30)))
        {
            manager.ToggleRotation();
        }
        EditorGUILayout.EndHorizontal();

        // Control buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Stop All Effects", GUILayout.Height(25)))
        {
            manager.StopAllEffects();
        }

        if (GUILayout.Button("Reset All Effects", GUILayout.Height(25)))
        {
            manager.ResetAllEffects();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Regenerate Variations", GUILayout.Height(25)))
        {
            manager.RegenerateEffectVariations();
        }

        if (GUILayout.Button("Trigger Special Actions", GUILayout.Height(25)))
        {
            manager.TriggerSpecialActions();
        }
        EditorGUILayout.EndHorizontal();

        // Status display
        GUILayout.Space(5);
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Effects: {manager.GetEffectStatus()}");

        if (manager.AnyEffectActive)
        {
            FormationCreator fc = manager.GetComponent<FormationCreator>();
            EditorGUILayout.LabelField($"Formation Type: {fc.currentFormation}");
            EditorGUILayout.LabelField($"Formation Count: {fc.formationCount}");

            // Show spin burst status
            if (manager.IsRotationActive && manager.rotationConfig.enableSpinBurst)
            {
                int burstsActive = 0;
                for (int i = 0; i < manager.FormationCount; i++)
                {
                    if (manager.IsFormationInSpinBurst(i)) burstsActive++;
                }
                EditorGUILayout.LabelField($"Spin Bursts Active: {burstsActive}/{manager.FormationCount}");
            }

            // Individual formation status
            if (manager.FormationCount > 1)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Individual Formations", EditorStyles.boldLabel);

                for (int i = 0; i < Mathf.Min(4, manager.FormationCount); i++)
                {
                    string status = $"F{i}: ";

                    if (manager.IsBreathingActive)
                    {
                        status += $"Scale {manager.GetFormationBreathingScale(i):F2}";
                    }

                    if (manager.IsRotationActive)
                    {
                        if (manager.IsBreathingActive) status += ", ";
                        status += $"Angle {manager.GetFormationRotationAngle(i):F1}°";

                        if (manager.rotationConfig.enableSpinBurst)
                        {
                            status += manager.IsFormationInSpinBurst(i) ?
                                $" [BURST {manager.GetFormationSpinBurstTimeLeft(i):F1}s]" :
                                $" ({manager.GetFormationTimeUntilNextBurst(i):F1}s)";
                        }
                    }

                    EditorGUILayout.LabelField(status);
                }

                if (manager.FormationCount > 4)
                {
                    EditorGUILayout.LabelField($"... and {manager.FormationCount - 4} more");
                }
            }
        }
    }
}
#endif