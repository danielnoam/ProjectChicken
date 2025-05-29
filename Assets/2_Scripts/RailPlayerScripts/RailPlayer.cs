using System;
using KBCore.Refs;
using UnityEngine;

[RequireComponent(typeof(RailPlayerInput))]
[RequireComponent(typeof(RailPlayerMovement))]
[RequireComponent(typeof(RailPlayerAiming))]
[RequireComponent(typeof(RailPlayerWeaponSystem))]
public class RailPlayer : MonoBehaviour
{

    [Header("Health Settings")]
    [SerializeField, Min(0)] private int maxHealth = 3;
    [SerializeField, Min(0)] private float maxShieldHealth = 100f;
    

    [Header("Path Settings")]
    [SerializeField] private bool alignToSplineDirection = true;
    [SerializeField] private float pathFollowSpeed = 5f;
    [SerializeField, Min(0)] private float splineRotationSpeed = 5f;
    

    private float _currentShieldHealth;
    private int _currentHealth;
    
    
    public float CurrentShieldHealth => _currentShieldHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;
    public bool HasShield => _currentShieldHealth > 0f;
    public bool AlignToSplineDirection => alignToSplineDirection;
    public float PathFollowSpeed => pathFollowSpeed;
    public float SplineRotationSpeed => splineRotationSpeed;

    private void Awake()
    {
        SetUpPlayer();
    }

    private void SetUpPlayer()
    {
        _currentHealth = maxHealth;
        _currentShieldHealth = maxShieldHealth;
    }
}
