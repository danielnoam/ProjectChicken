using UnityEngine;

public class ParticlesLookAtNearest : MonoBehaviour
{
    ParticleSystem ps;
    ParticleSystem.Particle[] particles;

    // Full rotation offset in degrees
    public Vector3 rotationOffset = new Vector3(0f, -90f, 0f);

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void LateUpdate()
    {
        int count = ps.GetParticles(particles);
        Vector3 center = ps.transform.position;

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPos = ps.transform.TransformPoint(particles[i].position);
            Vector3 dir = center - worldPos;

            Quaternion lookRot = Quaternion.LookRotation(dir) * Quaternion.Euler(rotationOffset);
            particles[i].rotation3D = lookRot.eulerAngles;
        }

        ps.SetParticles(particles, count);
    }
}
