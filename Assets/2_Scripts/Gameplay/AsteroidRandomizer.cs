
using DNExtensions;
using UnityEngine;
using VInspector;

[SelectionBase]
public class AsteroidRandomizer : MonoBehaviour
{

    
    [Header("Settings")]
    [SerializeField] private bool randomizeSizeOnAwake;
    [SerializeField,MinMaxRange(0.5f,2f)] private RangedFloat sizeMultiplierRange = new(0.5f,2);
    [SerializeField] private bool randomizeRotationOnAwake;
    [SerializeField,MinMaxRange(0, 360)] private RangedFloat rotationRange = new(0, 360);
    [SerializeField] private bool randomizeShapeOnAwake;
    [SerializeField,MinMaxRange(0.5f,2f)] private RangedFloat shapeMultiplierRange = new(0.5f,2);
    
    
    
    [Space(10)]
    [SerializeField] private bool rotate;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("References")]
    [SerializeField] private Transform shapeTransform;

    private Vector3 _rotationDirection;
    
    private void Awake()
    {
        if (randomizeSizeOnAwake) RandomizeSize();
        if (randomizeRotationOnAwake) RandomizeRotation();
        if (randomizeShapeOnAwake) RandomizeShape();
        if (rotate) _rotationDirection  = Random.onUnitSphere;
    }

    private void Update()
    {
        if (!rotate) return;
        transform.Rotate(_rotationDirection * (rotationSpeed * Time.deltaTime));
    }

    [Button]
    private void RandomizeSize()
    {
        transform.localScale = Vector3.one * Random.Range(sizeMultiplierRange.minValue, sizeMultiplierRange.maxValue);
    }
    
    [Button]
    private void RandomizeShape()
    {
        shapeTransform.localScale = new Vector3(Random.Range(shapeMultiplierRange.minValue, shapeMultiplierRange.maxValue),Random.Range(shapeMultiplierRange.minValue, shapeMultiplierRange.maxValue),Random.Range(shapeMultiplierRange.minValue, shapeMultiplierRange.maxValue)) ;
    }

    [Button]
    private void RandomizeRotation()
    {
        transform.eulerAngles = Vector3.one * Random.Range(rotationRange.minValue, rotationRange.maxValue);
    }
}
