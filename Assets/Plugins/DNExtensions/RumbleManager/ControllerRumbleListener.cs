
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using VInspector;

namespace DNExtensions
{
    [RequireComponent(typeof(PlayerInput))]
    [DisallowMultipleComponent]
    public class ControllerRumbleListener : MonoBehaviour, IDualShockHaptics
    {
        public static ControllerRumbleListener Instance { get; private set; }


        private readonly List<ControllerRumbleSource> _rumbleSources = new List<ControllerRumbleSource>();
        private readonly HashSet<ControllerRumbleEffect> _activeRumbleEffects = new HashSet<ControllerRumbleEffect>();
        private Gamepad _gamepad;
        private DualShockGamepad _dualShockGamepad;
        [SerializeField, ReadOnly] private PlayerInput playerInput;


        private void OnValidate()
        {
            if (!playerInput) playerInput = GetComponent<PlayerInput>();
        }

        private void Awake()
        {
            if (!Instance || Instance == this)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        private void OnEnable()
        {
            if (playerInput)
            {
                playerInput.onControlsChanged += OnControlsChanged;
                if (playerInput.currentControlScheme == "Gamepad")
                {
                    _gamepad = Gamepad.current;
                    _dualShockGamepad = DualShockGamepad.current;
                }
                else
                {
                    _gamepad = null;
                    _dualShockGamepad = null;
                }
            }
        }

        private void OnDisable()
        {
            if (playerInput)
            {
                playerInput.onControlsChanged -= OnControlsChanged;
                playerInput = null;
            }
            
        }

        private void Update()
        {
            if (_gamepad == null) return;

            _activeRumbleEffects.RemoveWhere(effect =>
            {
                effect.Update(Time.deltaTime);
                return effect.IsExpired;
            });

            if (_activeRumbleEffects.Count == 0)
            {
                SetMotorSpeeds(0f, 0f);
            }
            else
            {
                float combinedLow = 0f;
                float combinedHigh = 0f;

                foreach (var effect in _activeRumbleEffects)
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

        private void OnControlsChanged(PlayerInput input)
        {
            if (input.currentControlScheme == "Gamepad")
            {
                if (_gamepad != null)
                {
                    ResetHaptics();
                    SetLightBarColor(Color.white);
                }

                _gamepad = Gamepad.current;
                _dualShockGamepad = DualShockGamepad.current;
            }
            else
            {
                _gamepad = null;
                _dualShockGamepad = null;
            }

        }
        


        #region Rumble Effects ------------------------------------------------------------------------------

        public void AddRumbleEffect(ControllerRumbleEffect effect)
        {
            _activeRumbleEffects.Add(effect);
        }

        [Button]
        private void DisableAllRumbles()
        {
            _activeRumbleEffects.Clear();
            ResetHaptics();
        }


        #endregion Rumble Effects ------------------------------------------------------------------------------



        #region Rumble Sources ----------------------------------------------------------------------------------


        public void ConnectRumbleSource(ControllerRumbleSource source)
        {
            if (!source || _rumbleSources.Contains(source)) return;

            _rumbleSources.Add(source);

        }

        public void DisconnectRumbleSource(ControllerRumbleSource source)
        {
            if (!source || !_rumbleSources.Contains(source)) return;

            _rumbleSources.Remove(source);

        }

        #endregion Rumble Sources ----------------------------------------------------------------------------------



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
            _gamepad?.SetMotorSpeeds(lowFrequency, highFrequency);
        }

        public void SetLightBarColor(Color color)
        {
            _dualShockGamepad?.SetLightBarColor(color);
        }

        #endregion Motor Interface --------------------------------------------------------------------------------------




    }

}