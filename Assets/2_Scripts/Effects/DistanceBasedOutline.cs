using DNExtensions;
using UnityEngine;


public class DistanceBasedOutline : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField, MinMaxRange(0,10)] private RangedFloat outlineWidthRange = new (0f, 10f);
    [SerializeField] private float thinOutlineDistance = 80f;
    [SerializeField] private float thickOutlineDistance = 100f;
    
    [Header("References")]
    [SerializeField] private Outline outline;
    
    [Header("Debug")]
    [SerializeField, VInspector.ReadOnly] private float distance;
    
    
    private RailPlayer _player;
    
    
    private void OnEnable()
    {
        if (!_player) _player = FindFirstObjectByType<RailPlayer>();
    }
    


    private void Update()
    {
        if (!_player) return;
        
        distance = Vector3.Distance(transform.position, _player.transform.position);
        var outlineWidth = Mathf.InverseLerp(thickOutlineDistance, thinOutlineDistance,distance);
        outline.OutlineWidth = Mathf.Lerp(outlineWidthRange.maxValue, outlineWidthRange.minValue, outlineWidth);
    }
}
