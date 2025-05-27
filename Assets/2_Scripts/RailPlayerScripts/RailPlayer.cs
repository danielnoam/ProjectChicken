using System;
using KBCore.Refs;
using UnityEngine;

[RequireComponent(typeof(RailPlayerInput))]
[RequireComponent(typeof(RailPlayerMovement))]
[RequireComponent(typeof(RailPlayerAiming))]
[RequireComponent(typeof(RailPlayerWeaponSystem))]
public class RailPlayer : MonoBehaviour
{

    [Header("Player Settings")]
    [SerializeField, Min(0)] private int maxHealth = 3;
    [SerializeField, Min(0)] private float maxShieldHealth = 100f;
    

    

    private float _currentShieldHealth;
    private int _currentHealth;

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
