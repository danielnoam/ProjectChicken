using System;
using System.Collections.Generic;
using System.Linq;
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
    
    
    [Foldout("Effects")]
    [Header("General")]
    [SerializeField] private float hudFadeDuration = 3f;
    [SerializeField] private Color cooldownIconColor = Color.grey;
    
    [Header("Health")]
    [SerializeField] private float healthAnimationDuration = 0.5f;
    
    [Header("Shield")]
    [SerializeField] private float shieldPunchDuration = 0.2f;
    [SerializeField] private float shieldPunchStrength = 0.5f;
    
    [Header("Currency")]
    [SerializeField] private float currencyAnimationDuration = 0.7f;
    [SerializeField] private float currencyPunchDuration = 0.2f;
    [SerializeField] private float currencyPunchStrength = 0.2f;
    [SerializeField, Min(0), Tooltip("The difference between the previous currency and the current currency that must be reached to trigger a big currency animation")] 
    private int bigCurrencyDifference = 5;
    
    [Header("Dodge")]
    [SerializeField] private float dodgeAnimationDuration = 0.2f;
    [SerializeField] private float dodgePunchStrength = 0.2f;
    
    [Header("Weapons")]
    [SerializeField] private float weaponAnimationDuration = 0.2f;
    [SerializeField] private float weaponPunchStrength = 0.2f;
    
    [Header("Overheat Bar")]
    [SerializeField] private float heatBarAnimationDuration = 0.2f;
    [SerializeField] private float heatBarPunchDuration = 0.2f;
    [SerializeField] private float heatBarPunchStrength = 0.2f;
    [SerializeField] private Color heatedBarColor = Color.red;
    [SerializeField] private Color normalBarColor = Color.white;
    
    [Header("Overheat MiniGame")]
    [SerializeField] private float miniGameAnimationDuration = 0.2f;
    [SerializeField] private float miniGamePunchDuration = 0.3f;
    [SerializeField] private float miniGamePunchStrength = 0.2f;
    [SerializeField] private Color miniGameActiveColor = Color.blue;
    [SerializeField] private Color miniGameInactiveColor = Color.clear;
    
    [Header("Score")]
    [SerializeField] private float scoreAnimationDuration = 0.2f;
    [SerializeField] private float scorePunchDuration = 0.2f;
    [SerializeField] private float scorePunchStrength = 0.2f;
    [SerializeField, Min(0), Tooltip("The difference between the previous score and the current score that must be reached to trigger a big score animation")] 
    private int bigScoreDifference = 200;
    [SerializeField, Min(0), Tooltip("How many 0 is the score made out of")] private int scoreDigits = 7;
    [EndFoldout]
    
    [Header("Asset References")] 
    [SerializeField] private Image playerIconPrefab;
    [SerializeField] private Sprite heartIcon;
    
    [Header("Child References")] 
    [SerializeField, Child(Flag.Editable)] private CanvasGroup hudGroup;
    [SerializeField] private Transform playerHealthHolder;
    [SerializeField] private Image playerShieldIcon;
    [SerializeField] private Image playerWeaponIcon;
    [SerializeField] private Image playerSecondaryWeaponIcon;
    [SerializeField] private Image playerCurrencyIcon;
    [SerializeField] private Image playerHeatBar;
    [SerializeField] private Image playerMiniGameWindow;
    [SerializeField] private Image playerDodgeIcon;
    [SerializeField] private TextMeshProUGUI heatBarText;
    [SerializeField] private TextMeshProUGUI playerShieldText;
    [SerializeField] private TextMeshProUGUI playerCurrencyText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI keybindsText;
    
    [Header("Scene References")] 
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RailPlayer player;

    
    private Dictionary<Image, bool> _healthIcons;
    private Color _secondaryWeaponStartColor;
    private Color _weaponStartColor;
    private Color _dodgeStartColor;
    private Color _heatBarTextStartColor;
    private Sequence _keybindsSequence;
    private Sequence _hudSequence;
    private Sequence _heatBarSequence;
    private Sequence _scoreSequence;
    private Sequence _playerCurrencySequence;
    private int _previousScore;
    private int _score;
    private int _previousPlayerCurrency;
    private int _playerCurrency;
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
        
        PrimeTweenConfig.warnEndValueEqualsCurrent = false;
        SetUpUI();
    }

    private void Start()
    {
        if (player)
        {
            OnUpdateHealth(player.MaxHealth);
            OnUpdateShield(player.MaxShieldHealth);
            OnSpecialWeaponSwitched(null, null);
            OnWeaponHeatUpdated(0);
            OnUpdateCurrency(player.CurrentCurrency);
            OnDodgeCooldownUpdated(player.GetDodgeMaxCooldown());
        }
        
        if (levelManager)
        {
            OnScoreChanged(0);
        }
    }


    private void OnEnable()
    {
        if (player)
        {
            player.OnHealthChanged += OnUpdateHealth;
            player.OnShieldChanged += OnUpdateShield;
            player.OnCurrencyChanged += OnUpdateCurrency;
            player.OnSpecialWeaponSwitched += OnSpecialWeaponSwitched;
            player.OnSpecialWeaponCooldownUpdated += OnSpecialWeaponCooldownUpdated;
            player.OnBaseWeaponCooldownUpdated += OnBaseWeaponCooldownUpdated;
            player.OnBaseWeaponSwitched += OnBaseWeaponSwitched;
            player.OnSpecialWeaponDisabled += OnSpecialWeaponDisabled;
            player.OnWeaponHeatUpdated += OnWeaponHeatUpdated;
            player.OnWeaponOverheated += OnWeaponOverheated;
            player.OnWeaponHeatReset += OnWeaponHeatReset;
            player.OnWeaponHeatMiniGameWindowCreated += OnWeaponHeatMiniGameWindowCreated;
            player.OnWeaponHeatMiniGameSucceeded += OnOnWeaponHeatMiniGameSucceeded;
            player.OnWeaponHeatMiniGameFailed += OnOnWeaponHeatMiniGameFailed;
            player.OnDodgeCooldownUpdated += OnDodgeCooldownUpdated;
            player.OnDodge += OnDodge;
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
            player.OnHealthChanged -= OnUpdateHealth;
            player.OnShieldChanged -= OnUpdateShield;
            player.OnCurrencyChanged -= OnUpdateCurrency;
            player.OnSpecialWeaponSwitched -= OnSpecialWeaponSwitched;
            player.OnSpecialWeaponCooldownUpdated -= OnSpecialWeaponCooldownUpdated;
            player.OnBaseWeaponCooldownUpdated -= OnBaseWeaponCooldownUpdated;
            player.OnWeaponHeatUpdated -= OnWeaponHeatUpdated;
            player.OnWeaponOverheated -= OnWeaponOverheated;
            player.OnWeaponHeatReset -= OnWeaponHeatReset;
            player.OnWeaponHeatMiniGameWindowCreated -= OnWeaponHeatMiniGameWindowCreated;
            player.OnBaseWeaponSwitched -= OnBaseWeaponSwitched;
            player.OnSpecialWeaponDisabled -= OnSpecialWeaponDisabled;
            player.OnDodgeCooldownUpdated -= OnDodgeCooldownUpdated;
            player.OnDodge -= OnDodge;
        }
        
        if (levelManager)
        {
            levelManager.OnScoreChanged -= OnScoreChanged;
            levelManager.OnStageChanged -= OnStageChanged;
        }
    }
    


    private void Update()
    {
        scoreText.text = _score.ToString($"D{scoreDigits}"); // Shows right now up to a million
        playerCurrencyText.text = _playerCurrency.ToString();
    }




    #region SetUp -----------------------------------------------------------------------------------

    private void SetUpUI()
    {
        if (!player) return;
        
        // Clear existing health icons if any
        foreach (Transform child in playerHealthHolder)
        {
            Destroy(child.gameObject);
        }
        
        // Create new icons and cache references
        _healthIcons = new Dictionary<Image, bool>();
        for (int health = 0; health < player.MaxHealth; health++)
        {
            var healthObject = Instantiate(playerIconPrefab, playerHealthHolder);
            healthObject.name = $"HealthIcon{health}";
            healthObject.sprite = heartIcon;
                
            _healthIcons[healthObject] = false; 
        }
            
        _overheatBarHeight = playerHeatBar.rectTransform.sizeDelta.y;
        playerMiniGameWindow.color = miniGameInactiveColor;
        _weaponStartColor = playerWeaponIcon.color;
        _secondaryWeaponStartColor = playerSecondaryWeaponIcon.color;
        _dodgeStartColor = playerDodgeIcon.color;
        _heatBarTextStartColor = heatBarText.color;
        _previousScore = 0;
        _score = 0;
        _previousPlayerCurrency = 0;
        _playerCurrency = 0;

        ToggleHUD(false);
        ToggleKeybinds(false);
    }
    

    #endregion SetUp -----------------------------------------------------------------------------------


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

    private void OnUpdateHealth(int currentHealth)
    {
        int index = 0;
        foreach (var healthIcon in _healthIcons.Keys.ToList())
        {
            if (index < currentHealth) // If the heart is below the health you should see it
            {
                if (!_healthIcons[healthIcon]) // If it's not shown, fade in
                {
                    _healthIcons[healthIcon] = true; // Set to true when shown
                    
                    float bounceUpDuration = (healthAnimationDuration * 0.8f)/1.5f;
                    float bounceDownDuration = (healthAnimationDuration * 0.2f)/1.5f;
                    
                    Sequence.Create()
                        .Group(Tween.Alpha(healthIcon, endValue: 1f, duration: healthAnimationDuration))
                        .Chain(Tween.Scale(healthIcon.transform, endValue: Vector3.one * 1.5f, bounceUpDuration, ease: Ease.InOutSine))
                        .Chain(Tween.Scale(healthIcon.transform, endValue: Vector3.one, bounceDownDuration, ease: Ease.OutBounce))
                    ;
                }
            }
            else // else hide it
            {
                if (_healthIcons[healthIcon]) // If it's shown, fade out
                {
                    _healthIcons[healthIcon] = false; // Set to false when hidden
                    
                    Tween.Alpha(healthIcon, endValue: 0f, duration: healthAnimationDuration);
                    Tween.Scale(healthIcon.transform, endValue: Vector3.zero, healthAnimationDuration, ease: Ease.OutQuint);
                }
            }
            
            index++;
        }
    }

    private void OnUpdateShield(float currentShield)
    {
        playerShieldText.text = $"{currentShield:F0}%";

        if (currentShield >= player.MaxShieldHealth)
        {
            Tween.PunchScale(playerShieldIcon.transform, strength: Vector3.one * shieldPunchStrength, duration: shieldPunchDuration);
        }
    }

    private void OnSpecialWeaponSwitched(WeaponInstance previousWeaponInstance, WeaponInstance newWeaponInstance)
    {
        if (newWeaponInstance != null)
        {
            playerWeaponIcon.sprite = newWeaponInstance.weaponData.WeaponIcon;
            Tween.Alpha(playerSecondaryWeaponIcon, endValue: 1f, duration: weaponAnimationDuration);
            Tween.PunchScale(playerWeaponIcon.transform, strength: Vector3.one * weaponPunchStrength, duration: weaponAnimationDuration);
        }
        else
        {
            if (player.GetCurrentBaseWeapon() != null)
            {
                playerWeaponIcon.sprite = player.GetCurrentBaseWeapon().weaponData.WeaponIcon;
               playerSecondaryWeaponIcon.sprite = player.GetCurrentBaseWeapon().weaponData.WeaponIcon;
            }
            Tween.Alpha(playerSecondaryWeaponIcon, endValue: 0f, duration: weaponAnimationDuration);
        }
    }
    
        
    private void OnSpecialWeaponDisabled(WeaponInstance weapon)
    {
        if (player.GetCurrentBaseWeapon() != null) playerWeaponIcon.sprite = player.GetCurrentBaseWeapon().weaponData.WeaponIcon;
        Tween.Alpha(playerSecondaryWeaponIcon, endValue: 0f, duration: weaponAnimationDuration);

    }

    private void OnBaseWeaponSwitched(WeaponInstance weapon)
    {
        if (weapon == null) return;
        
        if (player.HasSpecialWeapon())
        {
            playerSecondaryWeaponIcon.sprite = weapon.weaponData.WeaponIcon;
            Tween.Alpha(playerSecondaryWeaponIcon, endValue: 1f, duration: weaponAnimationDuration);
        }
        else
        {
            playerWeaponIcon.sprite = weapon.weaponData.WeaponIcon;
            Tween.Alpha(playerSecondaryWeaponIcon, endValue: 0f, duration: weaponAnimationDuration);
        }
    }

    private void OnSpecialWeaponCooldownUpdated(WeaponInstance specialWeaponInstance, float cooldown)
    {
        if (specialWeaponInstance == null) return;
        
        float fillAmount = 1f - (cooldown / specialWeaponInstance.weaponData.FireRate);
        playerWeaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
    }
    
    private void OnBaseWeaponCooldownUpdated(WeaponInstance baseWeaponInstance, float cooldown)
    {
        float fillAmount = 1f - (cooldown / baseWeaponInstance.weaponData.FireRate);
        
        if (player.GetCurrentSpecialWeapon() != null)
        {
            playerSecondaryWeaponIcon.color = Color.Lerp(Color.clear, _secondaryWeaponStartColor, fillAmount);
        }
        else
        {
            playerWeaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
        }
    }
    
    private void OnWeaponHeatUpdated(float heat)
    {
        heatBarText.text = $"{heat:F0}%";
        
        float fillAmount = heat / player.GetMaxWeaponHeat();
        Color barFillColor = Color.Lerp(normalBarColor, heatedBarColor, fillAmount);
        Color textFillColor = Color.Lerp(_heatBarTextStartColor, heatedBarColor,fillAmount);
        float textAlpha = fillAmount < 0.3f ? 0f : Mathf.Lerp(0f, 1f, (fillAmount - 0.3f) / 0.7f);
        
        if (_heatBarSequence.isAlive) _heatBarSequence.Stop();
        _heatBarSequence = Sequence.Create()
                .Group(Tween.Color(heatBarText, startValue: heatBarText.color, endValue: textFillColor, heatBarAnimationDuration))
                .Group(Tween.Alpha(heatBarText, startValue: heatBarText.color.a, endValue: textAlpha, heatBarAnimationDuration))
                .Group(Tween.Color(playerHeatBar, startValue: playerHeatBar.color, endValue: barFillColor, heatBarAnimationDuration))
                .Group(Tween.UIFillAmount(playerHeatBar, startValue: playerHeatBar.fillAmount, endValue: fillAmount, heatBarAnimationDuration))
            ;
    }
    
    
    private void OnOnWeaponHeatMiniGameFailed()
    {
        Tween.Color(playerMiniGameWindow, startValue: playerMiniGameWindow.color, endValue: miniGameInactiveColor, miniGameAnimationDuration);
    }

    private void OnOnWeaponHeatMiniGameSucceeded()
    {
        Tween.Color(playerMiniGameWindow, startValue: playerMiniGameWindow.color, endValue: miniGameInactiveColor, miniGameAnimationDuration);
    }



    private void OnWeaponHeatMiniGameWindowCreated(float regenTime, float windowDuration, float windowStartTime)
    {
        float normalizedWindowSize = Mathf.Clamp01(windowDuration / regenTime);
        float windowHeight = _overheatBarHeight * normalizedWindowSize;

        // Set the size of the mini-game window
        playerMiniGameWindow.rectTransform.sizeDelta = new Vector2(
            playerMiniGameWindow.rectTransform.sizeDelta.x, 
            windowHeight
        );

        // Calculate the position based on windowStartTime
        // Since the heat bar fills from bottom (0) to top (1), and the timing counts down from regenTime to 0,
        // we need to invert the position calculation
        float normalizedEndPosition = Mathf.Clamp01((windowStartTime - windowDuration) / regenTime);
    
        // Calculate the center position of the window (halfway between start and end)
        float windowCenterPosition = normalizedEndPosition + (normalizedWindowSize * 0.5f);
    
        // Calculate the Y offset from center of the heat bar
        // Map from 0-1 range to the actual pixel range of the heat bar
        float yOffset = (windowCenterPosition - 0.5f) * _overheatBarHeight;

        // Set the anchored position
        playerMiniGameWindow.rectTransform.anchoredPosition = new Vector2(
            playerMiniGameWindow.rectTransform.anchoredPosition.x,
            yOffset
        );
    }
    
    
    private void OnWeaponOverheated()
    {
        Tween.PunchScale(heatBarText.transform, strength: Vector3.one * heatBarPunchStrength, duration: heatBarPunchDuration);
        
        Tween.PunchScale(playerHeatBar.transform, strength: Vector3.one * heatBarPunchStrength, duration: heatBarPunchDuration);
        
        Tween.PunchScale(playerMiniGameWindow.transform, strength: Vector3.one * miniGamePunchStrength, duration: miniGamePunchDuration);
        Tween.Color(playerMiniGameWindow, startValue: playerMiniGameWindow.color, endValue: miniGameActiveColor, miniGameAnimationDuration);
    }
    
    private void OnWeaponHeatReset()
    {
        Tween.PunchScale(playerHeatBar.transform, strength: Vector3.one * heatBarPunchStrength, duration: heatBarPunchDuration);
        
        Tween.Color(playerMiniGameWindow, startValue: playerMiniGameWindow.color, endValue: miniGameInactiveColor, miniGameAnimationDuration);
    }

    
    private void OnUpdateCurrency(int newCurrency)
    {
        int currencyDifferance = newCurrency - _previousPlayerCurrency;
        if (currencyDifferance >= bigCurrencyDifference)
        {
            if (_playerCurrencySequence.isAlive) _playerCurrencySequence.Stop();
            _playerCurrencySequence = Sequence.Create()
                
                    .Group(Tween.Custom(startValue: _previousPlayerCurrency, endValue: newCurrency, duration: currencyAnimationDuration, onValueChange: value => _playerCurrency = Mathf.RoundToInt(value)))
                    .Chain(Tween.PunchScale(playerCurrencyIcon.transform, strength: Vector3.one * currencyPunchStrength, duration: currencyPunchDuration))
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
        float fillAmount = 1f - (cooldown / player.GetDodgeMaxCooldown());
        playerDodgeIcon.color = Color.Lerp(cooldownIconColor, _dodgeStartColor, fillAmount);

        if (Mathf.Approximately(fillAmount, 1f)) 
        {
            Tween.PunchScale(playerDodgeIcon.transform, strength: Vector3.one * dodgePunchStrength, duration: dodgeAnimationDuration);
        }
    }
    
    private void OnDodge()
    {
        playerDodgeIcon.color = cooldownIconColor;
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

    #endregion Level UI ----------------------------------------------------------------------------------
}