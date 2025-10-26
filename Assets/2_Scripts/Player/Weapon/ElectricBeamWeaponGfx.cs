using System;
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


    private void Start()
    {
        if (player && ballEffect)
        {
            ballEffect.parent = player.Aiming.AimWorldPosition;
        }
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
            vfxGraph.SetFloat("Rotate", Random.Range(0f,180f));
            vfxGraph.SetFloat("Rotate 2", Random.Range(0f,180f));
            vfxGraph.SetFloat("Rotate 3", Random.Range(0f,180f));
            
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
}
