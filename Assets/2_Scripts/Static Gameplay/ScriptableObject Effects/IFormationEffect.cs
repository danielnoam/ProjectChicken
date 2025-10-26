using UnityEngine;
using System.Collections.Generic;

// Base interface for all formation effects
public interface IFormationEffect
{
    bool IsEnabled { get; set; }
    string EffectName { get; }

    void Initialize(int formationCount);
    void UpdateEffect(float deltaTime, float elapsedTime);
    void ApplyToFormation(List<Vector3> formationSlots, int formationIndex, List<Vector3> baseFormation, Vector3 centerPosition);
    void Reset();
    void ResetToDefaults(); // Reset effect values to ScriptableObject defaults

    // Optional methods for effects that need them
    void OnFormationChanged(int newFormationCount) { }
    void TriggerSpecialAction() { }
}

// Base effect data that all effects can extend
[System.Serializable]
public class BaseEffectData
{
    public int formationIndex;
    public Vector3 centerPosition;
    public List<Vector3> baseSlots = new List<Vector3>();
    public int startSlotIndex;
    public int slotCount;
}