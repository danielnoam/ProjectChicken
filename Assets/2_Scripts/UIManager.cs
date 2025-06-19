using System.Collections.Generic;
using System.Linq;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
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
    [SerializeField] private float currencyPunchStrength = 0.2f;
    
    [Header("Dodge")]
    [SerializeField] private float dodgeAnimationDuration = 0.2f;
    [SerializeField] private float dodgePunchStrength = 0.2f;
    
    [Header("Weapons")]
    [SerializeField] private float weaponAnimationDuration = 0.2f;
    [SerializeField] private float weaponPunchStrength = 0.2f;
    
    [Header("Overheat Bar")]
    [SerializeField] private float overheatedBarAnimationDuration = 0.2f;
    [SerializeField] private float overheatedBarPunchStrength = 0.2f;
    [SerializeField] private Color overheatedBarColor = Color.red;
    [SerializeField] private Color normalBarColor = Color.white;
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
    [SerializeField] private Image playerOverheatBar;
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
    
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        
        this.ValidateRefs();
    }

    private void Awake()
    {
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
            
            // Weapon Icons
            _weaponStartColor = playerWeaponIcon.color;
            _secondaryWeaponStartColor = playerSecondaryWeaponIcon.color;
            playerSecondaryWeaponIcon.sprite = player.GetCurrentBaseWeapon().WeaponIcon;
            playerSecondaryWeaponIcon.gameObject.SetActive(false);
            
            // Dodge Icon
            _dodgeStartColor = playerDodgeIcon.color;
            
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
        playerOverheatBar.fillAmount = fillAmount;
        playerOverheatBar.color = Color.Lerp(normalBarColor, overheatedBarColor, fillAmount);
    }
    
    private void OnWeaponOverheated()
    {
        Tween.PunchScale(playerOverheatBar.transform, strength: Vector3.one * overheatedBarPunchStrength, duration: overheatedBarAnimationDuration);
    }
    
    private void OnWeaponHeatReset()
    {
        Tween.PunchScale(playerOverheatBar.transform, strength: Vector3.one * overheatedBarPunchStrength, duration: overheatedBarAnimationDuration);
    }
    
    private void OnUpdateCurrency(int currency)
    {
        playerCurrencyText.text = $"{currency}";
        
        Tween.PunchScale(playerCurrencyIcon.transform, strength: Vector3.one * currencyPunchStrength, duration: currencyAnimationDuration);
        
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

    private void OnScoreChanged(int score)
    {
        scoreText.text = score.ToString("D7"); // Shows right now up to a million
    }

    #endregion Level UI ----------------------------------------------------------------------------------
}