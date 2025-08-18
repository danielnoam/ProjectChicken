using System;
using System.Collections;
using DNExtensions;
using KBCore.Refs;
using Unity.Cinemachine;
using UnityEngine;
using VInspector;

[RequireComponent(typeof(RailPlayer))]
[RequireComponent(typeof(ControllerVibrationSource))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class RailPlayerHealth : MonoBehaviour
{
    
    
    [Header("Shield Regen Settings")]
    [SerializeField, Min(0)] private float shieldRegenCooldown = 3f;
    [SerializeField, Min(0)] private float shieldRegenRate = 15f;
    
    [Header("Camera Shake")]
    [SerializeField] private CameraShakeSettings shieldDamagedShakeSettings;
    [SerializeField] private CameraShakeSettings healthDamagedShakeSettings;
    [SerializeField] private CameraShakeSettings deathShakeSettings;
    
    [Header("Controller Rumble")]
    [SerializeField] private ControllerVibrationEffectSettings shieldDamagedVibrationSettings;
    [SerializeField] private ControllerVibrationEffectSettings healthDamagedVibrationSettings;
    [SerializeField] private ControllerVibrationEffectSettings deathVibrationSettings;
    
    [Header("Audio")]
    [SerializeField, Child(Flag.Editable)] private AudioSource audioSource;
    [SerializeField] private SOAudioEvent healthDamageSfx;
    [SerializeField] private SOAudioEvent healthHealedSfx;
    [SerializeField] private SOAudioEvent shieldDamageSfx;
    [SerializeField] private SOAudioEvent shieldStartRegenSfx;
    [SerializeField] private SOAudioEvent shieldRegeneratedSfx;
    [SerializeField] private SOAudioEvent shieldDepletedSfx;
    [SerializeField] private SOAudioEvent deathSfx;
    
    [Header("References")]
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;
    [SerializeField, Self, HideInInspector] private CinemachineImpulseSource cinemachineImpulseSource;
    
    private float _damagedCooldown;
    private Coroutine _regenShieldCoroutine;
    

    public int CurrentHealth { get; private set; }
    public float CurrentShield { get; private set; }
    public float MaxShield { get; private set; }
    

    public event Action OnDeath;
    public event Action<int> OnHealthChanged;
    public event Action<float> OnShieldChanged;
    
    
    
    private void OnValidate()
    {
        this.ValidateRefs();
    }
    
    private void Awake()
    {
        CurrentHealth = player.GameSettings.BaseHealth;
        CurrentShield = player.GameSettings.BaseShield;
        MaxShield = player.GameSettings.BaseShield;
    }
    
    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth);
        OnShieldChanged?.Invoke(CurrentShield);
    }
    
    private void OnEnable()
    {
        if (player.LevelManager)
        {
            player.LevelManager.OnRestartedFromSavePoint += OnRestartedFromSavePoint;
        }
    }
    
    private void OnDisable()
    {
        if (player.LevelManager)
        {
            player.LevelManager.OnRestartedFromSavePoint -= OnRestartedFromSavePoint;
        }
    }
    
    private void Update()
    {
        CheckDamageCooldown();
    }
    
    private void OnRestartedFromSavePoint(SavePointInformation savePoint)
    {
        if (savePoint == null) return;
        
        CurrentHealth = player.GameSettings.BaseHealth;
        CurrentShield = player.GameSettings.BaseShield;
        MaxShield = player.GameSettings.BaseShield;
        
        OnHealthChanged?.Invoke(CurrentHealth);
        OnShieldChanged?.Invoke(CurrentShield);
    }
    
    #region Damage System
    
    public void TakeDamage(float damage)
    {
        if (damage <= 0 || !IsAlive() || player.Movement.IsDodging) return;
        
        StopShieldRegen();
        
        if (ShieldActive())
        {
            DamageShield(damage);
            return;
        }
        
        DamageHealth();
    }
    
    private void CheckDamageCooldown()
    {
        if (!IsAlive()) return;
        
        if (_damagedCooldown > 0)
        {
            _damagedCooldown -= Time.deltaTime;
        }
        
        if (_damagedCooldown <= 0 && _regenShieldCoroutine == null && CurrentShield < MaxShield)
        {
            StartShieldRegen();
        }
    }
    
    private void Die()
    {
        deathSfx?.Play(audioSource);
        controllerVibrationSource.Vibrate(deathVibrationSettings);
        
        if (cinemachineImpulseSource)
        {
            cinemachineImpulseSource.ImpulseDefinition.ImpulseShape = deathShakeSettings.impulseShape;
            cinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = deathShakeSettings.duration;
            cinemachineImpulseSource.DefaultVelocity = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f)
            );
            cinemachineImpulseSource.GenerateImpulseWithForce(deathShakeSettings.intensity);
        }
        
        OnDeath?.Invoke();
    }
    
    #endregion
    
    #region Health Management
    
    private void DamageHealth()
    {
        if (!IsAlive()) return;
        
        CurrentHealth -= 1;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
        else
        {
            if (cinemachineImpulseSource)
            {
                cinemachineImpulseSource.ImpulseDefinition.ImpulseShape = healthDamagedShakeSettings.impulseShape;
                cinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = healthDamagedShakeSettings.duration;
                cinemachineImpulseSource.DefaultVelocity = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                );
                cinemachineImpulseSource.GenerateImpulseWithForce(healthDamagedShakeSettings.intensity);
            }
            
            controllerVibrationSource.Vibrate(healthDamagedVibrationSettings);
            healthDamageSfx?.Play(audioSource);
        }
        
        OnHealthChanged?.Invoke(CurrentHealth);
    }
    
    public void HealHealth(int amount = 1)
    {
        if (amount <= 0) return;
        
        CurrentHealth += amount;
        if (CurrentHealth > player.GameSettings.MaxHealth)
        {
            CurrentHealth = player.GameSettings.MaxHealth;
        }
        
        healthHealedSfx?.Play(audioSource);
        OnHealthChanged?.Invoke(CurrentHealth);
    }
    
    #endregion
    
    #region Shield Management
    
    private IEnumerator RegenShieldRoutine()
    {
        shieldStartRegenSfx?.Play(audioSource);
        
        while (CurrentShield < MaxShield)
        {
            CurrentShield += shieldRegenRate * Time.deltaTime;
            if (CurrentShield >= MaxShield)
            {
                CurrentShield = MaxShield;
                shieldRegeneratedSfx?.Play(audioSource);
                yield break;
            }
            
            OnShieldChanged?.Invoke(CurrentShield);
            yield return null;
        }
    }
    
    private void StopShieldRegen()
    {
        if (_regenShieldCoroutine != null)
        {
            StopCoroutine(_regenShieldCoroutine);
            _regenShieldCoroutine = null;
        }
        
        _damagedCooldown = shieldRegenCooldown;
    }
    
    private void StartShieldRegen()
    {
        _regenShieldCoroutine = StartCoroutine(RegenShieldRoutine());
        _damagedCooldown = 0;
    }
    
    private void DamageShield(float damage)
    {
        if (damage <= 0 || !ShieldActive()) return;
        
        CurrentShield -= damage;
        
        if (CurrentShield < 0)
        {
            CurrentShield = 0;
            DamageHealth();
            shieldDepletedSfx?.Play(audioSource);
        }
        else
        {
            if (cinemachineImpulseSource)
            {
                cinemachineImpulseSource.ImpulseDefinition.ImpulseShape = shieldDamagedShakeSettings.impulseShape;
                cinemachineImpulseSource.ImpulseDefinition.ImpulseDuration = shieldDamagedShakeSettings.duration;
                cinemachineImpulseSource.DefaultVelocity = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                );
                cinemachineImpulseSource.GenerateImpulseWithForce(shieldDamagedShakeSettings.intensity);
            }
            
            controllerVibrationSource.Vibrate(shieldDamagedVibrationSettings);
            shieldDamageSfx?.Play(audioSource);
        }
        
        OnShieldChanged?.Invoke(CurrentShield);
    }
    
    public void HealShield(float amount = 25f)
    {
        if (CurrentShield >= MaxShield) return;
        
        CurrentShield += amount;
        if (CurrentShield >= MaxShield)
        {
            CurrentShield = MaxShield;
            shieldRegeneratedSfx?.Play(audioSource);
        }
        else
        {
            StartShieldRegen();
        }
        
        OnShieldChanged?.Invoke(CurrentShield);
    }
    
    #endregion
    
    
    #region Upgrades
    
    public void UpgradeHealthBy(int amount)
    {
        HealHealth(amount);
    }
    
    public void UpgradeMaxShieldBy(float amount)
    {
        MaxShield += amount;
        
        float maxPossibleShield = player.GameSettings ? player.GameSettings.MaxShield : float.MaxValue;
        if (MaxShield > maxPossibleShield)
        {
            MaxShield = maxPossibleShield;
        }
        
        StartShieldRegen();
    }
    
    #endregion
    
    
    
    #region Helper Methods
    
    public bool ShieldActive()
    {
        return CurrentShield > 0;
    }
    
    public bool IsAlive()
    {
        return CurrentHealth > 0;
    }
    
    
    #endregion
}