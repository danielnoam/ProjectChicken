using UnityEngine;

namespace DNExtensions
{
    public class ControllerVibrationEffect
    {
        public readonly float LowFrequency;
        public readonly float HighFrequency;
        public readonly float Duration;
        public readonly AnimationCurve LowFrequencyCurve;
        public readonly AnimationCurve HighFrequencyCurve;


        public float ElapsedTime { get; private set; }
        public bool IsExpired => ElapsedTime >= Duration;


        public ControllerVibrationEffect(float lowFrequency, float highFrequency, float duration, AnimationCurve lowFrequencyCurve = null, AnimationCurve highFrequencyCurve = null)
        {
            LowFrequency = Mathf.Clamp01(lowFrequency);
            HighFrequency = Mathf.Clamp01(highFrequency);
            Duration = Mathf.Max(0f, duration);
            LowFrequencyCurve = lowFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
            HighFrequencyCurve = highFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
        }



        public void Update(float deltaTime)
        {
            ElapsedTime += deltaTime;
        }
    }
    
    
    [System.Serializable]
    public class ControllerVibrationEffectSettings
    {
        [Range(0f,1f)] public float lowFrequency = 0.3f;
        [Range(0f,1f)] public float highFrequency = 0.3f;
        [Min(0)] public float duration = 0.3f;
        public AnimationCurve lowFrequencyCurve = AnimationCurve.Linear(0, 1, 1, 1);
        public AnimationCurve highFrequencyCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        
        public ControllerVibrationEffectSettings()
        {
        }
        
        public ControllerVibrationEffectSettings(float lowFrequency, float highFrequency, float duration, AnimationCurve lowFrequencyCurve = null, AnimationCurve highFrequencyCurve = null)
        {
            this.lowFrequency = Mathf.Clamp01(lowFrequency);
            this.highFrequency = Mathf.Clamp01(highFrequency);
            this.duration = Mathf.Max(0f, duration);
            this.lowFrequencyCurve = lowFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
            this.highFrequencyCurve = highFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
        }
    }
}