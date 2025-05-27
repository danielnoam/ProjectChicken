using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]



public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    
    [Header("References")]
    [SerializeField, Child] private SplineContainer levelPath;
    [SerializeField] private Transform currentPositonOnPath;
    
    public SplineContainer LevelPath => levelPath;
    public Transform CurrentPositionOnPath => currentPositonOnPath;
    
    private void Awake()
    {

        if (Instance != this || !Instance)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
    }


    private void OnDrawGizmos()
    {
        // Draw the path position
        if (currentPositonOnPath)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(currentPositonOnPath.position, 0.5f);
        }
    }
}

