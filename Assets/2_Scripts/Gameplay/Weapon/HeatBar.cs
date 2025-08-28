using DNExtensions;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class HeatBar : MonoBehaviour
{
 
    [Header("Emission")]
    [SerializeField, Range(0,1)] private float emissionStrength = 1;
    [SerializeField, MinMaxRange(0f,1f)] private RangedFloat emissionRange = new(0.1f, 1f);
    
    
    [Header("Animation")]
    [SerializeField] private float heatUpdateDuration = 0.3f;
    [SerializeField] private float heatBarPunchDuration = 0.3f;
    [SerializeField] private float heatBarPunchStrength = 0.1f;
    [SerializeField] private float miniGameAnimationDuration = 0.3f;
    [SerializeField] private float miniGamePunchDuration = 0.6f;
    [SerializeField] private float miniGamePunchStrength = 0.2f;
    
    
    [Header("References")]
    [SerializeField] private RailPlayerWeaponSystem weaponSystem;
    [SerializeField] private CanvasGroup heatBarGroup;
    [SerializeField] private Image heatBar;
    [SerializeField] private Image miniGameWindow;
    [SerializeField] private TextMeshProUGUI barText;


    private float _baseBarSize;
    private float _overheatBarHeight;
    private Color _miniGameActiveColor;
    private Color _miniGameInactiveColor;
    private Sequence _heatBarSequence;
    private Sequence _heatBarGroupSequence;
    private Material _heatBarMaterial;
    private static readonly int EmissionStrength = Shader.PropertyToID("_EmissionStrength");
    
    

    private void Awake()
    {
        if (heatBar)
        {
            _heatBarMaterial = new Material(heatBar.material);
            heatBar.material = _heatBarMaterial;
            heatBar.SetMaterialDirty();
        }
        _baseBarSize = heatBarGroup.transform.localScale.x;
        _overheatBarHeight = heatBar.rectTransform.sizeDelta.y;
        _miniGameActiveColor = miniGameWindow.color;
        miniGameWindow.color = Color.clear;
        heatBarGroup.alpha = 0f;
        heatBar.fillAmount = 0f;
        barText.alpha = 0;
        SetEmissionStrength(0);
    }

    private void OnEnable()
    {
        weaponSystem.OnWeaponHeatUpdatedEvent += OnHeatUpdated;
        weaponSystem.OnWeaponOverheatedEvent += OnOverheated;
        weaponSystem.OnWeaponHeatResetEvent += OnHeatReset;
        weaponSystem.OnWeaponHeatMiniGameWindowCreatedEvent += OnWeaponHeatMiniGameWindowCreated;
        weaponSystem.OnWeaponHeatMiniGameFailedEvent += OnOnWeaponHeatMiniGameFailed;
        weaponSystem.OnWeaponHeatMiniGameSucceededEvent += OnOnWeaponHeatMiniGameSucceeded;
        weaponSystem.OnAllowShootingChangedEvent += OnAllowShootingChanged;
    }




    private void OnDisable()
    {
        
        weaponSystem.OnWeaponHeatUpdatedEvent -= OnHeatUpdated;
        weaponSystem.OnWeaponOverheatedEvent -= OnOverheated;
        weaponSystem.OnWeaponHeatResetEvent -= OnHeatReset;
        weaponSystem.OnWeaponHeatMiniGameWindowCreatedEvent -= OnWeaponHeatMiniGameWindowCreated;
        weaponSystem.OnWeaponHeatMiniGameFailedEvent -= OnOnWeaponHeatMiniGameFailed;
        weaponSystem.OnWeaponHeatMiniGameSucceededEvent -= OnOnWeaponHeatMiniGameSucceeded;
        weaponSystem.OnAllowShootingChangedEvent -= OnAllowShootingChanged;
    }

    private void SetEmissionStrength(float strength)
    {
        if (!_heatBarMaterial) return;
    
        emissionStrength = Mathf.Clamp(strength, emissionRange.minValue, emissionRange.maxValue);
        _heatBarMaterial.SetFloat(EmissionStrength, emissionStrength);
    }
    
    private void OnAllowShootingChanged(bool state)
    {
        if (_heatBarGroupSequence.isAlive) _heatBarGroupSequence.Stop();
        _heatBarGroupSequence = Sequence.Create()
            .Group(Tween.Alpha(heatBarGroup, endValue: state ? 1f : 0f, 0.2f));
    }


    private void OnHeatUpdated(float heat)
    {

        barText.text = heat >= weaponSystem.MaxWeaponHeat ? "Overheated!" : $"{heat:F0}%";

        float fillAmount = heat / weaponSystem.MaxWeaponHeat;
        SetEmissionStrength(fillAmount);
        Color textFillColor = Color.Lerp(Color.white, Color.red, fillAmount);
        float textAlpha = fillAmount < 0.2f ? 0f : Mathf.Lerp(0f, 1f, (fillAmount - 0.2f) / 0.8f);
        
        if (_heatBarSequence.isAlive) _heatBarSequence.Stop();
        _heatBarSequence = Sequence.Create()
            .Group(Tween.Color(barText, startValue: barText.color, endValue: textFillColor, heatUpdateDuration))
            .Group(Tween.Alpha(barText, startValue: barText.color.a, endValue: textAlpha, heatUpdateDuration))
            .Group(Tween.UIFillAmount(heatBar, fillAmount, heatUpdateDuration));
    }

    private void OnOverheated()
    {
        Sequence.Create()
            .Group(Tween.PunchScale(heatBarGroup.transform, strength:Vector3.one * (_baseBarSize * heatBarPunchStrength), duration: heatBarPunchDuration));
    }
    
    private void OnHeatReset()
    {
        Tween.PunchScale(heatBarGroup.transform, strength: Vector3.one * (_baseBarSize * heatBarPunchStrength), duration: heatBarPunchDuration);
        Tween.Color(miniGameWindow, startValue: miniGameWindow.color, endValue: _miniGameInactiveColor, miniGameAnimationDuration);
    }
    
    
    private void OnOnWeaponHeatMiniGameFailed()
    {
        Tween.Color(miniGameWindow, startValue: miniGameWindow.color, endValue: _miniGameInactiveColor, miniGameAnimationDuration);
    }

    private void OnOnWeaponHeatMiniGameSucceeded()
    {
        Tween.Color(miniGameWindow, startValue: miniGameWindow.color, endValue: _miniGameInactiveColor, miniGameAnimationDuration);
    }



    private void OnWeaponHeatMiniGameWindowCreated(float regenTime, float windowDuration, float windowStartTime)
    {
        float normalizedWindowSize = Mathf.Clamp01(windowDuration / regenTime);
        float windowHeight = _overheatBarHeight * (normalizedWindowSize * 0.9f); // 0.9 to leave some padding

        // Set the size of the mini-game window
        miniGameWindow.rectTransform.sizeDelta = new Vector2(
            miniGameWindow.rectTransform.sizeDelta.x, 
            windowHeight
        );

        // Calculate the position based on windowStartTime
        // Since the heat bar fills from bottom (0) to top (1), and the timing counts down from regenTime to 0;
        // we need to invert the position calculation
        float normalizedEndPosition = Mathf.Clamp01((windowStartTime - windowDuration) / regenTime);
    
        // Calculate the center position of the window (halfway between start and end)
        float windowCenterPosition = normalizedEndPosition + (normalizedWindowSize * 0.5f);
    
        // Calculate the Y offset from the center of the heat bar
        // Map from 0-1 range to the actual pixel range of the heat bar
        float yOffset = (windowCenterPosition - 0.5f) * _overheatBarHeight;

        // Set the anchored position
        miniGameWindow.rectTransform.anchoredPosition = new Vector2(
            miniGameWindow.rectTransform.anchoredPosition.x,
            yOffset
        );
        
        Tween.PunchScale(miniGameWindow.transform, strength: Vector3.one * miniGamePunchStrength, duration: miniGamePunchDuration);
        Tween.Color(miniGameWindow, startValue: miniGameWindow.color, endValue: _miniGameActiveColor, miniGameAnimationDuration);
    }
    
}