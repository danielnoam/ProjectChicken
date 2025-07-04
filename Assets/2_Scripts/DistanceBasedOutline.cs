using System;
using KBCore.Refs;
using UnityEngine;
using VInspector;


[RequireComponent(typeof(Outline))]
public class DistanceBasedOutline : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private Vector2 outlineWidthRange = new Vector2(0f, 10f);
    [SerializeField] private float thinOutlineDistance = 5f;
    [SerializeField] private float thickOutlineDistance = 10f;
    
    [Header("References")]
    [SerializeField,Self,HideInInspector] private Outline outline;
    [SerializeField] private Camera mainCamera;
    
    [Header("Debug")]
    [SerializeField,ReadOnly] private float distance;


    private void OnValidate()
    {
        this.ValidateRefs();

        if (!mainCamera) mainCamera = FindFirstObjectByType<Camera>();
        

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

    private void Awake()
    {
        if (!mainCamera) mainCamera = Camera.current;
    }


    private void Update()
    {
        if (!mainCamera) return;
        
        distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        var outlineWidth = Mathf.InverseLerp(thinOutlineDistance, thickOutlineDistance, distance);
        outline.OutlineWidth = Mathf.Lerp(outlineWidthRange.x, outlineWidthRange.y, outlineWidth);
    }
}
