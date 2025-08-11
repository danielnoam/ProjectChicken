using System;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;



public class DistanceBasedOutline : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private Vector2 outlineWidthRange = new Vector2(0f, 10f);
    [SerializeField] private float thinOutlineDistance = 80f;
    [SerializeField] private float thickOutlineDistance = 100f;
    
    [Header("References")]
    [SerializeField] private Outline outline;
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private float distance;
    [SerializeField, ReadOnly] private Camera cam;



    
    private void OnValidate()
    {
        
        if (outlineWidthRange.x > outlineWidthRange.y)
        {
            outlineWidthRange.x = outlineWidthRange.y;
        }
        if (outlineWidthRange.y > 10f)
        {
            outlineWidthRange.y = 10f;
        }
        if (outlineWidthRange.x < 0)
        {
            outlineWidthRange.x = 0;
        }
        if (outlineWidthRange.y < 0)
        {
            outlineWidthRange.y = 0;
        }
    }

    
    
    private void Start()
    {
        cam = Camera.main;
    }
    


    private void Update()
    {
        if (!cam) return;
        
        distance = Vector3.Distance(transform.position, cam.transform.position);
        var outlineWidth = Mathf.InverseLerp(thinOutlineDistance, thickOutlineDistance, distance);
        outline.OutlineWidth = Mathf.Lerp(outlineWidthRange.x, outlineWidthRange.y, outlineWidth);
    }
}
