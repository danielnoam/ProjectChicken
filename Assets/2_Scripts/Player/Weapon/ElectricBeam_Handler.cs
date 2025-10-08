using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class ElectricBeam_Handler : MonoBehaviour
{
    public bool testBeam = false;
    [SerializeField] VisualEffect VFXGraph;
    [SerializeField] ParticleSystem particles_1;
    [SerializeField] ParticleSystem particles_2;

   [SerializeField] float preWarm = 0.1f;
    private void OnValidate()
    {
        if (testBeam)
        {
            particles_1.Simulate(preWarm, true, true, true);
            particles_2.Simulate(preWarm, true, true, true);
            
            VFXGraph.Play();
            particles_1.Play();
            particles_2.Play();
        }
        else
        {
            VFXGraph.Stop();
            particles_1.Stop();
            particles_2.Stop();
        }
    }
}
