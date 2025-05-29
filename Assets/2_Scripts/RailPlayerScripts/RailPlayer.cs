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

    
    
    

    #region Helper Methods --------------------------------------------------------------------------------------

    public bool HasShield()
    {
        return _currentShieldHealth > 0;
    }
    
    public bool IsAlive()
    {
        return _currentHealth > 0;
    }

    #endregion Helper Methods --------------------------------------------------------------------------------------


}
