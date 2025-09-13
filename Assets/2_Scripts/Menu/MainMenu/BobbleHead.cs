using DNExtensions;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BubbleHead : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float punchForce = 25f;
    [SerializeField] private float springStrength = 150f;
    [SerializeField] private float damping = 0.92f;
    [SerializeField] private float maxRotationAngle = 45f;
    [SerializeField] private float punchCooldown = 0.1f;
    
    [Header("Punch Counter")]
    [SerializeField] private int punchesNeeded = 5;
    [SerializeField] private float counterResetTime = 3f;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform head;
    [SerializeField] private SOAudioEvent punchSfx;
    [SerializeField] private SOAudioEvent punchThresholdReachedSfx;
    [SerializeField] private SOAudioEvent pressSfx;
    
    
    private Vector3 _baseHeadRotation;
    private Vector3 _currentRotation;
    private Vector3 _velocity;
    private int _currentPunchCount = 0;
    private float _lastPunchTime;
    private float _lastPunchExecuteTime;
    
    private void Awake()
    {
        _baseHeadRotation = head.localEulerAngles;
        _currentRotation = _baseHeadRotation;
    }
    
    private void Update()
    {
        if (Time.time - _lastPunchTime > counterResetTime && _currentPunchCount > 0)
        {
            ResetPunchCounter();
        }
        
        Vector3 displacement = _currentRotation - _baseHeadRotation;
        Vector3 springForce = -displacement * springStrength;
        
        _velocity += springForce * Time.deltaTime;
        _velocity *= damping;
        _currentRotation += _velocity * Time.deltaTime;
        
        _currentRotation.x = Mathf.Clamp(_currentRotation.x, 
            _baseHeadRotation.x - maxRotationAngle, 
            _baseHeadRotation.x + maxRotationAngle);
        _currentRotation.z = Mathf.Clamp(_currentRotation.z, 
            _baseHeadRotation.z - maxRotationAngle, 
            _baseHeadRotation.z + maxRotationAngle);
        
        head.localEulerAngles = _currentRotation;
    }
    
    private void OnMouseEnter()
    {
        TryAddPunch(new Vector3(-punchForce, 0, Random.Range(-punchForce * 0.3f, punchForce * 0.3f)));
    }
    
    private void OnMouseExit()
    {
        TryAddPunch(new Vector3(punchForce * 0.7f, 0, Random.Range(-punchForce * 0.2f, punchForce * 0.2f)));
    }
    
    private void OnMouseDown()
    {
        TryAddPunch(new Vector3(-punchForce, 0, Random.Range(-punchForce * 0.3f, punchForce * 0.3f)));
        pressSfx?.Play(audioSource);
    }
    
    private void TryAddPunch(Vector3 force)
    {
        if (Time.time - _lastPunchExecuteTime >= punchCooldown)
        {
            AddPunch(force);
        }
    }
    
    private void AddPunch(Vector3 force)
    {
        _velocity += force;
        _lastPunchExecuteTime = Time.time;
        _currentPunchCount++;
        _lastPunchTime = Time.time;
        punchSfx?.Play(audioSource);
        
        if (_currentPunchCount >= punchesNeeded)
        {
            OnPunchThresholdReached();
            ResetPunchCounter();
        }
    }
    
    
    private void OnPunchThresholdReached()
    {
        Vector3 celebrationPunch = new Vector3(
            Random.Range(-punchForce * 2f, punchForce * 2f),
            0,
            Random.Range(-punchForce * 2f, punchForce * 2f)
        );
        _velocity += celebrationPunch;
        punchThresholdReachedSfx?.Play(audioSource);
    }
    
    private void ResetPunchCounter()
    {
        _currentPunchCount = 0;
    }
}