using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class ElectricBeam_Handler : MonoBehaviour
{
    public bool testBeam = false;
    [SerializeField] VisualEffect VFXGraph;
    [SerializeField] ParticleSystem Particles_1;
    [SerializeField] ParticleSystem Particles_2;

   
    private void OnValidate()
    {
        if (testBeam)
        {
            VFXGraph.Play();
            Particles_1.Play();
            Particles_2.Play();

            Particles_1.loop = true;
            Particles_2.loop = true;
        }
        else
        {
            VFXGraph.Stop();
            Particles_1.Stop();
            Particles_2.Stop();
            Particles_1.loop = false;
            Particles_2.loop = false;
        }
    }
}
