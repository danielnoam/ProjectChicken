using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class LevelSection
{
    public string sectionName;
    [Range(0,100)] public float sectionLength;
    public SectionType sectionType = SectionType.ClearPath;
}

public enum SectionType
{
    ClearPath,
    EnemyWave,
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Settings")]
    [SerializeField, Min(2)] private LevelSection[] levelSections = new LevelSection[2];
    
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
}

