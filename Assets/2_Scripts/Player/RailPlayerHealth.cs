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
    [Header("Hit Frames")]
    [SerializeField, Min(0)] private float hitFrameDuration = 0.5f;
    
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
    
    [Header("Fullscreen FX")]
    [SerializeField] private PunchSettings healthDamageFsFX = new PunchSettings();
    [SerializeField] private PunchSettings shieldDamageFsFX = new PunchSettings();
    [SerializeField] private PunchSettings deathFsFX = new PunchSettings();
    
    
    [Header("SFX/VFX")]
    [SerializeField, Child(Flag.Editable)] private AudioSource healthAudioSource;
    [SerializeField, Child(Flag.Editable)] private AudioSource shieldAudioSource;
    [SerializeField] private SOAudioEvent healthDamageSfx;
    [SerializeField] private ParticleSystem healthDamageParticleEffect;
    [SerializeField] private SOAudioEvent healthHealedSfx;
    [SerializeField] private SOAudioEvent shieldDamageSfx;
    [SerializeField] private ParticleSystem shieldDamageParticleEffect;
    [SerializeField] private SOAudioEvent shieldStartRegenSfx;
    [SerializeField] private SOAudioEvent shieldRegeneratedSfx;
    [SerializeField] private SOAudioEvent shieldDepletedSfx;
    [SerializeField] private ParticleSystem shieldDepletedParticleEffect;
    [SerializeField] private SOAudioEvent deathSfx;
    [SerializeField] private ParticleSystem deathParticleEffect;
    
    
    [Header("References")]
    [SerializeField, Self, HideInInspector] private RailPlayer player;
    [SerializeField, Self, HideInInspector] private ControllerVibrationSource controllerVibrationSource;
    [SerializeField, Self, HideInInspector] private CinemachineImpulseSource cinemachineImpulseSource;
    
    [Separator]
    [SerializeField, VInspector.ReadOnly] private float damagedCooldown;
    [SerializeField, VInspector.ReadOnly] private float hitFrameTimer;
    [SerializeField, VInspector.ReadOnly] private bool regenSfxWasPlaying;
    private Coroutine _regenShieldCoroutine;


    public int CurrentHealth { get; private set; }
    public float CurrentShield { get; private set; }
    public float MaxShield { get; private set; }
    public bool InHitFrames => hitFrameTimer > 0;
    

    public event Action OnDeath;
    public event Action OnDamaged;
    public event Action<int> OnHealthChanged;
    public event Action<float> OnShieldChanged;
    
    
    private void OnValidate()
    {
        this.ValidateRefs();
    }


    private void OnEnable()
    {
        if (player.LevelManager)
        {
            player.LevelManager.OnPause += OnPause;
        }
    }

    private void OnDisable()
    {
        if (player.LevelManager)
        {
            player.LevelManager.OnPause -= OnPause;
        }
    }

    private void OnPause(bool paused)
    {
        if (paused)
        {
            if (shieldAudioSource.isPlaying)
            {
                regenSfxWasPlaying = shieldAudioSource.isPlaying;
                shieldAudioSource.Pause();
            }

        }
        else
        {
            if (regenSfxWasPlaying)
            {
                shieldAudioSource.UnPause();
                regenSfxWasPlaying = false;
            }
        }
    }

    private void Update()
    {
        CheckDamageCooldown();
        UpdateHitFrames();
    }
    

    public void SetUp(int health)
    {
        if (health < player.PlayerStats.BaseHealth) health = player.PlayerStats.BaseHealth;
        
        CurrentHealth = health;
        CurrentShield = player.PlayerStats.BaseShield;
        MaxShield = player.PlayerStats.BaseShield;
        
        OnHealthChanged?.Invoke(CurrentHealth);
        OnShieldChanged?.Invoke(CurrentShield);
        
        if (deathParticleEffect && deathParticleEffect.isPlaying) deathParticleEffect.Stop();
        
        hitFrameTimer = 0;
    }
    
    
    
    #region Damage System ------------------------------------------------------------------------------------------
    
    public void TakeDamage(float damage, float iframeMultiplier = 1f)
    {
        if (damage <= 0 || !IsAlive()) return;
        
        StopShieldRegen();
        
        OnDamaged?.Invoke();
        
        if (ShieldActive())
        {
            DamageShield(damage, iframeMultiplier);
        }
        else
        {
            DamageHealth(iframeMultiplier);
        }
        
    }
    
    private void CheckDamageCooldown()
    {
        if (!IsAlive()) return;
        
        if (damagedCooldown > 0)
        {
            damagedCooldown -= Time.deltaTime;
        }
        
        if (damagedCooldown <= 0 && _regenShieldCoroutine == null && CurrentShield < MaxShield)
        {
            StartShieldRegen();
        }
    }
    
    private void Die()
    {
        StopHitFrames();
        deathSfx?.Play(healthAudioSource);
        if (deathParticleEffect) deathParticleEffect.Play();
        controllerVibrationSource.Vibrate(deathVibrationSettings);
        FullScreenHitFXController.Instance?.Punch(deathFsFX);
        deathShakeSettings.GenerateImpulse(cinemachineImpulseSource);
        
        OnDeath?.Invoke();
    }
    
    private void StartHitFrames(float iframeMultiplier = 1f)
    {
        hitFrameTimer = hitFrameDuration * iframeMultiplier;
    }
    
    private void UpdateHitFrames()
    {
        if (hitFrameTimer > 0)
        {
            hitFrameTimer -= Time.deltaTime;
            
            if (hitFrameTimer <= 0)
            {
                StopHitFrames();
            }
        }
    }
    
    private void StopHitFrames()
    {
        hitFrameTimer = 0;
    }

    
    #endregion Damage System ------------------------------------------------------------------------------------------
    
    
    #region Health Management
    
    [Button]
    private void DamageHealth(float iframeMultiplier = 1f)
    {
        if (!IsAlive() || InHitFrames) return;
        
        CurrentHealth -= 1;
        
        StartHitFrames(iframeMultiplier);
        
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
        else
        {
            healthDamagedShakeSettings.GenerateImpulse(cinemachineImpulseSource);
            if (healthDamageParticleEffect) healthDamageParticleEffect.Play();
            controllerVibrationSource.Vibrate(healthDamagedVibrationSettings);
            FullScreenHitFXController.Instance?.Punch(healthDamageFsFX);
            healthDamageSfx?.Play(healthAudioSource);
            

        }
        
        OnHealthChanged?.Invoke(CurrentHealth);
    }
    
    public void HealHealth(int amount = 1)
    {
        if (amount <= 0) return;
        
        CurrentHealth += amount;
        if (CurrentHealth > player.PlayerStats.MaxHealth)
        {
            CurrentHealth = player.PlayerStats.MaxHealth;
        }
        
        if (deathParticleEffect && deathParticleEffect.isPlaying) deathParticleEffect.Stop();
        healthHealedSfx?.Play(healthAudioSource);
        OnHealthChanged?.Invoke(CurrentHealth);
    }
    
    #endregion
    
    
    #region Shield Management
    
    private IEnumerator RegenShieldRoutine()
    {
        shieldStartRegenSfx?.Play(shieldAudioSource);
        
        while (CurrentShield < MaxShield)
        {
            CurrentShield += shieldRegenRate * Time.deltaTime;
            if (CurrentShield >= MaxShield)
            {
                CurrentShield = MaxShield;
                shieldRegeneratedSfx?.Play(shieldAudioSource);
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
        
        damagedCooldown = shieldRegenCooldown;
    }
    
    private void StartShieldRegen()
    {
        _regenShieldCoroutine = StartCoroutine(RegenShieldRoutine());
        damagedCooldown = 0;
    }
    
    [Button]
    private void DamageShield(float damage, float iframeMultiplier = 1f)
    {
        if (damage <= 0 || !ShieldActive()) return;
        
        StopShieldRegen();
        CurrentShield -= damage;
        StartHitFrames(iframeMultiplier);
        
        shieldDamagedShakeSettings.GenerateImpulse(cinemachineImpulseSource);
        
        if (CurrentShield < 1)
        {
            CurrentShield = 0;
            shieldDepletedSfx?.Play(shieldAudioSource);
            if (shieldDepletedParticleEffect) shieldDepletedParticleEffect.Play();
            FullScreenHitFXController.Instance?.Punch(shieldDamageFsFX);
        }
        else
        {
            if (shieldDamageParticleEffect) shieldDamageParticleEffect.Play();
            controllerVibrationSource.Vibrate(shieldDamagedVibrationSettings);
            shieldDamageSfx?.Play(shieldAudioSource);
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
            shieldRegeneratedSfx?.Play(shieldAudioSource);
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
        
        float maxPossibleShield = player.PlayerStats ? player.PlayerStats.MaxShield : float.MaxValue;
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