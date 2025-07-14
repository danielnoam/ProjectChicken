using System;
using UnityEngine;
using VInspector;

namespace DNExtensions
{
    [DisallowMultipleComponent]
    public class RumbleSource : MonoBehaviour
    {

        private RumbleManager _rumbleManager;


        private void OnEnable()
        {
            if (_rumbleManager) return;
            _rumbleManager = RumbleManager.Instance;
            _rumbleManager?.ConnectRumbleSource(this);
        }

        private void OnDisable()
        {
            _rumbleManager?.DisconnectRumbleSource(this);
            _rumbleManager = null;
        }

        private void Start()
        {
            if (_rumbleManager) return;
            _rumbleManager = RumbleManager.Instance;
            _rumbleManager?.ConnectRumbleSource(this);
        }

        [Button]
        private void TestRumble(float lowFreq = 0.5f, float highFreq = 0.5f, float duration = 1)
        {
            Rumble(lowFreq, highFreq, duration);
        }

        public void Rumble(float lowFrequency, float highFrequency, float duration)
        {
            var effect = new RumbleEffect(lowFrequency, highFrequency, duration);
            _rumbleManager?.AddRumbleEffect(effect);
        }

        public void RumbleFadeOut(float lowFreq, float highFreq, float duration)
        {
            var effect = RumblePresets.CreateFadeOut(lowFreq, highFreq, duration);
            _rumbleManager?.AddRumbleEffect(effect);
        }

        public void RumblePulse(float lowFreq, float highFreq, float duration, int pulses = 3)
        {
            var effect = RumblePresets.CreatePulse(lowFreq, highFreq, duration, pulses);
            _rumbleManager?.AddRumbleEffect(effect);
        }



    }
}