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
        
        
        public ControllerRumbleEffectSettings()
        {
        }
        
        public ControllerRumbleEffectSettings(float lowFrequency, float highFrequency, float duration, AnimationCurve lowFrequencyCurve = null, AnimationCurve highFrequencyCurve = null)
        {
            this.lowFrequency = lowFrequency;
            this.highFrequency = highFrequency;
            this.duration = duration;
            this.lowFrequencyCurve = lowFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
            this.highFrequencyCurve = highFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
        }
    }
}