using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Breathing Effect", menuName = "Formation Effects/Breathing Effect")]
public class BreathingEffectConfig : ScriptableObject
{
    [Header("Scale Settings")]
    [Range(0.1f, 1.5f)]
    public float minScale = 0.7f;
    [Range(0.5f, 3f)]
    public float maxScale = 1.3f;
    [Range(1f, 20f)]
    public float cycleTime = 4f;
    public bool useSmoothCurve = true;

    [Header("Variation")]
    public bool enableCycleVariation = true;
    [Range(0f, 10f)]
    public float cycleVariation = 2f;
    public bool useRandomPhase = true;
}

[System.Serializable]
public class BreathingEffectData : BaseEffectData
{
    public float breathingCycleTime;
    public float breathingPhaseOffset;
    public float currentBreathingScale = 1f;
    public float startTime;
}

public class BreathingEffect : IFormationEffect
{
    public bool IsEnabled { get; set; } = true;
    public string EffectName => "Breathing";

    private BreathingEffectConfig config;
    private List<BreathingEffectData> formationData = new List<BreathingEffectData>();

    public BreathingEffect(BreathingEffectConfig configuration)
    {
        config = configuration;
    }

    public void Initialize(int formationCount)
    {
        formationData.Clear();
        float currentTime = Time.time;

        for (int i = 0; i < formationCount; i++)
        {
            var data = new BreathingEffectData
            {
                formationIndex = i,
                breathingCycleTime = config.cycleTime,
                breathingPhaseOffset = 0f,
                currentBreathingScale = 1f,
                startTime = currentTime
            };

            // Apply variations
            if (config.enableCycleVariation)
            {
                float variation = Random.Range(-config.cycleVariation, config.cycleVariation);
                data.breathingCycleTime = Mathf.Max(0.5f, config.cycleTime + variation);
            }

            if (config.useRandomPhase)
            {
                data.breathingPhaseOffset = Random.Range(0f, 2f * Mathf.PI);
            }

            formationData.Add(data);
        }
    }

    public void UpdateEffect(float deltaTime, float elapsedTime)
    {
        if (!IsEnabled || formationData.Count == 0) return;

        // Update each formation
        for (int i = 0; i < formationData.Count; i++)
        {
            var data = formationData[i];

            // Normal breathing behavior
            float timeSinceStart = elapsedTime - data.startTime;
            float adjustedTime = timeSinceStart + (data.breathingPhaseOffset / (2f * Mathf.PI)) * data.breathingCycleTime;
            float cycleProgress = (adjustedTime % data.breathingCycleTime) / data.breathingCycleTime;
            data.currentBreathingScale = CalculateBreathingScale(cycleProgress);
        }
    }

    public void ApplyToFormation(List<Vector3> formationSlots, int formationIndex, List<Vector3> baseFormation, Vector3 centerPosition)
    {
        if (!IsEnabled || formationIndex >= formationData.Count) return;

        var data = formationData[formationIndex];
        data.centerPosition = centerPosition;

        // Apply breathing scale to each slot
        int startIndex = formationIndex * baseFormation.Count;
        for (int j = 0; j < baseFormation.Count && (startIndex + j) < formationSlots.Count; j++)
        {
            int globalSlotIndex = startIndex + j;
            Vector3 originalSlot = formationSlots[globalSlotIndex];
            Vector3 offset = originalSlot - centerPosition;
            Vector3 scaledSlot = centerPosition + (offset * data.currentBreathingScale);
            formationSlots[globalSlotIndex] = scaledSlot;
        }
    }

    public void Reset()
    {
        float currentTime = Time.time;
        foreach (var data in formationData)
        {
            data.currentBreathingScale = 1f;
            data.startTime = currentTime;
        }
    }

    public void OnFormationChanged(int newFormationCount)
    {
        Initialize(newFormationCount);
    }

    private float CalculateBreathingScale(float cycleProgress)
    {
        float normalizedValue;

        if (config.useSmoothCurve)
        {
            normalizedValue = (Mathf.Sin(cycleProgress * 2f * Mathf.PI) + 1f) * 0.5f;
        }
        else
        {
            normalizedValue = cycleProgress <= 0.5f ?
                cycleProgress * 2f : 2f - (cycleProgress * 2f);
        }

        return Mathf.Lerp(config.minScale, config.maxScale, normalizedValue);
    }

    // Public getters
    public float GetFormationScale(int formationIndex)
    {
        return (formationIndex >= 0 && formationIndex < formationData.Count) ?
            formationData[formationIndex].currentBreathingScale : 1f;
    }

    public BreathingEffectConfig Config => config;

    public void ResetToDefaults()
    {
        // Reset to default configuration values
        float currentTime = Time.time;
        for (int i = 0; i < formationData.Count; i++)
        {
            var data = formationData[i];
            
            // Reset to base config values
            data.breathingCycleTime = config.cycleTime;
            data.breathingPhaseOffset = 0f;
            data.currentBreathingScale = 1f;
            data.startTime = currentTime;
            
            // Reapply variations as per config
            if (config.enableCycleVariation)
            {
                float variation = Random.Range(-config.cycleVariation, config.cycleVariation);
                data.breathingCycleTime = Mathf.Max(0.5f, config.cycleTime + variation);
            }
            
            if (config.useRandomPhase)
            {
                data.breathingPhaseOffset = Random.Range(0f, 2f * Mathf.PI);
            }
        }
    }
}