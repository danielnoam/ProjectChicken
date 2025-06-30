// Simple interface for objects that can take damage
public interface IDamageable
{
    void TakeDamage(float damage);
    bool IsAlive();
}