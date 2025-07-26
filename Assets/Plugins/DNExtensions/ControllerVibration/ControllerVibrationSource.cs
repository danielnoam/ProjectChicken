using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

namespace DNExtensions
{
    [DisallowMultipleComponent]
    public class ControllerVibrationSource : MonoBehaviour
    {
        private readonly List<ControllerVibrationListener> _vibrationListeners = new List<ControllerVibrationListener>();

        private void Awake()
        {
            foreach (var listener in FindObjectsByType<ControllerVibrationListener>(FindObjectsSortMode.None))
            {
                _vibrationListeners.Add(listener);
            }
        }

        private void OnEnable()
        {
            foreach (var listener in _vibrationListeners)
            {
                listener?.ConnectVibrationSource(this);
            }
        }

        private void OnDisable()
        {
            foreach (var listener in _vibrationListeners)
            {
                listener?.DisconnectVibrationSource(this);
            }
        }

        /// <summary>
        /// Sends vibration to all connected listeners (Takes custom parameters, frequencies are clamped between 0-1)
        /// </summary>
        public void Vibrate(float lowFrequency, float highFrequency, float duration, AnimationCurve lowFreqCurve = null, AnimationCurve highFreqCurve = null)
        {
            var effect = new ControllerVibrationEffect(lowFrequency, highFrequency, duration, lowFreqCurve, highFreqCurve);
            foreach (var listener in _vibrationListeners)
            {
                listener?.AddVibrationEffect(effect);
            }
        }

        /// <summary>
        /// Sends vibration to all connected listeners (Takes vibration effect settings)
        /// </summary>
        public void Vibrate(ControllerVibrationEffectSettings controllerVibrationEffectSettings)
        {
            var effect = new ControllerVibrationEffect(
                controllerVibrationEffectSettings.lowFrequency, 
                controllerVibrationEffectSettings.highFrequency, 
                controllerVibrationEffectSettings.duration, 
                controllerVibrationEffectSettings.lowFrequencyCurve,
                controllerVibrationEffectSettings.highFrequencyCurve);
            
            foreach (var listener in _vibrationListeners)
            {
                listener?.AddVibrationEffect(effect);
            }
        }

        /// <summary>
        /// Sends vibration to all connected listeners (Uses custom curves to fade out the effect)
        /// </summary>
        public void VibrateFadeOut(float lowFreq, float highFreq, float duration)
        {
            var fadeOutCurve = AnimationCurve.Linear(0, 1, 1, 0);
            var effect = new ControllerVibrationEffect(lowFreq, highFreq, duration, fadeOutCurve, fadeOutCurve);
            
            foreach (var listener in _vibrationListeners)
            {
                listener?.AddVibrationEffect(effect);
            }
        }

        
        /// <summary>
        /// Sends vibration to all connected listeners (Uses custom curves to fade in the effect)
        /// </summary>
        public void VibrateFadeIn(float lowFreq, float highFreq, float duration)
        {
            var fadeInCurve = AnimationCurve.Linear(0, 0, 1, 1);
            var effect = new ControllerVibrationEffect(lowFreq, highFreq, duration, fadeInCurve, fadeInCurve);
            
            foreach (var listener in _vibrationListeners)
            {
                listener?.AddVibrationEffect(effect);
            }
        }
        
        
        /// <summary>
        /// Sends vibration to all connected listeners (Uses custom curves to pulse the effect)
        /// </summary>
        public void VibratePulse(float lowFreq, float highFreq, float duration, int pulses = 3)
        {

            var pulseCurve = new AnimationCurve();
            for (var i = 0; i < pulses; i++)
            {
                var time = (float)i / pulses;
                pulseCurve.AddKey(time, 0f);
                pulseCurve.AddKey(time + 0.1f / pulses, 1f);
            }
            
            var effect = new ControllerVibrationEffect(lowFreq, highFreq, duration, pulseCurve, pulseCurve);
            
            foreach (var listener in _vibrationListeners)
            {
                listener?.AddVibrationEffect(effect);
            }
        }
    }
}