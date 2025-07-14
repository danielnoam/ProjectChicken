using UnityEngine;

namespace DNExtensions
{
    

    public class RumbleEffect
    {
        public readonly float LowFrequency;
        public readonly float HighFrequency;
        public readonly float Duration;
        public readonly AnimationCurve LowFrequencyCurve;
        public readonly AnimationCurve HighFrequencyCurve;


        public float ElapsedTime { get; private set; }
        public bool IsExpired => ElapsedTime >= Duration;


        public RumbleEffect(float lowFrequency, float highFrequency, float duration,
            AnimationCurve lowFrequencyCurve = null, AnimationCurve highFrequencyCurve = null)
        {
            LowFrequency = lowFrequency;
            HighFrequency = highFrequency;
            Duration = duration;
            LowFrequencyCurve = lowFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
            HighFrequencyCurve = highFrequencyCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
        }



        public void Update(float deltaTime)
        {
            ElapsedTime += deltaTime;
        }
    }

    public static class RumblePresets
    {
        public static RumbleEffect CreateFadeOut(float lowFreq, float highFreq, float duration)
        {
            var fadeOut = AnimationCurve.Linear(0, 1, 1, 0);
            return new RumbleEffect(lowFreq, highFreq, duration, fadeOut, fadeOut);
        }

        public static RumbleEffect CreatePulse(float lowFreq, float highFreq, float duration, int pulses = 3)
        {
            var pulseCurve = new AnimationCurve();
            for (int i = 0; i < pulses; i++)
            {
                float time = (float)i / pulses;
                pulseCurve.AddKey(time, 0f);
                pulseCurve.AddKey(time + 0.1f / pulses, 1f);
            }

            return new RumbleEffect(lowFreq, highFreq, duration, pulseCurve, pulseCurve);
        }
    }
}