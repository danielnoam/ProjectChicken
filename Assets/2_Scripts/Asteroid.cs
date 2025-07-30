using System;
using DNExtensions;
using UnityEngine;
using VInspector;
using Random = System.Random;

public class Asteroid : MonoBehaviour
{

    
    [Header("Settings")]
    [SerializeField] private bool randomizeSizeOnAwake;
    [SerializeField,MinMaxRange(0.5f,2f)] private RangedFloat sizeMultiplierRange = new(0.5f,2);
    [SerializeField] private bool randomizeRotationOnAwake;
    [SerializeField,MinMaxRange(0, 360)] private RangedFloat rotationRange = new(0, 360);

    [Space(10)]
    [SerializeField] private bool rotate = true;
    [SerializeField] private float rotationSpeed = 15f;


    private Vector3 _rotationDirection;
    
    private void Awake()
    {
        if (randomizeSizeOnAwake) RandomizeSize();
        if (randomizeRotationOnAwake) RandomizeRotation();
        if (rotate) _rotationDirection  = UnityEngine.Random.onUnitSphere;
    }

    private void Update()
    {
        if (!rotate) return;
        transform.Rotate(_rotationDirection * (rotationSpeed * Time.deltaTime));
    }

    [Button]
    private void RandomizeSize()
    {
        transform.localScale = Vector3.one * UnityEngine.Random.Range(sizeMultiplierRange.minValue, sizeMultiplierRange.maxValue);
    }

    [Button]
    private void RandomizeRotation()
    {
        transform.eulerAngles = Vector3.one * UnityEngine.Random.Range(rotationRange.minValue, rotationRange.maxValue);
    }

    private void OnDrawGizmosSelected()
    {
        
        Gizmos.DrawWireSphere(transform.position, 8f * sizeMultiplierRange.maxValue);
    }
}
