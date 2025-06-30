using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

public class ReticleController : MonoBehaviour
{
    [SerializeField, Range(0,1)] private float emissionStrength = 1;
    [SerializeField] private bool pulseEmission;
    [SerializeField] private List<Renderer> reticleRenderers = new List<Renderer>();
    
    private readonly List<Material> reticleMaterials = new List<Material>();
    private static readonly int EmissionStrength = Shader.PropertyToID("_EmissionStrength");
    private static readonly int EmissionEnabled = Shader.PropertyToID("_EmissionEnabled");

    private void Awake()
    {
        GetMaterialsFromRenderers();
    }
    
    private void Update()
    {
        if (pulseEmission)
        {
            PulseEmission();
        }
    }

    private void GetMaterialsFromRenderers()
    {
        reticleMaterials.Clear();
        
        foreach (Renderer rend in reticleRenderers)
        {
            if (!rend) continue;
            var materials = rend.materials;
            foreach (Material mat in materials)
            {
                if (mat)
                {
                    reticleMaterials.Add(mat);
                }
            }
        }
    }

    private void PulseEmission()
    {
        emissionStrength = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f; // 0-1 sine wave
        emissionStrength = Mathf.Clamp01(emissionStrength);
        SetEmissionStrength(emissionStrength);
    }
    
    [Button]
    public void SetEmissionEnabled(bool state)
    {
        float value = state ? 1.0f : 0.0f;
        foreach (Material mat in reticleMaterials)
        {
            if (mat)
                mat.SetFloat(EmissionEnabled, value);
        }
    }
    

    [Button]
    public void SetEmissionStrength(float strength)
    {
        foreach (Material mat in reticleMaterials)
        {
            if (mat)
                mat.SetFloat(EmissionStrength, strength);
        }
    }
}