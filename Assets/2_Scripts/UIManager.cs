using KBCore.Refs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Overheat Bar")]
    [SerializeField] private Color overheatedBarColor = Color.red;
    [SerializeField] private Color normalBarColor = Color.white;
    
    [Header("Asset References")] 
    [SerializeField] private Image playerIconPrefab;
    
    [Header("Scene References")] 
    [SerializeField, Scene(Flag.Editable)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.Editable)] private RailPlayer player;
    [SerializeField] private Transform playerHealthHolder;
    [SerializeField] private Image playerWeaponIcon;
    [SerializeField] private Image playerSecondaryWeaponIcon;
    [SerializeField] private Image playerOverheatBar;
    [SerializeField] private Image playerDodgeIcon;
    [SerializeField] private TextMeshProUGUI playerShieldText;
    [SerializeField] private TextMeshProUGUI playerCurrencyText;
    [SerializeField] private TextMeshProUGUI playerScoreText;

    
    private Image[] _healthIcons;
    
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
                _healthIcons[health] = healthObject;
            }
            
            
            // Setup secondary weapon icon
            playerSecondaryWeaponIcon.sprite = player.GetCurrentBaseWeapon().WeaponIcon;
            playerSecondaryWeaponIcon.gameObject.SetActive(false);
            
            // Update 
            OnUpdateHealth(player.MaxHealth);
            OnUpdateShield(player.MaxShieldHealth);
            OnWeaponHeatUpdated(0);
            OnUpdateCurrency(player.CurrentCurrency);
            OnDodgeCooldownUpdated(player.GetDodgeMaxCooldown());
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
        playerWeaponIcon.fillAmount = fillAmount;
    }
    
    private void OnBaseWeaponCooldownUpdated(SOWeapon baseWeapon, float cooldown)
    {
        float fillAmount = 1f - (cooldown / baseWeapon.FireRate);
        
        if (player.GetCurrentSpecialWeapon())
        {
            playerSecondaryWeaponIcon.fillAmount = fillAmount; 
        }
        else
        {
            playerWeaponIcon.fillAmount = fillAmount;
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
        playerDodgeIcon.color = Color.Lerp(Color.clear, Color.white, fillAmount);
    }
    
    private void OnDodge()
    {
        playerDodgeIcon.color = Color.clear;
    }
    


    #endregion Player UI ----------------------------------------------------------------------------------


    
    
}
