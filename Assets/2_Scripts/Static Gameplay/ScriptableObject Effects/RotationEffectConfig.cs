using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Rotation Effect", menuName = "Formation Effects/Rotation Effect")]
public class RotationEffectConfig : ScriptableObject
{
    [Header("Rotation Settings")]
    [Range(-360f, 360f)]
    public float rotationSpeed = 45f;

    [Header("Rotation Variation")]
    public bool enableSpeedVariation = true;
    [Range(0f, 180f)]
    public float speedVariation = 20f;
    public bool allowRandomDirection = true;
    public bool useRandomStartingAngle = true;

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
}

[System.Serializable]
public class RotationEffectData : BaseEffectData
{
    public float rotationSpeed;
    public float startingRotationAngle;
    public float currentRotationAngle;
    public int rotationDirection = 1;

    // Spin burst data
    public float spinBurstTimer;
    public float spinBurstTimeLeft;
    public bool isInSpinBurst;
    public float nextBurstInterval;

    public float startTime;
}

public class RotationEffect : IFormationEffect
{
    public bool IsEnabled { get; set; } = true;
    public string EffectName => "Rotation";

    private RotationEffectConfig config;
    private List<RotationEffectData> formationData = new List<RotationEffectData>();

    public RotationEffect(RotationEffectConfig configuration)
    {
        config = configuration;
    }

    public void Initialize(int formationCount)
    {
        formationData.Clear();
        float currentTime = Time.time;

        for (int i = 0; i < formationCount; i++)
        {
            var data = new RotationEffectData
            {
                formationIndex = i,
                rotationSpeed = config.rotationSpeed,
                startingRotationAngle = 0f,
                currentRotationAngle = 0f,
                rotationDirection = 1,
                startTime = currentTime
            };

            // Apply variations
            if (config.enableSpeedVariation)
            {
                float variation = Random.Range(-config.speedVariation, config.speedVariation);
                data.rotationSpeed = config.rotationSpeed + variation;
            }

            if (config.useRandomStartingAngle)
            {
                data.startingRotationAngle = Random.Range(0f, 360f);
                data.currentRotationAngle = data.startingRotationAngle;
            }

            if (config.allowRandomDirection && Random.value > 0.5f)
            {
                data.rotationDirection = -1;
            }

            // Spin burst setup
            if (config.enableSpinBurst)
            {
                data.nextBurstInterval = Random.Range(config.spinBurstMinInterval, config.spinBurstMaxInterval);
                data.spinBurstTimer = data.nextBurstInterval;
            }

            formationData.Add(data);
        }
    }

    public void UpdateEffect(float deltaTime, float elapsedTime)
    {
        if (!IsEnabled || formationData.Count == 0) return;

        for (int i = 0; i < formationData.Count; i++)
        {
            var data = formationData[i];

            // Normal rotation behavior
            // Update spin burst timing
            if (config.enableSpinBurst)
            {
                UpdateSpinBurstTiming(data, deltaTime);
            }

            // Calculate effective rotation speed
            float effectiveSpeed = data.rotationSpeed;
            if (data.isInSpinBurst)
            {
                effectiveSpeed *= config.spinBurstSpeedMultiplier;
            }

            // Update rotation angle
            data.currentRotationAngle += effectiveSpeed * data.rotationDirection * deltaTime;

            // Keep angle in 0-360 range
            data.currentRotationAngle = Mathf.Repeat(data.currentRotationAngle, 360f);
        }
    }

