using System;
using DNExtensions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class ElectricBeamWeaponGfx : WeaponGfx
{
    [SerializeField] private float preWarm = 0.1f;
    [SerializeField] private VisualEffect vfxGraph;
    [SerializeField] private ParticleSystem[] particles;
    [SerializeField] private Transform ballEffect;
    [SerializeField, MinMaxRange(10f,30f)] private RangedFloat beamScaleRange = new RangedFloat(10f, 30f);

    private float currentBeamLength;

    private void Start()
    {
        if (player && ballEffect)
        {
            ballEffect.parent = player.Aiming.AimWorldPosition;
        }
        
        currentBeamLength = beamScaleRange.minValue;
    }
    
    public override void AnimateUsage()
    {
        base.AnimateUsage();
        ToggleEffect(true);
    }
    
    public override void StopAnimation()
    {
        base.StopAnimation();
        
        ToggleEffect(false);
    }

    private void ToggleEffect(bool state)
    {
        if (state)
        {
            vfxGraph.Play();
            // vfxGraph.SetFloat("Rotate", Random.Range(0f,180f));
            // vfxGraph.SetFloat("Rotate 2", Random.Range(0f,180f));
            // vfxGraph.SetFloat("Rotate 3", Random.Range(0f,180f));
            
            
            UpdateBeamLength();
            
            foreach (var particle in particles)
            {
                if (!particle || !particle.gameObject.activeSelf || particle.IsAlive()) continue;
            
                particle.Simulate(preWarm, true, true, true);
                particle.Play(true);
            }
        }
        else
        {
            vfxGraph.Stop();
            foreach (var particle in particles)
            {
                if (!particle || !particle.gameObject.activeSelf) continue;
                particle.Clear();
                particle.Stop(true);
            }
        }
    }

    private void UpdateBeamLength()
    {
        if (!isShowing || !player) return;

        // Get normalized aim position (center is 0,0, ranges from -1 to 1)
        Vector2 normalizedAim = GetNormalizedAimPosition();

        // Calculate distance from center (0,0)
        float normalizedDistance = normalizedAim.magnitude;
    
        // The max distance from center (0,0) to corner (1,1) or (-1,1) is sqrt(2) ≈ 1.414
        normalizedDistance = Mathf.Clamp01(normalizedDistance / 1.414f);

        // Map normalized position to beam length range
        float targetBeamLength = Mathf.Lerp(beamScaleRange.minValue, beamScaleRange.maxValue, normalizedDistance);

        // Smooth the beam length changes using a persistent variable
        currentBeamLength = Mathf.Lerp(currentBeamLength, targetBeamLength, 25 * Time.deltaTime);

        // Set the VFX parameter
        vfxGraph.SetFloat("Main Body Scale", currentBeamLength);
    }

    
    
    private Vector2 GetNormalizedAimPosition()
    {
        return player ? player.Aiming.NormalizedAimPosition : Vector2.zero;
    }
}
