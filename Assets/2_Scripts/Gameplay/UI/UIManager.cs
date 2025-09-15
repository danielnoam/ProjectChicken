
using System;
using System.Collections;
using System.Collections.Generic;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using VInspector;





[SelectionBase]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    
    [Header("General")]
    [SerializeField] private Color cooldownIconColor = Color.grey;
    
    [Header("HUD")]
    [SerializeField] private float hudFadeDuration = 3f;
    
    [Header("Dynamic HUD")]
    [SerializeField, Tooltip("Hud position is affected by player movement")] private bool dynamicHud = true;
    [SerializeField, Tooltip("How much the hud moves by the base player movement")] private float hudPlayerMoveAmount = 6f;
    [SerializeField, Tooltip("How fast the hud will return to zero")] private float hudReturnSpeed = 2f;
    [SerializeField, Tooltip("Maximum shake intensity")] private float maxShakeIntensity = 15f;
    [SerializeField, Tooltip("How fast shake decays")] private float shakeDecayRate = 5f;
    [SerializeField, Tooltip("Shake frequency multiplier")] private float shakeFrequency = 10f;
    [SerializeField, Tooltip("Maximum shake rotation in degrees")] private float maxShakeRotation = 2f;
    
    [Header("Color HUD")]
    [SerializeField] private bool enableHudColorChange = true;
    [SerializeField] private Color hudHealthDamageColor = Color.red;
    [SerializeField] private Color hudShieldDamageColor = Color.blue;
    [SerializeField] private float hudColorPunchDuration = 0.2f;
    [SerializeField] private Graphic[] hudElementsToColor;
    
    [Header("Health")]
    [SerializeField] private float healthPunchDuration = 0.2f;
    [SerializeField] private float healthPunchStrength = 0.4f;
    [SerializeField] private Color healthPunchColor = Color.red;
    
    [Header("Shield")]
    [SerializeField] private float shieldAnimationDuration = 0.5f;
    [SerializeField] private float shieldPunchDuration = 0.2f;
    [SerializeField] private float shieldPunchStrength = 0.4f;
    [SerializeField] private Color shieldPunchColor = Color.blue;

    
    [Header("Currency")]
    [SerializeField] private float currencyAnimationDuration = 0.5f;
    [SerializeField] private float currencyPunchDuration = 0.2f;
    [SerializeField] private float currencyPunchStrength = 0.4f;
    [SerializeField, Min(0), Tooltip("The difference between the previous currency and the current currency that must be reached to trigger a big currency animation")] 
    private int bigCurrencyDifference = 4;

    
    [Header("Dodge")]
    [SerializeField] private float dodgePunchDuration = 0.2f;
    [SerializeField] private float dodgePunchStrength = 0.4f;
    
    [Header("Weapons")]
    [SerializeField] private float weaponPunchDuration = 0.2f;
    [SerializeField] private float weaponPunchStrength = 0.4f;
    
    [Header("Score")]
    [SerializeField] private float scoreAnimationDuration = 0.5f;
    [SerializeField] private float scorePunchDuration = 0.2f;
    [SerializeField] private float scorePunchStrength = 0.2f;
    [SerializeField, Min(0), Tooltip("The difference between the previous score and the current score that must be reached to trigger a big score animation")] 
    private int bigScoreDifference = 200;
    [SerializeField, Min(0), Tooltip("How many 0 is the score made out of")] private int scoreDigits = 7;
    
    
    [Header("Stage Title")]
    [SerializeField] private float stageTitleAnimationDuration = 1.5f;
    
    [Header("Pause Icon")]
    [SerializeField] private float pauseIconAnimationDuration = 0.2f;
    
    
    [Header("References")]
    [SerializeField] private CanvasGroup statsBarGroup;
    [SerializeField] private CanvasGroup scoreBarGroup;
    [SerializeField] private CanvasGroup weaponBarGroup;
    [SerializeField] private CanvasGroup dodgeBarGroup;
    [SerializeField] private CanvasGroup pauseGroup;
    [SerializeField] private CanvasGroup hudGroup;
    [SerializeField] private Image healthIcon;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image shieldIcon;
    [SerializeField] private TextMeshProUGUI shieldText;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Image dodgeIcon;
    [SerializeField] private TextMeshProUGUI dodgeCountText;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image pauseIconFill;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI stageTitleText;
    [SerializeField] private SOPlayerStats playerStats;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;

    
    private Color _weaponStartColor;
    private Color _dodgeStartColor;
    private Sequence _hudSequence;
    private Sequence _hudColorSequence;
    private Sequence _scoreSequence;
    private Sequence _playerHealthSequence;
    private Sequence _playerCurrencySequence;
    private Sequence _playerShieldSequence;
    private Sequence _playerShieldPunchSequence;
    private Sequence _stageTitleSequence;
    private Sequence _pauseSequence;
    private Sequence _statsBarSequence;
    private Sequence _scoreBarSequence;
    private Sequence _weaponBarSequence;
    private Sequence _dodgeBarSequence;
    private int _previousScore;
    private int _score;
    private int _previousPlayerCurrency;
    private int _playerCurrency;
    private int _playerHealth;
    private float _playerShield;
    private Vector3 _targetHudPosition;
    private Vector3 _shakeOffset;
    private float _currentShakeIntensity;
    private float _shakeTimer;
    private Quaternion _originalHudRotation;
    private float _currentShakeRotation;
    private Dictionary<Graphic, Color> _hudElementsColor;


    private void OnValidate()
    {
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        
        
        if (!player)
        {
            player = FindFirstObjectByType<RailPlayer>();
        }
    }

    private void Awake()
    {
        if (!Instance || Instance == this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        SetUpUI();
    }
    
    private void OnEnable()
    {
        if (player)
        {
            player.OnPauseTimerChanged += OnPauseTimerChanged;
            player.Health.OnDeath += OnPlayerDeath;
            player.Health.OnDamaged += OnPlayerDamaged;
            player.Health.OnHealthChanged += OnPlayerHealthChanged;
            player.Health.OnShieldChanged += OnPlayerShieldChanged;
            player.ResourceCollector.OnCurrencyChanged += OnPlayerCurrencyChanged;
            player.WeaponSystem.OnWeaponUsed += OnPlayerWeaponUsed;
            player.WeaponSystem.OnActiveWeaponSwitchedEvent += OnPlayerActiveWeaponSwitched;
            player.WeaponSystem.OnActiveWeaponCooldownUpdatedEvent += OnPlayerActiveWeaponCooldownUpdated;
            player.Movement.OnDodgeCooldownUpdated += OnDodgeCooldownUpdated;
            player.Movement.OnDodge += OnPlayerDodge;
            player.Movement.OnDodgeCountChanged += OnDodgeCountChanged;
        }

        if (levelManager)
        {
            levelManager.OnScoreChanged += OnScoreChanged;
            levelManager.OnStageChanged += OnStageChanged;
        }
    }



    private void OnDisable()
    {
        if (player)
        {
            player.OnPauseTimerChanged -= OnPauseTimerChanged;
            player.Health.OnDeath -= OnPlayerDeath;
            player.Health.OnDamaged -= OnPlayerDamaged;
            player.Health.OnHealthChanged -= OnPlayerHealthChanged;
            player.Health.OnShieldChanged -= OnPlayerShieldChanged;
            player.ResourceCollector.OnCurrencyChanged -= OnPlayerCurrencyChanged;
            player.WeaponSystem.OnWeaponUsed -= OnPlayerWeaponUsed;
            player.WeaponSystem.OnActiveWeaponSwitchedEvent -= OnPlayerActiveWeaponSwitched;
            player.WeaponSystem.OnActiveWeaponCooldownUpdatedEvent -= OnPlayerActiveWeaponCooldownUpdated;
            player.Movement.OnDodgeCooldownUpdated -= OnDodgeCooldownUpdated;
            player.Movement.OnDodge -= OnPlayerDodge;
            player.Movement.OnDodgeCountChanged -= OnDodgeCountChanged;
        }
        
        if (levelManager)
        {
            levelManager.OnScoreChanged -= OnScoreChanged;
            levelManager.OnStageChanged -= OnStageChanged;
        }
    }
    


    private void Update()
    {
        scoreText.text = _score.ToString($"D{scoreDigits}");
        healthText.text = _playerHealth.ToString();
        shieldText.text = $"{_playerShield:F0}%";
        currencyText.text = _playerCurrency.ToString();
        UpdateDynamicHUD();
    }



    private void SetUpUI()
    {
        SetUpHUDColors();
        _originalHudRotation = hudGroup.transform.localRotation;
        _weaponStartColor = weaponIcon.color;
        _dodgeStartColor = dodgeIcon.color;
        pauseGroup.alpha = 0f;
        pauseIconFill.fillAmount = 0f;
        stageTitleText.alpha = 0f;
        _previousScore = 0;
        _score = 0;
        _previousPlayerCurrency = 0;
        _playerCurrency = 0;
        _playerShield = 0f;
        dodgeCountText.text = "";
        
        
        ToggleUIGroup(hudGroup, false);
        ToggleUIGroup(statsBarGroup, false);
        ToggleUIGroup(scoreBarGroup, false);
        ToggleUIGroup(weaponBarGroup, false);
        ToggleUIGroup(dodgeBarGroup, false);
    }
    
    
    
    private void ToggleUIGroup(CanvasGroup group, bool state)
    {
        if (group) group.alpha = state ? 1f : 0f;
    }
    
    private void FadeUIGroup(CanvasGroup group, bool fadeIn, ref Sequence sequence)
    {
        if (sequence.isAlive) sequence.Stop();
        switch (fadeIn)
        {
            case true when group.alpha >= 1:
            case false when group.alpha <= 0:
                return;
        }
    
        float endValue = fadeIn ? 1 : 0;
    
        sequence = Sequence.Create()
            .Group(Tween.Alpha(group, group.alpha, endValue, hudFadeDuration));
    }
    
    private void UpdateStageTitle(string title)
    {
        if (_stageTitleSequence.isAlive) _stageTitleSequence.Stop();
            
        stageTitleText.alpha = 0f;
        stageTitleText.text = title;
        
        _stageTitleSequence = Sequence.Create()
            .Group(Tween.Alpha(stageTitleText, startDelay: 0.5f, startValue: 0, endValue: 1, duration: stageTitleAnimationDuration/0.6f))
            .Chain(Tween.Alpha(stageTitleText, 0, stageTitleAnimationDuration/0.4f));
    }

    

    #region HUD Color --------------------------------------------------------------------------------------------

    
    [Button]
    private void FindAllHUDElements()
    {
        if (!hudGroup) return;
    
        hudElementsToColor =  Array.Empty<Graphic>();
        hudElementsToColor = hudGroup.GetComponentsInChildren<Graphic>(includeInactive: true);
    }
    
    private void SetUpHUDColors()
    {
        if (hudElementsToColor == null || hudElementsToColor.Length == 0) return;
        
        _hudElementsColor = new Dictionary<Graphic, Color>();
        foreach (var graphic in hudElementsToColor)
        {
            if (graphic && !_hudElementsColor.ContainsKey(graphic))
            {
                _hudElementsColor.Add(graphic, graphic.color);
            }
        }
    }
    
    
    
    private void PunchHUDColor(Color targetColor, float duration)
    {
        if (_hudElementsColor == null || _hudElementsColor.Count == 0 || !enableHudColorChange) return;
    
        if (_hudColorSequence.isAlive) _hudColorSequence.Stop();
        _hudColorSequence = Sequence.Create();

        foreach (var (graphic, originalColor) in _hudElementsColor)
        {
            if (!graphic) continue;
            _hudColorSequence.Group(Tween.Color(graphic, graphic.color, targetColor, duration / 2f));
        }
        
        bool isFirst = true;
        foreach (var (graphic, originalColor) in _hudElementsColor)
        {
            if (!graphic) continue;
        
            if (isFirst)
            {
                _hudColorSequence.Chain(Tween.Color(graphic, targetColor, originalColor, duration / 2f));
                isFirst = false;
            }
            else
            {
                _hudColorSequence.Group(Tween.Color(graphic, targetColor, originalColor, duration / 2f));
            }
        }
    }
    
    
    [Button]
    private void PunchHUDHealthColor() => PunchHUDColor(hudHealthDamageColor, hudColorPunchDuration);
    [Button]
    private void PunchHUDShieldColor() => PunchHUDColor(hudShieldDamageColor,hudColorPunchDuration);
    
    

    #endregion HUD Color --------------------------------------------------------------------------------------------
    
    

    #region Shake ------------------------------------------------------------------------------------------------
    
    
    private void UpdateDynamicHUD()
    {
        if (!dynamicHud || !player) 
        {
            hudGroup.transform.localPosition = Vector3.zero;
            hudGroup.transform.localRotation = _originalHudRotation;
            return;
        }
        
        _targetHudPosition = new Vector3(
            player.Movement.InputDirection.x * hudPlayerMoveAmount,
            player.Movement.InputDirection.y * hudPlayerMoveAmount,
            0
        );


        UpdateShake();


        Vector3 finalPosition = _targetHudPosition + _shakeOffset;
        hudGroup.transform.localPosition = Vector3.Lerp(
            hudGroup.transform.localPosition, 
            finalPosition, 
            Time.deltaTime * hudReturnSpeed
        );

        Quaternion shakeRotation = Quaternion.AngleAxis(_currentShakeRotation, Vector3.forward);
        hudGroup.transform.localRotation = _originalHudRotation * shakeRotation;
    }

    
    
    private void UpdateShake()
    {
        if (!dynamicHud || _currentShakeIntensity <= 0.01f)
        {
            _shakeOffset = Vector3.zero;
            _currentShakeRotation = 0f;
            return;
        }


        _shakeTimer += Time.deltaTime * shakeFrequency;
        
        float shakeX = (Mathf.PerlinNoise(_shakeTimer, 0f) - 0.5f) * 2f;
        float shakeY = (Mathf.PerlinNoise(0f, _shakeTimer) - 0.5f) * 2f;
        
        _shakeOffset = new Vector3(
            shakeX * _currentShakeIntensity,
            shakeY * _currentShakeIntensity,
            0
        );


        _currentShakeRotation = (Mathf.PerlinNoise(_shakeTimer * 1.5f, _shakeTimer * 1.5f) - 0.5f) * maxShakeRotation * (_currentShakeIntensity / maxShakeIntensity);
        _currentShakeIntensity = Mathf.Lerp(_currentShakeIntensity, 0f, Time.deltaTime * shakeDecayRate);
    }


    private void AddHudShake(float intensity, float duration = 1f)
    {
        if (!dynamicHud) return;
        
        _currentShakeIntensity = Mathf.Max(_currentShakeIntensity, Mathf.Clamp(intensity, 0f, maxShakeIntensity));
        
        if (duration > 0f)
        {
            StartCoroutine(ShakeForDuration(duration));
        }
    }

    private IEnumerator ShakeForDuration(float duration)
    {
        float startIntensity = _currentShakeIntensity;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _currentShakeIntensity = Mathf.Lerp(startIntensity, 0f, t);
            yield return null;
        }
        
        _currentShakeIntensity = 0f;
    }
    
    
    private void ShakeHUDLight() => AddHudShake(2.5f, 0.2f);
    private void ShakeHUDMedium() => AddHudShake(7f, 0.4f);
    private void ShakeHUDHeavy() => AddHudShake(maxShakeIntensity, 0.8f);

    #endregion Shake ------------------------------------------------------------------------------------------------
    


    #region Events ------------------------------------------------------------------------------------------------

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        FadeUIGroup(hudGroup, stage.ShowHUD, ref _hudSequence);
        FadeUIGroup(statsBarGroup, stage.ShowStatsBar && stage.ShowHUD, ref _statsBarSequence);
        FadeUIGroup(scoreBarGroup, stage.ShowScore && stage.ShowHUD, ref _scoreBarSequence);
        FadeUIGroup(weaponBarGroup, stage.AllowPlayerShooting && stage.ShowHUD, ref _weaponBarSequence);
        FadeUIGroup(dodgeBarGroup, stage.AllowPlayerDodge && stage.ShowHUD, ref _dodgeBarSequence);
        UpdateStageTitle(stage.StageTitle);
    }
    
    
    private void OnPlayerDeath()
    {
        ShakeHUDHeavy();
        FadeUIGroup(hudGroup, false, ref _hudSequence);
    }
    
    private void OnPlayerDamaged()
    {
        ShakeHUDMedium();
    }
    
    private void OnPlayerWeaponUsed(WeaponInstance weaponInstance)
    {
        ShakeHUDLight();
    }
    
    private void OnPlayerDodge()
    {
        dodgeIcon.color = cooldownIconColor;
        ShakeHUDMedium();
    }

    
    private void OnPlayerHealthChanged(int currentHealth)
    {
        if (_playerHealthSequence.isAlive) _playerHealthSequence.Stop();
        _playerHealthSequence = Sequence.Create()
            .Group(Tween.PunchScale(healthIcon.transform, strength: Vector3.one * healthPunchStrength, duration: healthPunchDuration));
        
        if (_playerHealth < currentHealth)
        {
            PunchHUDHealthColor();
            
            _playerHealthSequence.Group(Tween.Color(healthIcon, healthIcon.color, healthPunchColor, healthPunchDuration));
            _playerHealthSequence.Chain(Tween.Color(healthIcon, healthPunchColor, Color.white, healthPunchDuration));
        }
        
        _playerHealth = currentHealth;
    }

    private void OnPlayerShieldChanged(float currentShield)
    {
        
        if (_playerShieldSequence.isAlive) _playerShieldSequence.Stop();
        if (currentShield < _playerShield)
        {
            PunchHUDShieldColor();
            
            _playerShieldSequence = Sequence.Create()
                .Group(Tween.Custom(
                    startValue: _playerShield, 
                    endValue: currentShield,
                    duration: shieldAnimationDuration,
                    onValueChange: value => _playerShield = Mathf.RoundToInt(value)));
            
        }
        else
        {
            _playerShield = currentShield;
        }
        
        
        if (currentShield >= player.Health.MaxShield - 1)
        {
            if (_playerShieldPunchSequence.isAlive) _playerShieldPunchSequence.Stop();
            _playerShieldPunchSequence = Sequence.Create()
                    .Group(Tween.PunchScale(shieldIcon.transform, strength: Vector3.one * shieldPunchStrength, duration: shieldPunchDuration))
                    .Group(Tween.Color(shieldIcon, healthIcon.color, shieldPunchColor, healthPunchDuration))
                    .Chain(Tween.Color(shieldIcon, shieldPunchColor, Color.white, healthPunchDuration));
        }

    }
    

    private void OnPlayerActiveWeaponSwitched(WeaponInstance previousWeaponInstance, WeaponInstance newWeaponInstance)
    {
        if (newWeaponInstance == null) return;
        
        weaponIcon.sprite = newWeaponInstance.CurrentWeaponData.WeaponIcon;
        Tween.PunchScale(weaponIcon.transform, strength: Vector3.one * weaponPunchStrength, duration: weaponPunchDuration);
    }
    
    
    private void OnPlayerActiveWeaponCooldownUpdated(WeaponInstance specialWeaponInstance, float cooldown)
    {
        if (specialWeaponInstance == null) return;
        
        float fillAmount = 1f - (cooldown / specialWeaponInstance.CurrentWeaponData.FireRate);
        weaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
    }
    
    
    private void OnPlayerCurrencyChanged(int newCurrency)
    {
        int currencyDifferance = newCurrency - _previousPlayerCurrency;
        if (currencyDifferance >= bigCurrencyDifference)
        {
            if (_playerCurrencySequence.isAlive) _playerCurrencySequence.Stop();
            _playerCurrencySequence = Sequence.Create()
                
                    .Group(Tween.Custom(startValue: _previousPlayerCurrency, endValue: newCurrency, duration: currencyAnimationDuration, onValueChange: value => _playerCurrency = Mathf.RoundToInt(value)))
                    .Chain(Tween.PunchScale(currencyIcon.transform, strength: Vector3.one * currencyPunchStrength, duration: currencyPunchDuration))
                    .OnComplete(() => _previousPlayerCurrency = newCurrency)
                ;
        }
        else
        {
            if (_playerCurrencySequence.isAlive) _playerCurrencySequence.Stop();
            _playerCurrencySequence = Sequence.Create()
                
                    .Group(Tween.Custom(startValue: _previousPlayerCurrency, endValue: newCurrency, duration: currencyAnimationDuration, onValueChange: value => _playerCurrency = Mathf.RoundToInt(value)))
                    .OnComplete(() => _previousPlayerCurrency = newCurrency)
                ;
        }
    }
    
    private void OnDodgeCooldownUpdated(float cooldown)
    {
        float fillAmount = 1f - cooldown;
        dodgeIcon.color = Color.Lerp(cooldownIconColor, _dodgeStartColor, fillAmount);

        if (Mathf.Approximately(fillAmount, 1f)) 
        {
            Tween.PunchScale(dodgeIcon.transform, strength: Vector3.one * dodgePunchStrength, duration: dodgePunchDuration);
        }
    }
    
    
    private void OnPauseTimerChanged(float time)
    {
        float pauseAlpha = time < 0.2f ? 0f : Mathf.Lerp(0f, 1f, (time - 0.2f) / 0.7f);
    
        if (_pauseSequence.isAlive) _pauseSequence.Stop();
        _pauseSequence = Sequence.Create()
            .Group(Tween.Alpha(pauseGroup, startValue: pauseGroup.alpha, endValue: pauseAlpha, pauseIconAnimationDuration))
            .Group(Tween.UIFillAmount(pauseIconFill, startValue: pauseIconFill.fillAmount, endValue: time, pauseIconAnimationDuration));
    }
    
    
    private void OnDodgeCountChanged(int dodgesRemining)
    {
        dodgeCountText.text = $"X{dodgesRemining}";
        Tween.PunchScale(dodgeCountText.transform, strength: Vector3.one * dodgePunchStrength, duration: dodgePunchDuration);
    }
    
    

    private void OnScoreChanged(int newScore)
    {
        int scoreDifference = newScore - _previousScore;
        if (scoreDifference >= bigScoreDifference)
        {
            if (_scoreSequence.isAlive) _scoreSequence.Stop();
            _scoreSequence = Sequence.Create()
                
                    .Group(Tween.Custom(startValue: _previousScore, endValue: newScore, duration: scoreAnimationDuration, onValueChange: value => _score = Mathf.RoundToInt(value)))
                    .Chain(Tween.PunchScale(scoreText.transform, strength: Vector3.one * scorePunchStrength, duration: scorePunchDuration))
                    .OnComplete(() =>
                    {
                        _previousScore = newScore;
                        scoreText.transform.localScale = Vector3.one;
                    })
                ;
        }
        else
        {
            if (_scoreSequence.isAlive) _scoreSequence.Stop();
            _scoreSequence = Sequence.Create()
                    .Group(Tween.Custom(startValue: _previousScore, endValue: newScore, duration: scoreAnimationDuration, onValueChange: value => _score = Mathf.RoundToInt(value)))
                    .OnComplete(() => _previousScore = newScore)
                ;
        }
    }
    

    #endregion Events ------------------------------------------------------------------------------------------------


    
}