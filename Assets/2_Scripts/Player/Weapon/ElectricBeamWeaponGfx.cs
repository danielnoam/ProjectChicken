using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class ElectricBeamWeaponGfx : WeaponGfx
{
    [SerializeField] private float preWarm = 0.1f;
    [SerializeField] private VisualEffect vfxGraph;
    [SerializeField] private ParticleSystem[] particles;
    
    
    
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
            
            foreach (var particle in particles)
            {
                if (!particle || !particle.gameObject.activeSelf) continue;
            
                particle.Simulate(preWarm, true, true, true);
                particle.Play();
            }
        }
        else
        {
            vfxGraph.Stop();
            foreach (var particle in particles)
            {
                if (!particle || !particle.gameObject.activeSelf) continue;
                particle.Stop();
            }
        }
	
        
    }
}
