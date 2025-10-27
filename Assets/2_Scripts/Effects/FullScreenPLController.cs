using System;
using System.Collections;
using DNExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;

public class FullScreenPLController : MonoBehaviour
{
    public static FullScreenPLController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField, Range(0, 1)] private float maxMaskLevel = 1f;
    [SerializeField, ColorUsage(true, true)] private Color maxColor = UnityEngine.Color.white;
    [SerializeField, MinMaxRange(0f, 1f)] private RangedFloat boundaryThreshold = new RangedFloat(0.6f, 0.8f);
    [SerializeField, Range(0, 1)] private float transitionSmoothing = 0.1f;
    [SerializeField] private bool includeVerticalBoundaries = true;

    [Header("Material References")] 
    [SerializeField] private Material leftPlayerLimitsMaterial;
    [SerializeField] private Material rightPlayerLimitsMaterial;

    private LevelManager _levelManager;
    private float _currentLeftIntensity;
    private float _currentRightIntensity;
    private float _targetLeftIntensity;
    private float _targetRightIntensity;

    private static readonly int Color = Shader.PropertyToID("_Color");
    private static readonly int Mask = Shader.PropertyToID("_Mask");

    public class PlayerLimitsSettings
    {
        public readonly Color color;
        public readonly float mask;

        public PlayerLimitsSettings()
        {
            color = UnityEngine.Color.white;
            mask = 0f;
        }

        public PlayerLimitsSettings(Color color, float mask)
        {
            this.color = color;
            this.mask = mask;
        }
    }

    private readonly PlayerLimitsSettings _offSettings = new PlayerLimitsSettings();

    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        ToggleOff();
    }

    private void Start()
    {
        _levelManager = FindFirstObjectByType<LevelManager>();

        _currentLeftIntensity = 0f;
        _currentRightIntensity = 0f;
        _targetLeftIntensity = 0f;
        _targetRightIntensity = 0f;
        ToggleOff();

        SceneManager.sceneLoaded += OnSceneChange;
    }

    private void Update()
    {
        CheckPlayerBoundaryPosition();
        UpdateEffectIntensities();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneChange;
    }

    private void OnApplicationQuit()
    {
        ToggleOff();
    }

    private void OnSceneChange(Scene scene, LoadSceneMode loadSceneMode)
    {
        _levelManager = FindFirstObjectByType<LevelManager>();

        _currentLeftIntensity = 0f;
        _currentRightIntensity = 0f;
        _targetLeftIntensity = 0f;
        _targetRightIntensity = 0f;
        ToggleOff();
    }

    private void CheckPlayerBoundaryPosition()
    {
        if (!_levelManager || !_levelManager.Player || !_levelManager.Player.Movement) return;

        Vector2 normalizedPos = _levelManager.Player.Movement.NormalizedMovementPosition;

        // Calculate intensities based on position within threshold range
        float leftIntensity = CalculateIntensity(-normalizedPos.x);
        float rightIntensity = CalculateIntensity(normalizedPos.x);
        float topIntensity = CalculateIntensity(normalizedPos.y);
        float bottomIntensity = CalculateIntensity(-normalizedPos.y);

        // Determine which side should be active and its intensity
        _targetLeftIntensity = 0f;
        _targetRightIntensity = 0f;

        // Check horizontal boundaries
        if (normalizedPos.x < -boundaryThreshold.minValue)
        {
            _targetLeftIntensity = leftIntensity;
        }
        else if (normalizedPos.x > boundaryThreshold.minValue)
        {
            _targetRightIntensity = rightIntensity;
        }

        // Check vertical boundaries and apply to nearest horizontal side (only if enabled)
        if (includeVerticalBoundaries && (normalizedPos.y > boundaryThreshold.minValue || normalizedPos.y < -boundaryThreshold.minValue))
        {
            float verticalIntensity = Mathf.Max(topIntensity, bottomIntensity);

            // Apply vertical intensity to the appropriate side based on horizontal position
            if (normalizedPos.x < 0)
            {
                _targetLeftIntensity = Mathf.Max(_targetLeftIntensity, verticalIntensity);
            }
            else
            {
                _targetRightIntensity = Mathf.Max(_targetRightIntensity, verticalIntensity);
            }
        }
    }

    private void UpdateEffectIntensities()
    {
        // Smoothly lerp current intensities toward target intensities
        float smoothSpeed = transitionSmoothing > 0 ? 1f / transitionSmoothing : 10f;

        _currentLeftIntensity = Mathf.Lerp(_currentLeftIntensity, _targetLeftIntensity, Time.deltaTime * smoothSpeed);
        _currentRightIntensity =
            Mathf.Lerp(_currentRightIntensity, _targetRightIntensity, Time.deltaTime * smoothSpeed);

        // Apply settings based on current intensities
        PlayerLimitsSettings leftSettings = new PlayerLimitsSettings(
            maxColor * _currentLeftIntensity,
            maxMaskLevel * _currentLeftIntensity
        );
        ApplySettings(leftSettings, leftPlayerLimitsMaterial);

        PlayerLimitsSettings rightSettings = new PlayerLimitsSettings(
            maxColor * _currentRightIntensity,
            maxMaskLevel * _currentRightIntensity
        );
        ApplySettings(rightSettings, rightPlayerLimitsMaterial);
    }

    private float CalculateIntensity(float position)
    {
        // Returns 0 when position < min, 1 when position > max, interpolated in between
        if (position < boundaryThreshold.minValue)
            return 0f;

        if (position > boundaryThreshold.maxValue)
            return 1f;

        // Inverse lerp between min and max
        float range = boundaryThreshold.maxValue - boundaryThreshold.minValue;
        if (range <= 0f)
            return position >= boundaryThreshold.minValue ? 1f : 0f;

        return (position - boundaryThreshold.minValue) / range;
    }


    private void ToggleOff()
    {
        ApplySettings(_offSettings, leftPlayerLimitsMaterial);
        ApplySettings(_offSettings, rightPlayerLimitsMaterial);
    }


    private void ApplySettings(PlayerLimitsSettings limitsSettings, Material targetMaterial)
    {
        if (!targetMaterial) return;

        targetMaterial.SetColor(Color, limitsSettings.color);
        targetMaterial.SetFloat(Mask, limitsSettings.mask);
    }

}