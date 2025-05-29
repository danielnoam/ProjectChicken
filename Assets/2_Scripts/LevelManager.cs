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

    public Vector3 GetSplineDirection()
    {
        if (!currentPositonOnPath || !levelPath) return Vector3.forward;

        // Convert world position to spline parameter
        SplineUtility.GetNearestPoint(levelPath.Spline, currentPositonOnPath.position, out var nearestPoint, out var t);
    
        // Get tangent at that parameter
        Vector3 tangent = levelPath.EvaluateTangent(t);
        return tangent.normalized;
    }
    
    public float GetCurrentSplineT()
    {
        if (!currentPositonOnPath || !levelPath) return 0f;

        // Convert world position to spline parameter
        SplineUtility.GetNearestPoint(levelPath.Spline, currentPositonOnPath.position, out var nearestPoint, out var t);
        return t;
    }

    private void OnDrawGizmos()
    {
        // Draw the path position
        if (currentPositonOnPath)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentPositonOnPath.position, 0.5f);
        }
    }
}

