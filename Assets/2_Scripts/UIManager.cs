using System;
using System.Collections.Generic;
using System.Linq;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using VHierarchy.Libs;
using VInspector;

public class UIManager : MonoBehaviour
{
    [Foldout("Effects")]
    [Header("General")]
    [SerializeField] private Color cooldownIconColor = Color.grey;
    
    [Header("Health")]
    [SerializeField] private float healthAnimationDuration = 0.5f;
    
    [Header("Shield")]
    [SerializeField] private float shieldAnimationDuration = 0.2f;
    [SerializeField] private float shieldPunchStrength = 0.5f;
    
    [Header("Currency")]
    [SerializeField] private float currencyAnimationDuration = 0.2f;
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
    [SerializeField] private Transform playerHealthHolder;
    [SerializeField] private Image playerShieldIcon;
    [SerializeField] private Image playerWeaponIcon;
    [SerializeField] private Image playerSecondaryWeaponIcon;
    [SerializeField] private Image playerCurrencyIcon;
    [SerializeField] private Image playerHeatBar;
    [SerializeField] private Image playerDodgeIcon;
    [SerializeField] private TextMeshProUGUI playerShieldText;
    [SerializeField] private TextMeshProUGUI playerCurrencyText;
    [SerializeField] private TextMeshProUGUI scoreText;
    
