

using UnityEngine;

public interface IDamageable
{
        void TakeDamage(float damage);
        void ApplyStun(float duration);
        void ApplyForce(Vector3 direction, float force);
}