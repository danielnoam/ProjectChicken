
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
    [SerializeField] private CanvasGroup pauseGroup;
    [SerializeField] private TextMeshProUGUI keybindsText;
    
    [Header("Health")]
    [SerializeField] private bool useTextForHealth;
    [SerializeField] private float healthAnimationDuration = 0.7f;
    [SerializeField] private float healthPunchDuration = 0.2f;
    [SerializeField] private float healthPunchStrength = 0.5f;
    [SerializeField] private Transform healthIconHolder;
    [SerializeField] private Image healthIcon;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthIconPrefab;
    
    [Header("Shield")]
    [SerializeField] private float shieldAnimationDuration = 0.7f;
    [SerializeField] private float shieldPunchDuration = 0.2f;
    [SerializeField] private float shieldPunchStrength = 0.5f;
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
    [SerializeField] private float dodgeAnimationDuration = 0.2f;
    [SerializeField] private float dodgePunchStrength = 0.2f;
    [SerializeField] private Image dodgeIcon;
    [SerializeField] private TextMeshProUGUI dodgeCountText;
    
    [Header("Weapons")]
    [SerializeField] private float weaponAnimationDuration = 0.2f;
    [SerializeField] private float weaponPunchStrength = 0.2f;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image secondaryWeaponIcon;
    
    [Header("Overheat Bar")]
    [SerializeField] private float heatBarAnimationDuration = 0.2f;
    [SerializeField] private float heatBarPunchDuration = 0.2f;
    [SerializeField] private float heatBarPunchStrength = 0.2f;
    [SerializeField] private Color heatedBarColor = Color.red;
    [SerializeField] private Color normalBarColor = Color.white;
    [SerializeField] private Image heatBar;
    [SerializeField] private TextMeshProUGUI barText;
    
    [Header("Overheat MiniGame")]
    [SerializeField] private float miniGameAnimationDuration = 0.2f;
    [SerializeField] private float miniGamePunchDuration = 0.3f;
    [SerializeField] private float miniGamePunchStrength = 0.2f;
    [SerializeField] private Color miniGameActiveColor = Color.blue;
    [SerializeField] private Color miniGameInactiveColor = Color.clear;
    [SerializeField] private Image miniGameWindow;
    
    [Header("Pause Icon")]
    [SerializeField] private float pauseIconAnimationDuration = 0.2f;
    [SerializeField] private Image pauseIconFill;
    
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
    
    [Header("Scene References")] 
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;

    
    private readonly Dictionary<Image, bool> _healthIcons = new Dictionary<Image, bool>();
    private Color _secondaryWeaponStartColor;
    private Color _weaponStartColor;
    private Color _dodgeStartColor;
    private Color _heatBarTextStartColor;
    private Sequence _keybindsSequence;
    private Sequence _hudSequence;
    private Sequence _heatBarSequence;
    private Sequence _scoreSequence;
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
    private float _overheatBarHeight;


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
            player.OnDeath += OnDeath;
            player.OnHealthChanged += OnUpdateHealth;
            player.OnShieldChanged += OnUpdateShield;
            player.OnCurrencyChanged += OnUpdateCurrency;
            player.OnPauseTimerChanged += OnPauseTimerChanged;
            player.PlayerWeapon.OnSpecialWeaponSwitched += OnSpecialWeaponSwitched;
            player.PlayerWeapon.OnSpecialWeaponCooldownUpdated += OnSpecialWeaponCooldownUpdated;
            player.PlayerWeapon.OnBaseWeaponCooldownUpdated += OnBaseWeaponCooldownUpdated;
            player.PlayerWeapon.OnBaseWeaponSwitched += OnBaseWeaponSwitched;
            player.PlayerWeapon.OnSpecialWeaponDisabled += OnSpecialWeaponDisabled;
            player.PlayerWeapon.OnWeaponHeatUpdated += OnWeaponHeatUpdated;
            player.PlayerWeapon.OnWeaponOverheated += OnWeaponOverheated;
            player.PlayerWeapon.OnWeaponHeatReset += OnWeaponHeatReset;
            player.PlayerWeapon.OnWeaponHeatMiniGameWindowCreated += OnWeaponHeatMiniGameWindowCreated;
            player.PlayerWeapon.OnWeaponHeatMiniGameSucceeded += OnOnWeaponHeatMiniGameSucceeded;
            player.PlayerWeapon.OnWeaponHeatMiniGameFailed += OnOnWeaponHeatMiniGameFailed;
            player.PlayerMovement.OnDodgeCooldownUpdated += OnDodgeCooldownUpdated;
            player.PlayerMovement.OnDodge += OnDodge;
            player.PlayerMovement.OnDodgeCountChanged += OnDodgeCountChanged;
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
            player.OnDeath -= OnDeath;
            player.OnHealthChanged -= OnUpdateHealth;
            player.OnShieldChanged -= OnUpdateShield;
            player.OnCurrencyChanged -= OnUpdateCurrency;
            player.OnPauseTimerChanged -= OnPauseTimerChanged;
            player.PlayerWeapon.OnSpecialWeaponSwitched -= OnSpecialWeaponSwitched;
            player.PlayerWeapon.OnSpecialWeaponCooldownUpdated -= OnSpecialWeaponCooldownUpdated;
            player.PlayerWeapon.OnBaseWeaponCooldownUpdated -= OnBaseWeaponCooldownUpdated;
            player.PlayerWeapon.OnWeaponHeatUpdated -= OnWeaponHeatUpdated;
            player.PlayerWeapon.OnWeaponOverheated -= OnWeaponOverheated;
            player.PlayerWeapon.OnWeaponHeatReset -= OnWeaponHeatReset;
            player.PlayerWeapon.OnWeaponHeatMiniGameWindowCreated -= OnWeaponHeatMiniGameWindowCreated;
            player.PlayerWeapon.OnBaseWeaponSwitched -= OnBaseWeaponSwitched;
            player.PlayerWeapon.OnSpecialWeaponDisabled -= OnSpecialWeaponDisabled;
            player.PlayerMovement.OnDodgeCooldownUpdated -= OnDodgeCooldownUpdated;
            player.PlayerMovement.OnDodge -= OnDodge;
            player.PlayerMovement.OnDodgeCountChanged -= OnDodgeCountChanged;
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
        _overheatBarHeight = heatBar.rectTransform.sizeDelta.y;
        miniGameWindow.color = miniGameInactiveColor;
        _weaponStartColor = weaponIcon.color;
        _secondaryWeaponStartColor = secondaryWeaponIcon.color;
        _dodgeStartColor = dodgeIcon.color;
        _heatBarTextStartColor = barText.color;
        heatBar.fillAmount = 0f;
        pauseGroup.alpha = 0f;
        pauseIconFill.fillAmount = 0f;
        waveTitleText.alpha = 0f;
        _previousScore = 0;
        _score = 0;
        _previousPlayerCurrency = 0;
        _playerCurrency = 0;
        _playerShield = 0f;
        dodgeCountText.text = "";
        SetupHeartIcons();
        
        ToggleHUD(false);
        ToggleKeybinds(false);
    }


    private void SetupHeartIcons()
    {
        if (!player || useTextForHealth || !healthIconPrefab) return;

        if (healthIconHolder.childCount > 0)
        {
            
            foreach (Transform child in healthIconHolder)
            {
                Destroy(child.gameObject);
            }
        }
        
        for (int health = 0; health < player.MaxHealth; health++)
        {
            var healthObject = Instantiate(healthIconPrefab, healthIconHolder);
            _healthIcons[healthObject] = false; 
        }

        healthIcon = null;
        healthText = null;
    }

    
    
    
    
    #region HUD --------------------------------------------------------------------------------

    private void OnStageChanged(SOLevelStage stage)
    {
        if (!stage) return;
        
        switch (stage.StageType)
        {
            case StageType.Intro:
                FadeHUD(false);
                FadeKeybinds(false);
                break;
            case StageType.Outro:
                FadeHUD(false);
                FadeKeybinds(false);
                break;
            case StageType.Checkpoint:
                FadeHUD(true);
                FadeKeybinds(stage.ShowPlayerKeybinds);
                break;
            case StageType.EnemyWave:
                FadeHUD(true);
                FadeKeybinds(stage.ShowPlayerKeybinds);
                break;
        }
        
        UpdateStageTitle(stage.StageTitle);
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
    
    private void FadeKeybinds(bool fadeIn)
    {
        if (_keybindsSequence.isAlive) _keybindsSequence.Stop();
        
        switch (fadeIn)
        {
            case true when keybindsText.alpha >= 1:
            case false when keybindsText.alpha <= 0:
                return;
        }
        
        float endValue = fadeIn ? 1 : 0;
        
        _keybindsSequence = Sequence.Create()
                .Group(Tween.Alpha(keybindsText, keybindsText.alpha, endValue, hudFadeDuration))
            ;
    
    }

    private void ToggleHUD(bool state)
    {
        hudGroup.alpha = state ? 1f : 0;
    }
    
    private void ToggleKeybinds(bool state)
    {
        keybindsText.alpha = state ? 1f : 0;
    }

    #endregion HUD --------------------------------------------------------------------------------
    

    #region Player UI ----------------------------------------------------------------------------------


    private void OnDeath()
    {
        FadeHUD(false);
    }
    
    private void OnUpdateHealth(int currentHealth)
    {
        if (!useTextForHealth)
        {
            if (_healthIcons.Count == 0)
            {
                SetupHeartIcons();
            }
            
            int index = 0;
            foreach (var icon in _healthIcons.Keys.ToList())
            {
                if (index < currentHealth) // If the heart is below the health, you should see it
                {
                    if (!_healthIcons[icon]) // If it's not shown, fade in
                    {
                        _healthIcons[icon] = true; // Set to true when shown
                    
                        float bounceUpDuration = healthAnimationDuration * 0.8f;
                        float bounceDownDuration = healthAnimationDuration * 0.2f;
                    
                        Sequence.Create()
                            .Group(Tween.Alpha(icon, endValue: 1f, duration: healthAnimationDuration / 0.5f))
                            .Group(Tween.Scale(icon.transform, endValue: Vector3.one * 1.4f, bounceUpDuration, ease: Ease.OutBounce))
                            .Group(Tween.Scale(icon.transform, endValue: Vector3.one, bounceDownDuration, ease: Ease.InOutSine ,startDelay: bounceUpDuration -0.05f))
                            ;
                    }
                }
                else // else hide it
                {
                    if (_healthIcons[icon]) // If it's shown, fade out
                    {
                        _healthIcons[icon] = false; // Set to false when hidden
                    
                        Tween.Alpha(icon, endValue: 0f, duration: healthAnimationDuration);
                        Tween.Scale(icon.transform, endValue: Vector3.zero, healthAnimationDuration, ease: Ease.OutQuint);
                    }
                }
            
                index++;
            }
        }
        else
        {
            if (healthIcon && _playerHealth != currentHealth)
            {
                Tween.PunchScale(healthIcon.transform, strength: Vector3.one * healthPunchStrength, duration: healthPunchDuration);
            }
        }
        
        _playerHealth = currentHealth;

    }

    private void OnUpdateShield(float currentShield)
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
        
        if (shieldIcon && currentShield >= player.MaxShieldHealth - 1)
        {
            Tween.PunchScale(shieldIcon.transform, strength: Vector3.one * shieldPunchStrength, duration: shieldPunchDuration);
        }

    }
    

    private void OnSpecialWeaponSwitched(WeaponInstance previousWeaponInstance, WeaponInstance newWeaponInstance)
    {
        if (newWeaponInstance != null)
        {
            weaponIcon.sprite = newWeaponInstance.WeaponData.WeaponIcon;
            Tween.Alpha(secondaryWeaponIcon, endValue: 1f, duration: weaponAnimationDuration);
            Tween.PunchScale(weaponIcon.transform, strength: Vector3.one * weaponPunchStrength, duration: weaponAnimationDuration);
        }
        else
        {
            if (player.PlayerWeapon.BaseWeaponInstance != null)
            {
                weaponIcon.sprite = player.PlayerWeapon.BaseWeaponInstance.WeaponData.WeaponIcon;
               secondaryWeaponIcon.sprite = player.PlayerWeapon.BaseWeaponInstance.WeaponData.WeaponIcon;
            }
            Tween.Alpha(secondaryWeaponIcon, endValue: 0f, duration: weaponAnimationDuration);
        }
    }
    
        
    private void OnSpecialWeaponDisabled(WeaponInstance weapon)
    {
        if (player.PlayerWeapon.BaseWeaponInstance != null) weaponIcon.sprite = player.PlayerWeapon.BaseWeaponInstance.WeaponData.WeaponIcon;
        Tween.Alpha(secondaryWeaponIcon, endValue: 0f, duration: weaponAnimationDuration);

    }

    private void OnBaseWeaponSwitched(WeaponInstance weapon)
    {
        if (weapon == null) return;
        
        if (player.PlayerWeapon.CurrentSpecialWeaponInstance != null)
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

    private void OnSpecialWeaponCooldownUpdated(WeaponInstance specialWeaponInstance, float cooldown)
    {
        if (specialWeaponInstance == null) return;
        
        float fillAmount = 1f - (cooldown / specialWeaponInstance.WeaponData.FireRate);
        weaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
    }
    
    private void OnBaseWeaponCooldownUpdated(WeaponInstance baseWeaponInstance, float cooldown)
    {
        float fillAmount = 1f - (cooldown / baseWeaponInstance.WeaponData.FireRate);
        
        if (player.PlayerWeapon.CurrentSpecialWeaponInstance != null)
        {
            secondaryWeaponIcon.color = Color.Lerp(Color.clear, _secondaryWeaponStartColor, fillAmount);
        }
        else
        {
            weaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
        }
    }
    
    private void OnWeaponHeatUpdated(float heat)
    {
        barText.text = $"{heat:F0}%";
        
        float fillAmount = heat / player.PlayerWeapon.MaxWeaponHeat;
        Color barFillColor = Color.Lerp(normalBarColor, heatedBarColor, fillAmount);
        Color textFillColor = Color.Lerp(_heatBarTextStartColor, heatedBarColor,fillAmount);
        float textAlpha = fillAmount < 0.3f ? 0f : Mathf.Lerp(0f, 1f, (fillAmount - 0.3f) / 0.7f);
        
        if (_heatBarSequence.isAlive) _heatBarSequence.Stop();
        _heatBarSequence = Sequence.Create()
                .Group(Tween.Color(barText, startValue: barText.color, endValue: textFillColor, heatBarAnimationDuration))
                .Group(Tween.Alpha(barText, startValue: barText.color.a, endValue: textAlpha, heatBarAnimationDuration))
                .Group(Tween.Color(heatBar, startValue: heatBar.color, endValue: barFillColor, heatBarAnimationDuration))
                .Group(Tween.UIFillAmount(heatBar, startValue: heatBar.fillAmount, endValue: fillAmount, heatBarAnimationDuration))
            ;
    }
    
    
    private void OnOnWeaponHeatMiniGameFailed()
    {
        Tween.Color(miniGameWindow, startValue: miniGameWindow.color, endValue: miniGameInactiveColor, miniGameAnimationDuration);
    }

    private void OnOnWeaponHeatMiniGameSucceeded()
    {
        Tween.Color(miniGameWindow, startValue: miniGameWindow.color, endValue: miniGameInactiveColor, miniGameAnimationDuration);
    }



    private void OnWeaponHeatMiniGameWindowCreated(float regenTime, float windowDuration, float windowStartTime)
    {
        float normalizedWindowSize = Mathf.Clamp01(windowDuration / regenTime);
        float windowHeight = _overheatBarHeight * normalizedWindowSize;

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
        Tween.Color(miniGameWindow, startValue: miniGameWindow.color, endValue: miniGameActiveColor, miniGameAnimationDuration);
    }
    
    
    private void OnWeaponOverheated()
    {
        Tween.PunchScale(barText.transform, strength: Vector3.one * heatBarPunchStrength, duration: heatBarPunchDuration);
        
        Tween.PunchScale(heatBar.transform, strength: Vector3.one * heatBarPunchStrength, duration: heatBarPunchDuration);
        
    }
    
    private void OnWeaponHeatReset()
    {
        Tween.PunchScale(heatBar.transform, strength: Vector3.one * heatBarPunchStrength, duration: heatBarPunchDuration);
        
        Tween.Color(miniGameWindow, startValue: miniGameWindow.color, endValue: miniGameInactiveColor, miniGameAnimationDuration);
    }

    
    private void OnUpdateCurrency(int newCurrency)
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
            Tween.PunchScale(dodgeIcon.transform, strength: Vector3.one * dodgePunchStrength, duration: dodgeAnimationDuration);
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
        Tween.PunchScale(dodgeCountText.transform, strength: Vector3.one * dodgePunchStrength, duration: dodgeAnimationDuration);
    }

    #endregion Player UI ----------------------------------------------------------------------------------

    
    #region Level UI ----------------------------------------------------------------------------------

    private void OnScoreChanged(int newScore)
    {
        int scoreDifference = newScore - _previousScore;
        if (scoreDifference >= bigScoreDifference)
        {
            if (_scoreSequence.isAlive) _scoreSequence.Stop();
            _scoreSequence = Sequence.Create()
                
                    .Group(Tween.Custom(startValue: _previousScore, endValue: newScore, duration: scoreAnimationDuration, onValueChange: value => _score = Mathf.RoundToInt(value)))
                    .Chain(Tween.PunchScale(scoreText.transform, strength: Vector3.one * scorePunchStrength, duration: scorePunchDuration))
                    .OnComplete(() => _previousScore = newScore)
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
    
    private void UpdateStageTitle(string title)
    {
        
        if (title == "" && _waveTitleSequence.isAlive)
        {
            _waveTitleSequence.OnComplete(() => waveTitleText.text = title);
        }
        else
        {
            if (_waveTitleSequence.isAlive) _waveTitleSequence.Stop();
            waveTitleText.text = title;
            _waveTitleSequence = Sequence.Create()
                    .Group(Tween.Alpha(waveTitleText, startDelay: 0.5f, startValue: 0, endValue: 1, duration:waveTitleAnimationDuration/0.6f))
                    .Chain(Tween.Alpha(waveTitleText, 0, waveTitleAnimationDuration/0.4f));
        }

    }

    #endregion Level UI ----------------------------------------------------------------------------------
}