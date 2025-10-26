using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using VInspector;

public class BackgroundObjectsAnimator : MonoBehaviour
{
    [Header("Space Jump Animation")]
    [SerializeField] private List<Transform> objectsToSpaceJump = new List<Transform>();
    [SerializeField] private float endZPosition = -537;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private Ease ease = Ease.InOutQuad;
    [SerializeField] private float globalStartDelay = 1.5f;
    [SerializeField] private float indexDelay = 0.1f;
    
    
    [Header("SineWave Animation")]
    [SerializeField] private bool useSineWave;
    [SerializeField] private float sineWaveStrength = 0.2f;
    [SerializeField] private float sineWaveSpeed = 1f;
    [SerializeField] private float randomnessSpeed = 0.5f;
    [SerializeField] private List<Transform> objectsToSineWave = new List<Transform>();
    
    private Sequence _backgroundSequence;
    private readonly List<Vector3> _startPositions = new List<Vector3>();
    private readonly List<float> _randomOffsets = new List<float>();
    private readonly List<float> _randomSpeeds = new List<float>();
    
    private void Awake()
    {
        StoreStartPositions();
        GenerateRandomValues();
    }

    private void Update()
    {
        if (_backgroundSequence.isAlive)
        {
            return;
        }
        
        if (useSineWave)
        {
            for (int i = 0; i < objectsToSineWave.Count; i++)
            {
                var obj = objectsToSineWave[i];
                if (obj)
                {
                    float randomOffset = i < _randomOffsets.Count ? _randomOffsets[i] : 0f;
                    float randomSpeed = i < _randomSpeeds.Count ? _randomSpeeds[i] : 1f;
                    
                    float wave = Mathf.Sin((Time.time * sineWaveSpeed * randomSpeed) + randomOffset) * sineWaveStrength;
                    obj.position = new Vector3(obj.position.x, obj.position.y + wave * Time.deltaTime, obj.position.z);
                }
            }
        }
    }

    private void StoreStartPositions()
    {
        _startPositions.Clear();
        
        foreach (var obj in objectsToSpaceJump)
        {
            if (obj)
            {
                _startPositions.Add(obj.position);
            }
        }
    }

    private void GenerateRandomValues()
    {
        _randomOffsets.Clear();
        _randomSpeeds.Clear();
        
        foreach (var obj in objectsToSineWave)
        {
            if (obj)
            {
                _randomOffsets.Add(UnityEngine.Random.Range(0f, Mathf.PI * 2f));
                _randomSpeeds.Add(UnityEngine.Random.Range(1f - randomnessSpeed, 1f + randomnessSpeed));
            }
        }
    }
    
    [Button]
    public void StartAnimation()
    {
        if (_backgroundSequence.isAlive)
        {
            _backgroundSequence.Stop();
        }
        
        _backgroundSequence = Sequence.Create()
            .ChainDelay(globalStartDelay)
            .ChainCallback(()=>{});
        
        for (int i = 0; i < objectsToSpaceJump.Count && i < _startPositions.Count; i++)
        {
            if (!objectsToSpaceJump[i]) continue;
            
            Vector3 startPos = _startPositions[i];
            Vector3 endPos = new Vector3(startPos.x, startPos.y, endZPosition);
            float totalDelay = i * indexDelay;
            
            _backgroundSequence.Group(
                Tween.Position(
                    objectsToSpaceJump[i], 
                    startDelay: totalDelay,
                    startValue: startPos,
                    endValue: endPos, 
                    duration: duration, 
                    ease: ease
                )
            );
        }
    }

    [Button]
    public void RegenerateRandomness()
    {
        GenerateRandomValues();
    }
    
    public void StopAnimation()
    {
        if (_backgroundSequence.isAlive)
        {
            _backgroundSequence.Stop();
        }
    }
}