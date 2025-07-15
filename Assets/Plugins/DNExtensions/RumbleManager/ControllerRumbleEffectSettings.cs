using UnityEngine;

namespace DNExtensions
{
    [System.Serializable]
    public class ControllerRumbleEffectSettings
    {
        [Min(0)] public float lowFrequency = 0.3f;
        [Min(0)] public float highFrequency = 0.3f;
        [Min(0)] public float duration = 0.3f;
        public AnimationCurve lowFrequencyCurve = AnimationCurve.Linear(0, 1, 1, 1);
        public AnimationCurve highFrequencyCurve = AnimationCurve.Linear(0, 1, 1, 1);
    }
}