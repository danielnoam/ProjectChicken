using System;
using UnityEngine;
using VInspector;

public class DitheringObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private SubscribeTo subscribeTo = SubscribeTo.Player;
    [SerializeField,ReadOnly] private Transform target;
    
    
    
    private enum SubscribeTo { None, Player}
    private bool SubscribeToNone => subscribeTo == SubscribeTo.None;
    private static readonly int PositionID = Shader.PropertyToID("_Dither_Object_Position");
    private Material _material;

    private void Awake()
    {
        Renderer randerer = GetComponent<Renderer>();
        _material = randerer.material;
        randerer.material = new Material(_material);
        _material = randerer.material;
    }

    private void Start()
    {
        if (SubscribeToNone) return;

        switch (subscribeTo)
        {
            case SubscribeTo.Player:
                
                if (!LevelManager.Instance) return;
                
                target = LevelManager.Instance.Player.transform;
                break;
        }
    }
    
    private void OnEnable()
    {
        if (SubscribeToNone) return;
        
        switch (subscribeTo)
        {
            case SubscribeTo.Player:

                if (!LevelManager.Instance) return;

                target = LevelManager.Instance.Player.transform;
                break;
        }
    }
    
    private void OnDisable()
    {
        target = null;
    }


    private void Update()
    {
        UpdateTargetPosition();
    }
    
    
    private void UpdateTargetPosition()
    {
        if (!target) return;
        _material.SetVector(PositionID, target.position);
    }
}