    public void ApplyToFormation(List<Vector3> formationSlots, int formationIndex, List<Vector3> baseFormation, Vector3 centerPosition)
    {
        if (!IsEnabled || formationIndex >= formationData.Count) return;

        var data = formationData[formationIndex];
        data.centerPosition = centerPosition;

        // Store current positions for rotation
        data.baseSlots.Clear();
        int startIndex = formationIndex * baseFormation.Count;
        for (int j = 0; j < baseFormation.Count && (startIndex + j) < formationSlots.Count; j++)
        {
            data.baseSlots.Add(formationSlots[startIndex + j]);
        }

        // Apply rotation
        float angleInRadians = data.currentRotationAngle * Mathf.Deg2Rad;

        for (int j = 0; j < data.baseSlots.Count; j++)
        {
            int globalSlotIndex = startIndex + j;
            if (globalSlotIndex < formationSlots.Count)
            {
                Vector3 currentSlot = data.baseSlots[j];
                Vector3 offset = currentSlot - centerPosition;

                // Simple 2D rotation
                float cos = Mathf.Cos(angleInRadians);
                float sin = Mathf.Sin(angleInRadians);

                Vector3 rotatedOffset = new Vector3(
                    offset.x * cos - offset.y * sin,
                    offset.x * sin + offset.y * cos,
                    offset.z
                );

                formationSlots[globalSlotIndex] = centerPosition + rotatedOffset;
            }
        }
    }

    public void Reset()
    {
        float currentTime = Time.time;
        foreach (var data in formationData)
        {
            data.currentRotationAngle = data.startingRotationAngle;
            data.isInSpinBurst = false;
            data.startTime = currentTime;

            if (config.enableSpinBurst)
            {
                data.nextBurstInterval = Random.Range(config.spinBurstMinInterval, config.spinBurstMaxInterval);
                data.spinBurstTimer = data.nextBurstInterval;
            }
        }
    }

    public void OnFormationChanged(int newFormationCount)
    {
        Initialize(newFormationCount);
    }

    public void TriggerSpecialAction()
    {
        // Trigger all spin bursts
        if (!config.enableSpinBurst || !IsEnabled) return;

        foreach (var data in formationData)
        {
            data.isInSpinBurst = true;
            data.spinBurstTimeLeft = config.spinBurstDuration;
            data.spinBurstTimer = 0f;
        }
    }

    private void UpdateSpinBurstTiming(RotationEffectData data, float deltaTime)
    {
        if (data.isInSpinBurst)
        {
            data.spinBurstTimeLeft -= deltaTime;
            if (data.spinBurstTimeLeft <= 0f)
            {
                // End spin burst
                data.isInSpinBurst = false;

                // Reroll new rotation speed with variation
                if (config.enableSpeedVariation)
                {
                    float variation = Random.Range(-config.speedVariation, config.speedVariation);
                    data.rotationSpeed = config.rotationSpeed + variation;
                }
                else
                {
                    data.rotationSpeed = config.rotationSpeed;
                }

                // 50% chance to switch direction
                if (Random.value < 0.5f)
                {
                    data.rotationDirection *= -1;
                }

                // Schedule next burst
                data.nextBurstInterval = Random.Range(config.spinBurstMinInterval, config.spinBurstMaxInterval);
                data.spinBurstTimer = data.nextBurstInterval;
            }
        }
        else
        {
            data.spinBurstTimer -= deltaTime;
            if (data.spinBurstTimer <= 0f)
            {
                data.isInSpinBurst = true;
                data.spinBurstTimeLeft = config.spinBurstDuration;
            }
        }
    }

    // Public getters
    public float GetFormationRotationAngle(int formationIndex)
    {
        return (formationIndex >= 0 && formationIndex < formationData.Count) ?
            formationData[formationIndex].currentRotationAngle : 0f;
    }

    public bool IsFormationInSpinBurst(int formationIndex)
    {
        return config.enableSpinBurst && formationIndex >= 0 && formationIndex < formationData.Count &&
               formationData[formationIndex].isInSpinBurst;
    }

    public float GetFormationTimeUntilNextBurst(int formationIndex)
    {
        return (config.enableSpinBurst && formationIndex >= 0 && formationIndex < formationData.Count) ?
               formationData[formationIndex].spinBurstTimer : 0f;
    }

    public float GetFormationSpinBurstTimeLeft(int formationIndex)
    {
        return (config.enableSpinBurst && formationIndex >= 0 && formationIndex < formationData.Count) ?
               formationData[formationIndex].spinBurstTimeLeft : 0f;
    }

    public RotationEffectConfig Config => config;
}