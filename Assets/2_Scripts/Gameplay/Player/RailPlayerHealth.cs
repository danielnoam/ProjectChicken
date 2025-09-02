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
    [SerializeField, Child(Flag.Editable)] private AudioSource audioSource;
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
    
    private float _damagedCooldown;
    private Coroutine _regenShieldCoroutine;
    private float _hitFrameTimer;
    

    public int CurrentHealth { get; private set; }
    public float CurrentShield { get; private set; }
    public float MaxShield { get; private set; }
    public bool InHitFrames => _hitFrameTimer > 0;
    

    public event Action OnDeath;
    public event Action OnDamaged;
    public event Action<int> OnHealthChanged;
    public event Action<float> OnShieldChanged;
    
    
    private void OnValidate()
    {
        this.ValidateRefs();
    }
    
    
    private void Update()
    {
        CheckDamageCooldown();
        UpdateHitFrames();
    }
    

    public void SetUp(int health)
    {
        if (health < player.GameSettings.BaseHealth) health = player.GameSettings.BaseHealth;
        
        CurrentHealth = health;
        CurrentShield = player.GameSettings.BaseShield;
        MaxShield = player.GameSettings.BaseShield;
        
        OnHealthChanged?.Invoke(CurrentHealth);
        OnShieldChanged?.Invoke(CurrentShield);
        
        if (deathParticleEffect && deathParticleEffect.isPlaying) deathParticleEffect.Stop();
        
        _hitFrameTimer = 0;
    }
    
    
    
    #region Damage System ------------------------------------------------------------------------------------------
    
    public void TakeDamage(float damage)
    {
        if (damage <= 0 || !IsAlive() || player.Movement.IsDodging) return;
        
        StopShieldRegen();
        
        OnDamaged?.Invoke();
        
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
        StopHitFrames();
        deathSfx?.Play(audioSource);
        if (deathParticleEffect) deathParticleEffect.Play();
        controllerVibrationSource.Vibrate(deathVibrationSettings);
        FullScreenHitFXController.Instance?.Punch(deathFsFX);
        
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
    
    private void StartHitFrames()
    {
        _hitFrameTimer = hitFrameDuration;
    }
    
    private void UpdateHitFrames()
    {
        if (_hitFrameTimer > 0)
        {
            _hitFrameTimer -= Time.deltaTime;
            
            if (_hitFrameTimer <= 0)
            {
                StopHitFrames();
            }
        }
    }
    
    private void StopHitFrames()
    {
        _hitFrameTimer = 0;
    }

    
    #endregion Damage System ------------------------------------------------------------------------------------------
    
    
    #region Health Management
    
    [Button]
    private void DamageHealth()
    {
        if (!IsAlive() || InHitFrames) return;
        
        CurrentHealth -= 1;
        
        StartHitFrames();
        
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
            
            if (healthDamageParticleEffect) healthDamageParticleEffect.Play();
            controllerVibrationSource.Vibrate(healthDamagedVibrationSettings);
            FullScreenHitFXController.Instance?.Punch(healthDamageFsFX);
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
        
        if (deathParticleEffect && deathParticleEffect.isPlaying) deathParticleEffect.Stop();
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
    
    [Button]
    private void DamageShield(float damage)
    {
        if (damage <= 0 || !ShieldActive()) return;
        
        CurrentShield -= damage;
        
        if (CurrentShield < 0)
        {
            CurrentShield = 0;
            DamageHealth();
            shieldDepletedSfx?.Play(audioSource);
            if (shieldDepletedParticleEffect) shieldDepletedParticleEffect.Play();
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
            
            if (shieldDamageParticleEffect) shieldDamageParticleEffect.Play();
            controllerVibrationSource.Vibrate(shieldDamagedVibrationSettings);
            FullScreenHitFXController.Instance?.Punch(shieldDamageFsFX);
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