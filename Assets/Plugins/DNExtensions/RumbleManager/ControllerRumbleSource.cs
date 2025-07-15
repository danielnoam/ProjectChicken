using System;
using UnityEngine;
using VInspector;

namespace DNExtensions
{
    [DisallowMultipleComponent]
    public class ControllerRumbleSource : MonoBehaviour
    {

        private ControllerRumbleListener _controllerRumbleListener;


        private void OnEnable()
        {
            if (_controllerRumbleListener) return;
            _controllerRumbleListener = ControllerRumbleListener.Instance;
            _controllerRumbleListener?.ConnectRumbleSource(this);
        }

        private void OnDisable()
        {
            _controllerRumbleListener?.DisconnectRumbleSource(this);
            _controllerRumbleListener = null;
        }

        private void Start()
        {
            if (_controllerRumbleListener) return;
            _controllerRumbleListener = ControllerRumbleListener.Instance;
            _controllerRumbleListener?.ConnectRumbleSource(this);
        }
        

        public void Rumble(float lowFrequency, float highFrequency, float duration, AnimationCurve lowFreqCurve = null, AnimationCurve highFreqCurve = null)
        {
            var effect = new ControllerRumbleEffect(lowFrequency, highFrequency, duration, lowFreqCurve,highFreqCurve);
            _controllerRumbleListener?.AddRumbleEffect(effect);
        }

        public void Rumble(ControllerRumbleEffectSettings controllerRumbleEffectSettings)
        {
            var effect = new ControllerRumbleEffect(controllerRumbleEffectSettings.lowFrequency, controllerRumbleEffectSettings.highFrequency, controllerRumbleEffectSettings.duration, controllerRumbleEffectSettings.lowFrequencyCurve,controllerRumbleEffectSettings.highFrequencyCurve);
            _controllerRumbleListener?.AddRumbleEffect(effect);
        }

        public void RumbleFadeOut(float lowFreq, float highFreq, float duration)
        {
            var effect = ControllerRumblePresets.CreateFadeOut(lowFreq, highFreq, duration);
            _controllerRumbleListener?.AddRumbleEffect(effect);
        }

        public void RumbleFadeIn(float lowFreq, float highFreq, float duration)
        {
            var effect = ControllerRumblePresets.CreateFadeIn(lowFreq, highFreq, duration);
            _controllerRumbleListener?.AddRumbleEffect(effect);
        }

        public void RumblePulse(float lowFreq, float highFreq, float duration, int pulses = 3)
        {
            var effect = ControllerRumblePresets.CreatePulse(lowFreq, highFreq, duration, pulses);
            _controllerRumbleListener?.AddRumbleEffect(effect);
        }

        
        [Button]
        private void TestRumble(float lowFreq = 0.5f, float highFreq = 0.5f, float duration = 1)
        {
            Rumble(lowFreq, highFreq, duration);
        }


    }
}