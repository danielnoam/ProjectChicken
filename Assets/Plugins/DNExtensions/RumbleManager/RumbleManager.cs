
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.SceneManagement;

namespace DNExtensions
{
    
    [DisallowMultipleComponent]
    public class RumbleManager : MonoBehaviour, IDualShockHaptics
    {
        public static RumbleManager Instance { get; private set; }


        private readonly List<RumbleSource> _rumbleSources = new List<RumbleSource>();
        private readonly HashSet<RumbleEffect> _activeRumbleEffects = new HashSet<RumbleEffect>();
        private Gamepad _gamepad;
        private DualShockGamepad _dualShockGamepad;
        private PlayerInput _currentPlayerInput;


        private void Awake()
        {
            if (!Instance || Instance == this)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            FindPlayerInput();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            if (_currentPlayerInput)
            {
                _currentPlayerInput.onControlsChanged -= OnControlsChanged;
                _currentPlayerInput = null;
            }
            
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
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


        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            FindPlayerInput();
        }
        
        private void FindPlayerInput()
        {
            if (_currentPlayerInput) return;
            
            _currentPlayerInput = FindFirstObjectByType<PlayerInput>();

            if (_currentPlayerInput)
            {
                _currentPlayerInput.onControlsChanged += OnControlsChanged;
                if (_currentPlayerInput.currentControlScheme == "Gamepad")
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


        #region Rumble Effects ------------------------------------------------------------------------------

        public void AddRumbleEffect(RumbleEffect effect)
        {
            _activeRumbleEffects.Add(effect);
        }

        private void DisableAllRumbles()
        {
            _activeRumbleEffects.Clear();
            ResetHaptics();
        }


        #endregion Rumble Effects ------------------------------------------------------------------------------



        #region Rumble Sources ----------------------------------------------------------------------------------


        public void ConnectRumbleSource(RumbleSource source)
        {
            if (!source || _rumbleSources.Contains(source)) return;

            _rumbleSources.Add(source);

        }

        public void DisconnectRumbleSource(RumbleSource source)
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