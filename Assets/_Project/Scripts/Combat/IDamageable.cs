public interface IDamageable
{
    bool IsAlive { get; }
    DamageResult TakeDamage(in DamageInfo info);
}