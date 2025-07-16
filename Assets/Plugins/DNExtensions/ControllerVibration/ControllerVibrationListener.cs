
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using VInspector;

namespace DNExtensions
{
    [RequireComponent(typeof(PlayerInput))]
    [DisallowMultipleComponent]
    public class ControllerVibrationListener : MonoBehaviour, IDualShockHaptics
    {
        [SerializeField, ReadOnly] private PlayerInput playerInput;
        
        
        [Header("Settings")]
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat lowFrequencyRange = new RangedFloat(0, 1f);
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat highFrequencyRange = new RangedFloat(0, 1f);
        
        private readonly List<ControllerVibrationSource> _vibrationSources = new List<ControllerVibrationSource>();
        private readonly HashSet<ControllerVibrationEffect> _activeVibrationEffects = new HashSet<ControllerVibrationEffect>();
        private Gamepad _gamepad;
        private DualShockGamepad _dualShockGamepad;



        private void OnValidate()
        {
            if (!playerInput) playerInput = GetComponent<PlayerInput>();
        }
        

        private void OnEnable()
        {
            if (!playerInput) return;
            
            playerInput.onControlsChanged += OnControlsChanged;
            if (playerInput.currentControlScheme == "Gamepad")
            {
                _gamepad = playerInput.devices[0] as Gamepad;
                _dualShockGamepad = playerInput.devices[0] as DualShockGamepad;
            }
            else
            {
                _gamepad = null;
                _dualShockGamepad = null;
            }
        }

        private void OnDisable()
        {
            if (!playerInput) return;
            
            playerInput.onControlsChanged -= OnControlsChanged;
            ResetHaptics();

        }
        
        
        private void OnControlsChanged(PlayerInput input)
        {
            if (input.currentControlScheme == "Gamepad")
            {
                if (_gamepad != null)
                {
                    ResetHaptics();
                    SetLightBarColor(Color.white);
                }

                _gamepad = playerInput.devices[0] as Gamepad;
                _dualShockGamepad = playerInput.devices[0] as DualShockGamepad;
            }
            else
            {
                _gamepad = null;
                _dualShockGamepad = null;
            }

        }

        private void Update()
        {
            if (_gamepad == null) return;

            _activeVibrationEffects.RemoveWhere(effect =>
            {
                effect.Update(Time.deltaTime);
                return effect.IsExpired;
            });

            if (_activeVibrationEffects.Count == 0)
            {
                SetMotorSpeeds(0f, 0f);
            }
            else
            {
                float combinedLow = 0f;
                float combinedHigh = 0f;

                foreach (var effect in _activeVibrationEffects)
                {
                    float normalizedTime = effect.ElapsedTime / effect.Duration;
                    float lowIntensity = effect.LowFrequency * effect.LowFrequencyCurve.Evaluate(normalizedTime);
                    float highIntensity = effect.HighFrequency * effect.HighFrequencyCurve.Evaluate(normalizedTime);

                    combinedLow = Mathf.Max(combinedLow, lowIntensity);
                    combinedHigh = Mathf.Max(combinedHigh, highIntensity);
                }

                SetMotorSpeeds(combinedLow, combinedHigh);
            }
        }

        

        #region Vibration Effects ------------------------------------------------------------------------------

        public void AddVibrationEffect(ControllerVibrationEffect effect)
        {
            _activeVibrationEffects.Add(effect);
        }


        public void DisableAllVibrations()
        {
            _activeVibrationEffects.Clear();
            ResetHaptics();
        }


        #endregion Vibration Effects ------------------------------------------------------------------------------



        #region Vibration Sources ----------------------------------------------------------------------------------


        public void ConnectVibrationSource(ControllerVibrationSource source)
        {
            if (!source || _vibrationSources.Contains(source)) return;

            _vibrationSources.Add(source);

        }

        public void DisconnectVibrationSource(ControllerVibrationSource source)
        {
            if (!source || !_vibrationSources.Contains(source)) return;

            _vibrationSources.Remove(source);

        }

        #endregion Vibration Sources ----------------------------------------------------------------------------------



        #region Motor Interface --------------------------------------------------------------------------------------


        public void PauseHaptics()
        {
            _gamepad?.PauseHaptics();
        }


        public void ResumeHaptics()
        {
            _gamepad?.ResumeHaptics();
        }


        public void ResetHaptics()
        {
            _gamepad?.ResetHaptics();
        }

        public void SetMotorSpeeds(float lowFrequency, float highFrequency)
        {
            lowFrequency  = lowFrequencyRange.Clamp(lowFrequency);
            highFrequency = highFrequencyRange.Clamp(highFrequency);
            _gamepad?.SetMotorSpeeds(lowFrequency, highFrequency);
        }

        public void SetLightBarColor(Color color)
        {
            _dualShockGamepad?.SetLightBarColor(color);
        }

        #endregion Motor Interface --------------------------------------------------------------------------------------




    }

}