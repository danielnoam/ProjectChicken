using KBCore.Refs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Icons")]
    [SerializeField] private Color cooldownIconColor = Color.grey;
        
    [Header("Overheat Bar")]
    [SerializeField] private Color overheatedBarColor = Color.red;
    [SerializeField] private Color normalBarColor = Color.white;
    
    [Header("Asset References")] 
    [SerializeField] private Image playerIconPrefab;
    [SerializeField] private Sprite heartIcon;
    
    [Header("Child References")] 
    [SerializeField] private Transform playerHealthHolder;
    [SerializeField] private Image playerWeaponIcon;
    [SerializeField] private Image playerSecondaryWeaponIcon;
    [SerializeField] private Image playerOverheatBar;
    [SerializeField] private Image playerDodgeIcon;
    [SerializeField] private TextMeshProUGUI playerShieldText;
    [SerializeField] private TextMeshProUGUI playerCurrencyText;
    [SerializeField] private TextMeshProUGUI scoreText;
    
    [Header("Scene References")] 
    [SerializeField, Scene(Flag.Editable)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.Editable)] private RailPlayer player;


    
    private Image[] _healthIcons;
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
            _healthIcons = new Image[player.MaxHealth];
            for (int health = 0; health < player.MaxHealth; health++)
            {
                var healthObject = Instantiate(playerIconPrefab, playerHealthHolder);
                healthObject.name = $"HealthIcon{health}";
                healthObject.sprite = heartIcon;
                _healthIcons[health] = healthObject;
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
        for (int i = 0; i < _healthIcons.Length; i++)
        {
            _healthIcons[i].gameObject.SetActive(i < currentHealth);
        }
    }

    private void OnUpdateShield(float currentShield)
    {
        playerShieldText.text = $"{currentShield:F0}%";
    }

    private void OnSpecialWeaponSwitched(SOWeapon previousWeapon, SOWeapon newWeapon)
    {
        if (newWeapon)
        {
            playerWeaponIcon.sprite = newWeapon.WeaponIcon;
            playerSecondaryWeaponIcon.gameObject.SetActive(true);
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
        // playerWeaponIcon.fillAmount = fillAmount;
        playerWeaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
    }
    
    private void OnBaseWeaponCooldownUpdated(SOWeapon baseWeapon, float cooldown)
    {
        float fillAmount = 1f - (cooldown / baseWeapon.FireRate);
        
        if (player.GetCurrentSpecialWeapon())
        {
            // playerSecondaryWeaponIcon.fillAmount = fillAmount; 
            playerSecondaryWeaponIcon.color = Color.Lerp(Color.clear, _secondaryWeaponStartColor, fillAmount);
        }
        else
        {
            // playerWeaponIcon.fillAmount = fillAmount;
            playerWeaponIcon.color = Color.Lerp(cooldownIconColor, _weaponStartColor, fillAmount);
        }
    }
    
    private void OnWeaponHeatUpdated(float heat)
    {
        float fillAmount = heat / player.GetMaxWeaponHeat();
        playerOverheatBar.fillAmount = fillAmount;
        playerOverheatBar.color = Color.Lerp(normalBarColor, overheatedBarColor, fillAmount);
    }
    
    private void OnUpdateCurrency(int currency)
    {
        playerCurrencyText.text = $"{currency}";
    }
    
    private void OnDodgeCooldownUpdated(float cooldown)
    {
        float fillAmount = 1f - (cooldown / player.GetDodgeMaxCooldown());
        playerDodgeIcon.color = Color.Lerp(cooldownIconColor, _dodgeStartColor, fillAmount);
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
