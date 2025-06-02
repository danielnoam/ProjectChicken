using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float damage = 25f;
    
    public float GetDamage()
    {
        return damage;
    }
    
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}