    [Header("Scene References")] 
    [SerializeField, Scene(Flag.Editable)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.Editable)] private RailPlayer player;

    
    private Dictionary<Image, bool> _healthIcons;
    private Color _secondaryWeaponStartColor;
    private Color _weaponStartColor;
    private Color _dodgeStartColor;
    private Sequence _heatBarSequence;
    private Sequence _scoreSequence;
    private Sequence _playerCurrencySequence;
    private int _previousScore;
    private int _score;
    private int _previousPlayerCurrency;
    private int _playerCurrency;
    
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        
        this.ValidateRefs();
    }

    private void Awake()
    {
        PrimeTweenConfig.warnEndValueEqualsCurrent = false;
        SetUpUI();
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
            player.OnWeaponHeatUpdated += OnWeaponHeatUpdated;
            player.OnWeaponOverheated += OnWeaponOverheated;
            player.OnWeaponHeatReset += OnWeaponHeatReset;
            player.OnDodgeCooldownUpdated += OnDodgeCooldownUpdated;
            player.OnDodge += OnDodge;
        }

        if (levelManager)
        {
            levelManager.OnScoreChanged += OnScoreChanged;
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
            player.OnDodgeCooldownUpdated -= OnDodgeCooldownUpdated;
            player.OnDodge -= OnDodge;
        }
        
        if (levelManager)
        {
            levelManager.OnScoreChanged -= OnScoreChanged;
        }
    }

    private void Update()
    {
        scoreText.text = _score.ToString($"D{scoreDigits}"); // Shows right now up to a million
        playerCurrencyText.text = _playerCurrency.ToString();
    }

    private void SetUpUI()
    {
        if (player)
        {
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
                
                _healthIcons[healthObject] = false; // Initially set to false
            }
            

            _weaponStartColor = playerWeaponIcon.color;
            _secondaryWeaponStartColor = playerSecondaryWeaponIcon.color;
            playerSecondaryWeaponIcon.sprite = player.GetCurrentBaseWeapon().WeaponIcon;
            playerSecondaryWeaponIcon.gameObject.SetActive(false);
            _dodgeStartColor = playerDodgeIcon.color;
            _previousScore = 0;
            _score = 0;
            _previousPlayerCurrency = 0;
            _playerCurrency = 0;
            
            // Update 
            OnUpdateHealth(player.MaxHealth);
            OnUpdateShield(player.MaxShieldHealth);
            OnSpecialWeaponSwitched(null,null);
            OnWeaponHeatUpdated(0);
            OnUpdateCurrency(player.CurrentCurrency);
            OnDodgeCooldownUpdated(player.GetDodgeMaxCooldown());
        }

        
        if (levelManager)
        {
            OnScoreChanged(0);
        }
    }

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

        Tween.PunchScale(playerShieldIcon.transform, strength: Vector3.one * shieldPunchStrength, duration: shieldAnimationDuration);
    }

    private void OnSpecialWeaponSwitched(SOWeapon previousWeapon, SOWeapon newWeapon)
    {
        if (newWeapon)
        {
            playerWeaponIcon.sprite = newWeapon.WeaponIcon;
            playerSecondaryWeaponIcon.gameObject.SetActive(true);
            Tween.PunchScale(playerWeaponIcon.transform, strength: Vector3.one * weaponPunchStrength, duration: weaponAnimationDuration);
        }
        else
        {
            playerWeaponIcon.sprite = player.GetCurrentBaseWeapon().WeaponIcon;
            playerSecondaryWeaponIcon.gameObject.SetActive(false);
        }
    }

    private void OnSpecialWeaponCooldownUpdated(SOWeapon specialWeapon, float cooldown)
    {
        float fillAmount = 1f - (cooldown / specialWeapon.FireRate);
        playerWeaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
    }
    
    private void OnBaseWeaponCooldownUpdated(SOWeapon baseWeapon, float cooldown)
    {
        float fillAmount = 1f - (cooldown / baseWeapon.FireRate);
        
        if (player.GetCurrentSpecialWeapon())
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
        float fillAmount = heat / player.GetMaxWeaponHeat();
        Color fillColor = Color.Lerp(normalBarColor, heatedBarColor, fillAmount);
        
        if (_heatBarSequence.isAlive) _heatBarSequence.Stop();
        _heatBarSequence = Sequence.Create()
                .Group(Tween.Color(playerHeatBar, startValue: playerHeatBar.color, endValue: fillColor, heatBarAnimationDuration))
                .Group(Tween.UIFillAmount(playerHeatBar, startValue: playerHeatBar.fillAmount, endValue: fillAmount, heatBarAnimationDuration))

            ;
    }
    
    private void OnWeaponOverheated()
    {
        Tween.PunchScale(playerHeatBar.transform, strength: Vector3.one * heatBarPunchStrength, duration: heatBarPunchDuration);
    }
    
    private void OnWeaponHeatReset()
    {
        Tween.PunchScale(playerHeatBar.transform, strength: Vector3.one * heatBarPunchStrength, duration: heatBarPunchDuration);
    }
    
    private void OnUpdateCurrency(int newCurrency)
    {
        

        
        
        int currencyDifferance = newCurrency - _previousPlayerCurrency;
        if (currencyDifferance >= bigCurrencyDifference)
        {
            if (_playerCurrencySequence.isAlive) _playerCurrencySequence.Stop();
            _playerCurrencySequence = Sequence.Create()
                
                    .Group(Tween.Custom(startValue: _previousPlayerCurrency, endValue: newCurrency, duration: currencyAnimationDuration, onValueChange: value => _playerCurrency = value.ToInt()))
                    .Chain(Tween.PunchScale(playerCurrencyIcon.transform, strength: Vector3.one * currencyPunchStrength, duration: currencyPunchDuration))
                    .OnComplete(() => _previousPlayerCurrency = newCurrency)
                ;
        }
        else
        {
            if (_playerCurrencySequence.isAlive) _playerCurrencySequence.Stop();
            _playerCurrencySequence = Sequence.Create()
                
                    .Group(Tween.Custom(startValue: _previousPlayerCurrency, endValue: newCurrency, duration: currencyAnimationDuration, onValueChange: value => _playerCurrency = value.ToInt()))
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
                
                    .Group(Tween.Custom(startValue: _previousScore, endValue: newScore, duration: scoreAnimationDuration, onValueChange: value => _score = value.ToInt()))
                    .Chain(Tween.PunchScale(scoreText.transform, strength: Vector3.one * scorePunchStrength, duration: scoreAnimationDuration))
                    .OnComplete(() => _previousScore = newScore)
                ;
        }
        else
        {
            if (_scoreSequence.isAlive) _scoreSequence.Stop();
            _scoreSequence = Sequence.Create()
                
                    .Group(Tween.Custom(startValue: _previousScore, endValue: newScore, duration: scoreAnimationDuration, onValueChange: value => _score = value.ToInt()))
                    .OnComplete(() => _previousScore = newScore)
                ;
        }
        

    }

    #endregion Level UI ----------------------------------------------------------------------------------
}