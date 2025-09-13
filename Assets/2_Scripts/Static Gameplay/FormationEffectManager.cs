using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using KBCore.Refs;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FormationEffectManager : MonoBehaviour
{
    [Header("Effect Configurations")]
    public BreathingEffectConfig breathingConfig;
    public RotationEffectConfig rotationConfig;

    [Header("Manual Effect Toggles")]
    [Tooltip("Manual overrides - these will be ignored when stage-based activation is enabled")]
    public bool enableBreathing = false;
    public bool enableRotation = false;

    [Header("Stage-Based Effect Activation")]
    [SerializeField] private bool useStageBasedActivation = true;
    [SerializeField, Range(0f, 1f)] private float activationThreshold = 0.9f; // 90% registration threshold
    [SerializeField, Range(0f, 1f)] private float effectActivationChance = 0.7f; // Chance to activate any effects
    [SerializeField] private bool showStageDebugLogs = true;
    [SerializeField, Range(1f, 10f)] private float minEffectDuration = 2f; // Minimum time effects must stay active

    [Header("References")]
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private ChickenCombatManagerV4 combatManager;

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

    // Stage-based activation state
    private bool hasRolledForCurrentStage = false;
    private bool stageEffectsActive = false;
    private int expectedChickensForStage = 0;
    private bool isTrackingStageRegistration = false;
    private float effectActivationTime = 0f; // Timestamp when effects were activated
    private int lastRegisteredCount = 0; // Track registration changes

    // State tracking for toggles (only used when manual control is enabled)
    private bool previousEnableBreathing = false;
    private bool previousEnableRotation = false;

    // Properties for inspector display
    [Header("Stage Activation Status (Read Only)")]
    [SerializeField] private bool currentStageHasRolled = false;
    [SerializeField] private bool currentStageEffectsActive = false;
    [SerializeField] private int registeredChickens = 0;
    [SerializeField] private int expectedChickens = 0;
    [SerializeField] private float registrationProgress = 0f;

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

        // Find references if not assigned
        if (!levelManager) levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (!combatManager) combatManager = FindFirstObjectByType<ChickenCombatManagerV4>(FindObjectsInactive.Include);

        currentFormationType = formationCreator.currentFormation;
        effectStartTime = Time.time;

        // Initialize all effects
        foreach (var effect in effects)
        {
            effect.Initialize(formationCreator.formationCount);
        }

        // Initialize toggle state tracking (only relevant for manual mode)
        previousEnableBreathing = enableBreathing;
        previousEnableRotation = enableRotation;

        isInitialized = true;

        // Start effects based on mode
        if (useStageBasedActivation)
        {
            // In stage-based mode, effects start disabled
            SetEffectsEnabled(false, false);
        }
        else if (AnyEffectActive)
        {
            // In manual mode, start effects if any are enabled
            StartEffects();
        }
    }

    void OnEnable()
    {
        // Subscribe to LevelManager events
        if (levelManager)
        {
            levelManager.OnStageChanged += OnStageChanged;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from LevelManager events
        if (levelManager)
        {
            levelManager.OnStageChanged -= OnStageChanged;
        }
        if (formationCreator != null)
        {
            formationCreator.GenerateFormation();
        }
    }

    void Update()
    {
        if (!isInitialized || formationCreator == null)
            return;

        // Update inspector display values
        UpdateInspectorValues();

        // Handle stage-based activation
        if (useStageBasedActivation)
        {
            UpdateStageBasedActivation();
        }
        else
        {
            // Handle manual toggle changes (only in manual mode)
            HandleManualToggleChanges();
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
        if (GetCurrentBreathingState() || GetCurrentRotationState())
        {
            UpdateAndApplyEffects();
        }
    }

    // Event handler for stage changes
    private void OnStageChanged(SOLevelStage newStage)
    {
        if (!useStageBasedActivation) return;

        if (showStageDebugLogs)
            Debug.Log($"FormationEffectManager: Stage changed to {newStage?.name}");

        // Turn off all effects when stage changes
        SetEffectsEnabled(false, false);
        stageEffectsActive = false;
        effectActivationTime = 0f;
        lastRegisteredCount = 0;

        // Reset stage tracking
        hasRolledForCurrentStage = false;
        isTrackingStageRegistration = false;

        // If this is an enemy wave stage, start tracking for effect activation
        if (newStage && newStage.StageType == StageType.EnemyWave)
        {
            var formation = newStage.FormationStageData;
            if (formation != null)
            {
                expectedChickensForStage = formation.NumberOfSlots * formation.FormationCount;
                isTrackingStageRegistration = true;

                if (showStageDebugLogs)
                    Debug.Log($"FormationEffectManager: Started tracking registration for {expectedChickensForStage} expected chickens");
            }
        }
    }

    private void UpdateInspectorValues()
    {
        currentStageHasRolled = hasRolledForCurrentStage;
        currentStageEffectsActive = stageEffectsActive;
        registeredChickens = combatManager ? combatManager.TotalCombatChickens : 0;
        expectedChickens = expectedChickensForStage;
        registrationProgress = expectedChickensForStage > 0 ? (float)registeredChickens / expectedChickensForStage : 0f;
    }

    private void UpdateStageBasedActivation()
    {
        if (!isTrackingStageRegistration || hasRolledForCurrentStage) return;
        if (combatManager == null) return;

        // Check if we've reached the threshold
        float progress = expectedChickensForStage > 0 ? (float)combatManager.TotalCombatChickens / expectedChickensForStage : 0f;

        if (progress >= activationThreshold)
        {
            // Time to roll for effect activation
            RollForEffectActivation();
            hasRolledForCurrentStage = true;
            isTrackingStageRegistration = false;
        }
    }

    private void RollForEffectActivation()
    {
        if (showStageDebugLogs)
            Debug.Log($"FormationEffectManager: Rolling for effect activation (threshold reached: {registrationProgress:P1})");

        // Roll to see if we activate any effects
        float activationRoll = Random.Range(0f, 1f);
        
        if (activationRoll > effectActivationChance)
        {
            if (showStageDebugLogs)
                Debug.Log($"FormationEffectManager: No effects activated this stage (rolled {activationRoll:F2} > {effectActivationChance:F2})");
            return;
        }

        // Determine which effects to activate (equal odds for each option)
        int effectChoice = Random.Range(0, 3); // 0=breathing, 1=rotation, 2=both

        bool activateBreathing = false;
        bool activateRotation = false;

        switch (effectChoice)
        {
            case 0:
                activateBreathing = true;
                if (showStageDebugLogs)
                    Debug.Log("FormationEffectManager: Activated BREATHING effect for this stage");
                break;
            case 1:
                activateRotation = true;
                if (showStageDebugLogs)
                    Debug.Log("FormationEffectManager: Activated ROTATION effect for this stage");
                break;
            case 2:
                activateBreathing = true;
                activateRotation = true;
                if (showStageDebugLogs)
                    Debug.Log("FormationEffectManager: Activated BOTH BREATHING and ROTATION effects for this stage");
                break;
        }

        // Apply the selected effects
        SetEffectsEnabled(activateBreathing, activateRotation);
        stageEffectsActive = activateBreathing || activateRotation;
        effectActivationTime = Time.time; // Record when effects were activated

        if (stageEffectsActive)
        {
            StartEffects();
        }
    }

    // Check if all enemies are dead and turn off effects if needed
    private void CheckForStageCompletion()
    {
        if (!useStageBasedActivation || !stageEffectsActive) return;
        if (combatManager == null) return;

        // Don't check for completion too soon after effect activation
        if (effectActivationTime > 0f && Time.time - effectActivationTime < minEffectDuration)
            return;

        // Get current chicken count
        int currentChickens = combatManager.TotalCombatChickens;
        
        // Track registration changes to detect actual elimination vs temporary drops
        if (currentChickens != lastRegisteredCount)
        {
            lastRegisteredCount = currentChickens;
            
            if (showStageDebugLogs && Time.frameCount % 60 == 0) // Log occasionally
                Debug.Log($"FormationEffectManager: Chicken count changed to {currentChickens}");
        }

        // Add additional validation - only disable if we're confident the stage is complete
        if (currentChickens == 0)
        {
            // Wait a bit more to make sure this isn't a temporary state
            if (effectActivationTime > 0f && Time.time - effectActivationTime < minEffectDuration + 1f)
            {
                if (showStageDebugLogs && Time.frameCount % 60 == 0) // Log once per second
                    Debug.Log($"FormationEffectManager: Detected 0 chickens, waiting for confirmation (effects active for {Time.time - effectActivationTime:F1}s)");
                return;
            }

            if (showStageDebugLogs)
                Debug.Log($"FormationEffectManager: All enemies eliminated confirmed after {Time.time - effectActivationTime:F1}s, turning off stage effects");

            SetEffectsEnabled(false, false);
            stageEffectsActive = false;
            effectActivationTime = 0f;
        }
    }

    private void HandleManualToggleChanges()
    {
        // Handle toggle changes (only in manual mode)
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
    }

    private void SetEffectsEnabled(bool breathing, bool rotation)
    {
        if (useStageBasedActivation)
        {
            // In stage-based mode, we directly control the effect states
            if (breathingEffect != null)
                breathingEffect.IsEnabled = breathing;
            if (rotationEffect != null)
                rotationEffect.IsEnabled = rotation;
        }
        else
        {
            // In manual mode, update the public toggles
            enableBreathing = breathing;
            enableRotation = rotation;
        }
    }

    private bool GetCurrentBreathingState()
    {
        if (useStageBasedActivation)
            return breathingEffect != null && breathingEffect.IsEnabled;
        else
            return enableBreathing && breathingEffect != null && breathingEffect.IsEnabled;
    }

    private bool GetCurrentRotationState()
    {
        if (useStageBasedActivation)
            return rotationEffect != null && rotationEffect.IsEnabled;
        else
            return enableRotation && rotationEffect != null && rotationEffect.IsEnabled;
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
        if (useStageBasedActivation)
        {
            // In stage-based mode, effects are controlled by the stage system
            // The public toggles are ignored
            return;
        }

        // In manual mode, use the public toggles
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

        // Check for stage completion in stage-based mode
        if (useStageBasedActivation)
        {
            CheckForStageCompletion();
        }
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

    // Individual effect control methods (for manual mode)
    public void StopBreathing()
    {
        if (useStageBasedActivation) return; // Ignore in stage-based mode
        
        enableBreathing = false;
        if (breathingEffect != null)
            breathingEffect.IsEnabled = false;
        formationCreator.GenerateFormation(); // Reset to original formation
    }

    public void StartBreathing()
    {
        if (useStageBasedActivation) return; // Ignore in stage-based mode
        
        enableBreathing = true;
        if (breathingEffect != null)
            breathingEffect.IsEnabled = true;
    }

    public void StopRotation()
    {
        if (useStageBasedActivation) return; // Ignore in stage-based mode
        
        enableRotation = false;
        if (rotationEffect != null)
            rotationEffect.IsEnabled = false;
        formationCreator.GenerateFormation(); // Reset to original formation
    }

    public void StartRotation()
    {
        if (useStageBasedActivation) return; // Ignore in stage-based mode
        
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
        if (useStageBasedActivation)
        {
            SetEffectsEnabled(false, false);
            stageEffectsActive = false;
            effectActivationTime = 0f;
        }
        else
        {
            enableBreathing = false;
            enableRotation = false;
        }

        if (breathingEffect != null)
            breathingEffect.IsEnabled = false;
        if (rotationEffect != null)
            rotationEffect.IsEnabled = false;

        formationCreator.GenerateFormation();
    }

    [ContextMenu("Force Roll for Effects")]
    public void ForceRollForEffects()
    {
        if (!useStageBasedActivation)
        {
            Debug.Log("FormationEffectManager: Force roll only works in stage-based activation mode");
            return;
        }

        hasRolledForCurrentStage = false;
        RollForEffectActivation();
        hasRolledForCurrentStage = true;
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

    // Toggle methods (only work in manual mode)
    public void ToggleBreathing()
    {
        if (GetCurrentBreathingState())
            StopBreathing();
        else
            StartBreathing();
    }

    public void ToggleRotation()
    {
        if (GetCurrentRotationState())
            StopRotation();
        else
            StartRotation();
    }

    // Properties
    public bool IsBreathingActive => GetCurrentBreathingState();
    public bool IsRotationActive => GetCurrentRotationState();
    public bool AnyEffectActive => IsBreathingActive || IsRotationActive;
    public int FormationCount => formationCreator?.formationCount ?? 0;
    public bool UseStageBasedActivation => useStageBasedActivation;
    public bool StageEffectsActive => stageEffectsActive;
    public float RegistrationProgress => registrationProgress;

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

        if (IsBreathingActive)
        {
            activeEffects.Add("Breathing");
        }

        if (IsRotationActive)
        {
            activeEffects.Add("Rotation");
        }

        if (activeEffects.Count == 0)
        {
            return useStageBasedActivation ? "Stage Effects: None" : "All Effects Stopped";
        }

        string effectsStr = activeEffects.Count == 1 ? activeEffects[0] : string.Join(" + ", activeEffects);
        string modeStr = useStageBasedActivation ? "Stage Effects: " : "";
        return $"{modeStr}{effectsStr} Active";
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
        // Find references if not assigned
        if (!levelManager) levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (!combatManager) combatManager = FindFirstObjectByType<ChickenCombatManagerV4>(FindObjectsInactive.Include);

        this.ValidateRefs();

        if (Application.isPlaying && isInitialized)
        {
            needsEffectDataUpdate = true;
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

        // Show mode-specific information
        if (manager.UseStageBasedActivation)
        {
            EditorGUILayout.HelpBox($"Stage-Based Mode: Effects are controlled automatically by stage progression.\nRegistration Progress: {manager.RegistrationProgress:P1}", MessageType.Info);
            
            if (GUILayout.Button("Force Roll for Effects", GUILayout.Height(30)))
            {
                manager.ForceRollForEffects();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Manual Mode: Use the toggles above or buttons below to control effects.", MessageType.Info);
            
            // Effect toggles (only in manual mode)
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
        }

        // Control buttons (work in both modes)
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