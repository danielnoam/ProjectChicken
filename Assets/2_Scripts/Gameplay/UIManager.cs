
using System;
using System.Collections.Generic;
using System.Linq;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;





[SelectionBase]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    
    [Header("General")]
    [SerializeField] private float hudFadeDuration = 3f;
    [SerializeField] private Color cooldownIconColor = Color.grey;
    [SerializeField, Child(Flag.Editable)] private CanvasGroup hudGroup;
    [SerializeField] private SOGameSettings gameSettings;
    
    [Header("Health")]
    [SerializeField] private float healthPunchDuration = 0.2f;
    [SerializeField] private float healthPunchStrength = 0.5f;
    [SerializeField] private Color healthPunchColor = Color.red;
    [SerializeField] private Image healthIcon;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Shield")]
    [SerializeField] private float shieldAnimationDuration = 0.7f;
    [SerializeField] private float shieldPunchDuration = 0.2f;
    [SerializeField] private float shieldPunchStrength = 0.5f;
    [SerializeField] private Color shieldPunchColor = Color.blue;
    [SerializeField] private Image shieldIcon;
    [SerializeField] private TextMeshProUGUI shieldText;
    
    [Header("Currency")]
    [SerializeField] private float currencyAnimationDuration = 0.7f;
    [SerializeField] private float currencyPunchDuration = 0.2f;
    [SerializeField] private float currencyPunchStrength = 0.2f;
    [SerializeField, Min(0), Tooltip("The difference between the previous currency and the current currency that must be reached to trigger a big currency animation")] 
    private int bigCurrencyDifference = 4;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private TextMeshProUGUI currencyText;
    
    [Header("Dodge")]
    [SerializeField] private float dodgePunchDuration = 0.2f;
    [SerializeField] private float dodgePunchStrength = 0.2f;
    [SerializeField] private Image dodgeIcon;
    [SerializeField] private TextMeshProUGUI dodgeCountText;
    
    [Header("Weapons")]
    [SerializeField] private float weaponAnimationDuration = 0.2f;
    [SerializeField] private float weaponPunchDuration = 0.2f;
    [SerializeField] private float weaponPunchStrength = 0.2f;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image secondaryWeaponIcon;
    
    [Header("Score")]
    [SerializeField] private float scoreAnimationDuration = 0.2f;
    [SerializeField] private float scorePunchDuration = 0.2f;
    [SerializeField] private float scorePunchStrength = 0.2f;
    [SerializeField, Min(0), Tooltip("The difference between the previous score and the current score that must be reached to trigger a big score animation")] 
    private int bigScoreDifference = 200;
    [SerializeField, Min(0), Tooltip("How many 0 is the score made out of")] private int scoreDigits = 7;
    [SerializeField] private TextMeshProUGUI scoreText;
    
    [Header("Wave Title")]
    [SerializeField] private float waveTitleAnimationDuration = 0.2f;
    [SerializeField] private TextMeshProUGUI waveTitleText;
    
    [Header("Pause Icon")]
    [SerializeField] private float pauseIconAnimationDuration = 0.2f;
    [SerializeField] private Image pauseIconFill;
    [SerializeField] private CanvasGroup pauseGroup;
    
    [Header("Scene References")] 
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;

    
    private readonly Dictionary<Image, bool> _healthIcons = new Dictionary<Image, bool>();
    private Color _secondaryWeaponStartColor;
    private Color _weaponStartColor;
    private Color _dodgeStartColor;
    private Sequence _keybindsSequence;
    private Sequence _hudSequence;
    private Sequence _scoreSequence;
    private Sequence _playerHealthSequence;
    private Sequence _playerCurrencySequence;
    private Sequence _playerShieldSequence;
    private Sequence _waveTitleSequence;
    private Sequence _pauseSequence;
    private int _previousScore;
    private int _score;
    private int _previousPlayerCurrency;
    private int _playerCurrency;
    private int _playerHealth;
    private float _playerShield;



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
            player.Health.OnDeath += OnDeath;
            player.Health.OnHealthChanged += OnPlayerHealthChanged;
            player.Health.OnShieldChanged += OnPlayerShieldChanged;
            player.ResourceCollector.OnCurrencyChanged += OnPlayerCurrencyChanged;
            player.WeaponSystem.OnSpecialWeaponSwitchedEvent += SpecialWeaponSystemSwitched;
            player.WeaponSystem.OnSpecialWeaponCooldownUpdatedEvent += SpecialWeaponSystemCooldownUpdated;
            player.WeaponSystem.OnBaseWeaponCooldownUpdatedEvent += BaseWeaponSystemCooldownUpdated;
            player.WeaponSystem.OnBaseWeaponSwitchedEvent += BaseWeaponSystemSwitched;
            player.WeaponSystem.OnSpecialWeaponDisabledEvent += SpecialWeaponSystemDisabled;
            player.Movement.OnDodgeCooldownUpdated += OnDodgeCooldownUpdated;
            player.Movement.OnDodge += OnDodge;
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
            player.Health.OnDeath -= OnDeath;
            player.Health.OnHealthChanged -= OnPlayerHealthChanged;
            player.Health.OnShieldChanged -= OnPlayerShieldChanged;
            player.ResourceCollector.OnCurrencyChanged -= OnPlayerCurrencyChanged;
            player.WeaponSystem.OnSpecialWeaponSwitchedEvent -= SpecialWeaponSystemSwitched;
            player.WeaponSystem.OnSpecialWeaponCooldownUpdatedEvent -= SpecialWeaponSystemCooldownUpdated;
            player.WeaponSystem.OnBaseWeaponCooldownUpdatedEvent -= BaseWeaponSystemCooldownUpdated;
            player.WeaponSystem.OnBaseWeaponSwitchedEvent -= BaseWeaponSystemSwitched;
            player.WeaponSystem.OnSpecialWeaponDisabledEvent -= SpecialWeaponSystemDisabled;
            player.Movement.OnDodgeCooldownUpdated -= OnDodgeCooldownUpdated;
            player.Movement.OnDodge -= OnDodge;
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
        if (healthText) healthText.text = _playerHealth.ToString();
        shieldText.text = $"{_playerShield:F0}%";
        currencyText.text = _playerCurrency.ToString();
    }
    
    
    private void SetUpUI()
    {
        _weaponStartColor = weaponIcon.color;
        _secondaryWeaponStartColor = secondaryWeaponIcon.color;
        _dodgeStartColor = dodgeIcon.color;
        pauseGroup.alpha = 0f;
        pauseIconFill.fillAmount = 0f;
        waveTitleText.alpha = 0f;
        _previousScore = 0;
        _score = 0;
        _previousPlayerCurrency = 0;
        _playerCurrency = 0;
        _playerShield = 0f;
        dodgeCountText.text = "";
        
        
        ToggleHUD(false);
    }

    
    private void FadeHUD(bool fadeIn)
    {
        if (_hudSequence.isAlive) _hudSequence.Stop();
        switch (fadeIn)
        {
            case true when hudGroup.alpha >= 1:
            case false when hudGroup.alpha <= 0:
                return;
        }
        
        float endValue = fadeIn ? 1 : 0;
        
        
        _hudSequence = Sequence.Create()
                .Group(Tween.Alpha(hudGroup, hudGroup.alpha, endValue, hudFadeDuration))
            ;
    
    }
    

    private void ToggleHUD(bool state)
    {
        hudGroup.alpha = state ? 1f : 0;
    }
    
    
    private void UpdateStageTitle(string title)
    {
        if (_waveTitleSequence.isAlive) _waveTitleSequence.Stop();
            
        waveTitleText.alpha = 0f;
        waveTitleText.text = title;
        
        _waveTitleSequence = Sequence.Create()
            .Group(Tween.Alpha(waveTitleText, startDelay: 0.5f, startValue: 0, endValue: 1, duration: waveTitleAnimationDuration/0.6f))
            .Chain(Tween.Alpha(waveTitleText, 0, waveTitleAnimationDuration/0.4f));
    }
    
    
    
    

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        FadeHUD(stage.ShowHUD);
        UpdateStageTitle(stage.StageTitle);
    }
    
    
    private void OnDeath()
    {
        FadeHUD(false);
    }
    
    private void OnPlayerHealthChanged(int currentHealth)
    {
        if (_playerHealthSequence.isAlive) _playerHealthSequence.Stop();
        _playerHealthSequence = Sequence.Create()
            .Group(Tween.PunchScale(healthIcon.transform, strength: Vector3.one * healthPunchStrength, duration: healthPunchDuration));
        
        if (currentHealth > _playerHealth)
        {
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
            Tween.PunchScale(shieldIcon.transform, strength: Vector3.one * shieldPunchStrength, duration: shieldPunchDuration);
        }

    }
    

    private void SpecialWeaponSystemSwitched(WeaponInstance previousWeaponInstance, WeaponInstance newWeaponInstance)
    {
        if (newWeaponInstance != null)
        {
            weaponIcon.sprite = newWeaponInstance.WeaponData.WeaponIcon;
            Tween.Alpha(secondaryWeaponIcon, endValue: 1f, duration: weaponAnimationDuration);
            Tween.PunchScale(weaponIcon.transform, strength: Vector3.one * weaponPunchStrength, duration: weaponPunchDuration);
        }
        else
        {
            if (player.WeaponSystem.BaseWeaponInstance != null)
            {
                weaponIcon.sprite = player.WeaponSystem.BaseWeaponInstance.WeaponData.WeaponIcon;
               secondaryWeaponIcon.sprite = player.WeaponSystem.BaseWeaponInstance.WeaponData.WeaponIcon;
            }
            Tween.Alpha(secondaryWeaponIcon, endValue: 0f, duration: weaponAnimationDuration);
        }
    }
    
        
    private void SpecialWeaponSystemDisabled(WeaponInstance weapon)
    {
        if (player.WeaponSystem.BaseWeaponInstance != null) weaponIcon.sprite = player.WeaponSystem.BaseWeaponInstance.WeaponData.WeaponIcon;
        Tween.Alpha(secondaryWeaponIcon, endValue: 0f, duration: weaponAnimationDuration);

    }

    private void BaseWeaponSystemSwitched(WeaponInstance weapon)
    {
        if (weapon == null) return;
        
        if (player.WeaponSystem.CurrentSpecialWeaponInstance != null)
        {
            secondaryWeaponIcon.sprite = weapon.WeaponData.WeaponIcon;
            Tween.Alpha(secondaryWeaponIcon, endValue: 1f, duration: weaponAnimationDuration);
        }
        else
        {
            weaponIcon.sprite = weapon.WeaponData.WeaponIcon;
            Tween.Alpha(secondaryWeaponIcon, endValue: 0f, duration: weaponAnimationDuration);
        }
    }

    private void SpecialWeaponSystemCooldownUpdated(WeaponInstance specialWeaponInstance, float cooldown)
    {
        if (specialWeaponInstance == null) return;
        
        float fillAmount = 1f - (cooldown / specialWeaponInstance.WeaponData.FireRate);
        weaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
    }
    
    private void BaseWeaponSystemCooldownUpdated(WeaponInstance baseWeaponInstance, float cooldown)
    {
        float fillAmount = 1f - (cooldown / baseWeaponInstance.WeaponData.FireRate);
        
        if (player.WeaponSystem.CurrentSpecialWeaponInstance != null)
        {
            secondaryWeaponIcon.color = Color.Lerp(Color.clear, _secondaryWeaponStartColor, fillAmount);
        }
        else
        {
            weaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
        }
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
    
    private void OnDodge()
    {
        dodgeIcon.color = cooldownIconColor;
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
    
    
}